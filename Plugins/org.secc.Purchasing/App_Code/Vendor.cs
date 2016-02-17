using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using System.Text;
using System.Data.SqlClient;
using org.secc.Purchasing.DataLayer;
using org.secc.Purchasing.Enums;
using Rock.Model;


namespace org.secc.Purchasing
{
    [Serializable]
    public class Vendor: PurchasingBase
    {
        #region Fields
        private Person mCreatedByPerson = null;
        private Person mModifiedByPerson = null;
        private string mCreatedByUser;
        private DateTime mDateCreated;
        
        private List<PurchaseOrder> mPurchaseOrders = null;

        #endregion

        #region Properties
        public int VendorID { get; set; }
        public Guid VendorGUID { get; set; }
        public string VendorName { get; set; }
        public DateTime DateModified { get; set; }
        public string ModifiedByUser { get; set; }
        public int OrganizationID { get; set; }
        public bool Active { get; set; }
        public Helpers.Address Address { get; set; }
        public Helpers.PhoneNumber Phone { get; set; }
        public string WebAddress { get; set; }
        public string Terms { get; set; }

        public string CreatedByUser
        {
            get
            {
                return mCreatedByUser;
            }
            set
            {
                mCreatedByUser = value;
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

        [XmlIgnore]
        public Person CreatedByPerson 
        {
            get
            {
                if (mCreatedByPerson == null && !String.IsNullOrEmpty(CreatedByUser))
                {
                    mCreatedByPerson = userLoginService.GetByUserName(CreatedByUser).Person;
                }

                return mCreatedByPerson;
            }
        }

        [XmlIgnore]
        public Person ModifiedByPerson 
        {
            get
            {
                if (mModifiedByPerson == null && !String.IsNullOrEmpty(ModifiedByUser))
                    mModifiedByPerson = userLoginService.GetByUserName(ModifiedByUser).Person;

                return mModifiedByPerson;
            }
        }

        [System.Xml.Serialization.XmlIgnore]
        public List<PurchaseOrder> PurchaseOrders
        {
            get
            {
                return mPurchaseOrders;
            }
            set
            {
                mPurchaseOrders = value;
            }
        }
        #endregion

        #region Constructors
        public Vendor()
        {
            Initialize();
        }

        public Vendor(int vendorID)
        {
            Load(vendorID, 1);
        }

        public Vendor(int vendorID, int organizationID)
        {
            Load(vendorID, organizationID);
        }

        public Vendor(Guid vendorGUID)
        {
            Load(vendorGUID);
        }

        public Vendor(VendorData v)
        {
            Load(v);
        }
        #endregion

        #region Public

        public static List<Vendor> LoadByName(string vName, bool activeOnly)
        {
            try
            {
                //VendorList = Where(v => v.vendor_name.ToLower().Contains(vName.ToLower()) && v.organization_id == 1);
                using ( PurchasingContext Context = ContextHelper.GetDBContext() )
                {
                    var vendorQuery = Context.VendorDatas
                                        .Where( v => v.vendor_name.ToLower().Contains( vName.ToLower() ) )
                                        .Where( v => v.organization_id == 1 );

                    if ( activeOnly )
                    {
                        vendorQuery = vendorQuery.Where( v => v.active == activeOnly );
                    }

                    return vendorQuery.Select( v => new Vendor( v ) ).ToList();
                }
            }
            catch (Exception ex)
            {

                throw new VendorException("An error has occurred when loading the vendor list by name.", ex);
            }
        }

        public static List<Vendor> LoadVendors(bool activeOnly)
        {
            List<Vendor> VendorList = new List<Vendor>();
            try
            {

                using ( PurchasingContext Context = ContextHelper.GetDBContext() )
                {
                    var VendorQuery = Context.VendorDatas.Select( v => v );

                    if ( activeOnly )
                    {
                        VendorQuery = VendorQuery.Where( v => v.active );
                    }

                    return VendorQuery.Select( v => new Vendor( v ) ).ToList();
                }
            }
            catch (Exception ex)
            {

                throw new VendorException("An error has occurred when loading the vendor list.", ex);
            }


        }

        public static void Delete(int vendorID, string uid)
        {
            Delete(vendorID, 1, uid);
        }

        public static void Delete(int vendorID, int organizationID, string uid)
        {
            Vendor OriginalVendor = new Vendor(vendorID, organizationID);

            try
            {
                if (vendorID <= 0)
                    throw new ArgumentException("Vendor ID is required to perform delete.", "VendorID");
                if (organizationID <= 0)
                    throw new ArgumentNullException("Organization ID is required to perform delete", "OrganizationID");
                if (String.IsNullOrEmpty(uid))
                    throw new ArgumentNullException("User ID is required to perform delete.", "uid");

                using (PurchasingContext context = ContextHelper.GetDBContext())
                {
                    VendorData Data = context.VendorDatas.Where(x => x.vendor_id == vendorID && x.organization_id == organizationID).FirstOrDefault();
                    context.VendorDatas.DeleteOnSubmit(Data);

                    context.SubmitChanges();
                }

                OriginalVendor.SaveHistory(HistoryType.DELETE, null, uid); 

            }
            catch (Exception ex)
            {
                throw new VendorException("An error has occurred while deleting vendor.", ex);
            }


        }

        public int Save(string uid)
        {
            Vendor OriginalVendor = null;
            HistoryType ht;

            Dictionary<string,string> ValErrors = new Dictionary<string, string>();
            if (!Validated(ref ValErrors))
            {
                throw new VendorNotValidException("Was not able to validate vendor.", ValErrors);
            }


            using (PurchasingContext Context = ContextHelper.GetDBContext())
            {
                VendorData Data;

                if (VendorID > 0)
                {
                    OriginalVendor = new Vendor(VendorID, OrganizationID);
                    ht = HistoryType.UPDATE;
                    Data = Context.VendorDatas.Where(x => x.vendor_id == VendorID && x.organization_id == OrganizationID).FirstOrDefault();
                }
                else
                {
                    ht = HistoryType.ADD;
                    Data = new VendorData();
                    Data.date_created = DateTime.Now;
                    Data.created_by = uid;
                    Data.vendor_guid = Guid.NewGuid();
                    Data.organization_id = 1;
                }

                Data.vendor_name = VendorName;

                if (Address == null || String.IsNullOrEmpty(Address.StreetAddress))
                    Data.address = null;
                else
                    Data.address = Address.ToArenaFormat();

                if (Phone == null || String.IsNullOrEmpty(Phone.Number))
                    Data.phone = null;
                else
                    Data.phone = Phone.ToArenaFormat();

                if (String.IsNullOrEmpty(WebAddress))
                    Data.web_address = null;
                else
                    Data.web_address = WebAddress;

                if (String.IsNullOrEmpty(Terms))
                    Data.terms = null;
                else
                    Data.terms = Terms;

                Data.modified_by = uid;
                Data.date_modified = DateTime.Now;
                Data.active = Active;

                if (VendorID == 0)
                    Context.VendorDatas.InsertOnSubmit(Data);

                try
                {
                    Context.SubmitChanges();
                }
                catch (System.Data.Linq.ChangeConflictException ex)
                {
                    StringBuilder sb = new StringBuilder();
                    foreach (System.Data.Linq.ObjectChangeConflict c in Context.ChangeConflicts)
                    {
                        sb.AppendFormat("Table {0} ID: {1}", Context.Mapping.GetTable(c.Object.GetType()), ((VendorData)c.Object).vendor_id);

                    }
                    throw new Exception(sb.ToString(), ex);
                }


                Load(Data);
            }
            SaveHistory(ht, OriginalVendor, uid);
            return VendorID;
        }

        #endregion

        #region Private

        private void Initialize()
        {
            VendorID = 0;
            VendorGUID = Guid.Empty;
            VendorName = String.Empty;
            mDateCreated = DateTime.MinValue;
            mCreatedByUser = String.Empty;
            mCreatedByPerson = null;
            DateModified = DateTime.MinValue;
            ModifiedByUser = String.Empty;
            mModifiedByPerson = null;
            Address = null;
            Phone = null;
            WebAddress = String.Empty;
            Terms = String.Empty;

            OrganizationID = 1;
            Active = true;
        }

        private void Load(Guid guid)
        {
            using ( PurchasingContext Context = ContextHelper.GetDBContext() )
            {
                VendorData Data = Context.VendorDatas.FirstOrDefault( v => v.vendor_guid == guid );
            }
            
        }

        private void Load(int vendorId, int organizationId)
        {
            using (PurchasingContext Context = ContextHelper.GetDBContext())
            {
                VendorData Data = Context.VendorDatas.Where( v => v.organization_id == organizationId ).Where( v => v.vendor_id == vendorId ).FirstOrDefault();

                Load(Data);
            }
        }

        private void Load(VendorData data)
        {
            Initialize(); 
            if(data != null)
            {
                VendorID= data.vendor_id;
                VendorName = data.vendor_name;
                VendorGUID = data.vendor_guid;
                OrganizationID = data.organization_id;
                mDateCreated = data.date_created;
                mCreatedByUser = data.created_by;
                DateModified = data.date_modified;
                ModifiedByUser = data.modified_by;
                if(!String.IsNullOrEmpty(data.address))
                {
                    Address = new Helpers.Address(data.address);
                }

                if(!String.IsNullOrEmpty(data.phone))
                {
                    Phone = new Helpers.PhoneNumber(data.phone);
                }

                WebAddress = data.web_address;
                Active = data.active;
                Terms = data.terms;
            }
        }

        private bool Validated(ref Dictionary<string, string> ValErrors)
        {
 
            if(String.IsNullOrEmpty(VendorName))
                ValErrors.Add("Vendor Name", "Vendor Name is required.");
            if (VendorID > 0 && VendorGUID == Guid.Empty)
                ValErrors.Add("Vendor GUID", "Vendor GUID is required.");

            return (ValErrors.Count == 0);
        }


        private void SaveHistory(HistoryType ht, Vendor v, string uid)
        {
            History h = new History();
            h.ObjectTypeName = this.GetType().ToString();
            h.ChangeType = ht;
            h.Active = true;
            h.OrganizationID = OrganizationID;
            switch (ht)
            {
                case HistoryType.ADD:
                    h.Identifier = VendorID;
                    h.OriginalXML = null;
                    h.UpdatedXML = Serialize(this);
                    break;
                case HistoryType.UPDATE:
                    h.Identifier = VendorID;
                    h.OriginalXML = v.Serialize(this);
                    h.UpdatedXML = Serialize(this);
                    break;
                case HistoryType.DELETE:
                    h.Identifier = VendorID;
                    h.OriginalXML = Serialize(this);
                    h.UpdatedXML = null;
                    break;
            }

            h.Save(uid);
        }


        #endregion
    }

    [Serializable]
    public class VendorNotValidException : Exception
    {
        private Dictionary<string, string> mInvalidProperties = null;

        public VendorNotValidException() { }
        public VendorNotValidException(string message) : base(message) { }

        public VendorNotValidException(string message, Dictionary<string, string> invalidProps)
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

        public VendorNotValidException(string message, Exception inner) : base(message, inner) { }
        protected VendorNotValidException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }

    }

    [Serializable]
    public class VendorException : Exception
    {
        public VendorException() { }
        public VendorException(string message) : base(message) { }
        public VendorException(string message, Exception inner) : base(message, inner) { }
        protected VendorException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
