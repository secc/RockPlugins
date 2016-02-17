using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Data.SqlClient;
using org.secc.Purchasing.DataLayer;
using org.secc.Purchasing.Enums;
using org.secc.Purchasing.Accounting;
using Rock.Model;

namespace org.secc.Purchasing
{
    [Serializable]
    [XmlType("PurchasingCampus")]
    public class Campus: Rock.Model.Campus
    {
        private int mCompanyID;
        private int mFundID;
        private DateTime mXrefDateCreated;
        private string mXrefCreatedBy;
        private DateTime mXrefDateModified;
        private string mXrefModifiedBy;
        private bool mXrefActive;
        public Company mCompany;
        public Fund mFund;

        public int CompanyID
        {
            get
            {
                return mCompanyID;
            }
            set
            {
                mCompanyID = value;
            }
        }

        public int FundID
        {
            get
            {
                return mFundID;
            }
            set
            {
                mFundID = value;
            }
        }

        public DateTime XrefDateCreated
        {
            get
            {
                return mXrefDateCreated;
            }
            set
            {
                mXrefDateCreated = value;
            }
        }

        public string XrefCreatedBy
        {
            get
            {
                return mXrefCreatedBy;
            }
            set
            {
                mXrefCreatedBy = value;
            }
        }

        public DateTime XrefDateModified
        {
            get
            {
                return mXrefDateModified;
            }
            set
            {
                mXrefDateModified = value;
            }
        }

        public string XrefModifiedBy
        {
            get
            {
                return mXrefModifiedBy;
            }
            set
            {
                mXrefModifiedBy = value;
            }
        }

        public bool XrefActive
        {
            get
            {
                return mXrefActive;
            }
            set
            {
                mXrefActive = value;
            }
        }

        [XmlIgnore]
        public Company Company
        {
            get
            {
                if (mCompany == null && mCompanyID > 0)
                    mCompany = new Company(CompanyID);

                return mCompany;
            }
        }

        [XmlIgnore]
        public Fund Fund
        {
            get
            {
                if (mFund == null && mCompanyID > 0 && mFundID > 0)
                    mFund = new Fund(mCompanyID, mFundID);

                return mFund;
            }
        }

        public Campus():base() { }
        
        public Campus(int campusID)
        {
            throw new NotImplementedException();
        }

        public Campus(int companyID, int fundID):base()
        {
            throw new NotImplementedException();
        }

        public Campus(CampusData c)
        {
            Load(c);
        }

        public static List<Campus> GetCampuses()
        {
            using ( PurchasingContext Context = ContextHelper.GetDBContext() )
            {
                return Context.CampusDatas
                    .Select( c => new Campus( c ) )
                    .ToList();
            }
        }

        public static List<Campus> GetCampusesByCompany(int companyID)
        {
            if (companyID <= 0)
                throw new ArgumentNullException("Company ID", "Company ID is required.");

            using ( PurchasingContext Context = ContextHelper.GetDBContext() )
            {
                return Context.CampusDatas
                    .Where( c => c.campus_company_id == companyID )
                    .Select( c => new Campus( c ) )
                    .ToList();
            }
        }

        [Description("This delete method is not used to delete Purchasing Campus use Delete(campusID, userID)")]
        private void Delete(int campusID)
        {
            throw new NotImplementedException("To delete Purchasingcampus use Delete(campusID, userID)");
        }

        [Description("Only Deletes Purchasing Campus")]
        public void Delete(int campusID, string userName)
        {
            try
            {
                if (campusID <= 0)
                    throw new ArgumentNullException("CampusID", "CampusID is required.");

                if (String.IsNullOrEmpty(userName))
                    throw new ArgumentNullException("User Name", "User Name is required");

                Campus OriginalCampus = new Campus(this.Id);

                using (PurchasingContext context = ContextHelper.GetDBContext())
                {
                    CampusData d = context.CampusDatas.Where(c => c.campus_id == this.Id).FirstOrDefault();
                    context.CampusDatas.DeleteOnSubmit(d);

                    context.SubmitChanges();
                }

                SaveHistory(null, HistoryType.DELETE, userName);
            }
            catch (Exception ex)
            {

                throw new CampusException("An error has occurred while deleting Purchasing Campus", ex);
            }

        }

        public static Vendor Deserialize(string xmlString)
        {
            System.IO.StringReader sr = new System.IO.StringReader(xmlString);
            XmlSerializer XmlSerial = new XmlSerializer(typeof(Campus));
            Vendor V = (Vendor)XmlSerial.Deserialize(sr);

            return V;
        }

        [Description("This method only saves the purchasing campus.")]
        public void Save(string userName)
        {
            try
            {
                Dictionary<string, string> ValErrors = Validate();
                if (ValErrors.Count > 0)
                    throw new CampusNotValidException("Campus is not valid", ValErrors);

                using (PurchasingContext context = ContextHelper.GetDBContext())
                {
                    CampusData Data;
                    if (XrefDateCreated > DateTime.MinValue)
                    {
                        Data = context.CampusDatas.Where(x => x.campus_id == Id).FirstOrDefault();
                        Data.campus_company_id = CompanyID;
                        Data.campus_fund_id = FundID;
                        Data.date_modified = DateTime.Now;
                        Data.modified_by = userName;
                        Data.active = XrefActive;
                        context.CampusDatas.InsertOnSubmit(Data);
                    }
                    else
                    {
                        Data = new CampusData();
                        Data.campus_id = Id;
                        Data.campus_company_id = CompanyID;
                        Data.campus_fund_id = FundID;
                        Data.date_created = DateTime.Now;
                        Data.created_by = userName;
                        Data.date_modified = DateTime.Now;
                        Data.modified_by = userName;
                        Data.active = XrefActive;
                    }
                    context.SubmitChanges();

                    Load(Data.campus_id);
                }

            }
            catch (Exception ex)
            {
                throw new CampusException("An error has occurred while saving campus.", ex);
            }
        }

        public void SaveHistory(Campus originalCampus, HistoryType hType, string userName)
        {
            History h = new History();
            h.ChangeType = hType;
            h.ObjectTypeName = this.GetType().ToString();
            h.OrganizationID = 1;
            h.Identifier = Id;

            switch (hType)
            {
                case HistoryType.ADD:
                    h.OriginalXML = null;
                    h.UpdatedXML = this.Serialize();
                    break;
                case HistoryType.UPDATE:
                    h.OriginalXML = originalCampus.Serialize();
                    h.UpdatedXML = this.Serialize();
                    break;
                case HistoryType.DELETE:
                    h.OriginalXML = this.Serialize();
                    h.UpdatedXML = null;
                    break;
            }

            h.Save(userName);
        }

        private string Serialize()
        {
            System.IO.StringWriter sw = new System.IO.StringWriter();
            XmlSerializer xmlSerial = new XmlSerializer(this.GetType());
            xmlSerial.Serialize(sw, this);

            return sw.ToString();
        }

        public Dictionary<string, string> Validate()
        {
            Dictionary<string, string> ValidationErrors = new Dictionary<string, string>();

            if (Id <= 0)
                ValidationErrors.Add("CampusID", "Campus ID is required.");
            if (CompanyID <= 0)
                ValidationErrors.Add("CompanyID", "Company ID is required.");
            if (FundID <= 0)
                ValidationErrors.Add("FundID", "Fund ID is required.");

            return ValidationErrors;
        }

        private void Init()
        {
            CompanyID = 0;
            FundID = 0;
            XrefDateCreated = DateTime.MinValue;
            XrefCreatedBy = String.Empty;
            XrefDateModified = DateTime.MinValue;
            XrefModifiedBy = String.Empty;
            XrefActive = true;
            mFund = null;
            mCompany = null;
        }

        private void InitBase()
        {
            Id = -1;
            CreatedDateTime = DateTime.MinValue;
            ModifiedDateTime = DateTime.MinValue;
            CreatedByPersonAliasId = -1;
            ModifiedByPersonAliasId = -1;
            Guid = Guid.Empty;
            Name = string.Empty;
            LeaderPersonAlias = null;
            LeaderPersonAliasId = -1;
            LocationId = -1;
        }

        private void Load(CampusData c)
        {
            Init();
            if (c != null)
            {
                CampusService campusService = new CampusService(new Rock.Data.RockContext());

                Rock.Model.Campus RockCampus = campusService.Get(c.campus_id);
                Id = RockCampus.Id;
                CreatedDateTime = RockCampus.CreatedDateTime;
                ModifiedDateTime = RockCampus.ModifiedDateTime;
                CreatedByPersonAliasId = RockCampus.CreatedByPersonAliasId;
                ModifiedByPersonAliasId = RockCampus.ModifiedByPersonAliasId;
                Guid = RockCampus.Guid;
                Name = RockCampus.Name;
                LeaderPersonAliasId = RockCampus.LeaderPersonAliasId;
                LocationId = RockCampus.LocationId;
                CompanyID = c.campus_company_id;
                FundID = c.campus_fund_id;
                XrefDateCreated = c.date_created;
                XrefCreatedBy = c.created_by;
                XrefDateModified = c.date_modified;
                XrefModifiedBy = c.modified_by;
                XrefActive = c.active;
            }
        }

        private void Load(int campusId)
        {
            using (PurchasingContext context = ContextHelper.GetDBContext())
            {
                Load( context.CampusDatas.FirstOrDefault( c => c.campus_id == campusId ) );
            }
        }

        private void LoadByCampusID(int campusID)
        {
            if (campusID <= 0)
                throw new ArgumentNullException("Campus ID", "Campus ID is required.");
            Load( campusID );
        }

        private void LoadByCompanyFund(int companyID, int fundID)
        {
            if (companyID <= 0)
                throw new ArgumentNullException("Company ID", "Company ID is required.");
            if (fundID <= 0)
                throw new ArgumentNullException("Fund ID", "Fund ID is required.");

            using ( PurchasingContext Context = ContextHelper.GetDBContext() )
            {
                var campusData = Context.CampusDatas
                                    .Where( cd => cd.campus_company_id == companyID )
                                    .Where( cd => cd.campus_fund_id == fundID )
                                    .FirstOrDefault();

                Load( campusData );
            }
        }
    }

    [Serializable]
    public class CampusNotValidException : Exception
    {
        private Dictionary<string, string> mInvalidProperties = null;

        public CampusNotValidException() { }
        public CampusNotValidException(string message) : base(message) { }

        public CampusNotValidException(string message, Dictionary<string, string> invalidProps)
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

    public CampusNotValidException(string message, Exception inner) : base(message, inner) { }
    protected CampusNotValidException(
        System.Runtime.Serialization.SerializationInfo info,
        System.Runtime.Serialization.StreamingContext context)
        : base(info, context) { }

    }

    [Serializable]
    public class CampusException : Exception
    {
        public CampusException() { }
        public CampusException(string message) : base(message) { }
        public CampusException(string message, Exception inner) : base(message, inner) { }
        protected CampusException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
