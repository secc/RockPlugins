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
using System.Data.SqlClient;
using Rock.Model;
using org.secc.Purchasing.DataLayer;
using org.secc.Purchasing.Enums;

namespace org.secc.Purchasing
{
    public class History
    {
        #region Fields
        public string mCreatedByUser = String.Empty;
        public Person mCreatedByPerson = null;
        public DateTime mDateCreated = DateTime.MinValue;

        #endregion

        #region Properties
        public int HistoryID { get; set; }
        public String ObjectTypeName { get; set; }
        public int Identifier { get; set; }
        public HistoryType ChangeType { get; set; }
        public string OriginalXML { get; set; }
        public string UpdatedXML { get; set; }
        public int OrganizationID { get; set; }
        public DateTime DateModified { get; set; }
        public string ModifiedByUser { get; set; }
        public bool Active { get; set; }

        public string CreatedByUser
        {
            get
            {
                return mCreatedByUser;
            }
        }

        public DateTime DateCreated
        {
            get
            {
                return mDateCreated;
            }
        }

        public Person CreatedByPerson
        {
            get
            {
                if (mCreatedByPerson == null && !String.IsNullOrEmpty(CreatedByUser))
                {
                        var userLoginService = new UserLoginService( new Rock.Data.RockContext() );
                        mCreatedByPerson = userLoginService.Queryable().Where(u => u.UserName == CreatedByUser).Select(p => p.Person).FirstOrDefault();

                }

                return mCreatedByPerson;
            }
        }

        #endregion

        #region Constructors
        public History()
        {
            Initialize();
        }

        public History(int historyID)
        {
            Load( historyID, 1);
        }

        public History(int historyID, int organizationID)
        {
            Load( historyID, organizationID );
        }

        public History(HistoryData hd)
        {
            Load(hd);
        }
        #endregion

        #region Public

        public static List<History> LoadHistoryByObjectIdentifier(string objectType, int identifier, bool activeOnly)
        {
            return LoadHistoryByObjectIdentifier(objectType, identifier, 1, activeOnly);
        }

        public static List<History> LoadHistoryByObjectIdentifier(string objectType, int identifier, int organizationID, bool activeOnly)
        {
            try
            {
                if (String.IsNullOrEmpty(objectType))
                    throw new ArgumentNullException("ObjectTypeName", "ObjectTypeName is required.");
                if (identifier <= 0)
                    throw new ArgumentNullException("Identifier", "Identifier is required.");
                if (organizationID <= 0)
                    throw new ArgumentNullException("OrganizationID", "OrganizationID is required.");

                using ( PurchasingContext Context = ContextHelper.GetDBContext() )
                {
                    var query = Context.HistoryDatas
                                    .Where( h => h.object_type_name == objectType )
                                    .Where( h => h.identifier == identifier )
                                    .Where( h => h.organization_id == organizationID );


                    if ( activeOnly )
                    {
                        query = query.Where( h => h.active );
                    }

                    return query.Select( h => new History( h ) ).ToList();
                }
            }
            catch (Exception ex)
            {

                throw new VendorException("An error has occurred when loading history.", ex);
            }
        }

        public static List<History> LoadHistoryByObjectIdentifierDate(string objectType, int identifier, DateTime startDate, DateTime endDate, bool activeOnly)
        {
            return LoadHistoryByObjectIdentifierDate(objectType, identifier, startDate, endDate, 1, activeOnly);
        }

        private static List<History> LoadHistoryByObjectIdentifierDate(string objectType, int identifier, DateTime startDate, DateTime endDate, int organizationID, bool activeOnly)
        {
            try
            {

                if (String.IsNullOrEmpty(objectType))
                    throw new ArgumentNullException("ObjectTypeName", "ObjectTypeName is required.");
                if (identifier <= 0)
                    throw new ArgumentNullException("Identifier", "Identifier is required.");
                if (startDate == DateTime.MinValue)
                    throw new ArgumentNullException("StartDate", "StartDate is required.");
                if (endDate == DateTime.MinValue)
                    throw new ArgumentNullException("EndDate", "EndDate is required.");
                if (organizationID <= 0)
                    throw new ArgumentNullException("OrganizationID", "Organization ID is required.");


                using ( PurchasingContext Context = ContextHelper.GetDBContext() )
                {
                    var query = Context.HistoryDatas
                                    .Where( h => h.object_type_name == objectType )
                                    .Where( h => h.identifier == identifier )
                                    .Where( h => h.date_created >= startDate )
                                    .Where( h => h.date_created <= endDate );

                    if(activeOnly)
                    {
                        query.Where( h => h.active );
                    }

                    return query.Select( h => new History( h ) ).ToList();
                }
            }
            catch (Exception ex)
            {

                throw new HistoryException("An error has occurred when loading History.", ex);
            }
        }



        public int Save(string uid)
        {
            try
            {
                if (HistoryID > 0)
                    throw new Exception("Can not overwrite history.");
                if (String.IsNullOrEmpty(uid))
                    throw new ArgumentNullException("User Login ID is required.");

                Dictionary<string, string> InvalidProps = new Dictionary<string, string>();

                if (!Validated(ref InvalidProps))
                    throw new HistoryNotValidException("Was not able to validate history.", InvalidProps);
                HistoryData data = new HistoryData();

                data.object_type_name = ObjectTypeName;
                data.identifier = Identifier;
                data.organization_id = OrganizationID;
                data.history_type = (int)ChangeType; 
                data.original_xml = OriginalXML;
                data.updated_xml = UpdatedXML;
                data.date_created = DateTime.Now;
                data.created_by = uid;
                data.date_modified = DateTime.Now;
                data.modified_by = uid;
                data.active = Active;


                using (PurchasingContext context = ContextHelper.GetDBContext())
                {
                    context.HistoryDatas.InsertOnSubmit(data);
                    context.SubmitChanges();
                }
                Load(data);
                return HistoryID;
            }
            catch(HistoryNotValidException nvEx)
            {
                throw nvEx;
            }
            catch (Exception ex)
            {
                throw new HistoryException("An error has occurred while saving History.", ex);
            }
        }
        #endregion

        #region private

        private void Initialize()
        {
            HistoryID = 0;
            ObjectTypeName = String.Empty;
            OriginalXML = String.Empty;
            UpdatedXML = String.Empty;
            ChangeType = HistoryType.ADD;
            OrganizationID = 1;
            mCreatedByUser = String.Empty;
            mDateCreated = DateTime.MinValue;
            Active = true;
        }

        private void Load(HistoryData hd)
        {
            Initialize();
            HistoryID = hd.history_id;
            ObjectTypeName = hd.object_type_name;
            Identifier = hd.identifier;
            OriginalXML = hd.original_xml;
            UpdatedXML = hd.updated_xml;
            ChangeType = (HistoryType)hd.history_type;
            OrganizationID = hd.organization_id;
            mDateCreated = hd.date_created;
            mCreatedByUser = hd.created_by;
            DateModified = hd.date_modified;
            ModifiedByUser = hd.modified_by;
            Active = hd.active;

        }

        private void Load(int historyId, int organizationId)
        {
            using (PurchasingContext context = ContextHelper.GetDBContext())
            {
                var historyItem = context.HistoryDatas
                                    .Where( h => h.history_id == historyId )
                                    .Where( h => h.organization_id == organizationId )
                                    .FirstOrDefault();

                Load( historyItem );
            }
        }

        private bool Validated(ref Dictionary<string, string> invalidProps)
        {          

            if (String.IsNullOrEmpty(ObjectTypeName))
            {
                invalidProps.Add("ObjectTypeName", "Object Type Name is required");
            }

            if (OrganizationID <= 0)
            {
                invalidProps.Add("OrganizationID", "Organization ID is required.");
            }

            if (Identifier <= 0)
            {
                invalidProps.Add("Identifier", "Identifier is required.");
            }

            if (String.IsNullOrEmpty(OriginalXML) && String.IsNullOrEmpty(UpdatedXML))
            {
                invalidProps.Add("OriginalXML or UpdatedXML", "Original or Updated XML is required.");
            }

            return (invalidProps.Count == 0);
        }

        #endregion
    }

    [Serializable]
    public class HistoryNotValidException : Exception
    {
        private Dictionary<string, string> mInvalidProperties = null;

        public HistoryNotValidException() { }
        public HistoryNotValidException(string message) : base(message) { }

        public HistoryNotValidException(string message, Dictionary<string, string> invalidProps)
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

        public HistoryNotValidException(string message, Exception inner) : base(message, inner) { }
        protected HistoryNotValidException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }

    }

    public class HistoryStaging
    {
        int Identifier { get; set; }
        Enums.HistoryType ChangeType { get; set; }
        object Original { get; set; }

        public HistoryStaging() { }

        public HistoryStaging(int id, Enums.HistoryType ht, object original)
        {
            Identifier = id;
            ChangeType = ht;
            Original = original;
        }
    }

    [Serializable]
    public class HistoryNotFoundException : Exception
    {
        public HistoryNotFoundException() { }
        public HistoryNotFoundException(string message) : base(message) { }
        public HistoryNotFoundException(string message, Exception inner) : base(message, inner) { }
        protected HistoryNotFoundException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }

    [Serializable]
    public class HistoryException : Exception
    {
        public HistoryException() { }
        public HistoryException(string message) : base(message) { }
        public HistoryException(string message, Exception inner) : base(message, inner) { }
        protected HistoryException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
