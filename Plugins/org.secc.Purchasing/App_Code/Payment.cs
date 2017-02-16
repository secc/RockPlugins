using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

using org.secc.Purchasing.DataLayer;
using Rock.Model;

namespace org.secc.Purchasing
{
    public class Payment : PurchasingBase
    {
        #region Fields
        private PaymentMethod mPaymentMethod;
        private PurchaseOrder mPurchaseOrder;
        private Person mCreatedBy;
        private Person mModifiedBy;
        private List<PaymentCharge> mCharges;
        #endregion

        #region Properties
        public int PaymentID { get; set; }
        public int PaymentMethodID { get; set; }
        public int PurchaseOrderID { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal PaymentAmount { get; set; }
        public string CreatedByUserID { get; set; }
        public string ModifiedByUserID { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }
        public bool Active { get; set; }
        public string Note { get; set; }

        [XmlIgnore]
        public PaymentMethod PaymentMethod
        {
            get
            {
                if ((mPaymentMethod == null || mPaymentMethod.PaymentMethodID != PaymentMethodID) && PaymentMethodID > 0)
                {
                    mPaymentMethod = new PaymentMethod(PaymentMethodID);
                }
                return mPaymentMethod;
            }
        }

        [XmlIgnore]
        public PurchaseOrder PurchaseOrder
        {
            get
            {
                if((mPurchaseOrder == null || mPurchaseOrder.PurchaseOrderID != PurchaseOrderID) && PurchaseOrderID > 0)
                {
                    mPurchaseOrder = new PurchaseOrder(PurchaseOrderID);
                }

                return mPurchaseOrder;
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
                if (ModifiedBy == null && !String.IsNullOrEmpty(ModifiedByUserID))
                    mModifiedBy = userLoginService.GetByUserName(ModifiedByUserID).Person;
                return mModifiedBy;
            }
        }

        [XmlIgnore]
        public List<PaymentCharge> Charges
        {
            get
            {
                if (mCharges == null && PaymentID > 0)
                    mCharges = LoadCharges();
                return mCharges;
            }
            set
            {
                mCharges = value;
            }
        }
        #endregion

        #region Constructors
        public Payment() 
        {
            Init();
        }
        public Payment(int paymentID)
        {
            Load(paymentID);
        }
        public Payment(PaymentData data)
        {
            Load(data);
        }
        #endregion

        #region Public

        public void RefreshCharges()
        {
            Charges = LoadCharges();
        }

        public void DeactivateCharges(string uid)
        {
            foreach (var item in Charges)
            {
                item.Active = false;
                item.Save(uid);
            }
        }

        public void Save(string uid)
        {
            try
            {
                if (String.IsNullOrEmpty(uid))
                    throw new ArgumentNullException("UID", "User ID is required.");

                Dictionary<string, string> ValErrors = Validate();
                if (ValErrors.Count > 0)
                    throw new RequisitionNotValidException("Payment is not valid", ValErrors);

                using (PurchasingContext Context = ContextHelper.GetDBContext())
                {
                    Enums.HistoryType ChangeType;
                    Payment Original = null;
                    PaymentData data;

                    if (PaymentID <= 0)
                    {
                        data = new PaymentData();
                        ChangeType = Enums.HistoryType.ADD;
                        data.date_created = DateTime.Now;
                        data.created_by = uid;
                    }
                    else
                    {
                        data = Context.PaymentDatas.FirstOrDefault(x => x.payment_id == PaymentID);
                        ChangeType = Enums.HistoryType.UPDATE;
                        Original = new Payment(data);
                    }

                    data.payment_method_id = PaymentMethodID;
                    data.purchase_order_id = PurchaseOrderID;
                    data.payment_date = PaymentDate;
                    data.payment_amount = PaymentAmount;
                    data.modified_by = uid;
                    data.date_modified = DateTime.Now;
                    data.active = Active;
                    data.note = Note;

                    if (PaymentID <= 0)
                        Context.PaymentDatas.InsertOnSubmit(data);

                    Context.SubmitChanges();
                    Load(data);
                    SaveHistory(ChangeType, Original, uid);
                }
            }
            catch (Exception ex)
            {
                throw new RequisitionException("An error has occurred while saving payment.", ex);
            }
        }

        public void SaveHistory(Enums.HistoryType ct, Payment original, string uid)
        {
            History h = new History();
            h.ObjectTypeName = this.GetType().ToString();
            h.Identifier = PaymentID;
            h.OrganizationID = PurchaseOrder.OrganizationID;
            h.ChangeType = ct;

            switch (ct)
            {
                case org.secc.Purchasing.Enums.HistoryType.ADD:
                    h.OriginalXML = null;
                    h.UpdatedXML = Serialize(this);
                    break;
                case org.secc.Purchasing.Enums.HistoryType.UPDATE:
                    h.OriginalXML = Serialize(original);
                    h.UpdatedXML = Serialize(this);
                    break;
                case org.secc.Purchasing.Enums.HistoryType.DELETE:
                    h.OriginalXML = Serialize(this);
                    h.UpdatedXML = null;
                    break;
            }

            h.Active = true;
            h.Save(uid);
        }

        #endregion

        #region Private
        private void Init()
        {
            PaymentID = 0;
            PaymentMethodID = 0;
            PurchaseOrderID = 0;
            PaymentAmount = 0;
            PaymentDate = DateTime.MinValue;
            DateCreated = DateTime.MinValue;
            DateModified = DateModified;
            CreatedByUserID = String.Empty;
            ModifiedByUserID = String.Empty;
            Active = true;
            Note = string.Empty;
        }

        private void Load(PaymentData data)
        {
            Init();
            if (data != null)
            {
                PaymentID = data.payment_id;
                PaymentMethodID = data.payment_method_id;
                PurchaseOrderID = data.purchase_order_id;
                PaymentAmount = data.payment_amount;
                PaymentDate = data.payment_date;
                DateCreated = data.date_created;
                DateModified = data.date_modified;
                CreatedByUserID = data.created_by;
                ModifiedByUserID = data.modified_by;
                Active = data.active;
                Note = data.note;
            }
        }

        private void Load(int paymentId)
        {
            using (PurchasingContext Context = ContextHelper.GetDBContext())
            {
                Load(Context.PaymentDatas.FirstOrDefault(p => p.payment_id == paymentId));
            }
        }

        private List<PaymentCharge> LoadCharges()
        {
            List<PaymentCharge> ChargeList = new List<PaymentCharge>();

            using (PurchasingContext Context = ContextHelper.GetDBContext())
            {
                foreach (PaymentChargeData charge in Context.PaymentDatas.FirstOrDefault(x => x.payment_id == PaymentID).PaymentChargeDatas)
                {
                    ChargeList.Add(new PaymentCharge(charge));
                }
            }

            return ChargeList;
        }

        private Dictionary<string, string> Validate()
        {
            Dictionary<string, string> ValErrors = new Dictionary<string, string>();

            if (PaymentMethodID <= 0)
                ValErrors.Add("Payment Method ID", "Payment Method is required.");
            else if (PaymentMethod == null || PaymentMethod.PaymentMethodID <= 0)
                ValErrors.Add("Payment Method ID", "Selected payment method not found.");

            if (PurchaseOrderID <= 0)
                ValErrors.Add("Purchase Order ID", "Purchase Order ID is required.");
            else if (PurchaseOrder == null || PurchaseOrder.PurchaseOrderID <= 0)
                ValErrors.Add("Purchase Order ID", "Purchase Order not found.");

            if (PaymentDate == DateTime.MinValue)
                ValErrors.Add("Invoice Date", "Invoice Date is required in m/d/yyyy format.");

            if (PaymentDate.Date > DateTime.Now.Date)
                ValErrors.Add("Invoice Date", "Invoice Date can not be a future date.");

            return ValErrors;
        }


        #endregion
    }
}
