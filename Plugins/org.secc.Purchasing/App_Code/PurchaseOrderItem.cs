using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using org.secc.Purchasing.DataLayer;
using Rock.Model;

namespace org.secc.Purchasing
{
    public class PurchaseOrderItem : PurchasingBase
    {
        #region Fields
        private PurchaseOrder mPurchaseOrder;
        private RequisitionItem mRequisitionItem;
        private Person mCreatedBy;
        private Person mModifiedBy;
        private List<ReceiptItem> mReceiptItems;
        #endregion

        #region Properties
        public int PurchaseOrderItemID { get; set; }
        public int PurchaseOrderID { get; set; }
        public int ItemID { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public bool HasBeenReceived { get; set; }
        public string CreatedByUserID { get; set; }
        public string ModifiedByUserID { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateModifed { get; set; }
        public bool Active { get; set; }


        [XmlIgnore]
        public PurchaseOrder PurchaseOrder
        {
            get
            {
                if (mPurchaseOrder == null && PurchaseOrderID > 0)
                    mPurchaseOrder = new PurchaseOrder(PurchaseOrderID);
                return mPurchaseOrder;
            }
        }

        [XmlIgnore]
        public RequisitionItem RequisitionItem
        {
            get
            {
                if (mRequisitionItem == null && ItemID > 0)
                    mRequisitionItem = new RequisitionItem(ItemID);
                return mRequisitionItem;
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
        public List<ReceiptItem> ReceiptItems
        {
            get
            {
                if (mReceiptItems == null)
                    mReceiptItems = GetReceiptItems();
                return mReceiptItems;
            }
            set
            {
                mReceiptItems = value;
            }
        }

        #endregion

        #region Constructor
        public PurchaseOrderItem()
        {
            Init();
        }

        public PurchaseOrderItem(int purchaseOrderID, int itemID)
        {
            Load( purchaseOrderID, itemID );
        }

        public PurchaseOrderItem(int poItemID)
        {
            Load(poItemID);
        }
        public PurchaseOrderItem(PurchaseOrderItemData data)
        {
            Load(data);
        }
        #endregion

        #region Public
        public static List<PurchaseOrderItem> LoadByPurchaseOrder(int poID)
        {
            try
            {
                if (poID <= 0)
                    throw new ArgumentNullException("poID", "Purchase Order ID is required.");

                using ( PurchasingContext Context = ContextHelper.GetDBContext() )
                {
                    return Context.PurchaseOrderItemDatas
                        .Where( poi => poi.purchase_order_id == poID )
                        .Select( poi => new PurchaseOrderItem( poi ) )
                        .ToList();
                }
            }
            catch (Exception ex)
            {

                throw new RequisitionException("An error has occurred when loading Purchase Order Items.", ex);
            }
        }

        public static List<PurchaseOrderItem> LoadByRequistionItem(int itemID)
        {
            try
            {
                if (itemID <= 0)
                    throw new ArgumentNullException("itemID", "Item ID is required.");

                using ( PurchasingContext Context = ContextHelper.GetDBContext() )
                {
                    return Context.PurchaseOrderItemDatas
                            .Where( poi => poi.requisition_item_id == itemID )
                            .Select( poi => new PurchaseOrderItem( poi ) )
                            .ToList();
                }
            }
            catch (Exception ex)
            {
                throw new RequisitionException("An error has occurred when loading Purchase Order Items.", ex);
            }
        }

        public void Save(string uid)
        {
            try
            {
                if (String.IsNullOrEmpty(uid))
                    throw new ArgumentNullException("uid", "User Name is required");
                Dictionary<string, string> ValErrors = Validate();
                if (ValErrors.Count > 0)
                    throw new RequisitionNotValidException("Purchase Order Item not valid", ValErrors);


                using (PurchasingContext Context = ContextHelper.GetDBContext())
                {
                    Enums.HistoryType ChangeType;
                    PurchaseOrderItem Original = null;
                    PurchaseOrderItemData Data = null;
                    if (PurchaseOrderItemID > 0)
                    {
                        Data = Context.PurchaseOrderItemDatas.FirstOrDefault(x => x.purchase_order_item_id == PurchaseOrderItemID);
                        Original = new PurchaseOrderItem(Data);
                        ChangeType = Enums.HistoryType.UPDATE;
                    }
                    else
                    {
                        Data = new PurchaseOrderItemData();
                        ChangeType = Enums.HistoryType.ADD;
                        Data.date_created = DateTime.Now;
                        Data.created_by = uid;
                    }

                    Data.requisition_item_id = ItemID;
                    Data.purchase_order_id = PurchaseOrderID;
                    Data.quantity = Quantity;
                    Data.has_been_received = HasBeenReceived;
                    Data.active = Active;
                    Data.modified_by = uid;
                    Data.date_modified = DateTime.Now;

                    if ( Price != 0 )
                        Data.price = Price;
                    else
                        Data.price = null;

                    if (PurchaseOrderItemID <= 0)
                        Context.PurchaseOrderItemDatas.InsertOnSubmit(Data);

                    Context.SubmitChanges();

                    Load(Data);
                    SaveHistory(ChangeType, Original, uid);
                }
            }
            catch (Exception ex)
            {
                throw new RequisitionException("An error has occurred while saving Purchase Order Item.", ex);
            }
        }

        public void SaveHistory(Enums.HistoryType ht, PurchaseOrderItem original, string uid)
        {
            History h = new History();
            h.ObjectTypeName = this.GetType().ToString();
            h.Identifier = PurchaseOrderItemID;
            h.ChangeType = ht;

            switch (ht)
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
            h.OrganizationID = PurchaseOrder.OrganizationID;
            h.Save(uid);
        }

        #endregion

        #region Private

        private List<ReceiptItem> GetReceiptItems()
        {
            List<ReceiptItem> ReceiptItems = new List<ReceiptItem>();
            using (PurchasingContext Context = ContextHelper.GetDBContext())
            {
                foreach (var ri in Context.PurchaseOrderItemDatas.FirstOrDefault(x => x.purchase_order_item_id == PurchaseOrderItemID).ReceiptItemDatas)
                {
                    ReceiptItems.Add(new ReceiptItem(ri));
                }
            }
            return ReceiptItems;
        }

        private void Init()
        {
            PurchaseOrderItemID = 0;
            PurchaseOrderID = 0;
            ItemID = 0;
            Quantity = 0;
            HasBeenReceived = false;
            CreatedByUserID = String.Empty;
            ModifiedByUserID = String.Empty;
            DateCreated = DateTime.MinValue;
            DateModifed = DateTime.MinValue;
            Active = true;
            Price = 0;

            mPurchaseOrder = null;
            mRequisitionItem = null;
            mCreatedBy = null;
            mModifiedBy = null;
        }

        private void Load(PurchaseOrderItemData data)
        {
            Init();

            if (data != null)
            {
                PurchaseOrderItemID = data.purchase_order_item_id;
                PurchaseOrderID = data.purchase_order_id;
                ItemID = data.requisition_item_id;
                Quantity = data.quantity;
                HasBeenReceived = data.has_been_received;
                CreatedByUserID = data.created_by;
                ModifiedByUserID = data.modified_by;
                DateCreated = data.date_created;
                DateModifed = data.date_modified;
                Active = data.active;
                if (data.price != null)
                    Price = (decimal)data.price;
            }
        }

        private void Load(int poItemId)
        {
            try
            {
                using (PurchasingContext Context = ContextHelper.GetDBContext())
                {
                    Load( Context.PurchaseOrderItemDatas.FirstOrDefault( poi => poi.purchase_order_item_id == poItemId ) );
                }
            }
            catch (Exception ex)
            {
                throw new RequisitionException("An error has occurred while loading Purchase Order Items", ex);
            }

        }

        private void Load( int poId, int reqItemId )
        {
            try
            {
                using ( PurchasingContext Context = ContextHelper.GetDBContext() )
                {
                    var poiData = Context.PurchaseOrderItemDatas
                                    .Where( poi => poi.purchase_order_id == poId )
                                    .Where( poi => poi.requisition_item_id == reqItemId )
                                    .FirstOrDefault();

                    Load( poiData );
                }
            }
            catch ( Exception ex )
            {

                throw new RequisitionException( "An error has occurred while loading Purchase Order Items", ex );
            }
        }

        private Dictionary<string, string> Validate()
        {
            Dictionary<string, string> ValErrors = new Dictionary<string, string>();

            if (PurchaseOrderID <= 0)
                ValErrors.Add("PurchaseOrderID", "Purchase Order ID is required.");
            else if (PurchaseOrder == null || PurchaseOrder.PurchaseOrderID <= 0)
                ValErrors.Add("Purchase Order", string.Format("Could not find purchase order {0}.", PurchaseOrderID));

            if (ItemID <= 0)
                ValErrors.Add("ItemID", "Requisition Item ID is required.");
            else if (RequisitionItem == null || RequisitionItem.ItemID <= 0)
                ValErrors.Add("ItemID", string.Format("Could not find requisition item {0}.", ItemID));

            if (PurchaseOrderItemID > 0 && 
                    RequisitionItem.POItems
                    .Where(x => x.PurchaseOrderID != PurchaseOrderID && x.Active)
                    .Where(x => x.PurchaseOrder.Active)
                    .Where(x => x.PurchaseOrder.StatusLUID != PurchaseOrder.PurchaseOrderStatusCancelledLUID())
                    .Select(x => x.Quantity).Sum() + Quantity > RequisitionItem.Quantity)
                ValErrors.Add("Quantity", string.Format("Quantity added for item {0} is greater than the item's quantity that is not added to a PO. Verify and retry", RequisitionItem.Description));
            else if (PurchaseOrderItemID == 0 && 
                        RequisitionItem.POItems
                        .Where(x => x.Active)
                        .Where(x => x.PurchaseOrder.Active)
                        .Where(x => x.PurchaseOrder.StatusLUID != PurchaseOrder.PurchaseOrderStatusCancelledLUID())
                        .Select(x =>  x.Quantity).Sum() + Quantity > RequisitionItem.Quantity)
            {
                ValErrors.Add("Quantity", string.Format("Quantity added for item {0}  is greater than the items quantity that is not added to a PO.", RequisitionItem.Description));
            }

            //if (Price < 0)
            //    ValErrors.Add("Price", "Price must be equal to or greater than 0.");

            if (Quantity == 0)
            {
                ValErrors.Add("Quantity", "Quantity is required.");
            }

            return ValErrors;
        }
        #endregion
    }

    public class ItemToAddToPO
    {
        public int RequisitionItemID { get; set; }
        public int QuantityToAdd { get; set; }
        public decimal ItemPrice { get; set; }

        public ItemToAddToPO() { }
        public ItemToAddToPO(int itemID, int qty, decimal price)
        {
            RequisitionItemID = itemID;
            QuantityToAdd = qty;
            ItemPrice = price;
        }
    }
}
