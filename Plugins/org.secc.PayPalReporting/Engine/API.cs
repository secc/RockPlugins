using System;
using System.Linq;
using Rock;
using PayPal.PayPalAPIInterfaceService.Model;
using PayPal.PayPalAPIInterfaceService;
using System.Collections.Generic;

namespace org.secc.PayPalReporting.Engine
{
    class API
    {
        private String mode = "live";
        public String Mode {
            get {
                return mode;
            }
            set
            {
                mode = value;
            }
        }
        
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

        public Decimal GetTransactionFee(Model.Transaction tx)
        {
            TransactionSearchReq request = new TransactionSearchReq();

            request.TransactionSearchRequest = new TransactionSearchRequestType();

            request.TransactionSearchRequest.TransactionID = tx.MerchantTransactionId;
            request.TransactionSearchRequest.StartDate = tx.TimeCreated.Value.ToString("yyyy-MM-ddTHH:mm:ss");
            request.TransactionSearchRequest.EndDate = tx.TimeCreated.Value.ToString("yyyy-MM-ddTHH:mm:ss");

            Dictionary<string, string> config = new Dictionary<string, string>();
            config.Add("mode", Mode);
            config.Add("account1.apiUsername", Username);
            config.Add("account1.apiPassword", Password);
            config.Add("account1.apiSignature", Signature);

            // Create the PayPalAPIInterfaceServiceService service object to make the API call
            PayPalAPIInterfaceServiceService service = new PayPalAPIInterfaceServiceService(config);

            TransactionSearchResponseType resp = service.TransactionSearch(request);
          
            PaymentTransactionSearchResultType payment;

            if (resp.Errors.Count > 0)
            {
                throw new Exception(resp.Errors.Select(e => e.ShortMessage).Aggregate((current, next) => current + ", " + next));
            }

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
                                                        && x.GrossAmount.value.AsDouble() == tx.Amount
                                                        && x.Timestamp.AsDateTime().Value.AddDays(-1) <= tx.TimeCreated
                                                        && x.Timestamp.AsDateTime().Value >= tx.TimeCreated).FirstOrDefault();
                }

            }

            if (payment != null)
            {
                return decimal.Parse(payment.FeeAmount.value);
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
