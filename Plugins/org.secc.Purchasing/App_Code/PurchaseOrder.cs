using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using org.secc.Purchasing.DataLayer;
using Rock.Model;
using Rock;
using System.Data.SqlClient;

namespace org.secc.Purchasing
{
    public class PurchaseOrder : PurchasingBase
    {

        #region Fields

        public static Guid PurchaseOrderTypeGuid = new Guid("E873ADBF-42F8-41F4-BF7D-4877601CC15A");
        public static Guid PurchaseOrderStatusGuid = new Guid("C2EA5FA6-3767-4827-87CA-4A6CBE538608");
        public static Guid PurchaseOrderStatusOrderedGuid = new Guid("6A3AC70A-3ED6-4977-86F3-14C3C5101888");
        public static Guid PurchaseOrderStatusPartiallyReceived = new Guid("B57A7433-EA14-43EE-8316-3C061EFCA671");
        public static Guid PurchaseOrderStatusReceived = new Guid("EE8D8BD4-E4DE-4E06-AE96-7D15B8034C14");
        public static Guid PurchaseOrderStatusBilled = new Guid("0F33DCA2-7D73-41A4-A466-A74150B7211F");
        public static Guid PurchaseOrderStatusClosed = new Guid("BE4FD01A-2EFD-48A1-AFFD-A6F504E0640C");
        public static Guid PurchaseOrderStatusReopened = new Guid("CB900747-0DDA-47BB-AFBD-5BDA02A4826A");
        public static Guid PurchaseOrderStatusOpen = new Guid("4797DD09-D109-4D43-BA8E-80E139CFDAED");
        public static Guid PurchaseOrderStatusCancelled = new Guid( "3BEA3A4C-D9B0-47E3-86C0-FA1BF1E88188" );


        private Vendor mVendor;
        private Person mCreatedBy;
        private Person mModifiedBy;
        private DefinedValue mPurchaseOrderType;
        private DefinedValue mStatus;
        private Person mOrderedBy;
        private PaymentMethod mDefaultPaymentMethod;

        private List<PurchaseOrderItem> mItems;
        private List<Receipt> mReceipts;
        private List<Payment> mPayments;
        private List<Note> mNotes;
        private List<Attachment> mAttachment;
        #endregion

        #region Properties
        public int PurchaseOrderID { get; set; }
        public int VendorID { get; set; }
        public int PurchaseOrderTypeLUID { get; set; }
        public int OrganizationID { get; set; }
        public DateTime DateOrdered { get; set; }
        public int OrderedByID { get; set; }
        public DateTime DateReceived { get; set; }
        public DateTime DateClosed { get; set; }
        public int StatusLUID { get; set; }
        public string ShipToName { get; set; }
        public string ShipToAttn { get; set; }
        public Helpers.Address ShipToAddress { get; set; }
        public Decimal ShippingCharge { get; set; }
        public Decimal OtherCharge { get; set; }
        public string CreatedByUserID { get; set; }
        public string ModifiedByUserID { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }
        public int DefaultPaymentMethodID { get; set; }
        public bool Active { get; set; }
        public string Terms { get; set; }
        public bool HasBeenBilled { get; set; }

        [XmlIgnore]
        public List<PurchaseOrderItem> Items
        {
            get
            {
                if (mItems == null)
                   mItems = GetItems();

                return mItems;
            }
            set
            {
                mItems = value;
            }
        }

        [XmlIgnore]
        public List<Receipt> Receipts
        {
            get
            {
                if (mReceipts == null)
                    mReceipts = GetReceipts();
                return mReceipts;
            }
            set
            {
                mReceipts = value;
            }
        }

        [XmlIgnore]
        public DefinedValue PurchaseOrderType
        {
            get
            {
                if ((mPurchaseOrderType == null || mPurchaseOrderType.Id != PurchaseOrderTypeLUID) && PurchaseOrderTypeLUID > 0)
                    mPurchaseOrderType = definedValueService.Get(PurchaseOrderTypeLUID);
                return mPurchaseOrderType;
            }
        }

        [XmlIgnore]
        public DefinedValue Status
        {
            get
            {
                if ((mStatus == null || mStatus.Id != StatusLUID) && StatusLUID > 0)
                    mStatus = definedValueService.Get(StatusLUID);

                return mStatus;
            }
        }


        [XmlIgnore]
        public Vendor Vendor
        {
            get
            {
                if ((mVendor == null || mVendor.VendorID != VendorID) && VendorID > 0)
                    mVendor = new Vendor(VendorID);
                return mVendor;                
            }
        }

        [XmlIgnore]
        public Person OrderedBy
        {
            get
            {
                if ((mOrderedBy == null || mOrderedBy.Id != OrderedByID) && OrderedByID > 0)
                    mOrderedBy = personAliasService.Get(OrderedByID).Person;

                return mOrderedBy;                    
            }
        }

        [XmlIgnore]
        public Person CreatedBy
        {
            get
            {
                if(mCreatedBy == null && !String.IsNullOrEmpty(CreatedByUserID))
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
        public List<Payment> Payments
        {
            get
            {
                if (mPayments == null)
                    mPayments = GetPayments();
                return mPayments;
            }
            set
            {
                mPayments = value;
            }
        }

        [XmlIgnore]
        public List<Note> Notes
        {
            get
            {
                if (mNotes == null)
                    mNotes = GetNotes();

                return mNotes;
            }
            set
            {
                mNotes = value;
            }
        }

        [XmlIgnore]
        public List<Attachment> Attachments
        {
            get
            {
                if (mAttachment == null)
                    mAttachment = GetAttachments();
                return mAttachment;
            }
            set
            {
                mAttachment = value;
            }
        }

        [XmlIgnore]
        public PaymentMethod DefaultPaymentMethod
        {
            get
            {
                if (mDefaultPaymentMethod == null || mDefaultPaymentMethod.PaymentMethodID != DefaultPaymentMethodID)
                {
                    mDefaultPaymentMethod = new PaymentMethod(DefaultPaymentMethodID);
                }
                return mDefaultPaymentMethod;
            }
        }

        #endregion

        #region Constructors
        public PurchaseOrder()
        {
            Init();
        }

        public PurchaseOrder(int poID)
        {
            Load(poID);
        }

        public PurchaseOrder(PurchaseOrderData data)
        {
            Load(data);
        }
        
        #endregion

        #region Public

        public static List<DefinedValue> GetPurchaseOrderStatuses(bool activeOnly)
        {
            List<DefinedValue> StatusList = new List<DefinedValue>();

            if (activeOnly)
                StatusList.AddRange(definedTypeService.Get(PurchaseOrderStatusGuid).DefinedValues.Where(l => l.IsValid));
            else
                StatusList.AddRange(definedTypeService.Get(PurchaseOrderStatusGuid).DefinedValues);

            return StatusList;
        }

        public static List<DefinedValue> GetPurchaseOrderTypes(bool activeOnly)
        {
            List<DefinedValue> POTypes = new List<DefinedValue>();

            if (activeOnly)
                POTypes.AddRange(definedTypeService.Get(PurchaseOrderTypeGuid).DefinedValues.Where(l => l.IsValid));
            else
                POTypes.AddRange(definedTypeService.Get(PurchaseOrderTypeGuid).DefinedValues);

            return POTypes;
        }

        public static int PurchaseOrderOrderedStatusLUID()
        {
            return definedValueService.Get(PurchaseOrderStatusOrderedGuid).Id;
        }

        public static int PurchaseOrderStatusPartiallyReceivedLUID()
        {
            return definedValueService.Get(PurchaseOrderStatusPartiallyReceived).Id;
        }

        public static int PurchaseOrderStatusReceivedLUID()
        {
            return definedValueService.Get(PurchaseOrderStatusReceived).Id;
        }

        public static int PurchaseOrderStatusBilledLUID()
        {
            return definedValueService.Get(PurchaseOrderStatusBilled).Id;
        }

        public static int PurchaseOrderStatusClosedLUID()
        {
            return definedValueService.Get(PurchaseOrderStatusClosed).Id;
        }

        public static int PurchaseOrderStatusReopenedLUID()
        {
            return definedValueService.Get(PurchaseOrderStatusReopened).Id;
        }

        public static int PurchaseOrderStatusOpenLUID()
        {
            return definedValueService.Get(PurchaseOrderStatusOpen).Id;
        }

        public static int PurchaseOrderStatusCancelledLUID()
        {
            return definedValueService.Get(PurchaseOrderStatusCancelled).Id;
        }

        public static List<PurchaseOrderListItem> GetPurchaseOrderList(Dictionary<string,string> filter)
        {
            List<PurchaseOrderListItem> ListItems = new List<PurchaseOrderListItem>();

            using (PurchasingContext Context = ContextHelper.GetDBContext())
            {

                var Query = Context.PurchaseOrderDatas
                    .GroupJoin( Context.PersonAliasDatas,
                        po => po.ordered_by,
                        pm1 => ( Int32? ) ( pm1.Id ),
                        ( po, pm2 ) => new
                        {
                            po = po,
                            pm2 = pm2
                        } )
                    .SelectMany( temp0 => temp0.pm2.DefaultIfEmpty(),
                        ( temp0, LOJpm ) => new
                        {
                            temp0 = temp0,
                            LOJpm = LOJpm
                        } )
                    .Join( Context.LookupDatas,
                        temp1 => temp1.temp0.po.status_luid,
                        s => s.Id,
                        ( temp1, s ) => new
                        {
                            temp1 = temp1,
                            s = s
                        } )
                    .Join( Context.LookupDatas,
                        temp2 => temp2.temp1.temp0.po.purchase_order_type_luid,
                        t => t.Id,
                        ( temp2, t ) => new
                        {
                            PurchaseOrderID = temp2.temp1.temp0.po.purchase_order_id,
                            VendorID = temp2.temp1.temp0.po.vendor_id,
                            VendorName = temp2.temp1.temp0.po.VendorData.vendor_name,
                            TypeLUID = temp2.temp1.temp0.po.purchase_order_type_luid,
                            TypeName = t.Value,
                            StatusLUID = temp2.temp1.temp0.po.status_luid,
                            StatusName = temp2.s.Value,
                            OrderDate = temp2.temp1.temp0.po.date_ordered,
                            Active = temp2.temp1.temp0.po.active,
                            OrderedByID = temp2.temp1.temp0.po.ordered_by,
                            ItemDetailsCount = temp2.temp1.temp0.po.PurchaseOrderItemDatas
                                .Where( poi => ( poi.active == true ) )
                                .Count(),
                            PaymentTotal = ( Decimal? ) ( temp2.temp1.temp0.po.PaymentDatas.Where( pay => ( pay.active == true ) )
                                .Select( pay => pay.payment_amount )
                                .Sum() ) ?? 0,
                            PaymentMethods = temp2.temp1.temp0.po.PaymentDatas.Where( pay => ( pay.active == true ) ).ToList(),
                            NoteCount = Context.NoteDatas
                                .Where( n => ( ( ( n.active == true ) && ( n.object_type_name == typeof( PurchaseOrder ).ToString() ) ) &&
                                     ( n.identifier == temp2.temp1.temp0.po.purchase_order_id ) ) )
                                    .Count(),
                            AttachmentCount = Context.AttachmentDatas
                                .Where( a => ( ( ( a.active == true ) && ( a.parent_object_type_name == typeof( PurchaseOrder ).ToString() ) ) &&
                                     ( a.parent_identifier == temp2.temp1.temp0.po.purchase_order_id ) ) )
                                .Count(),
                            Items = temp2.temp1.temp0.po.PurchaseOrderItemDatas.ToList()
                        } );



                if (filter.ContainsKey("StatusLUID"))
                {
                    List<int> StatusLUIDs = filter["StatusLUID"].Split(",".ToCharArray()).Select(x => int.Parse(x)).ToList();
                    Query = Query.Where(p => StatusLUIDs.Contains(p.StatusLUID));
                }

                if (filter.ContainsKey("TypeLUID"))
                {
                    List<int> TypeLUIDs = filter["TypeLUID"].Split(",".ToCharArray()).Select(x => int.Parse(x)).ToList();
                    Query = Query.Where(p => TypeLUIDs.Contains(p.TypeLUID));
                }

                int POID = 0;
                if (filter.ContainsKey("PONumber") && int.TryParse(filter["PONumber"], out POID))
                {
                    Query = Query.Where(p => p.PurchaseOrderID == POID);
                }

                int vid = 0;
                if (filter.ContainsKey("VendorID") && int.TryParse(filter["VendorID"], out vid))
                    Query = Query.Where(p => p.VendorID == vid);

                DateTime OrderedOnStart;
                if (filter.ContainsKey("OrderedOnStart") && DateTime.TryParse(filter["OrderedOnStart"], out OrderedOnStart))
                    Query = Query.Where(p => p.OrderDate >= OrderedOnStart);

                DateTime OrderedOnEnd;
                if (filter.ContainsKey("OrderedOnEnd") && DateTime.TryParse(filter["OrderedOnEnd"], out OrderedOnEnd))
                    Query = Query.Where(p => p.OrderDate <= OrderedOnEnd);

                DateTime PaymentStart;
                if ( filter.ContainsKey( "PaymentStart" ) && DateTime.TryParse( filter["PaymentStart"], out PaymentStart ) )
                    Query = Query.Where( p => p.PaymentMethods.Where(pm => pm.payment_date >= PaymentStart).Any() );

                DateTime PaymentEnd;
                if ( filter.ContainsKey( "PaymentEnd" ) && DateTime.TryParse( filter["PaymentEnd"], out PaymentEnd ) )
                    Query = Query.Where( p => p.PaymentMethods.Where( pm => pm.payment_date <= PaymentEnd ).Any() );
                
                int pmid = 0;
                if ( filter.ContainsKey( "PaymentMethodID" ) && int.TryParse( filter["PaymentMethodID"], out pmid ) )
                    Query = Query.Where( p => p.PaymentMethods.Where(pm => pm.payment_method_id == pmid ).Any() );

                int obID = 0;
                if (filter.ContainsKey("OrderedByID") && int.TryParse(filter["OrderedByID"], out obID))
                    Query = Query.Where(p => p.OrderedByID == obID);

                bool showInactive = false;
                if (!filter.ContainsKey("ShowInactive") || !bool.TryParse(filter["ShowInactive"], out showInactive) || !showInactive)
                    Query = Query.Where(p => p.Active);


                if ( filter.ContainsKey( "GLAccount" ) && !String.IsNullOrWhiteSpace( filter["GLAccount"] ) )
                {
                    string[] projAccount = filter["GLAccount"].Split( "-".ToCharArray() );
                    int fundId = 0;
                    int deptId = 0;
                    int acctId = 0;

                    int.TryParse( projAccount[0], out fundId );
                    int.TryParse( projAccount[1], out deptId );
                    int.TryParse( projAccount[2], out acctId );

                    Query = Query.Where( q => q.Items.Where(i => i.RequisitionItemData.fund_id == fundId ).Any() )
                                .Where( q => q.Items.Where( i => i.RequisitionItemData.department_id == deptId ).Any() )
                                .Where( q => q.Items.Where( i => i.RequisitionItemData.account_id == acctId ).Any() );
                }

                var command = Context.GetCommand(Query.Select( p => new PurchaseOrderListItem
                {
                    PurchaseOrderID = p.PurchaseOrderID,
                    VendorID = p.VendorID,
                    VendorName = p.VendorName,
                    POTypeLUID = p.TypeLUID,
                    POType = p.TypeName,
                    StatusLUID = p.StatusLUID,
                    Status = p.StatusName,
                    ItemDetailCount = p.ItemDetailsCount,
                    TotalPayments = p.PaymentTotal,
                    NoteCount = p.NoteCount,
                    AttachmentCount = p.AttachmentCount
                } ).AsQueryable());
                
                SqlParameter[] parameters = new SqlParameter[] { };
                Array.Resize( ref parameters, command.Parameters.Count );
                command.Parameters.CopyTo( parameters, 0 );

                ListItems.AddRange( Context.ExecuteQuery<PurchaseOrderListItem>( command.CommandText + " OPTION (RECOMPILE)", parameters.Select( p => p.Value ).ToArray() ) );
            }

            return ListItems;
        }

        public int AddPayment(int paymentMethod, DateTime paymentDate, decimal paymentAmount, string uid)
        {
            Payment p = new Payment();
            p.PaymentMethodID = paymentMethod;
            p.PurchaseOrderID = PurchaseOrderID;
            p.PaymentDate = paymentDate;
            p.PaymentAmount = paymentAmount;

            p.Save(uid);
            RefreshPayments();

            return p.PaymentID;
           
        }

        public bool HasChanged()
        {
            bool IsDifferent = false;
            PurchaseOrder Original = new PurchaseOrder(PurchaseOrderID);

            if (VendorID != Original.VendorID)
                IsDifferent = true;

            if (!IsDifferent && PurchaseOrderTypeLUID != Original.PurchaseOrderTypeLUID)
                IsDifferent = true;

            if (!IsDifferent && OrganizationID != Original.OrganizationID)
                IsDifferent = true;

            if (!IsDifferent && DateOrdered != Original.DateOrdered)
                IsDifferent = true;

            if (!IsDifferent && OrderedByID != Original.OrderedByID)
                IsDifferent = true;

            if (!IsDifferent && StatusLUID != Original.StatusLUID)
                IsDifferent = true;

            if (!IsDifferent && DateReceived != Original.DateReceived)
                IsDifferent = true;

            if (!IsDifferent && DateClosed != Original.DateClosed)
                IsDifferent = true;

            if (!IsDifferent && ShipToAddress != Original.ShipToAddress)
                IsDifferent = true;

            if (!IsDifferent && ShipToName != Original.ShipToName)
                IsDifferent = true;

            if (!IsDifferent && ShipToAttn != Original.ShipToAttn)
                IsDifferent = true;

            if (!IsDifferent && ShippingCharge != Original.ShippingCharge)
                IsDifferent = true;

            if (!IsDifferent && OtherCharge != Original.OtherCharge)
                IsDifferent = true;

            if (!IsDifferent && DefaultPaymentMethodID != Original.DefaultPaymentMethodID)
                IsDifferent = true;

            if (!IsDifferent && Terms != Original.Terms)
                IsDifferent = true;

            return IsDifferent;
        }

        public bool HasBeenReceived(out bool isPartiallyReceived, string uid)
        {
            isPartiallyReceived = false;
            bool isFullyReceived = true;

            foreach (PurchaseOrderItem poItem in Items.Where(i => i.Active))
            {
                int QtyReceived = poItem.ReceiptItems.Where(x => x.Active).Select(x => (int?)x.QuantityReceived ?? 0).Sum();
                bool originalItemRecievedStatus = poItem.HasBeenReceived;

                if (QtyReceived == 0)
                {
                    isFullyReceived = false;
                    poItem.HasBeenReceived = false;
                }
                else if (QtyReceived != 0 && poItem.Quantity > QtyReceived)
                {
                    isFullyReceived = false;
                    isPartiallyReceived = true;
                    poItem.HasBeenReceived = false;
                }
                else
                {
                    isPartiallyReceived = true;
                    poItem.HasBeenReceived = true;
                }

                if (originalItemRecievedStatus != poItem.HasBeenReceived)
                {
                    poItem.Save(uid);
                }

            }

            RefreshItems();
            return isFullyReceived;
        }

        public bool IsClosed()
        {
            if (Status == null)
                return false;
            Status.LoadAttributes();
            return Status.GetAttributeValue("IsClosed").AsBoolean();
        }

        public static List<PurchaseOrder> LoadOpenPOs()
        {

            using ( PurchasingContext Context = ContextHelper.GetDBContext() )
            {
                return Context.PurchaseOrderDatas
                        .Where( p => p.status_luid == PurchaseOrderStatusOpenLUID() )
                        .Where( p => p.active )
                        .Select( p => new PurchaseOrder( p ) )
                        .ToList();
            }
        }

        public static List<PurchaseOrder> LoadByVendor(int vendorId)
        {

            using (PurchasingContext Context = ContextHelper.GetDBContext())
            {
                return Context.PurchaseOrderDatas
                        .Where(p => p.vendor_id == vendorId)
                        .Select(p => new PurchaseOrder(p))
                        .ToList();
            }
        }

        public static List<PurchaseOrder> LoadByStatus(int[] POStatuses)
        {
            using ( PurchasingContext Context = ContextHelper.GetDBContext() )
            {
                return Context.PurchaseOrderDatas
                        .Where( po => POStatuses.Contains( po.status_luid ) )
                        .Where( po => po.active )
                        .Select( po => new PurchaseOrder( po ) )
                        .ToList();
            }
        }

        public void MarkAsBilled(string uid)
        {
            StatusLUID = PurchaseOrderStatusBilledLUID();
            HasBeenBilled = true;
            Save(uid);
        }

        public void Reopen(string uid)
        {
            Status.LoadAttributes();

            if (Status.GetAttributeValue("IsClosed").AsBoolean())
            {
                StatusLUID = PurchaseOrderStatusReopenedLUID();
                DateClosed = DateTime.MinValue;
                Save(uid);
            }
        }

        public void Close(string uid)
        {
            //if (DateReceived > DateTime.MinValue && Payments.Count > 0 && (StatusLUID == PurchaseOrderStatusBilledLUID() || StatusLUID == PurchaseOrderStatusReopenedLUID()))
            //{
            //    StatusLUID = PurchaseOrderStatusClosedLUID();
            //    DateClosed = DateTime.Now;
            //    Save(uid);
            //}
            //else
            //    throw new RequisitionException("Can not close purchase order.");

            try
            {
                decimal TotalPayments = Payments.Where(x => x.Active).Select(x => x.PaymentAmount).Sum();
                decimal TotalCharges = Payments.Where(x => x.Active).Select(x => (decimal?)x.Charges.Where(c => c.Active).Select(c => (decimal?)c.Amount ?? 0).Sum() ?? 0).Sum();
                Dictionary<string, string> CloseErrors = new Dictionary<string, string>();
                if (DateReceived == DateTime.MinValue)
                    CloseErrors.Add("Date Received", "Purchase Order has not been received");
                if (StatusLUID != PurchaseOrderStatusBilledLUID() && StatusLUID != PurchaseOrderStatusReopenedLUID())
                    CloseErrors.Add("Status", "Purchase Order must have been billed.");
                if (Payments.Where(x => x.Active).Select(x => x.PaymentAmount).Sum() < GetTotal())
                    CloseErrors.Add("Payments", "Payments are less than total.");
                if (TotalPayments > TotalCharges)
                    CloseErrors.Add("Unapplied Payments Exists", "Payments that have not been fully applied to requisitions exist.");
                if (CloseErrors.Count > 0)
                    throw new RequisitionNotValidException("Could not close purchase order", CloseErrors);

                StatusLUID = PurchaseOrderStatusClosedLUID();
                DateClosed = DateTime.Now;
                Save(uid);
            }
            catch (Exception ex)
            {

                throw new RequisitionException("An error has occurred while closing purchase order.", ex); 
            }
                
        }

        public void Cancel( string uid )
        {
            try
            {
                Dictionary<string, string> cancelErrors = new Dictionary<string, string>();

                if ( Status.Order >= definedValueService.Get( PurchaseOrderStatusPartiallyReceived ).Order )
                {
                    cancelErrors.Add( "Status", "A purchase order can only be canceled before any items are received." );
                }

                if ( !Active )
                {
                    cancelErrors.Add( "Active", "A purchase order must be active to be canceled." );
                }

                if ( cancelErrors.Count > 0 )
                {
                    throw new RequisitionNotValidException( "Could not close purchase order.", cancelErrors );
                }

                StatusLUID = PurchaseOrderStatusCancelledLUID();
                DateClosed = DateTime.Now;
                Save( uid );
            }
            catch ( Exception ex )
            {
                throw new RequisitionException( "An error has occurred while canceling purchase order.", ex );
            }
        }

        public void RefreshItems()
        {
            Items = GetItems();
        }

        public void ReceivePackage(int receivedByID, int carrierLUID, string dateReceivedStr, Dictionary<int, int> itemsToReceive, string uid)
        {
            Receipt r = new Receipt();
            r.PurchaseOrderID = PurchaseOrderID;
            r.CarrierLUID = carrierLUID;
            r.ReceivedByID = receivedByID;
            DateTime tempDateReceived;
            DateTime.TryParse(dateReceivedStr, out tempDateReceived);
            r.DateReceived = tempDateReceived;
            r.Active = true;

            r.Save(uid);

            foreach (var item in itemsToReceive)
            {
                ReceiptItem ri = new ReceiptItem();
                ri.ReceiptID = r.ReceiptID;
                ri.POItemID = item.Key;
                ri.QuantityReceived = item.Value;
                ri.Active = true;
                ri.Save(uid);
            }

            RefreshReceipts();
            bool isPartiallyReceived = false;
            bool isFullyReceived = HasBeenReceived(out isPartiallyReceived, uid);

            if (isFullyReceived)
            {
                StatusLUID = PurchaseOrderStatusReceivedLUID();
                DateReceived = DateTime.Now;
            }
            else if (isPartiallyReceived)
            {
                StatusLUID = PurchaseOrderStatusPartiallyReceivedLUID();
            }

            Save(uid);
        }

        public void RefreshPayments()
        {
            Payments = GetPayments();
        }

        public void RefreshReceipts()
        {
            Receipts = GetReceipts();
        }

        public void RefreshNotes()
        {
            Notes = GetNotes();
        }

        public void RefreshAttachments()
        {
            Attachments = GetAttachments();
        }

        public bool RemoveItem(int poItemID, string uid)
        {
            bool HasBeenRemoved = false;
            PurchaseOrderItem POItem = Items.FirstOrDefault(x => x.PurchaseOrderItemID == poItemID);

            Status.LoadAttributes();
            if (!Status.GetAttributeValue("IsClosed").AsBoolean() && POItem != null && POItem.ItemID > 0)
            {
                POItem.Active = false;
                POItem.Save(uid);
                POItem.RequisitionItem.Requisition.SyncStatus(uid);

                RefreshItems();
                HasBeenRemoved = true;
            }

            return HasBeenRemoved;
        }

        public bool RemovePayment(int paymentID, string uid)
        {
            bool HasBeenRemoved = false;
            Payment p = Payments.FirstOrDefault(x => x.PaymentID == paymentID);

            //PO is not closed.
            Status.LoadAttributes();
            if (!Status.GetAttributeValue("IsClosed").AsBoolean() && p.PaymentID > 0)
            {
                p.DeactivateCharges(uid);
                p.Active = false;
                p.Save(uid);
                RefreshPayments();
                HasBeenRemoved = true;

                HasBeenBilled = false;
                bool isPartiallyReceived = false;

                if (HasBeenReceived(out isPartiallyReceived, uid))
                {
                    StatusLUID = PurchaseOrderStatusReceivedLUID();
                }
                else
                {
                    if (isPartiallyReceived)
                        StatusLUID = PurchaseOrderStatusPartiallyReceivedLUID();
                    else
                        StatusLUID = PurchaseOrderOrderedStatusLUID();

                    DateReceived = DateTime.MinValue;
                }

                Save(uid);

            }

            return HasBeenRemoved;
        }

        public bool RemoveReceipt(int receiptId, string uid)
        {
            bool HasBeenRemoved = false;
            Receipt receipt = Receipts.FirstOrDefault(r => r.ReceiptID == receiptId);

            if (!IsClosed())
            {
                foreach (ReceiptItem ri in receipt.ReceiptItems)
                {
                    ri.Active = false;
                    ri.Save(uid);
                }

                receipt.Active = false;
                receipt.Save(uid);
                RefreshReceipts();

                bool isPartiallyReceived = false;

                HasBeenReceived(out isPartiallyReceived, uid);
                DateReceived = DateTime.MinValue;

                if (isPartiallyReceived)
                {
                    StatusLUID = PurchaseOrderStatusPartiallyReceivedLUID();
                }
                else
                {
                    StatusLUID = PurchaseOrderOrderedStatusLUID();
                }

                Save(uid);
                
                HasBeenRemoved = true;
            }

            return HasBeenRemoved;
        }

        public void Save(string uid)
        {
            try
            {
                if (string.IsNullOrEmpty(uid))
                    throw new ArgumentNullException("UID", "User Name is required.");
                Dictionary<string, string> ValErrors = Validate();
                if (ValErrors.Count > 0)
                    throw new RequisitionNotValidException("Purchase Order is not valid.", ValErrors);

                using (PurchasingContext Context = ContextHelper.GetDBContext())
                {
                    Enums.HistoryType ChangeType;
                    PurchaseOrder Original = null;

                    PurchaseOrderData data = null;
                    if (PurchaseOrderID > 0)
                    {
                        ChangeType = Enums.HistoryType.UPDATE;
                        data = Context.PurchaseOrderDatas.FirstOrDefault(x => x.purchase_order_id == PurchaseOrderID);
                        Original = new PurchaseOrder(data);
                    }
                    else
                    {
                        ChangeType = Enums.HistoryType.ADD;
                        data = new PurchaseOrderData();
                        data.created_by = uid;
                        data.date_created = DateTime.Now;
                    }

                    data.vendor_id = VendorID;
                    data.purchase_order_type_luid = PurchaseOrderTypeLUID;
                    data.organization_id = OrganizationID;

                    if (DateOrdered > DateTime.MinValue)
                        data.date_ordered = DateOrdered;
                    else
                        data.date_ordered = null;

                    if (OrderedByID > 0)
                        data.ordered_by = OrderedByID;
                    else
                        data.ordered_by = null;

                    data.status_luid = StatusLUID;

                    if (DateReceived > DateTime.MinValue)
                        data.date_received = DateReceived;
                    else
                        data.date_received = null;

                    if (DateClosed > DateTime.MinValue)
                        data.date_closed = DateClosed;
                    else
                        data.date_closed = null;

                    if (!String.IsNullOrEmpty(ShipToName))
                        data.ship_to_name = ShipToName;
                    else
                        data.ship_to_name = null;

                    if (!String.IsNullOrEmpty(ShipToAttn))
                        data.ship_to_attention = ShipToAttn;
                    else
                        data.ship_to_attention = null;

                    if (ShipToAddress != null && ShipToAddress.IsValid())
                        data.ship_to_address = ShipToAddress.ToArenaFormat();
                    else
                        data.ship_to_address = null;

                    if (ShippingCharge != 0)
                        data.shipping_charge = ShippingCharge;
                    else
                        data.shipping_charge = null;

                    if (OtherCharge != 0)
                        data.other_charge = OtherCharge;
                    else
                        data.other_charge = null;

                    if (DefaultPaymentMethodID > 0)
                        data.default_payment_method_id = DefaultPaymentMethodID;
                    else
                        data.default_payment_method_id = null;

                    data.modified_by = uid;
                    data.date_modified = DateTime.Now;
                    data.active = Active;

                    if (!String.IsNullOrEmpty(Terms))
                        data.terms = Terms.Trim();
                    else
                        data.terms = null;

                    data.has_been_billed = HasBeenBilled;

                    if (PurchaseOrderID <= 0)
                        Context.PurchaseOrderDatas.InsertOnSubmit(data);

                    Context.SubmitChanges();

                    Load(data);
                    SaveHistory(ChangeType, Original, uid);
                }
            }
            catch (Exception ex)
            {
                throw new RequisitionException("An error has occurred while saving purchase order.", ex);
            }
        }

        public void SaveHistory(Enums.HistoryType ht, PurchaseOrder original, string uid)
        {
            History h = new History();

            h.ObjectTypeName = this.GetType().ToString();
            h.Identifier = PurchaseOrderID;
            h.ChangeType = ht;
            h.OrganizationID = OrganizationID;

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
            h.Save(uid);
        }

        public void SaveNote(string noteText, string uid)
        {
            Note n = new Note();
            n.ObjectTypeName = this.GetType().ToString();
            n.Identifier = PurchaseOrderID;
            n.Body = string.Format("Order submitted: {0}",noteText);
            n.Active = true;
            n.Save(uid);
        }

        public void SubmitOrder(DateTime orderDate, int orderedBy, string uid)
        {
            DateOrdered = orderDate;
            OrderedByID = orderedBy;
            StatusLUID = PurchaseOrderOrderedStatusLUID();
            Save(uid);
        }

        public void UpdatePOItems(List<ItemToAddToPO> ItemList, string uid)
        {
            foreach (var item in ItemList)
            {
                PurchaseOrderItem i = Items.FirstOrDefault(x => x.ItemID == item.RequisitionItemID && x.Price == item.ItemPrice && x.Active);

                if (i == null)
                    i = new PurchaseOrderItem();

                i.PurchaseOrderID = PurchaseOrderID;
                i.Quantity += item.QuantityToAdd;
                i.ItemID = item.RequisitionItemID;
                i.Price = item.ItemPrice;
                i.HasBeenReceived = false;

                i.Save(uid);
            }

            RefreshItems();
        }

        #endregion

        #region Private

        private List<Attachment> GetAttachments()
        {
            List<Attachment> AttachmentList = new List<Attachment>();

            if (PurchaseOrderID > 0)
                AttachmentList.AddRange(Attachment.GetObjectAttachments(this.GetType().ToString(), PurchaseOrderID, true));

            return AttachmentList;
        }

        private List<PurchaseOrderItem> GetItems()
        {
            List<PurchaseOrderItem> Items = new List<PurchaseOrderItem>();
            if (PurchaseOrderID <= 0)
                return Items;

            using (PurchasingContext Context = ContextHelper.GetDBContext())
            {
                foreach (var item in Context.PurchaseOrderDatas.Where(x=> x.purchase_order_id == PurchaseOrderID).FirstOrDefault().PurchaseOrderItemDatas.ToList())
                {
                    Items.Add(new PurchaseOrderItem(item));
                }
            }

            return Items;
        }

        private List<Note> GetNotes()
        {
            List<Note> NoteList = new List<Note>();

            if (PurchaseOrderID > 0)
            {
                NoteList.AddRange(Note.GetNotes(this.GetType().ToString(), PurchaseOrderID, true));
            }
            return NoteList;
        }

        private List<Payment> GetPayments()
        {
            List<Payment> PaymentList = new List<Payment>();

            using (PurchasingContext Context = ContextHelper.GetDBContext())
            {
                foreach (PaymentData p in Context.PurchaseOrderDatas.FirstOrDefault(x => x.purchase_order_id == PurchaseOrderID).PaymentDatas)
                {
                    PaymentList.Add(new Payment(p));
                }
            }

            return PaymentList;
        }

        private List<Receipt> GetReceipts()
        {
            List<Receipt> Receipts = new List<Receipt>();

            using (PurchasingContext Context = ContextHelper.GetDBContext())
            {
                foreach (var item in Context.PurchaseOrderDatas.FirstOrDefault(x => x.purchase_order_id == PurchaseOrderID).ReceiptDatas)
                {
                    Receipts.Add(new Receipt(item));
                }
            }

            return Receipts;
        }

        private decimal GetTotal()
        {
            decimal SubTotal = Items.Where(x => x.Active).Select(x => x.Price * x.Quantity).Sum();

            return SubTotal + ShippingCharge + OtherCharge;
        }

        private void Init()
        {
            PurchaseOrderID = 0;
            VendorID = 0;
            PurchaseOrderTypeLUID = 0;
            OrganizationID = 1;
            DateOrdered = DateTime.MinValue;
            OrderedByID = 0;
            DateReceived = DateTime.MinValue;
            DateClosed = DateTime.MinValue;
            StatusLUID = 0;
            ShipToName = String.Empty;
            ShipToAttn = String.Empty;
            ShipToAddress = null;
            ShippingCharge = 0;
            OtherCharge = 0;
            CreatedByUserID = String.Empty;
            ModifiedByUserID = String.Empty;
            DateCreated = DateTime.MinValue;
            DateModified = DateTime.MinValue;
            DefaultPaymentMethodID = 0;
            Terms = String.Empty;
            Active = true;
            HasBeenBilled = false;
              
            mVendor = null;
            mCreatedBy = null;
            mModifiedBy = null;
            mPurchaseOrderType = null;
            mStatus = null;
            mOrderedBy = null;
            mReceipts = null;
            mDefaultPaymentMethod = null;

        }

        private void Load(PurchaseOrderData data)
        {
            if (data != null)
            {
                Init();
                PurchaseOrderID = data.purchase_order_id;
                VendorID = data.vendor_id;
                PurchaseOrderTypeLUID = data.purchase_order_type_luid;
                OrganizationID = data.organization_id;
                CreatedByUserID = data.created_by;
                ModifiedByUserID = data.modified_by;
                DateCreated = data.date_created;
                DateModified = data.date_modified;
                Active = data.active;
                if (data.date_ordered != null)
                    DateOrdered = (DateTime)data.date_ordered;
                if (data.ordered_by != null)
                    OrderedByID = (int)data.ordered_by;
                StatusLUID = data.status_luid;

                if (data.ship_to_name != null)
                    ShipToName = data.ship_to_name;

                if (data.ship_to_attention != null)
                    ShipToAttn = data.ship_to_attention;

                if (data.ship_to_address != null)
                {
                    ShipToAddress = new Helpers.Address(data.ship_to_address);
                }

                if (data.date_received != null)
                    DateReceived = (DateTime)data.date_received;
                if (data.date_closed != null)
                    DateClosed = (DateTime)data.date_closed;

                if (data.shipping_charge != null)
                    ShippingCharge = (decimal)data.shipping_charge;
                if (data.other_charge != null)
                    OtherCharge = (decimal)data.other_charge;
                HasBeenBilled = data.has_been_billed;

                Terms = data.terms;

                if (data.default_payment_method_id != null)
                {
                    DefaultPaymentMethodID = (int)data.default_payment_method_id;
                }
            }
        }

        private void Load(int poId)
        {
            using (PurchasingContext Context = ContextHelper.GetDBContext())
            {
                Load( Context.PurchaseOrderDatas.FirstOrDefault( po => po.purchase_order_id == poId ) );
            }
        }

        private Dictionary<string, string> Validate()
        {
            Dictionary<string, string> ValErrors = new Dictionary<string, string>();

            if (VendorID <= 0)
                ValErrors.Add("Vendor", "Vendor selection is required.");
            if (OrganizationID <= 0)
                ValErrors.Add("Organization ID", "Organization ID is required.");
            if (StatusLUID <= 0 || GetPurchaseOrderStatuses(true).Where(x => x.Id == StatusLUID) == null)
                ValErrors.Add("Status", "Status is not vaild.");
            if (PurchaseOrderTypeLUID <= 0 || GetPurchaseOrderTypes(true).Where(x => x.Id == PurchaseOrderTypeLUID) == null)
                ValErrors.Add("Purchase Order Type", "Purchase Order Type is required.");

            if (ShipToAddress != null && !ShipToAddress.IsValid())
                ValErrors.Add("Ship To Address", "Ship To Address is not valid.");

            if (DefaultPaymentMethodID > 0 && DefaultPaymentMethod.PaymentMethodID <= 0)
                ValErrors.Add("Payment Method", "Default Payment Method is invalid.");

            return ValErrors;
          
        }

        #endregion
    }

    public class PurchaseOrderListItem
    {
        public int PurchaseOrderID { get; set; }
        public int VendorID { get; set; }
        public string VendorName { get; set; }
        public int POTypeLUID { get; set; }
        public string POType { get; set; }
        public int StatusLUID { get; set; }
        public string Status { get; set; }
        public int ItemDetailCount { get; set; }
        public decimal TotalPayments { get; set; }
        public int NoteCount { get; set; }
        public int AttachmentCount { get; set; }
    }

}
