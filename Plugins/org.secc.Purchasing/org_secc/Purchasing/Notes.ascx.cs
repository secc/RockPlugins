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
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using org.secc.Purchasing;
using Rock;
using Rock.Web.UI;

namespace RockWeb.Plugins.org_secc.Purchasing
{
    public partial class Notes : UserControl
    {
        public int Identifier
        {
            get
            {
                int ID = 0;
                if (ViewState["uc_Notes_NoteTypeIdentifier"] != null)
                    ID = (int)ViewState["uc_Notes_NoteTypeIdentifier"];
                return ID;
            }
            set
            {
                ViewState["uc_Notes_NoteTypeIdentifier"] = value;
            }
        }

        public bool AllowCancel
        {
            get
            {
                bool allowCancel = false;
                if (ViewState["ucNotes_AllowCancel"] != null)
                    allowCancel = (bool)ViewState["ucNotes_AllowCancel"];
                return allowCancel;
            }
            set
            {
                ViewState["ucNotes_AllowCancel"] = value;
            }
        }

        //public bool AllowEdit
        //{
        //    get
        //    {
        //        bool allowEdit = false;
        //        if ( ViewState["ucNotes_AllowEdit"] != null )
        //        {
        //            allowEdit = (bool)ViewState["ucNotes_AllowEdit"];
        //        }
        //        return allowEdit;
        //    }
        //    set
        //    {
        //        ViewState["ucNotes_AllowEdit"] = value;
        //    }
        //}

        public string Callback
        {
            get
            {
                string callbackName = String.Empty;
                if (ViewState["ucNotes_Callback"] != null)
                    callbackName = ViewState["ucNotes_Callback"].ToString();
                return callbackName;
            }
            set
            {
                ViewState["ucNotes_Callback"] = value;
            }
        }

        public string CancelButtonText
        {
            get
            {
                string cancelText = String.Empty;
                if ( ViewState["ucNotes_CancelButtonText"] != null )
                {
                    cancelText = ViewState["ucNotes_CancelButtonText"].ToString();
                }

                if ( String.IsNullOrEmpty( cancelText ) )
                {
                    cancelText = "Cancel";
                }

                return cancelText;
            }
            set
            {
                ViewState["ucNotes_CancelButtonText"] = value;

                if ( string.IsNullOrEmpty( value ) )
                {
                    btnCancel.Text = "Cancel";
                }
                else
                {
                    btnCancel.Text = value;
                }
            }
        }

        public string Instructions
        {
            get
            {
                string instructions = String.Empty;
                if (ViewState["ucNotes_Instructions"] != null)
                    instructions = ViewState["ucNotes_Instructions"].ToString();
                return instructions;
            }
            set
            {
                ViewState["ucNotes_Instructions"] = value;
                lblInstruction.Text = value;
                lblInstruction.Visible = !String.IsNullOrEmpty(value);
            }
        }

        public string NotePrefix
        {
            get
            {
                string prefix = String.Empty;
                if (ViewState["ucNotes_NotePrefix"] != null)
                    prefix = ViewState["ucNotes_NotePrefix"].ToString();
                return prefix;
            }
            set
            {
                ViewState["ucNotes_NotePrefix"] = value;
            }
        }

        public string ObjectTypeName
        {
            get
            {
                string otName = String.Empty;
                if (ViewState["uc_Notes_ObjectTypeName"] != null)
                    otName = ViewState["uc_Notes_ObjectTypeName"].ToString();
                return otName;
            }
            set
            {
                ViewState["uc_Notes_ObjectTypeName"] = value;
            }
        }

        public bool ReadOnly
        {
            get
            {
                bool readOnly = false;

                if (ViewState["ucNotes_ReadOnly"] != null)
                    bool.TryParse(ViewState["ucNotes_ReadOnly"].ToString(), out readOnly);

                return readOnly;
            }
            set
            {
                ViewState["ucNotes_ReadOnly"] = value;

            }
        }

        public bool UserHasParentEditPermission
        {
            get
            {
                bool userCanEditParent = false;

                if ( ViewState["ucNotes_UserCanEditParent"] != null )
                {
                    bool.TryParse( ViewState["ucNotes_UserCanEditParent"].ToString(), out userCanEditParent );
                }

                return userCanEditParent;
            }
            set
            {
                ViewState["ucNotes_UserCanEditParent"] = value;
            }
        }

        public String CurrentUserName
        {
            get
            {
                if (ViewState["ucNotes_CurrentUserName"] != null)
                {
                    return ViewState["ucNotes_CurrentUserName"].ToString();
                }
                return null;
            }
            set
            {
                ViewState["ucNotes_CurrentUserName"] = value;
            }
        }
        #region Page Events


        protected void Page_Load(object sender, EventArgs e)
        {
          if (!Page.IsPostBack)
                ResetVariableProperties();
        }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            if (SaveNote())
            {
                int NoteID = 0;

                int.TryParse(hfNoteID.Value, out NoteID);
                ClearNoteDetail();
                mpNoteDetails.Hide();
                BindNoteList();

                RefreshParent(sender, e);

                if (!String.IsNullOrEmpty(Callback))
                {
                    Callback = Callback.Replace("##NoteID##", NoteID.ToString());
                    ScriptManager.RegisterStartupScript(upNoteDetails, upNoteDetails.GetType(), "NoteDetailCallback" + DateTime.Now.Ticks, Callback, true);

                }

            }
        }

        protected void btnCancel_Click(object sender, EventArgs e)
        {
            ClearNoteDetail();
            mpNoteDetails.Hide();
        }

        protected void btnReset_Click(object sender, EventArgs e)
        {
            ResetPopupFields();
        }

        protected void dgNote_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                DataRowView drv = (DataRowView)e.Row.DataItem;
                LinkButton lbHide = (LinkButton)e.Row.FindControl("lbHide");
                LinkButton lbEdit = (LinkButton)e.Row.FindControl("lbEdit");

                bool IsEditable = UserCanEditItem(drv["CreatedByUser"].ToString());
                lbHide.CommandArgument = drv["NoteID"].ToString();
                lbHide.Visible = IsEditable;

                lbEdit.CommandArgument = drv["NoteID"].ToString();
                lbEdit.Visible = IsEditable;
                
            }
        }

        protected void dgNote_ItemCommand(object sender, CommandEventArgs e)
        {
            int noteID = 0;
                int.TryParse(e.CommandArgument.ToString(), out noteID);

            if (noteID <= 0)
                return;

            switch (e.CommandName.ToLower())
            {
                case "hidenote":
                    HideNote(noteID);
                    RefreshParent(sender, e);
                    break;
                case "editnote":
                    ShowNoteDetail( noteID );
                    break;
                default:
                    break;
            }
        }


        #endregion

        #region Public
        public void LoadNoteList(string otn, int identifier)
        {
            ObjectTypeName = otn;
            Identifier = identifier;
            BindNoteList();
        }


        public void ResetVariableProperties()
        {
            Callback = String.Empty;
            CancelButtonText = String.Empty;
            Instructions = String.Empty;
            AllowCancel = true;
            NotePrefix = String.Empty;
        }
        public void ShowNoteDetail()
        {
            ShowNoteDetail(0);
            btnCancel.Visible = AllowCancel;
        }

        public event EventHandler RefreshParent;

        #endregion

        #region Private
        private void BindNoteList()
        {

            DataTable dt = new DataTable();
            dt.Columns.AddRange(new DataColumn[] {
            new DataColumn("NoteID"),
            new DataColumn("Body"),
            new DataColumn("CreatedBy"),
            new DataColumn("CreatedByUser"),
            new DataColumn("ModifiedBy"),
            new DataColumn("ModifiedByUser"),
            new DataColumn("LastModifiedDate"),
            new DataColumn("Active")
        });

            if (Identifier > 0)
            {
                var Items = Note.GetNotes(ObjectTypeName, Identifier, true).Select(n =>
                        new
                        {
                            NoteID = n.NoteID,
                            Body = n.Body,
                            CreatedBy = n.CreatedBy == null || n.CreatedBy.PrimaryAliasId <= 0 ? n.CreatedByUserID : n.CreatedBy.FullName,
                            CreatedByUser = n.CreatedByUserID,
                            LastModifiedDate = n.DateModified,
                            ModifiedBy = n.ModifiedBy == null || n.ModifiedBy.PrimaryAliasId <= 0 ? n.ModifiedByUserID : n.ModifiedBy.FullName,
                            ModifiedByUser = n.ModifiedByUserID,
                            Active = n.Active
                        }).OrderBy(n => n.NoteID);



                foreach (var i in Items)
                {
                    dt.Rows.Add(new object[] {
                i.NoteID,
                i.Body,
                i.CreatedBy,
                i.CreatedByUser,
                i.ModifiedBy, 
                i.ModifiedByUser,
                String.Format("{0:g}", i.LastModifiedDate), 
                i.Active
            });
                }
            }
            dgNote.DataSource = dt;
            dgNote.DataBind();


        }

        private void ClearNoteDetail()
        {
            SetErrorMessage(String.Empty);
            hfNoteID.Value = String.Empty;
            txtNote.Text = String.Empty;
        }

        private void HideNote(int noteID)
        {
            if (noteID <= 0)
                return;

            Note n = new Note(noteID);

            n.Active = false;

            n.Save(CurrentUserName);
            BindNoteList();
        }

        private void LoadNote(int noteID)
        {
            if (noteID <= 0)
                return;

            Note n = new Note(noteID);
            if (n.NoteID > 0)
            {
                hfNoteID.Value = n.NoteID.ToString();
                txtNote.Text = n.Body;

            }
        }

        private void ResetPopupFields()
        {
            int noteID = 0;
            int.TryParse(hfNoteID.Value, out noteID);

            ClearNoteDetail();
            LoadNote(noteID);

        }

        private bool SaveNote()
        {
            try
            {
                SetErrorMessage(String.Empty);
                int noteID = 0;
                int.TryParse(hfNoteID.Value, out noteID);

                Note n = null;
                if (noteID > 0)
                    n = new Note(noteID);
                else
                    n = new Note();

                n.ObjectTypeName = ObjectTypeName;
                n.Identifier = Identifier;
                if (!String.IsNullOrEmpty(NotePrefix) && !String.IsNullOrEmpty(txtNote.Text.Trim()))
                    n.Body = NotePrefix + " " + txtNote.Text;
                else
                    n.Body = txtNote.Text.Trim();
                DateTime DisplayAtTopExpire = DateTime.MinValue;
                n.Active = true;

                n.Save(CurrentUserName);

                ClearNoteDetail();
                LoadNote(n.NoteID);
                return true;
            }
            catch (RequisitionException rEx)
            {
                if (rEx.InnerException.GetType() == typeof(RequisitionNotValidException))
                {
                    Dictionary<string, string> ValErrors = ((RequisitionNotValidException)rEx.InnerException).InvalidProperties;
                    if (ValErrors.Count > 0)
                    {
                        System.Text.StringBuilder sb = new System.Text.StringBuilder();
                        foreach (var item in ValErrors)
                        {
                            sb.AppendFormat("<strong>{0}</strong> - {1}<br />", item.Key, item.Value);
                        }

                        SetErrorMessage(sb.ToString());
                    }
                }
                else
                {
                    throw rEx;
                }

                return false;
            }
        }


        private void SetErrorMessage(string msg)
        {
            lblPopupError.Text = msg;
            lblPopupError.Visible = !String.IsNullOrEmpty(msg);
            popupError.Visible = !String.IsNullOrEmpty(msg);
        }

        private void SetNoteEditibility(bool isEditiable)
        {
            if (isEditiable && ReadOnly)
                isEditiable = false;

            txtNote.ReadOnly = !isEditiable;

            btnSave.Visible = isEditiable;
            btnReset.Visible = isEditiable;

            bool ro = !isEditiable;
            //ScriptManager.RegisterClientScriptBlock(this.upNoteDetails, this.upNoteDetails.GetType(), "NoteEditablity" + DateTime.Now.Ticks.ToString(), "setReadOnlyClass(" + ro.ToString().ToLower() + ");", true);

        }

        private void ShowNoteDetail(int noteID)
        {
            ClearNoteDetail();
            if (noteID > 0)
                LoadNote(noteID);
            SetNoteEditibility(true);
            mpNoteDetails.Show();
        }


        private bool UserCanEditItem(string createdByUid)
        {
            
            if (ReadOnly)
                return !ReadOnly;

            return (createdByUid == CurrentUserName || UserHasParentEditPermission );

        }

        #endregion
    }
}