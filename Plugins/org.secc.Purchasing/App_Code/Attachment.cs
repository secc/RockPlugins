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
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using org.secc.Purchasing.DataLayer;
using Rock.Model;


namespace org.secc.Purchasing
{
    public class Attachment : PurchasingBase
    {
        #region Fields
        private Person mCreatedBy;
        private Person mModifiedBy;
        private BinaryFile mDataBlob;

        private static Guid mPurchasingDocumentTypeGuid = new Guid("5070FDCF-8A68-4139-B07D-CB5E82CE1114");

        #endregion

        #region Properties
        public int AttachmentID { get; set; }
        public string ParentObjectTypeName { get; set; }
        public int ParentIdentifier { get; set; }
        public int BlobID { get; set; }
        public int OrganizationID { get; set; }
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
        public BinaryFile DataBlob
        {
            get
            {
                if (mDataBlob == null && BlobID > 0)
                {
                    mDataBlob = new BinaryFileService(new Rock.Data.RockContext()).Get(BlobID);
                }

                return mDataBlob;
            }
        }

        #endregion

        #region Constructors
        public Attachment()
        {
            Init();
        }

        public Attachment(int attachmentId)
        {
            Load(attachmentId);
        }

        public Attachment(AttachmentData data)
        {
            Load(data);
        }
        #endregion

        #region Public
        public static BinaryFileType GetPurchasingDocumentType()
        {
            return new BinaryFileTypeService(new Rock.Data.RockContext()).Get(mPurchasingDocumentTypeGuid);
        }

        public static List<Attachment> GetObjectAttachments(string otn, int identifier, bool activeOnly)
        {
            try
            {
                using ( PurchasingContext Context = ContextHelper.GetDBContext() )
                {
                    var attachQuery = Context.AttachmentDatas
                                        .Where( a => a.parent_object_type_name == otn )
                                        .Where( a => a.parent_identifier == identifier );

                    if ( activeOnly )
                    {
                        attachQuery = attachQuery.Where( a => a.active );
                    }

                    return attachQuery.Select( a => new Attachment( a ) ).ToList();

                }
            }
            catch ( Exception ex )
            {
                throw new RequisitionException("An error has occurred while loading attachments.", ex);
            }


        }

        public static Attachment LoadByBlobID(string otn, int id, int blobID)
        {

            using ( PurchasingContext Context = ContextHelper.GetDBContext() )
            {
                return Context.AttachmentDatas.Where( x => x.parent_object_type_name == otn )
                            .Where( x => x.parent_identifier == id )
                            .Where( x => x.blob_id == blobID )
                            .Select( x => new Attachment( x ) )
                            .FirstOrDefault();

            }
        }

        public void Save(string uid)
        {
            try
            {
                if (String.IsNullOrEmpty(uid))
                    throw new ArgumentNullException("uid", "User ID is required.");
                Dictionary<string, string> ValErrors = Validate();
                if (ValErrors.Count > 0)
                    throw new RequisitionNotValidException("Attachment is not valid", ValErrors);
                Attachment Original = null;
                Enums.HistoryType ChangeType;
                using (PurchasingContext Context = ContextHelper.GetDBContext())
                {
                    AttachmentData data;

                    if (AttachmentID > 0)
                    {
                        data = Context.AttachmentDatas.FirstOrDefault(x => x.attachment_id == AttachmentID);
                        Original = new Attachment(data);
                        ChangeType = Enums.HistoryType.UPDATE;
                    }
                    else
                    {
                        data = new AttachmentData();
                        ChangeType = Enums.HistoryType.ADD;
                        data.date_created = DateTime.Now;
                        data.created_by = uid;
                    }
                    data.parent_object_type_name = ParentObjectTypeName;
                    data.parent_identifier = ParentIdentifier;
                    data.blob_id = BlobID;
                    data.organization_id = OrganizationID;
                    data.modified_by = uid;
                    data.date_modified = DateTime.Now;
                    data.active = Active;

                    if (AttachmentID <= 0)
                        Context.AttachmentDatas.InsertOnSubmit(data);

                    Context.SubmitChanges();

                    Load(data);

                }
                SaveHistory(ChangeType, Original, uid);
            }
            catch (Exception ex)
            {
                throw new RequisitionException("An error has occurred while saving attachment.", ex);
            }
        }


        #endregion

        #region Private
        private void Init()
        {
            AttachmentID = 0;
            ParentObjectTypeName = String.Empty;
            ParentIdentifier = 0;
            BlobID = 0;
            OrganizationID = 1;
            CreatedByUserID = String.Empty;
            ModifiedByUserID = String.Empty;
            DateCreated = DateTime.MinValue;
            DateModified = DateTime.MinValue;
            Active = true;

            mCreatedBy = null;
            mModifiedBy = null;
            mDataBlob = null;
        }

        private void Load(AttachmentData data)
        {
            Init();
            if (data != null)
            {
                AttachmentID = data.attachment_id;
                ParentObjectTypeName = data.parent_object_type_name;
                ParentIdentifier = data.parent_identifier;
                BlobID = data.blob_id;
                OrganizationID = data.organization_id;
                CreatedByUserID = data.created_by;
                ModifiedByUserID = data.modified_by;
                DateCreated = data.date_created;
                DateModified = data.date_modified;

                Active = data.active;               

            }
        }

        private void Load(int attachmentId)
        {
            try
            {
                using (PurchasingContext Context = ContextHelper.GetDBContext())
                {
                    Load( Context.AttachmentDatas.FirstOrDefault( a => a.attachment_id == attachmentId ) );

                }
            }
            catch (Exception ex)
            {
                throw new RequisitionException("An error has occurred while loading attachment.", ex);
            }
        }

        private void SaveHistory(Enums.HistoryType chgType, Attachment original, string uid)
        {
            History h = new History();
            h.ObjectTypeName = this.GetType().ToString();
            h.Identifier = AttachmentID;
            h.ChangeType = chgType;
            h.OrganizationID = OrganizationID;
            h.Active = true;
            switch (chgType)
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

        private Dictionary<string, string> Validate()
        {
            Dictionary<string, string> ValErrors = new Dictionary<string, string>();
            if (String.IsNullOrEmpty(ParentObjectTypeName))
                ValErrors.Add("Parent Object Type Name", "Name/Namespace of Parent Object Type is required.");
            if (ParentIdentifier <= 0)
                ValErrors.Add("Parent Identifier", "Parent Object Type Identifier is required.");
            if (BlobID <= 0)
                ValErrors.Add("Blob ID", "Blob ID is required and must be greater than 0.");
            else if (DataBlob.Id <= 0)
                ValErrors.Add("File Not Found", "Referenced file is not found.");

            return ValErrors;
        }


        #endregion

    }
}
