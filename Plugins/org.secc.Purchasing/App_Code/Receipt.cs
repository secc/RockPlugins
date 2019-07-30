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
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.Purchasing
    
{
    public class Receipt : PurchasingBase
    {
        #region Fields
        private static Guid ShippingCarrierLTGuid = new Guid("3B0AD7B4-845B-4726-8E89-6C2DCD25DF12");
        private Person mReceievedBy;
        private DefinedValueCache mShippingCarrier;
        private Person mCreatedBy;
        private Person mModifiedBy;
        private PurchaseOrder mPurchaseOrder;
        private List<ReceiptItem> mReceiptItems;
        #endregion

        #region Properties
        public int ReceiptID { get; set; }
        public int CarrierLUID { get; set; }
        public int PurchaseOrderID { get; set; }
        public int ReceivedByID { get; set; }
        public DateTime DateReceived { get; set; }
        public string CreatedByUserID { get; set; }
        public string ModifiedByUserID { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }
        public bool Active { get; set; }

        [XmlIgnore]
        public DefinedValueCache ShippingCarrier
        {
            get
            {
                if ((mShippingCarrier == null || mShippingCarrier.Id != CarrierLUID))
                    mShippingCarrier = DefinedValueCache.Get(CarrierLUID);
                return mShippingCarrier;
            }
        }

        public PurchaseOrder PurchaseOrder
        {
            get
            {
                if ((mPurchaseOrder == null || mPurchaseOrder.PurchaseOrderID != PurchaseOrderID) && PurchaseOrderID > 0)
                    mPurchaseOrder = new PurchaseOrder(PurchaseOrderID);
                return mPurchaseOrder;
            }
        }
        
        [XmlIgnore]
        public Person ReceivedBy
        {
            get
            {
                if ((mReceievedBy == null || mReceievedBy.Id != ReceivedByID) && ReceivedByID > 0)
                {
                    mReceievedBy = personAliasService.Get(ReceivedByID).Person;
                }
                return mReceievedBy;
            }
        }

        [XmlIgnore]
        public Person CreatedBy
        {
            get
            {
                if ((mCreatedBy == null) && !String.IsNullOrEmpty(CreatedByUserID))
                    mCreatedBy = userLoginService.GetByUserName(CreatedByUserID).Person;
                return mCreatedBy;
            }
        }

        [XmlIgnore]
        public Person ModifiedBy
        {
            get
            {
                if ((mModifiedBy == null) && !String.IsNullOrEmpty(ModifiedByUserID))
                    mModifiedBy = userLoginService.GetByUserName(ModifiedByUserID).Person;
                return mModifiedBy;
            }
        }

        public List<ReceiptItem> ReceiptItems
        {
            get
            {
                if (mReceiptItems == null)
                {
                   mReceiptItems =  GetReceiptItems();
                }

                return mReceiptItems;
            }
            set
            {
                mReceiptItems = value;
            }
        }

        #endregion

        #region Constructors
        public Receipt() 
        {
            Init();
        }

        public Receipt(int receiptID)
        {
            Load( receiptID );
        }

        public Receipt(ReceiptData data)
        {
            Load(data);
        }
        #endregion

        #region Public

        public static List<DefinedValueCache> GetShippingCarriers(bool activeOnly)
        {
            List<DefinedValueCache> ShippingCarriers = DefinedTypeCache.Get(ShippingCarrierLTGuid).DefinedValues.ToList();
            if (activeOnly)
                ShippingCarriers.RemoveAll(l => l.IsActive == false);

            return ShippingCarriers;
        }

        public void RefreshReceiptItems()
        {
            ReceiptItems = GetReceiptItems();
        }

        public void Save(string uid)
        {
            try
            {
                if (String.IsNullOrEmpty(uid))
                    throw new ArgumentNullException("User ID", "User ID is required to save receipt.");
                Dictionary<string, string> ValErrors = Validate();
                if (ValErrors.Count > 0)
                    throw new RequisitionNotValidException("Receipt is not valid.", ValErrors);

                using (PurchasingContext Context = ContextHelper.GetDBContext())
                {
                    Enums.HistoryType ChangeType;
                    Receipt Original = null;
                    ReceiptData Data;

                    if (ReceiptID <= 0)
                    {
                        Data = new ReceiptData();
                        Data.created_by = uid;
                        Data.date_created = DateTime.Now;
                        ChangeType = Enums.HistoryType.ADD;
                    }
                    else
                    {
                        Data = Context.ReceiptDatas.FirstOrDefault(r => r.receipt_id == ReceiptID);
                        Original = new Receipt(Data);
                        ChangeType = Enums.HistoryType.UPDATE;
                    }

                    Data.carrier_luid = CarrierLUID;
                    Data.purchase_order_id = PurchaseOrderID;
                    Data.received_by_id = ReceivedByID;
                    Data.date_received = DateReceived;
                    Data.modified_by = uid;
                    Data.date_modified = DateTime.Now;
                    Data.active = Active;

                    if (ReceiptID <= 0)
                        Context.ReceiptDatas.InsertOnSubmit(Data);

                    Context.SubmitChanges();
                    Load(Data);
                    SaveHistory(ChangeType, Original, uid);
                }
            }
            catch (Exception ex)
            {
                throw new RequisitionException("An error has occurred while saving receipt", ex);
            }
        }

        public void SaveHistory(Enums.HistoryType changeType, Receipt original, string uid)
        {
            History h = new History();
            h.ObjectTypeName = this.GetType().ToString();
            h.Identifier = ReceiptID;
            h.OrganizationID = PurchaseOrder.OrganizationID;
            h.ChangeType = changeType;

            switch (changeType)
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
        private List<ReceiptItem> GetReceiptItems()
        {
            List<ReceiptItem> Items = new List<ReceiptItem>();
            using (PurchasingContext Context = ContextHelper.GetDBContext())
            {
                foreach (ReceiptItemData data in Context.ReceiptDatas.FirstOrDefault(r => r.receipt_id == ReceiptID).ReceiptItemDatas)
                {
                    Items.Add(new ReceiptItem(data));
                }
            }
            return Items;
        }

        private void Init()
        {
            ReceiptID = 0;
            CarrierLUID = 0;
            PurchaseOrderID = 0;
            ReceivedByID = 0;
            DateReceived = DateTime.MinValue;
            CreatedByUserID = String.Empty;
            ModifiedByUserID = String.Empty;
            DateCreated = DateTime.MinValue;
            DateModified = DateTime.MinValue;
            Active = true;

            mReceievedBy = null;
            mShippingCarrier = null;
            mCreatedBy = null;
            mModifiedBy = null;
            mPurchaseOrder = null;
            mReceiptItems = null;
        }

        private void Load(ReceiptData data)
        {
            Init();
            if (data != null)
            {
                ReceiptID = data.receipt_id;
                CarrierLUID = data.carrier_luid;
                PurchaseOrderID = data.purchase_order_id;
                ReceivedByID = data.received_by_id;
                DateReceived = data.date_received;
                CreatedByUserID = data.created_by;
                ModifiedByUserID = data.modified_by;
                DateCreated = data.date_created;
                DateModified = data.date_modified;
                Active = data.active;
            }
        }

        private void Load(int receiptId)
        {
            using (PurchasingContext Context = ContextHelper.GetDBContext())
            {
                Load( Context.ReceiptDatas.FirstOrDefault( r => r.receipt_id == receiptId ) );
            }
        }


        private Dictionary<string, string> Validate()
        {
            Dictionary<string, string> ValErrors = new Dictionary<string, string>();

            if (CarrierLUID <= 0 || GetShippingCarriers(true).Where(l => l.Id == CarrierLUID).Count() == 0)
                ValErrors.Add("Shipping Carrier", "Shipping Carrier not found.");
            if (PurchaseOrderID <= 0 || PurchaseOrder == null || PurchaseOrder.PurchaseOrderID <= 0 )
                ValErrors.Add("Purchase Order", "Purchase Order not found.");
            if (ReceivedByID <= 0 || ReceivedBy == null || ReceivedBy.Id <= 0)
                ValErrors.Add("Received By", "Received By is required");
            if (DateReceived == DateTime.MinValue)
                ValErrors.Add("Date Received", "Date Received is required.");

            return ValErrors;

        }

        #endregion

    }
}
