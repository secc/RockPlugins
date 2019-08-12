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

using Rock;
using org.secc.Purchasing.DataLayer;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.Purchasing
{
    public class Approval : PurchasingBase
    {
        #region Fields
        private Person mApprover;
        private DefinedValueCache mApprovalStatus;
        private Person mCreatedBy;
        private Person mModifiedBy;
        private DefinedValueCache mApprovalType;
        private List<Note> mNotes;
        

        private static Guid ApprovalStatusGUID = new Guid("E35037F0-A04D-403C-A9D7-F95AC63B5550");
        private static Guid ApprovedStatusGUID = new Guid("4B4F5816-C265-42E9-BA5F-5A1FEEBADE4F");
        public static Guid ApprovedAndForwardedGUID = new Guid("A7B0C826-7C85-4E0A-986B-E72A5265DFF1");
        private static Guid NotSubmittedGUID = new Guid( "92163224-AE0D-4734-A05F-8B1E46200043" );
        private static Guid NotApprovedStatusGUID = new Guid("89E5BB4E-C87E-4715-84A1-E23CF5F4825F");
        private static Guid PendingApprovalStatusGUID = new Guid("8885A480-5850-439C-AD38-E3AC8FCD25C7");

        private static Guid RequisitionApprovalTypeGUID = new Guid( "952EC9FE-6827-4CD8-9980-63B511447333" );
        private static Guid MinistryApprovalTypeGUID = new Guid( "8F840130-8B50-45DB-8AEE-FABE541C7AF1" );
        private static Guid FinanceApprovalTypeGUID = new Guid( "91BBB5E5-3A3E-4E82-878C-2573DAC00B9A" );
        private static Guid LeadTeamApprovalTypeGUID = new Guid( "0B6680DD-0D67-456F-832E-89E7399DE05F" );
        #endregion

        #region Properties
        public int ApprovalID { get; set; }
        public string ObjectTypeName { get; set; }
        public int Identifier { get; set; }
        public int ApproverID { get; set; }
        public int ApprovalStatusLUID { get; set; }
        public string CreatedByUserID { get; set; }
        public string ModifiedByUserID { get; set; }
        public DateTime DateApproved { get; set; }
        public DateTime DateNotified { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }
        public bool Active { get; set; }
        public int OrganizationID { get; set; }
        public int ApprovalTypeLUID { get; set; }


        [XmlIgnore]
        public Person Approver
        {
            get
            {
                if ( ( mApprover == null || mApprover.Id != ApproverID ) && ApproverID > 0 )
                    mApprover = personAliasService.Get(ApproverID).Person;
                return mApprover;
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
        public DefinedValueCache ApprovalStatus
        {
            get
            {
                if ((mApprovalStatus == null || mApprovalStatus.Id != ApprovalStatusLUID) && ApprovalStatusLUID > 0)
                    mApprovalStatus = DefinedValueCache.Get(ApprovalStatusLUID);
                return mApprovalStatus;
            }
        }

        [XmlIgnore]
        public List<Note> Notes
        {
            get
            {
                if ( mNotes == null )
                {
                    LoadNotes();
                }

                return mNotes;
            }
        }

        public DefinedValueCache ApprovalType
        {
            get
            {
                if ( ( mApprovalType == null && ApprovalTypeLUID > 0 ) || ( mApprovalType != null && mApprovalType.Id != ApprovalTypeLUID ) )
                {
                    mApprovalType = DefinedValueCache.Get(ApprovalTypeLUID);
                }

                return mApprovalType;
            }
        }



        #endregion

        #region Constructor
        public Approval()
        {
            Init();
        }

        public Approval(int approvalID)
        {
            Load( approvalID );
        }

        public Approval(ApprovalData data)
        {
            Load(data);
        }


        private static List<DefinedValueCache> GetApprovalStatus(bool activeOnly)
        {
            List<DefinedValueCache> Statuses = DefinedTypeCache.Get(ApprovalStatusGUID).DefinedValues.ToList();

            if (activeOnly)
                Statuses.RemoveAll(s => s.IsActive != activeOnly);

            return Statuses;
        }

        #endregion

        #region Public

        public void AddNote( string note, string userId )
        {
            Note n = new Note();
            n.ObjectTypeName = this.GetType().ToString();
            n.Identifier = ApprovalID;
            n.Body = note;
            n.Save( userId );
            RefreshNotes();

        }

        public void Approve(string uid)
        {
            Approve(uid, ApprovedStatusLUID());
        }

        public void Approve(string uid, int approvalStatusLUID)
        {
            if (String.IsNullOrEmpty(uid))
                throw new ArgumentNullException("uid", "User ID is required.");
            if (userLoginService.GetByUserName(uid).Person.PrimaryAliasId == ApproverID)
            {
                ApprovalStatusLUID = approvalStatusLUID;
                   
                DateApproved = DateTime.Now;
                Save(uid);
            }
        }

        public void Decline(string uid)
        {
            if (String.IsNullOrEmpty(uid))
                throw new ArgumentNullException("uid", "User ID is required.");

            if (userLoginService.GetByUserName(uid).Person.PrimaryAliasId == ApproverID)
            {
                ApprovalStatusLUID = NotApprovedStatusLUID();
                DateApproved = DateTime.MinValue;
                Save(uid);
            }
        }

        public void Delete(string uid)
        {

            if (String.IsNullOrEmpty(uid))
                throw new ArgumentNullException("uid", "User ID is required.");

            Person CurrentUser = userLoginService.GetByUserName(uid).Person;
            Active = false;
            Save(uid);
        }

        public void Resubmit(string uid)
        {
            ApprovalStatusLUID = PendingApprovalStatusLUID();
            DateApproved = DateTime.MinValue;
            Save(uid);
        }

        public static int ApprovedStatusLUID()
        {
            return DefinedValueCache.Get(ApprovedStatusGUID).Id;
        }

        public static int ApprovedAndForwardLUID()
        {
            return DefinedValueCache.Get(ApprovedAndForwardedGUID).Id;
        }

        public static int NotApprovedStatusLUID()
        {
            return DefinedValueCache.Get(NotApprovedStatusGUID).Id;
        }

        public static int NotSubmittedStatusLUID()
        {
            return DefinedValueCache.Get(NotSubmittedGUID).Id;
        }

        public static int PendingApprovalStatusLUID()
        {
            return DefinedValueCache.Get(PendingApprovalStatusGUID).Id;
        }

        public static int RequisitionApprovalTypeLUID()
        {
            return DefinedValueCache.Get(RequisitionApprovalTypeGUID).Id;
        }

        public static int MinistryApprovalTypeLUID()
        {
            return DefinedValueCache.Get(MinistryApprovalTypeGUID).Id;
        }

        public static int FinanceApprovalTypeLUID()
        {
            return DefinedValueCache.Get(FinanceApprovalTypeGUID).Id;
        }

        public static int LeadTeamApprovalTypeLUID()
        {
            return DefinedValueCache.Get( LeadTeamApprovalTypeGUID ).Id;
        }


        public bool HasChanged()
        {
            Approval Original = new Approval(ApprovalID);
            bool Changed = false;
            if (this.GetHashCode() != Original.GetHashCode())
            {
                if ( !Changed && ObjectTypeName != Original.ObjectTypeName )
                    Changed = true;
                if ( !Changed && Identifier != Original.Identifier )
                    Changed = true;
                if (!Changed && ApproverID != Original.ApproverID)
                    Changed = true;
                if(!Changed && ApprovalStatusLUID != Original.ApprovalStatusLUID)
                    Changed = true;
                if (!Changed && DateApproved != Original.DateApproved)
                    Changed = true;
                if (!Changed && Active != Original.Active)
                    Changed = true;
                if (!Changed && OrganizationID != Original.OrganizationID)
                    Changed = true;
                if (!Changed && DateNotified != Original.DateNotified)
                    Changed = true;
                if ( !Changed && ApprovalTypeLUID != Original.ApprovalTypeLUID )
                    Changed = true;
            }

            return Changed;

        }

        public void RefreshNotes()
        {
            LoadNotes();
        }

        public void Save(string uid)
        {
            try
            {
                if (String.IsNullOrEmpty(uid))
                    throw new ArgumentNullException("uid", "User ID is required.");

                Dictionary<string, string> ValErrors = Validate();
                if (ValErrors.Count > 0)
                    throw new RequisitionNotValidException("Approval is not valid.", ValErrors);

                Approval Original = null;
                Enums.HistoryType ChangeType;

                using (PurchasingContext Context = ContextHelper.GetDBContext())
                {
                    ApprovalData Data;
                    if (ApprovalID > 0)
                    {
                        Data = Context.ApprovalDatas.FirstOrDefault(x => x.approval_id == ApprovalID);
                        Original = new Approval(Data);
                        ChangeType = Enums.HistoryType.UPDATE;
                    }
                    else
                    {
                        Data = new ApprovalData();
                        ChangeType = Enums.HistoryType.ADD;
                        Data.date_created = DateTime.Now;
                        Data.created_by = uid;
                    }

                    Data.object_type_name = ObjectTypeName;
                    Data.identifier = Identifier;

                    if ( ApproverID > 0 )
                    {
                        Data.approver_id = ApproverID;
                    }
                    else
                    {
                        Data.approver_id = null;
                    }

                    Data.approval_status_luid = ApprovalStatusLUID;

                    if (DateApproved == DateTime.MinValue)
                        Data.date_approved = null;
                    else
                        Data.date_approved = DateApproved;

                    if (DateNotified == DateTime.MinValue)
                        Data.date_notified = null;
                    else
                        Data.date_notified = DateNotified;

                    Data.organization_id = OrganizationID;
                    Data.active = Active;
                    Data.date_modified = DateTime.Now;
                    Data.modified_by = uid;
                    Data.approval_type_luid = ApprovalTypeLUID;

                    if (ApprovalID <= 0)
                        Context.ApprovalDatas.InsertOnSubmit(Data);

                    Context.SubmitChanges();
                    Load(Data);
                }

                SaveHistory(ChangeType, Original, uid);

            }
            catch (Exception ex)
            {
                throw new RequisitionNotValidException("An error has occurred while saving approval.", ex);
            }
        }


        public void SaveHistory(Enums.HistoryType ct, Approval original, string uid)
        {
            History h = new History();

            h.ObjectTypeName = this.GetType().ToString();
            h.Identifier = ApprovalID;
            h.ChangeType = ct;

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

            h.OrganizationID = this.OrganizationID;
            h.Save(uid);

        }

        #endregion

        #region Private

        private bool CanBeDeleted(int currentPersonId)
        {
            bool CanDelete = false;

            if (ApprovalID > 0)
            {
                if (ApprovalStatusLUID == PendingApprovalStatusLUID())
                {
                    if (CreatedBy != null && CreatedBy.Id == currentPersonId)
                    {
                        CanDelete = true;
                    }
                }
            }

            return CanDelete;
        }

        private void Init()
        {
            ApprovalID = 0;
            ObjectTypeName = null;
            Identifier = 0;
            ApproverID = 0;
     
            ApprovalStatusLUID = 0;
            CreatedByUserID = String.Empty;
            ModifiedByUserID = String.Empty;
            DateApproved = DateTime.MinValue;
            DateCreated = DateTime.MinValue;
            DateModified = DateTime.MinValue;
            DateNotified = DateTime.MinValue;
            Active = true;
            mApprover = null;
            mApprovalStatus = null;
            mApprovalType = null;
            mNotes = null;
            OrganizationID = 1;
        }

        private void Load(ApprovalData data)
        {
            Init();
            if (data != null)
            {
                ApprovalID = data.approval_id;
                ObjectTypeName = data.object_type_name;
                Identifier = data.identifier;

                if ( data.approver_id == null )
                {
                    ApproverID = 0;
                }
                else
                {
                    ApproverID = (int)data.approver_id;
                }

                ApprovalStatusLUID = data.approval_status_luid;
                CreatedByUserID = data.created_by;
                ModifiedByUserID = data.modified_by;

                if (data.date_approved != null)
                    DateApproved = (DateTime)data.date_approved;

                if (data.date_notified != null)
                    DateNotified = (DateTime)data.date_notified;

                DateCreated = data.date_created;
                DateModified = (DateTime)data.date_modified;
                Active = data.active;
                OrganizationID = data.organization_id;

                ApprovalTypeLUID = data.approval_type_luid;
            }
        }

        private void Load(int approvalId)
        {
            try 
	        {	        
		        using (PurchasingContext Context = ContextHelper.GetDBContext())
                {
                    Load( Context.ApprovalDatas.FirstOrDefault( a => a.approval_id == approvalId ) );
                }
	        }
	        catch (Exception ex)
	        {
		
		        throw new RequisitionException("An error has occurred while loading Requisition Approval.", ex);
	        }

        }

        private void LoadNotes()
        {
            mNotes = new List<Note>();

            using ( PurchasingContext context = ContextHelper.GetDBContext() )
            {
                mNotes.AddRange( context.NoteDatas
                            .Where( n => n.object_type_name == this.GetType().ToString() )
                            .Where( n => n.identifier == ApprovalID )
                            .Select( n => new Note( n ) ) );
            }
        }

        private Dictionary<string, string> Validate()
        {
            Dictionary<string, string> ValErrors = new Dictionary<string, string>();

            if ( String.IsNullOrWhiteSpace( ObjectTypeName ) )
            {
                ValErrors.Add( "Object Type Name", "The object type name is required." );
            }
            if (Identifier <= 0)
                ValErrors.Add("Identifier", "The identifier for the parent object is required.");
            if (ApproverID <= 0 && ApprovalTypeLUID <= 0)
                ValErrors.Add("Approver", "Approver or Approval Type must be selected.");
            if (ApprovalStatusLUID <= 0)
                ValErrors.Add("Approval Status", "Approval status must be selected.");
            else if (ApprovalStatus == null || GetApprovalStatus(false).Where(l => l.Id == ApprovalStatusLUID).Count() == 0)
                ValErrors.Add("Approval Status", "The selected approval status is not valid. Please select another approval status.");

            return ValErrors;
        }
        #endregion


        public string GetLastNote()
        {
            var lastNoteText = Notes.OrderByDescending( n => n.NoteID )
                                .Where( n => n.Active )
                                .Select( n => n.Body )
                                .FirstOrDefault();

            return lastNoteText;
        }
    }

    public class ApprovalListItem
    {
        public int ApprovalId { get; set; }
        public int ApprovalTypeLUID { get; set; }
        public string ApprovalTypeName { get; set; }
        public int? ApproverId { get; set; }
        public string ApproverFullName { get; set; }
        public int ApprovalStatusLUID { get; set; }
        public string ApprovalStatusName { get; set; }
        public string DateApprovedString { get; set; }
        public string LastComment { get; set; }
        public int CreatedByPersonId { get; set; }
    }

}
