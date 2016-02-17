using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using org.secc.Purchasing.DataLayer;
using org.secc.Purchasing.Enums;
using Rock.Model;

namespace org.secc.Purchasing
{
    public class PaymentMethod : PurchasingBase
    {
        #region Fields
        private int mPaymentMethodID;
        private string mName;
        private string mDescription;
        private string mCreatedByUserID;
        private string mModifiedByUserID;
        private int mCreditCardID;
        private DateTime mDateCreated;
        private DateTime mDateModified;
        private bool mActive;
        private int mOrganizationID;
        private Person mCreatedBy;
        private Person mModifiedBy;
        private CreditCard mCreditCard;
        #endregion

        #region Properties
        public int PaymentMethodID
        {
            get
            {
                return mPaymentMethodID;
            }
            set
            {
                mPaymentMethodID = value;
            }
        }

        public string Name
        {
            get
            {
                return mName;
            }
            set
            {
                mName = value;
            }
        }

        public string Description
        {
            get
            {
                return mDescription;
            }
            set
            {
                mDescription = value;
            }
        }

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

        private int OrganizationID
        {
            get
            {
                return mOrganizationID;
            }
            set
            {
                mOrganizationID = value;
            }
        }

        [XmlIgnore]
        public Person CreatedBy
        {
            get
            {
                if (mCreatedBy == null && !String.IsNullOrEmpty(CreatedByUserID))
                    mCreatedBy = userLoginService.GetByUserName(CreatedByUserID).Person;

                return mCreatedBy;
            }
        }

        [XmlIgnore]
        public Person ModifiedBy
        {
            get
            {
                if (mModifiedBy == null && !String.IsNullOrEmpty(ModifiedByUserID))
                    mModifiedBy = userLoginService.GetByUserName(ModifiedByUserID).Person;
                return mModifiedBy;
            }
        }

        public CreditCard CreditCard
        {
            get
            {
                if (mCreditCard == null && mCreditCardID > 0)
                    mCreditCard = new CreditCard(CreditCardID);

                return mCreditCard;
            }
            set
            {
                mCreditCard = value;
            }
        }


        #endregion

        #region Constructor
        public PaymentMethod()
        {
            Init();
        }

        public PaymentMethod(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Payment Method ID", "Payment Method ID must be greater than 0.");
            Load(id);
        }

        public PaymentMethod(PaymentMethodData data)
        {
            Load(data);
        }
        #endregion

        #region Public

        public static List<PaymentMethod> LoadPaymentMethods()
        {
            return GetPaymentMethods( false );
        }


        public static List<PaymentMethod> GetPaymentMethods(bool activeOnly)
        {
            using ( PurchasingContext Context = ContextHelper.GetDBContext() )
            {
                var query = Context.PaymentMethodDatas
                                .Where( pm => pm.organization_id == 1 );

                if ( activeOnly )
                {
                    query.Where( pm => pm.active );
                }

                return query.Select( pm => new PaymentMethod( pm ) ).ToList();
            }
        
        }

        public static List<PaymentMethod> GetPaymentMethods(string name, bool activeOnly)
        {
            using ( PurchasingContext Context = ContextHelper.GetDBContext() )
            {
                var query = Context.PaymentMethodDatas
                                .Where( pm => pm.name == name );

                if ( activeOnly )
                {
                    query = query.Where( pm => pm.active );
                }

                return query.Select( pm => new PaymentMethod( pm ) ).ToList();
            }
            
        }


        public void Save(string uid)
        {
            try
            {
                Dictionary<string, string> ValErrors = Validate();
                if (ValErrors.Count > 0)
                    throw new RequisitionNotValidException("Payment Method is not valid.", ValErrors);

                HistoryType UpdateType;
                PaymentMethod Original = null;

                using (PurchasingContext Context = ContextHelper.GetDBContext())
                {
                    PaymentMethodData Data = new PaymentMethodData();
                    if (PaymentMethodID == 0)
                    {
                        UpdateType = HistoryType.ADD;
                        Data = new PaymentMethodData();
                        Data.date_created = DateTime.Now;
                        Data.created_by = uid;
                        Data.organization_id = 1;
                    }
                    else
                    {
                        Data = Context.PaymentMethodDatas.Where(x => x.payment_method_id == PaymentMethodID).FirstOrDefault();
                        Original = new PaymentMethod(Data);
                        UpdateType = HistoryType.UPDATE;
                    }

                    Data.name = Name;

                    if (String.IsNullOrEmpty(Description))
                        Data.description = null;
                    else
                        Data.description = Description;

                    Data.date_modified = DateTime.Now;
                    Data.modified_by = uid;
                    Data.active = Active;

                    if (CreditCard != null)
                    {
                        if (Data.PaymentCreditCardData == null)
                        {
                            Data.PaymentCreditCardData = new PaymentCreditCardData();
                            Data.PaymentCreditCardData.created_by = uid;
                            Data.PaymentCreditCardData.date_created = DateTime.Now;
                        }

                        Data.PaymentCreditCardData.card_last_four = CreditCard.CardLastFour;
                        Data.PaymentCreditCardData.card_expiration_date = CreditCard.CardExpirationDate;
                        Data.PaymentCreditCardData.card_holder_id = CreditCard.CardHolderID;
                        Data.PaymentCreditCardData.modified_by = uid;
                        Data.PaymentCreditCardData.date_modified = DateTime.Now;
                    }
                    else
                    {
                        if (Data.PaymentCreditCardData != null)
                        {
                            PaymentCreditCardData CC = Data.PaymentCreditCardData;
                            CC.active = false;
                            CC.date_modified = DateTime.Now;
                            CC.modified_by = uid;

                            Data.PaymentCreditCardData = null;
                        }
                    }

                    if (PaymentMethodID == 0)
                        Context.PaymentMethodDatas.InsertOnSubmit(Data);

                    Context.SubmitChanges();
                    Load(Data);
                    SaveHistory(UpdateType, Original, uid);

                }
            }
            catch (Exception ex)
            {

                throw new PaymentMethodException("An error has occurred while saving Payment Method", ex);
            }
        }

        #endregion

        #region Private

        public void Init()
        {
            mPaymentMethodID = 0;
            mName = String.Empty;
            mDescription = String.Empty;
            mCreditCardID = 0;
            mCreatedByUserID = String.Empty;
            mModifiedByUserID = String.Empty;
            mDateCreated = DateTime.MinValue;
            mDateModified = DateTime.MinValue;
            mOrganizationID = 0;
            mActive = true;
            mCreatedBy = null;
            mModifiedBy = null;
        }

        private void Load(PaymentMethodData data)
        {
            Init();
            if (data != null)
            {
                mPaymentMethodID = data.payment_method_id;
                mName = data.name;
                mDescription = data.description;
                
                if (data.credit_card_id == null)
                    mCreditCardID = 0;
                else
                    mCreditCardID = (int)data.credit_card_id;

                mCreatedByUserID = data.created_by;
                mModifiedByUserID = data.modified_by;
                mDateCreated = data.date_created;

                if (data.date_modified != null)
                    mDateModified = (DateTime) data.date_modified;

                mActive = data.active;
                mOrganizationID = data.organization_id;

                if (mCreditCardID > 0)
                    mCreditCard = new CreditCard(data.PaymentCreditCardData);

            }
        }

        private void Load(int paymentMethodId)
        {
            try
            {
                using (PurchasingContext Context = ContextHelper.GetDBContext())
                    Load(Context.PaymentMethodDatas.FirstOrDefault(pm => pm.payment_method_id == paymentMethodId));
            }
            catch (Exception ex)
            {
                throw new PaymentMethodException("An error has occurred while loading Payment Method.", ex);
            }
        }   

        private void SaveHistory(HistoryType chgType, PaymentMethod orig, string uid)
        {
            History PayHistory = new History();
            PayHistory.ObjectTypeName = this.GetType().ToString();
            PayHistory.Identifier = this.PaymentMethodID;
            PayHistory.OrganizationID = this.OrganizationID;

            switch (chgType)
            {
                case HistoryType.ADD:
                    PayHistory.UpdatedXML = Serialize(this);
                    break;
                case HistoryType.UPDATE:
                    PayHistory.OriginalXML = Serialize(orig);
                    PayHistory.UpdatedXML = Serialize(this);
                    break;
                case HistoryType.DELETE:
                    PayHistory.OriginalXML = Serialize(orig);
                    break;
            }

            PayHistory.Save(uid);
        }

        private Dictionary<string, string> Validate()
        {
            Dictionary<string, string> ValidationErrors = new Dictionary<string, string>();

            return ValidationErrors;
        }

        #endregion
    }

    [Serializable]
    public class PaymentMethodException : Exception
    {
        public PaymentMethodException() { }
        public PaymentMethodException(string message) : base(message) { }
        public PaymentMethodException(string message, Exception inner) : base(message, inner) { }
        protected PaymentMethodException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }

    [Serializable]
    public class PaymentMethodNotValidException : Exception
    {
        private Dictionary<string, string> mInvalidProperties = null;

        public PaymentMethodNotValidException() { }
        public PaymentMethodNotValidException(string message) : base(message) { }

        public PaymentMethodNotValidException(string message, Dictionary<string, string> invalidProps)
            : base(message)
        {
            mInvalidProperties = invalidProps;
        }

        public virtual Dictionary<string, string> InvalidProperties
        {
            get
            {
                return mInvalidProperties;
            }
        }

        public override string Message
        {
            get
            {
                string msg = base.Message;
                if (mInvalidProperties.Count > 0)
                {

                    System.Text.StringBuilder sb = new StringBuilder();
                    sb.AppendLine("The following fields are not valid:");
                    foreach (KeyValuePair<string, string> kvp in InvalidProperties)
                    {
                        sb.Append(kvp.Key + " - " + kvp.Value);
                    }

                    msg += "\n" + sb.ToString();
                }

                return msg;
            }
        }

        public PaymentMethodNotValidException(string message, Exception inner) : base(message, inner) { }
        protected PaymentMethodNotValidException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }

    }
}
