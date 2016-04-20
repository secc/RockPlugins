using com.paypal.soap.api;
using org.secc.PayPalReporting.Services;
using System;
using System.Linq;
using Rock;

namespace org.secc.PayPalReporting.Engine
{
    class API
    {
        public String Username { get; set; }
        public String Password { get; set; }
        public String Signature { get; set; }

        public enum PFPTransactionType
        {
            Sale,
            Credit,
            DelayedCapture
        }
        public enum TransactionType
        {
            Payment,
            Refund
        }

        public Decimal GetTransactionFee(Transaction tx)
        {
            var profile = PayPalSvc.CreateProfile(Username, Password, Signature, Services.PayPalEnvironment.Live);

            TransactionSearchResponseType resp = PayPalSvc.TransactionSearchByID(profile, tx.MerchantTransactionId, tx.TimeCreated.Value, tx.TimeCreated.Value);

            PaymentTransactionSearchResultType payment;

            if (resp.PaymentTransactions.Count() <= 1)
            {
                payment = resp.PaymentTransactions.FirstOrDefault();
            }
            else
            {
                payment = resp.PaymentTransactions.Where(x => x.TransactionID == tx.MerchantTransactionId).FirstOrDefault();

                if (payment == null)
                {
                    //transaction not found. try to search any linked transactions for timestamp, amount and type
                    //check previous day as well because of Paypal using GMT
                    payment = resp.PaymentTransactions.Where(x => x.Type.ToUpper() == GetTransactionType(tx.Type).ToString().ToUpper()
                                                        && x.GrossAmount.Value.AsDouble() == tx.Amount
                                                        && x.Timestamp.Date.AddDays(-1) <= tx.TimeCreated
                                                        && x.Timestamp.Date >= tx.TimeCreated).FirstOrDefault();
                }

            }

            if (payment != null)
            {
                return decimal.Parse(payment.FeeAmount.Value);
            }
            return 0;
        }
        private TransactionType GetTransactionType(String type)
        {
           PFPTransactionType pfpType = (PFPTransactionType)Enum.Parse(typeof(PFPTransactionType), type.Replace(" ", ""), true);

            TransactionType txType;

            switch (pfpType)
            {
                case PFPTransactionType.DelayedCapture:
                case PFPTransactionType.Sale:
                    txType = TransactionType.Payment;
                    break;
                case PFPTransactionType.Credit:
                    txType = TransactionType.Refund;
                    break;
                default:
                    txType = TransactionType.Payment;
                    break;
            }

            return txType;
        }


    }
}
