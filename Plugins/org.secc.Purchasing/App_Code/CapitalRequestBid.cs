using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;


using org.secc.Purchasing.DataLayer;
using Rock.Model;

namespace org.secc.Purchasing
{
    public class CapitalRequestBid : PurchasingBase
    {

        #region Fields
        private static string DocumentTypeGuidValue = "5070FDCF-8A68-4139-B07D-CB5E82CE1114";

        private CapitalRequest mCapitalRequest;
        private Vendor mVendor;
        private BinaryFile mQuoteBlob;
        private Person mCreatedBy;
        private Person mModifiedBy;
        #endregion


        #region Properties
        public int BidID { get; set; }
        public int CapitalRequestID { get; set; }
        public int VendorID { get; set; }
        public string VendorName { get; set; }
        public string VendorContactName { get; set; }
        public Helpers.PhoneNumber VendorContactPhone { get; set; }
        public string VendorContactEmail { get; set; }
        public int QuoteBlobID { get; set; }
        public decimal BidAmount { get; set; }
        public bool IsPreferredBid { get; set; }
        public string CreatedByUser { get; set; }
        public string ModifiedByUser { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }
        public bool Active { get; set; }

        [XmlIgnore]
        public string VendorContact
        {
            get
            {
                if ( string.IsNullOrWhiteSpace( VendorContactName ) && VendorContactPhone == null  && string.IsNullOrWhiteSpace( VendorContactEmail ) )
                {
                    return "(not provided)";
                }
                StringBuilder sb = new StringBuilder();
                if ( !string.IsNullOrWhiteSpace( VendorContactName ) )
                {
                    sb.AppendFormat( "<div id=\"vendorContactName\">{0}</div>", VendorContactName );
                }
                if ( !string.IsNullOrWhiteSpace( VendorContactEmail ) )
                {
                    sb.AppendFormat( "<div class=\"vendorContactEmail\">{0}</div>", VendorContactEmail );
                }
                if ( VendorContactPhone != null )
                {
                    sb.AppendFormat( "<div class=\"vendorContactPhone\">{0}</div>", VendorContactPhone.FormatNumber( true ) );
                }
                return sb.ToString();
            }
        }

        [XmlIgnore]
        public string VendorNameValue
        {
            get
            {
                string vendorName = string.Empty;
                if ( VendorID > 0 )
                {
                    vendorName = Vendor.VendorName;
                }
                else
                {
                    vendorName = VendorName;
                }

                return vendorName;
            }
        }

        [XmlIgnore]
        public CapitalRequest CapitalRequest
        {
            get
            {
                if ( ( mCapitalRequest == null && CapitalRequestID > 0 ) || ( mCapitalRequest != null && mCapitalRequest.CapitalRequestId != CapitalRequestID ) )
                {
                    mCapitalRequest = new CapitalRequest( CapitalRequestID );
                }

                return mCapitalRequest;
            }
        }

        [XmlIgnore]
        public Vendor Vendor
        {
            get
            {
                if ( ( mVendor == null && VendorID > 0 ) || ( mVendor != null && mVendor.VendorID != VendorID ) )
                {
                    mVendor = new Vendor( VendorID );
                }

                return mVendor;
            }
        }

        [XmlIgnore]
        public Person CreatedBy
        {
            get
            {
                if ( mCreatedBy == null && !String.IsNullOrEmpty( CreatedByUser ) )
                {
                    mCreatedBy = userLoginService.GetByUserName(CreatedByUser).Person;
                }

                return mCreatedBy;
            }
        }

        [XmlIgnore]
        public Person ModifiedBy
        {
            get
            {
                if ( mModifiedBy == null && !String.IsNullOrEmpty( ModifiedByUser ) )
                {
                    mModifiedBy =  userLoginService.GetByUserName(ModifiedByUser).Person;
                }

                return mModifiedBy;
            }
        }

        [XmlIgnore]
        public BinaryFile QuoteBlob
        {
            get
            {
                if ( mQuoteBlob == null && QuoteBlobID > 0 )
                {
                    mQuoteBlob = new BinaryFileService(new Rock.Data.RockContext()).Queryable().Where(f => f.Id == QuoteBlobID).FirstOrDefault();

                }

                return mQuoteBlob;
            }
        }
        #endregion


        #region Constructors
        public CapitalRequestBid()
        {
            Init();
        }

        public CapitalRequestBid( int bidID )
        {
            Load( bidID );
        }

        public CapitalRequestBid( CapitalRequestBidData data )
        {
            Load( data );
        }
        #endregion

        #region Public Static Methods

        public static int GetDocumentTypeID()
        {
            return new BinaryFileTypeService(new Rock.Data.RockContext()).Queryable().Where(t => t.Guid == Guid.Parse(DocumentTypeGuidValue)).Select(t =>t.Id).FirstOrDefault();
        }

        public static List<CapitalRequestBid> LoadByCapitalRequest(int capitalRequestID)
        {
            using ( PurchasingContext context = ContextHelper.GetDBContext() )
            {
                return context.CapitalRequestDatas.FirstOrDefault( r => r.capital_request_id == capitalRequestID )
                    .CapitalRequestBidDatas
                    .Select( b => new CapitalRequestBid( b ) ).ToList();
            }
        }
        
        #endregion

        #region Public Methods
        public void Save(string userID)
        {
            try
            {
                Dictionary<string, string> valErrors = new Dictionary<string, string>();
                if ( !Validate( out valErrors ) )
                {
                    throw new RequisitionNotValidException( "Capital Request Bid is not valid", valErrors );
                }

                Enums.HistoryType ht;
                CapitalRequestBid originalBid = null;

                using ( PurchasingContext context = ContextHelper.GetDBContext() )
                {
                    CapitalRequestBidData data = null;

                    

                    if ( BidID > 0 )
                    {
                        ht = Enums.HistoryType.UPDATE;
                        data = context.CapitalRequestBidDatas.FirstOrDefault( b => b.bid_id == BidID );
                        originalBid = new CapitalRequestBid( data );
                    }
                    else
                    {
                        ht = Enums.HistoryType.ADD;
                        data = new CapitalRequestBidData();
                    }

                    data.capital_request_id = CapitalRequestID;

                    if ( VendorID > 0 )
                    {
                        data.vendor_id = VendorID;
                        data.vendor_name = null;
                    }
                    else
                    {
                        data.vendor_id = null;
                        data.vendor_name = VendorName;
                    }

                    if ( String.IsNullOrWhiteSpace( VendorContactName ) )
                    {
                        data.vendor_contact_name = null;
                    }
                    else
                    {
                        data.vendor_contact_name = VendorContactName;
                    }

                    if ( VendorContactPhone == null || String.IsNullOrWhiteSpace( VendorContactPhone.Number ) )
                    {
                        data.vendor_contact_phone = null;
                    }
                    else
                    {
                        data.vendor_contact_phone = VendorContactPhone.ToArenaFormat();
                    }


                    if ( String.IsNullOrWhiteSpace( VendorContactEmail ) )
                    {
                        data.vendor_contact_email = null;
                    }
                    else
                    {
                        data.vendor_contact_email = VendorContactEmail;
                    }

                    data.quote_blob_id = QuoteBlobID;
                    data.bid_amount = BidAmount;
                    data.active = Active;
                    data.organization_id = 1;
                    data.modified_by = userID;
                    data.date_modified = DateTime.Now;

                    if(BidID == 0)
                    {
                        data.created_by = userID;
                        data.date_created = DateTime.Now;

                        context.CapitalRequestBidDatas.InsertOnSubmit( data );
                    }

                    context.SubmitChanges();
                    Load( data );
                    SaveHistory( ht, originalBid, userID );
                }
            }
            catch ( Exception ex )
            {
                throw new RequisitionException( "An error occurred while saving a bid.", ex );
            }
        }
        #endregion

        #region Private Methods
        private void Init()
        {
            mCapitalRequest = null;
            mVendor = null;
            mQuoteBlob = null;
            mCreatedBy = null;
            mModifiedBy = null;
            VendorContactPhone = null;

            BidID = 0;
            CapitalRequestID = 0;
            VendorID = 0;
            VendorName = null;
            VendorContactName = null;
            VendorContactPhone = null;
            VendorContactEmail = null;
            QuoteBlobID = 0;
            BidAmount = 0;
            IsPreferredBid = false;
            CreatedByUser = null;
            ModifiedByUser = null;
            DateCreated = DateTime.MinValue;
            DateModified = DateTime.MinValue;
            Active = true;
        }

        private void Load(int bidID)
        {
            using ( PurchasingContext context = ContextHelper.GetDBContext() )
            {
                var data = context.CapitalRequestBidDatas
                            .FirstOrDefault( b => b.bid_id == bidID );

                Load( data );
            }
        }

        private void Load( CapitalRequestBidData data )
        {
            Init();

            if ( data != null )
            {
                BidID = data.bid_id;
                CapitalRequestID = data.capital_request_id;

                if ( data.vendor_id != null )
                {
                    VendorID = (int)data.vendor_id;

                    VendorName = Vendor.VendorName;
                }
                else
                {
                    VendorName = data.vendor_name;
                }

                VendorContactName = data.vendor_contact_name;

                if ( !String.IsNullOrWhiteSpace( data.vendor_contact_phone ) )
                {
                    VendorContactPhone = new Helpers.PhoneNumber( data.vendor_contact_phone );
                }

                VendorContactEmail = data.vendor_contact_email;
                QuoteBlobID = data.quote_blob_id;
                IsPreferredBid = data.is_preferred_bid;
                BidAmount = data.bid_amount;
                CreatedByUser = data.created_by;
                ModifiedByUser = data.modified_by;
                DateCreated = data.date_created;
                DateModified = data.date_modified;
                Active = data.active;
            }
        }

        private void SaveHistory( Enums.HistoryType ht, CapitalRequestBid original, string userID )
        {
            History h = new History();
            h.ObjectTypeName = this.GetType().ToString();
            h.Identifier = BidID;
            h.ChangeType = ht;
            h.Active = true;

            switch ( ht )
            {
                case org.secc.Purchasing.Enums.HistoryType.ADD:
                    h.OriginalXML = null;
                    h.UpdatedXML = base.Serialize( this );
                    break;
                case org.secc.Purchasing.Enums.HistoryType.UPDATE:
                    h.OriginalXML = base.Serialize( original );
                    h.UpdatedXML = base.Serialize( this );
                    break;
                case org.secc.Purchasing.Enums.HistoryType.DELETE:
                    h.OriginalXML = base.Serialize( this );
                    h.UpdatedXML = null;
                    break;
            }

            h.Save( userID );
        }

        private bool Validate( out Dictionary<string, string> valErrors )
        {
            valErrors = new Dictionary<string, string>();
            if ( CapitalRequestID <= 0 )
            {
                valErrors.Add( "CapitalRequestID", "Capital Request ID is required." );
            }

            if ( VendorID == 0 && String.IsNullOrWhiteSpace( VendorName ) )
            {
                valErrors.Add( "Vendor", "A vendor is required." );
            }


            if ( QuoteBlobID <= 0 )
            {
                valErrors.Add( "Quote", "A quote must be attached to the bid. " );
            }

            if ( BidAmount <= 0 )
            {
                valErrors.Add( "Quoted Price", "Quoted Price is required and must be greater than 0." );

            }

            if ( VendorContactPhone != null && !VendorContactPhone.IsValid() )
            {
                valErrors.Add( "Vendor Phone", "Vendor Phone is not formatted correctly." );
            }

            return valErrors.Count == 0;
        }
        #endregion
    }

    public class CapitalRequestBidListItem
    {
        #region Properties
        public int BidID { get; set; }
        public string VendorName { get; set; }
        public string VendorContact { get; set; }
        public decimal BidAmount { get; set; }
        public string QuoteTitle { get; set; }
        public Guid QuoteGuid { get; set; }
        public bool IsPreferredBid { get; set; }
        public bool Active { get; set; }
        #endregion
    }
}
