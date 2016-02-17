using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Rock;
using org.secc.Purchasing.DataLayer;
using Rock.Model;


namespace org.secc.Purchasing
{
    public class Note : PurchasingBase
    {
        #region Fields
        private Person mCreatedBy;
        private Person mModifiedBy;
        #endregion

        #region Properties
        public int NoteID { get; set; }
        public string ObjectTypeName { get; set; }
        public int Identifier { get; set; }
        public string Body { get; set; }
        public string CreatedByUserID { get; set; }
        public string ModifiedByUserID { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }
        public bool Active { get; set; }
        public int OrganizationID { get; set; }

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
        #endregion 

        #region Constructors
        public Note() 
        {
            Init();
        }
        public Note(int id)
        {
            Load(id);
        }

        public Note(NoteData data)
        {
            Load(data);
        }
        #endregion

        #region Public
        public static List<Note> GetNotes(string objectName, int identifier, bool activeOnly)
        {
            try
            {
                if (String.IsNullOrEmpty(objectName))
                    throw new ArgumentNullException("objectName", "Object name is required.");
                if (identifier < 0)
                    throw new ArgumentNullException("identifier", "Identifier must be provided.");

                using ( PurchasingContext Context = ContextHelper.GetDBContext() )
                {
                    var NoteQuery = Context.NoteDatas
                                    .Where( n => n.object_type_name == objectName )
                                    .Where( n => n.identifier == identifier );

                    if ( activeOnly )
                    {
                        NoteQuery = NoteQuery.Where( n => n.active );
                    }

                    return NoteQuery.Select( n => new Note( n ) ).ToList();
                }


            }
            catch (Exception ex)
            {

                throw new RequisitionException("An error has occurred when loading notes.", ex);
            }
        }

        public void Save(string uid)
        {
            try
            {
                if (String.IsNullOrEmpty(uid))
                    throw new ArgumentNullException("UID", "User ID is required.");
                Note Original = null;
                Enums.HistoryType ChangeType;
                Dictionary<string, string> ValErrors = Validate();
                if (ValErrors.Count > 0)
                    throw new RequisitionNotValidException("Note is not valid.", ValErrors);

                using (PurchasingContext Context = ContextHelper.GetDBContext())
                {
                    NoteData data = null;

                    if (NoteID > 0)
                    {
                        data = Context.NoteDatas.FirstOrDefault(n => n.note_id == NoteID);
                        Original = new Note(data);
                        ChangeType = Enums.HistoryType.UPDATE;
                    }
                    else
                    {
                        data = new NoteData();
                        ChangeType = Enums.HistoryType.ADD;
                        data.created_by = uid;
                        data.date_created = DateTime.Now;
                    }

                    data.object_type_name = ObjectTypeName;
                    data.identifier = Identifier;
                    data.body = Body;
                    data.modified_by = uid;
                    data.date_modified = DateTime.Now;
                    data.active = Active;
                    data.organization_id = OrganizationID;

                    if (NoteID <= 0)
                        Context.NoteDatas.InsertOnSubmit(data);

                    Context.SubmitChanges();

                    Load(data);

                    SaveHistory(ChangeType, Original, uid);
                }
            }
            catch (Exception ex)
            {
               throw new RequisitionException("An error has occurred while saving note", ex);
            }
        }

        public void SaveHistory(Enums.HistoryType ht, Note original, string uid)
        {
            if(String.IsNullOrEmpty(uid))
                throw new ArgumentNullException("uid", "User ID (uid) is required to save note history.");

            History h = new History();
            h.ObjectTypeName = this.GetType().ToString();
            h.Identifier = NoteID;
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

        public static void Delete(int noteID, string uid)
        {
            //TODO: Validate Permissions
            try
            {
                if (noteID <= 0)
                    throw new ArgumentNullException("Note ID", "Note ID is required.");
                if (String.IsNullOrEmpty(uid))
                    throw new ArgumentNullException("UID", "User ID is required.");

                using (PurchasingContext Context = ContextHelper.GetDBContext())
                {
                    NoteData data = Context.NoteDatas.Where(x => x.note_id == noteID).FirstOrDefault();

                    if (data == null)
                        throw new RequisitionException("Note not found.");
                    Context.NoteDatas.DeleteOnSubmit(data);

                    Context.SubmitChanges();
                }

            }
            catch (Exception ex)
            {
                throw new RequisitionException("Unable to delete note.", ex);
            }
        }

        #endregion

        #region private
        private void Init()
        {
            NoteID = 0;
            ObjectTypeName = String.Empty;
            Identifier = 0;
            Body = String.Empty;
            CreatedByUserID = String.Empty;
            ModifiedByUserID = String.Empty;
            DateCreated = DateTime.MinValue;
            DateModified = DateTime.MinValue;
            Active = true;
            OrganizationID = 1;
            mCreatedBy = null;
            mModifiedBy = null;
        }

        private void Load(NoteData data)
        {
            Init();
            if (data != null)
            {
                NoteID = data.note_id;
                ObjectTypeName = data.object_type_name;
                Identifier = data.identifier;
                Body = data.body;
                CreatedByUserID = data.created_by;
                ModifiedByUserID = data.modified_by;
                DateCreated = data.date_created;
                DateModified = data.date_modified;
                Active = data.active;
                OrganizationID = data.organization_id;
            }
        }

        private void Load(int id)
        {
            try
            {
                using (PurchasingContext Context = ContextHelper.GetDBContext())
                {
                    Load( Context.NoteDatas.FirstOrDefault( n => n.note_id == id ) );
                }
            }
            catch (RequisitionException ex)
            {
                throw new RequisitionException("An error has occurred while loading notes.", ex);
            }
        }

        private Dictionary<string, string> Validate()
        {
            Dictionary<string, string> ValErrors = new Dictionary<string, string>();

            if (String.IsNullOrEmpty(ObjectTypeName))
                ValErrors.Add("ObjectTypeName", "Object Type Name is Required.");
            //else if (!base.TypeExists(ObjectTypeName))
            //    ValErrors.Add("ObjectTypeName", "Object not found.");
            if (Identifier <= 0)
                ValErrors.Add("Identifier", "Identifier is required and must be greater than 0");

            if(String.IsNullOrEmpty(Body))
                ValErrors.Add("Body", "Note body is required.");

            
            return ValErrors;
        }

        #endregion

    }
}
