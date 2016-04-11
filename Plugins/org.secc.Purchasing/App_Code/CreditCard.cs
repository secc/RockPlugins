using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using org.secc.Purchasing.DataLayer;
using Rock.Model;

namespace org.secc.Purchasing
{
    [Serializable]
    public class CreditCard : PurchasingBase
    {
        #region Fields
        private int mCreditCardID;
        private string mCardLastFour;
        private DateTime mCardExpirationDate;
        private int mCardholderID;
        private string  mCreatedByUserID;
        private string mModifiedByUserID;
        private DateTime mDateCreated;
        private DateTime mDateModified;
        private bool mActive;
        private Person mCardholder;
        private Person mCreatedBy;
        private Person mModifiedBy;
        #endregion

        #region Properties
        public int CreditCardID
        {
            get
            {
                return mCreditCardID;
            }
            set
            {
                mCreditCardID = value;
            }
        }

        public string CardLastFour
        {
            get
            {
                return mCardLastFour;
            }
            set
            {
                mCardLastFour = value;
            }
        }

        public DateTime CardExpirationDate
        {
            get
            {
                return mCardExpirationDate;
            }
            set
            {
                mCardExpirationDate = value;
            }
        }

        public int CardHolderID
        {
            get
            {
                return mCardholderID;
            }
            set
            {
                mCardholderID = value;
            }
        }

        public string CreatedByUserID
        {
            get
            {
                return mCreatedByUserID;
            }
            set
            {
                mCreatedByUserID = value;
            }
        }

        public string ModifiedByUserID
        {
            get
            {
                return mModifiedByUserID;
            }
            set
            {
                mModifiedByUserID = value;
            }

        }

        public DateTime DateCreated
        {
            get
            {
                return mDateCreated;
            }
            set
            {
                mDateCreated = value;
            }
        }

        public DateTime DateModified
        {
            get
            {
                return mDateModified;
            }
            set
            {
                mDateModified = value;
            }
        }

        public bool Active
        {
            get
            {
                return mActive;
            }
            set
            {
                mActive = value;
            }
        }

        [XmlIgnore]
        public Person Cardholder
        {
            get
            {
                if (mCardholder == null && mCardholderID > 0)
                    mCardholder = personAliasService.Get(mCardholderID).Person;

                return mCardholder;
            }
        }

        [XmlIgnore]
        public Person CreatedBy
        {
            get
            {
                if (mCreatedBy == null && !String.IsNullOrEmpty(mCreatedByUserID))
                    mCreatedBy = userLoginService.GetByUserName(CreatedByUserID).Person;

                return mCreatedBy;
            }
        }

        [XmlIgnore]
        public Person ModifiedBy
        {
            get
            {
                if(mModifiedBy == null && !String.IsNullOrEmpty(ModifiedByUserID))
                    mModifiedBy = userLoginService.GetByUserName(ModifiedByUserID).Person;

                return mModifiedBy;
            }
        }

        #endregion

        #region Constructors
        public CreditCard()
        {
            Init();
        }

        public CreditCard(int ccID)
        {
            if (ccID <= 0)
                throw new ArgumentException("Credit Card ID", "Credit Card ID must be greater than 0.");

            Load(ccID);
        }

        public CreditCard(PaymentCreditCardData data)
        {
            Load(data);
        }
        #endregion

        #region Private
        private void Init()
        {
            mCreditCardID = 0;
            mCardLastFour = String.Empty;
            mCardExpirationDate = DateTime.MinValue;
            mCardholderID = 0;
            mCreatedByUserID = String.Empty;
            mModifiedByUserID = String.Empty;
            mDateCreated = DateTime.MinValue;
            mDateModified = DateTime.MinValue;
            mActive = true;
            mCreatedBy = null;
            mModifiedBy = null;
            mCardholder = null;
        }

        private void Load(PaymentCreditCardData data)
        {
            Init();
            if (data != null)
            {
                mCreditCardID = data.credit_card_id;
                mCardLastFour = data.card_last_four;
                mCardExpirationDate = data.card_expiration_date;

                if (data.card_holder_id == null)
                    mCardholderID = 0;
                else
                    mCardholderID = (int)data.card_holder_id;

                mCreatedByUserID = data.created_by;
                mModifiedByUserID = data.modified_by;
                mDateCreated = data.date_created;

                if (data.date_modified == null)
                    mDateModified = DateTime.MinValue;
                else
                    mDateModified = (DateTime)data.date_modified;

                mActive = data.active;
            }
        }

        private void Load(int ccId)
        {
            using ( PurchasingContext context = ContextHelper.GetDBContext() )
                Load( context.PaymentCreditCardDatas.FirstOrDefault( h => h.credit_card_id == ccId ) );
        }


        private PaymentMethod GetPaymentMethod()
        {
            using (PurchasingContext Context = ContextHelper.GetDBContext())
            {
                PaymentMethodData PayData= Context.PaymentCreditCardDatas.Where(c => c.credit_card_id == CreditCardID).FirstOrDefault().PaymentMethodDatas.FirstOrDefault();

                return new PaymentMethod(PayData);
            }
        }
        #endregion
    }
}
