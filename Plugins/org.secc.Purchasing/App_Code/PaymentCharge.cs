// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License shoud be included with this file.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

using org.secc.Purchasing.DataLayer;
using org.secc.Purchasing.Accounting;
using Rock.Model;
namespace org.secc.Purchasing
{
    public class PaymentCharge : PurchasingBase
    {
        #region Fields
        Requisition mRequisition;
        Payment mPayment;
        Account mAccount;
        Person mCreatedBy;
        Person mModifiedBy;

        #endregion

        #region Properties
        public int ChargeID { get; set; }
        public int PaymentID { get; set; }
        public int RequisitionID { get; set; }
        public int CompanyID { get; set; }
        public int FundID { get; set; }
        public int DepartmentID { get; set; }
        public int AccountID { get; set; }
        public DateTime FYStartDate { get; set; }
        public decimal Amount { get; set; }
        public string CreatedByUserID { get; set; }
        public string ModifiedByUserID { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }
        public bool Active { get; set; }

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

        [XmlIgnore]
        public Payment Payment
        {
            get
            {
                if ((mPayment == null || mPayment.PaymentID != PaymentID) && PaymentID > 0)
                    mPayment = new Payment(PaymentID);
                return mPayment;
            }
        }

        [XmlIgnore]
        public Requisition Requisition
        {
            get
            {
                if((mRequisition == null || mRequisition.RequisitionID != RequisitionID) && RequisitionID > 0)
                    mRequisition = new Requisition(RequisitionID);
                return mRequisition;
            }
        }

        [XmlIgnore]
        public Account Account
        {
            get
            {
                if ((mAccount == null || (mAccount.CompanyID != CompanyID || mAccount.FundID != FundID || mAccount.DepartmentID != DepartmentID || mAccount.AccountID != AccountID)) &&
                    CompanyID > 0 && FundID > 0 && DepartmentID > 0 && AccountID > 0 && FYStartDate > DateTime.MinValue)
                    mAccount = new Account(CompanyID, FundID, DepartmentID, AccountID, FYStartDate);
                return mAccount;
            }
        }



        #endregion

        #region Constructor
        public PaymentCharge()
        {
            Init();
        }

        public PaymentCharge(int paymentChargeID)
        {
            Load(paymentChargeID);
        }

        public PaymentCharge(PaymentChargeData data)
        {
            Load(data);
        }
        
        #endregion

        #region Public
        public void Save(string uid)
        {
            try
            {
                if (String.IsNullOrEmpty(uid))
                    throw new ArgumentNullException("uid", "User ID is required.");

                Dictionary<string, string> ValErrors = Validate();
                if (ValErrors.Count > 0)
                    throw new RequisitionNotValidException("Payment Charge is not valid.", ValErrors);

                using (PurchasingContext Context = ContextHelper.GetDBContext())
                {
                    Enums.HistoryType ChangeType;
                    PaymentCharge Original = null;
                    PaymentChargeData Data = null;

                    if (ChargeID == 0)
                    {
                        ChangeType = Enums.HistoryType.ADD;

                        Data = new PaymentChargeData();
                        Data.created_by = uid;
                        Data.date_created = DateTime.Now;

                    }
                    else
                    {
                        ChangeType = Enums.HistoryType.UPDATE;
                        Data = Context.PaymentChargeDatas.FirstOrDefault(x => x.charge_id == ChargeID);
                        Original = new PaymentCharge(Data);
                    }

                    Data.payment_id = PaymentID;
                    Data.requisition_id = RequisitionID;
                    Data.company_id = CompanyID;
                    Data.fund_id = FundID;
                    Data.department_id = DepartmentID;
                    Data.account_id = AccountID;
                    Data.fiscal_year_start = FYStartDate;
                    Data.amount = Amount;
                    Data.modified_by = uid;
                    Data.date_modified = DateTime.Now;
                    Data.active = Active;

                    if (ChargeID == 0)
                        Context.PaymentChargeDatas.InsertOnSubmit(Data);

                    Context.SubmitChanges();
                    Load(Data);

                    SaveHistory(ChangeType, Original, uid);
                }
            }
            catch (Exception ex)
            {
                throw new RequisitionException("An error has occurred while saving payment charge.", ex);
            }
        }

        public void SaveHistory(Enums.HistoryType ct, PaymentCharge original, string uid)
        {
            History h = new History();
            h.ObjectTypeName = this.GetType().ToString();
            h.Identifier = ChargeID;
            h.OrganizationID = Requisition.OrganizationID;
            h.ChangeType = ct;
            h.Active = true;

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

            h.Save(uid);
        }

        #endregion

        #region Private
        private void Init()
        {
            ChargeID = 0;
            PaymentID = 0;
            RequisitionID = 0;
            CompanyID = Company.GetDefaultCompany().CompanyID;
            FundID = 0;
            DepartmentID = 0;
            AccountID = 0;
            FYStartDate = new DateTime(DateTime.Now.Year, 1, 1);
            Amount = 0;
            CreatedByUserID = String.Empty;
            ModifiedByUserID = String.Empty;
            DateCreated = DateTime.MinValue;
            DateModified = DateTime.MinValue;
            Active = true;

            mCreatedBy = null;
            mModifiedBy = null;
            mPayment = null;
            mRequisition = null;

        }

        private void Load(int paymentChargeId)
        {
            try
            {

                using (PurchasingContext Context = ContextHelper.GetDBContext())
                {
                    Load(Context.PaymentChargeDatas.FirstOrDefault(pc => pc.charge_id == paymentChargeId));
                }
            }
            catch (Exception ex)
            {
                throw new RequisitionException("An error occurred while loading payment charge.", ex);
            }
        }

        private void Load(PaymentChargeData data)
        {
            Init();
            if (data != null)
            {
                ChargeID = data.charge_id;
                PaymentID = data.payment_id;
                RequisitionID = data.requisition_id;
                CompanyID = data.company_id;
                FundID = data.fund_id;
                DepartmentID = data.department_id;
                AccountID = data.account_id;
                FYStartDate = data.fiscal_year_start;
                Amount = data.amount;
                CreatedByUserID = data.created_by;
                ModifiedByUserID = data.modified_by;
                DateCreated = data.date_created;
                DateModified = data.date_modified;
                Active = data.active;
            }
        }

        private Dictionary<string, string> Validate()
        {
            Dictionary<string, string> ValErrors = new Dictionary<string, string>();

            if (PaymentID <= 0)
                ValErrors.Add("PaymentID", "Payment ID is required.");
            if (PaymentID > 0 && (Payment == null || Payment.PaymentID <= 0))
                ValErrors.Add("PaymentID", "Payment not found.");

            if (RequisitionID <= 0)
                ValErrors.Add("RequisitionID", "Requisition is required.");
            if(RequisitionID > 0 && (Requisition == null || Requisition.RequisitionID <= 0))
                ValErrors.Add("RequisitionID", "Requisition not found.");

            if (CompanyID <= 0)
                ValErrors.Add("CompanyID", "Company ID must be greater than 0.");

            if (FundID <= 0)
                ValErrors.Add("FundID", "Fund ID must be greater than 0.");

            if (DepartmentID <= 0)
                ValErrors.Add("DepartmentID", "Department ID must be greater than 0.");

            if (AccountID <= 0)
                ValErrors.Add("AccountID", "Account ID must be greater than 0.");

            if (CompanyID > 0 && FundID > 0 && DepartmentID > 0 && AccountID > 0 && (Account == null || Account.AccountID <= 0))
                ValErrors.Add("AccountID", "Account not found");

            if ((Payment.PaymentAmount > 0 && Amount < 0) || (Payment.PaymentAmount < 0 && Amount > 0))
            {
                ValErrors.Add("Charge Amount", "Charge Amount is not valid.");
            }


            if (ChargeID > 0)
            {
                if (Math.Abs(Payment.Charges.Where(x => x.Active == true && x.ChargeID != ChargeID).Select(x => x.Amount).Sum()) + Math.Abs(Amount) > Math.Abs(Payment.PaymentAmount))
                    ValErrors.Add("Charge Amounts", "Total charge amount exceeds payment amount.");
            }
            else
            {
                if (Math.Abs(Payment.Charges.Where(x => x.Active == true).Select(x => x.Amount).Sum()) + Math.Abs(Amount) > Math.Abs(Payment.PaymentAmount))
                    ValErrors.Add("Charge Amounts", "Total charge amount exceeds payment amount.");
            }


            return ValErrors;

        }

        #endregion
    }
}
