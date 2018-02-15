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
using org.secc.Purchasing.Enums;
using org.secc.Purchasing.Accounting;
using Rock.Model;

namespace org.secc.Purchasing
{
    public class RequisitionItem : PurchasingBase
    {
        #region Fields
        private Requisition mRequisition;
        private Person mCreatedBy;
        private Person mModifiedBy;
        private Account mAccount;
        private List<PurchaseOrderItem> mPOItems;

        private Guid ShippingTypeLTGuid = new Guid("DFAC2EB7-97D0-4318-ACF3-554ADC2272F3");
        private Guid SubmittedToPurchasingLUGuid = new Guid("22026706-A51D-44D7-8072-945AAB64A364");
        #endregion

        #region Properties
        public int ItemID { get; set; }
        public int RequisitionID { get; set; }
        public int Quantity { get; set; }
        public string ItemNumber { get; set; }
        public string Description { get; set; }
        public DateTime DateNeeded { get; set; }
        public int CompanyID { get; set; }
        public int FundID { get; set; }
        public int DepartmentID { get; set; }
        public int AccountID { get; set; }
        public DateTime FYStartDate { get; set; }
        public string CreatedByUserID { get; set; }
        public string ModifiedByUserID { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }
        public bool Active { get; set; }
        public decimal Price { get; set; }
        public bool IsExpeditiedShippingAllowed { get; set; }
 
        [XmlIgnore]
        public Requisition Requisition
        {
            get
            {
                if (mRequisition == null && RequisitionID > 0)
                    mRequisition = new Requisition(RequisitionID);
                return mRequisition;
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

        [XmlIgnore]
        public Account Account
        {
            get
            {
                if(CompanyID > 0 && FundID > 0 && DepartmentID > 0 && AccountID > 0 && FYStartDate > DateTime.MinValue)
                    mAccount = new Account(CompanyID, FundID, DepartmentID, AccountID, FYStartDate);
                
                return mAccount;
            }
        }

        [XmlIgnore]
        public List<PurchaseOrderItem> POItems
        {
            get
            {
                if (mPOItems == null && ItemID > 0)
                    mPOItems = PurchaseOrderItem.LoadByRequistionItem(ItemID);

                return mPOItems;
            }
        }

        #endregion

        #region Constructors
        public RequisitionItem()
        {
            Init();
        }

        public RequisitionItem(int itemID)
        {
            Load(itemID);
        }

        public RequisitionItem(RequisitionItemData d)
        {
            Load(d);
        }
        #endregion 

        #region Public

        public bool CanBeDeleted()
        {
            bool Deleteable = true;
            if (Deleteable && POItems.Where(poi => poi.Active == true).Where(poi => poi.PurchaseOrder.Active).Where(poi => poi.PurchaseOrder.StatusLUID != PurchaseOrder.PurchaseOrderStatusCancelledLUID()).Count() > 0)
                Deleteable = false;
            return Deleteable;

        }

        public bool HasChanged()
        {
            bool Changed = false;
            RequisitionItem Original = new RequisitionItem(ItemID);

            if (this.GetHashCode() != Original.GetHashCode())
            {
                if (RequisitionID != Original.RequisitionID)
                    Changed = true;
                if (!Changed && Quantity != Original.Quantity)
                    Changed = true;
                if (!Changed && Description != Original.Description)
                    Changed = true;
                if (!Changed && DateNeeded != Original.DateNeeded)
                    Changed = true;
                if (!Changed && CompanyID != Original.CompanyID)
                    Changed = true;
                if (!Changed && FundID != Original.FundID)
                    Changed = true;
                if (!Changed && DepartmentID != Original.DepartmentID)
                    Changed = true;
                if (!Changed && AccountID != Original.AccountID)
                    Changed = true;
                if (!Changed && FYStartDate != Original.FYStartDate)
                    Changed = true;
                if (!Changed && Active != Original.Active)
                    Changed = true;
                if (!Changed && Price != Original.Price)
                    Changed = true;
                if (!Changed && IsExpeditiedShippingAllowed != Original.IsExpeditiedShippingAllowed)
                    Changed = true;
            }

            return Changed;
        }


        public int GetQuantityReceived()
        {
            return POItems
                    .Where(x => x.Active)
                    .Where(x => x.PurchaseOrder.Active)
                    .Where(x => x.PurchaseOrder.StatusLUID != PurchaseOrder.PurchaseOrderStatusCancelledLUID())
                    .Select(x => x.ReceiptItems.Where(ri => ri.Active).Where(ri=> ri.Receipt.Active).Select(ri => ri.QuantityReceived).Sum()).Sum();
        }
        

        public void Save(string uid)
        {
            try
            {
                RequisitionItem Original = null;
                Enums.HistoryType ChangeType;
                Dictionary<string, string> ValErrors = Validate();
                if (ValErrors.Count > 0) throw new RequisitionNotValidException("The requested item is not valid.", ValErrors);

                using (PurchasingContext Context = ContextHelper.GetDBContext())
                {
                    RequisitionItemData data;
                    if (ItemID > 0)
                    {
                        data = Context.RequisitionItemDatas.FirstOrDefault(x => x.requisition_item_id == ItemID);
                        ChangeType = HistoryType.UPDATE;
                        Original = new RequisitionItem(data);
                    }
                    else
                    {
                        data = new RequisitionItemData();
                        data.created_by = uid;
                        data.date_created = DateTime.Now;
                        ChangeType = HistoryType.ADD;
                    }
                    data.date_modified = DateTime.Now;
                    data.modified_by = uid;

                    data.requisition_id = RequisitionID;
                    data.quantity = Quantity;
                    data.description = Description;
                    
                    if(DateNeeded != DateTime.MinValue)
                        data.date_needed = DateNeeded;

                    if (!String.IsNullOrEmpty(ItemNumber))
                        data.item_number = ItemNumber;
                    else
                        data.item_number = null;
                    
                    data.company_id = CompanyID;
                    data.fund_id = FundID;
                    data.department_id = DepartmentID;
                    data.account_id = AccountID;
                    data.active = Active;
                    data.fiscal_year_start = FYStartDate;
                    data.is_expedited_shipping_allowed = IsExpeditiedShippingAllowed;
                    if ( Price != 0 )
                    {
                        data.price = Price;
                    }
                    else
                    {
                        data.price = null;
                    }

                    if (ItemID == 0)
                        Context.RequisitionItemDatas.InsertOnSubmit(data);

                    Context.SubmitChanges();

                    Load(data);

                    SaveHistory(ChangeType, Original, uid);
                }

            }
            catch (Exception ex)
            {
                throw new RequisitionException("An error has occurred while saving requisition item.", ex);
            }
        }

        public void SaveHistory(HistoryType ht, RequisitionItem orig, string uid)
        {
            History h = new History();
            h.ObjectTypeName = this.GetType().ToString();
            h.Identifier = ItemID;
            h.ChangeType = ht;

            switch (ht)
            {
                case HistoryType.ADD:
                    h.OriginalXML = null;
                    h.UpdatedXML = base.Serialize(this);
                    break;
                case HistoryType.UPDATE:
                    h.OriginalXML = base.Serialize(orig);
                    h.UpdatedXML = base.Serialize(this);
                    break;
                case HistoryType.DELETE:
                    h.OriginalXML = base.Serialize(this);
                    break;
            }

            h.OrganizationID = Requisition.OrganizationID;
            h.Active = true;
            h.Save(uid);
        }


        public void SoftDelete(string uid)
        {
            Active = false;
            Save(uid);
        }

        #endregion

        #region Private
        private void Init()
        {
            ItemID = 0;
            RequisitionID = 0;
            Quantity = 0;
            Description = String.Empty;
            DateNeeded = DateTime.MinValue;
            ItemNumber = String.Empty;
            CompanyID = 0;
            FundID = 0;
            DepartmentID = 0;
            AccountID = 0;
            FYStartDate = new DateTime(DateTime.Now.Year, 1, 1);
            CreatedByUserID = String.Empty;
            ModifiedByUserID = String.Empty;
            DateCreated = DateTime.MinValue;
            DateModified = DateTime.MinValue;
            Active = true;
            Price = 0;
            IsExpeditiedShippingAllowed = false;
            mRequisition = null;
            mCreatedBy = null;
            mModifiedBy = null;


        }

        private void Load(RequisitionItemData data)
        {
            Init();
            if (data != null)
            {
                ItemID = data.requisition_item_id;
                RequisitionID = data.requisition_id;
                Quantity =  data.quantity;
                Description = data.description;

                if (data.date_needed != null)
                    DateNeeded = (DateTime)data.date_needed;

                if (data.item_number != null)
                    ItemNumber = data.item_number;

                CompanyID = data.company_id;
                FundID = data.fund_id;
                DepartmentID = data.department_id;
                AccountID = data.account_id;
                CreatedByUserID = data.created_by;
                ModifiedByUserID = data.modified_by;
                DateCreated = data.date_created;
                DateModified = (DateTime)data.date_modified;
                IsExpeditiedShippingAllowed = data.is_expedited_shipping_allowed;
                if(data.fiscal_year_start != null)
                    FYStartDate = (DateTime)data.fiscal_year_start;

                if (data.price == null)
                    Price = 0;
                else
                    Price = (decimal)data.price;
                Active = data.active;
            }
        }

        private void Load(int requisitionItemId)
        {
            using (PurchasingContext Context = ContextHelper.GetDBContext())
            {
                Load( Context.RequisitionItemDatas.FirstOrDefault( ri => ri.requisition_item_id == requisitionItemId ) );
            }
        }

        private Dictionary<string, string> Validate()
        {
            Dictionary<string, string> ValErrors = new Dictionary<string, string>();
            if (ItemID <= 0)
            {
                if (DateNeeded != DateTime.MinValue && DateNeeded < DateTime.Now.Date)
                    ValErrors.Add("Date Needed", "Date Needed can not be in the past.");
            }
            else
            {
                RequisitionItem Original = new RequisitionItem(ItemID);

                if (Original.Quantity != Quantity)
                {
                    if (Quantity < POItems.Where(poi => poi.Active).Select(poi => (int?)poi.Quantity ?? 0).Sum())
                    {
                        ValErrors.Add("Quantity", "Quantity is less than the quantity assigned to purchase orders.");
                    }
                }
            }

            if(Quantity <= 0)
                ValErrors.Add("Quantity", "Quantity must be greater than 0.");
            if(String.IsNullOrEmpty(Description))
                ValErrors.Add("Description", "Description must be provided.");
            

            if (CompanyID <= 0 || FundID <= 0 || DepartmentID <= 0 || AccountID <= 0 || FYStartDate == DateTime.MinValue)
            { 
                ValErrors.Add("Account", "Please select a valid account");
            } else if (Account == null || Account.AccountID <= 0)
            {
                ValErrors.Add("Account", "Account not found.");
            }

            if (ItemID <= 0 && FYStartDate.Year < DateTime.Now.Year)
                ValErrors.Add("Fiscal Year", "Fiscal year can not be in the past.");


            //if (Price < 0)
            //    ValErrors.Add("Estimated Cost", "Estimated cost can not be negative.");

            return ValErrors;
        }
        
        #endregion

    }

    public class RequisitionItemListItem
    {
        public int ItemID { get; set; }
        public int Quantity { get; set; }
        public int QuantityReceived { get; set; }
        public string ItemNumber { get; set; }
        public string Description { get; set; }
        public DateTime? DateNeeded { get; set; }
        public bool ExpeditedShipping { get; set; }
        public string AccountNumber { get; set; }
        public List<int> PONumbers { get; set; }
        public decimal? EstimatedCost { get; set; }
        public decimal? LineItemCost { get; set; }
        public bool Active { get; set; }

    }

}
