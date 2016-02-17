using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;

using Rock.Model;
using Rock.Web.UI;
using Rock.Web.UI.Controls;
using org.secc.Purchasing.DataLayer;

namespace RockWeb.Plugins.org_secc.Purchasing
{
    public partial class StaffSearch : UserControl
    {
        #region Properties

        public string ParentPersonControlID
        {
            get
            {
                string controlID = String.Empty;
                if (ViewState["StaffSearch_ControlID"] != null)
                    controlID = ViewState["StaffSearch_ControlID"].ToString();
                return controlID;
            }
            set
            {
                ViewState["StaffSearch_ControlID"] = value;
            }
        }

        public string ParentRefreshButtonID
        {
            get
            {
                string buttonControlID = String.Empty;
                if (ViewState["StaffSearch_RefreshButtonID"] != null)
                    buttonControlID = ViewState["StaffSearch_RefreshButtonID"].ToString();
                return buttonControlID;
            }
            set
            {
                ViewState["StaffSearch_RefreshButtonID"] = value;
            }
        }
        public string Title
        {
            get
            {
                string title = String.Empty;
                if (ViewState["StaffSearch_Title"] != null)
                    title = ViewState["StaffSearch_Title"].ToString();
                return title;
            }
            set
            {
                ViewState["StaffSearch_Title"] = value;
            }
        }

        public bool AllowMultipleSelections
        {
            get
            {
                bool isAllowed = false;
                if (ViewState["StaffSearch_AllowMultipleSelections"] != null)
                    isAllowed = (bool)ViewState["StaffSearch_AllowMultipleSelections"];
                return isAllowed;
            }
            set
            {
                ViewState["StaffSearch_AllowMultipleSelections"] = value;
            }
        }


        public int MinistryAreaAttributeID
        {
            get
            {
                int attributeID = 3062;
                if (ViewState["StaffSearch_MinistryAreaAttributeID"] != null)
                    attributeID = (int)ViewState["StaffSearch_MinistryAreaAttributeID"];
                return attributeID;
            }
            set
            {
                ViewState["StaffSearch_MinistryAreaAttributeID"] = value;
            }
        }

        public int PositionAttributeID
        {
            get
            {
                int positionID = 29;
                if (ViewState["StaffSearch_PositionAttributeID"] != null)
                    positionID = (int)ViewState["StaffSearch_PositionAttributeID"];

                return positionID;
            }
            set
            {
                ViewState["StaffSearch_PositionAttributeID"] = value;
            }
        }

        public string Instructions
        {
            get
            {
                string inst = String.Empty;

                if (ViewState["StaffSearch_Instructions"] != null)
                    inst = ViewState["StaffSearch_Instructions"].ToString();

                return inst;
            }

            set
            {
                ViewState["StaffSearch_Instructions"] = value;
            }
        }

        public bool ShowPersonDetailLink
        {
            get
            {
                bool isShown = false;
                if (ViewState["StaffSearch_ShowPersonDetailLink"] != null)
                    isShown = (bool)ViewState["StaffSearch_ShowPersonDetailLink"];
                return isShown;
            }
            set
            {
                ViewState["StaffSearch_ShowPersonDetailLink"] = value;
            }
        }

        private StaffSearchMode Mode
        {
            get
            {
                StaffSearchMode mode = StaffSearchMode.Search;

                if (ViewState["StaffSearch_PageMode"] != null && ViewState["StaffSearch_PageMode"].GetType() == typeof(StaffSearchMode))
                    mode = (StaffSearchMode)ViewState["StaffSearch_PageMode"];

                return mode;
            }
            set
            {
                ViewState["StaffSearch_PageMode"] = value;
            }
        }

        private bool ShowPhoto
        {
            get
            {
                bool isShown = false;

                if (ViewState["StaffSearch_ShowPhoto"] != null)
                    isShown = (bool)ViewState["StaffSearch_ShowPhoto"];

                return isShown;
            }
        }

        #endregion 

        #region Page Events

        protected override void OnInit(EventArgs e)
        {
            
            base.OnInit(e);
        }
        protected void btnSubmit_Click(object sender, EventArgs e)
        {
            string CmdName = ((Button)sender).CommandName;

            switch (CmdName.ToLower())
            {
                case "search":
                    SearchStaff();
                    break;
                case "select":
                    SelectStaff();
                    break;
            }
        }

        protected void btnCancel_click(object sender, EventArgs e)
        {
            mpStaffSearch.Hide();
        }

        protected void btnClear_Click(object sender, EventArgs e)
        {
            switch (btnClear.CommandName.ToLower())
            {
                case "clear":
                    ResetSearchFields();
                    break;
                case "searchagain":
                    SetMode(StaffSearchMode.Search);
                    break;
            }
            SetMode(StaffSearchMode.Search);
        }

        protected void dgSearchResults_ReBind(object sender, EventArgs e)
        {
            BindStaffMemberList();
        }

        protected void dgSearchResults_ItemDataBound(object sender, DataGridItemEventArgs e)
        {
            if (e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem)
            {
                DataRowView dataItem = (DataRowView)e.Item.DataItem;
                PlaceHolder ph = (PlaceHolder)e.Item.FindControl("phPersonName");
                /*PersonLabel personLbl = new PersonLabel();
                personLbl.PersonName = dataItem["full_name_last_first"].ToString();
                personLbl.PersonGUID = dataItem["person_guid"].ToString();
                personLbl.ShowPhotoIcon = ShowPhoto;
                personLbl.Restricted = dataItem["restricted"] == DBNull.Value ? false : (bool)dataItem["restricted"];

                if (ShowPersonDetailLink)
                {
                    personLbl.PersonUrl = string.Format("/default.aspx?page={0}&guid={1}", PersonDetailPage, dataItem["person_guid"]);
                    personLbl.PersonUrlTarget = "_blank";
                }*/
                Label personLbl = new Label();
                personLbl.Text = dataItem["full_name_last_first"].ToString();

                ph.Controls.Add(personLbl);

                CheckBox ckBox = (CheckBox)e.Item.FindControl("chkItem");
                RadioButton rButton = (RadioButton)e.Item.FindControl("rdoItem");

                ckBox.Visible = AllowMultipleSelections;
                rButton.Visible = !AllowMultipleSelections;
                rButton.GroupName = "SelectPeople";

            }
            else if (e.Item.ItemType == ListItemType.Header)
            {
                CheckBox ckBoxHeader = (CheckBox)e.Item.FindControl("chkAllItems");
                ckBoxHeader.Visible = AllowMultipleSelections;
            }
        }
        #endregion

        #region Public
        public void Show()
        {

            Dictionary<string, string> ValErrors = ValidatePreReqs();
            if (ValErrors.Count > 0)
                throw new org.secc.Purchasing.RequisitionNotValidException("Staff Search Properties are not valid.", ValErrors);
            BindMinistryAreaList();
            SetMode(StaffSearchMode.Search);
            mpStaffSearch.Show();
        }
        #endregion

        #region Private
        private void BindMinistryAreaList()
        {
            ddlMinistry.Items.Clear();
            ddlMinistry.DataValueField = "Id";
            ddlMinistry.DataTextField = "Value";
            ddlMinistry.DataSource = GetMinistryAreas().OrderBy(x => x.Value);
            ddlMinistry.DataBind();

            if (ddlMinistry.Items.Count > 0)
            {
                ddlMinistry.Items.Insert(0, new ListItem("<All>", "0"));
                ddlMinistry.SelectedValue = "0";
            }
        }

        private void BindStaffMemberList()
        {
            InitStaffMemberList();
            dgSearchResults.DataSource = GetStaffMemberTable();
            dgSearchResults.DataBind();
        }

        private List<DefinedValue> GetMinistryAreas()
        {
            List<DefinedValue> MinistryAreas = new List<DefinedValue>();
            DefinedValueService definedValueService = new DefinedValueService(new Rock.Data.RockContext());
            return definedValueService.GetByDefinedTypeId(MinistryAreaAttributeID).ToList() ;
        }

        private string GetSelectedStaffPersonIDs()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            string Delim = ",";
            foreach (DataGridItem item in dgSearchResults.Rows)
            {
                if (item.ItemType == ListItemType.Item || item.ItemType == ListItemType.AlternatingItem)
                {
                    if (AllowMultipleSelections)
                    {
                        if (((CheckBox)item.FindControl("chkItem")).Checked)
                        {
                            sb.Append(item.Cells[0].Text + Delim);
                        }
                    }
                    else
                    {
                        if (((RadioButton)item.FindControl("rdoItem")).Checked)
                        {
                            sb.Append(item.Cells[0].Text + Delim);
                            break;
                        }

                    }
                }
            }

            string PersonIDs;

            if (String.IsNullOrEmpty(sb.ToString()))
                PersonIDs = sb.ToString();
            else
                PersonIDs = sb.ToString().Substring(0, sb.ToString().Length - 1);
            return PersonIDs;
        }

        private DataTable GetStaffMemberTable()
        {
            DataTable dt;
            int PersonID = 0;
            int MinistryLUID = 0;
            string Name1 = String.Empty;
            string Name2 = String.Empty;

            int.TryParse(ddlMinistry.SelectedValue, out MinistryLUID);

            if (ParseName(out PersonID, out Name1, out Name2))
            {
                if (PersonID > 0)
                    dt = new StaffMemberData().GetStaffMembersDTByPersonID(MinistryAreaAttributeID, PositionAttributeID, PersonID);
                else
                {
                    if (MinistryLUID > 0)
                        dt = new StaffMemberData().GetStaffMembersDTByNameMinistry(MinistryAreaAttributeID, PositionAttributeID, Name1, Name2, MinistryLUID);
                    else
                        dt = new StaffMemberData().GetStaffMembersDTByName(MinistryAreaAttributeID, PositionAttributeID, Name1, Name2);
                }
            }
            else
            {
                dt = new StaffMemberData().GetStaffMembersDTByMinistryID(MinistryAreaAttributeID, PositionAttributeID, MinistryLUID);
            }
            return dt;
        }

        private void InitStaffMemberList()
        {
            dgSearchResults.Visible = true;
            dgSearchResults.ItemType = "Staff Members";
            dgSearchResults.AllowSorting = true;
            /*dgSearchResults.NoResultText = "No staff members were found that matched your criteria.";
            dgSearchResults.MergeEnabled = false;
            dgSearchResults.EditEnabled = false;
            dgSearchResults.MailEnabled = false;
            dgSearchResults.AddEnabled = false;
            dgSearchResults.ExportEnabled = false;
            dgSearchResults.DeleteEnabled = false;*/

        }


        private bool ParseName(out int personID, out string name1, out string name2)
        {
            bool IsParsable = false;
            personID = 0;
            name1 = String.Empty;
            name2 = String.Empty;

            if (!String.IsNullOrEmpty(txtName.Text.Trim()))
            {
                if (!int.TryParse(txtName.Text, out personID))
                {
                    string[] NameParts = txtName.Text.Trim().Split(" ".ToCharArray());

                    if (NameParts.Length > 1)
                    {
                        name1 = NameParts[0];
                        System.Text.StringBuilder sbNameBuild2 = new System.Text.StringBuilder();
                        for (int i = 1; i < NameParts.Length; i++)
                        {
                            sbNameBuild2.Append(NameParts[i] + " ");
                        }

                        name2 = sbNameBuild2.ToString().Trim();
                    }
                    else
                    {
                        name1 = NameParts[0];
                    }

                    IsParsable = true;
                }
                else
                {
                    IsParsable = true;
                }
            }

            return IsParsable;

        }

        private void ResetSearchFields()
        {
            SetErrorMessage(String.Empty);
            txtName.Text = String.Empty;
            

            if (ddlMinistry.Items.FindByValue("0") != null)
                ddlMinistry.SelectedValue = "0";
        }

        private void SearchStaff()
        {
            if (!ValidateStaffFields())
                return;
            BindStaffMemberList();
            SetMode(StaffSearchMode.Results);
        }

        private void SelectStaff()
        {
            SetErrorMessage(String.Empty);

            string DelimitedPeopleIDs = GetSelectedStaffPersonIDs();

            if (String.IsNullOrEmpty(DelimitedPeopleIDs.Trim()))
            {
                return;
            }
            else
            {
                ScriptManager.RegisterStartupScript(upStaffSearchMain, upStaffSearchMain.GetType(),
                        "SelectStaff" + DateTime.Now.Ticks, string.Format("chooseStaffMembers(\"{0}\",\"{1}\",\"{2}\");", DelimitedPeopleIDs, ParentPersonControlID, ParentRefreshButtonID), true);
            }
        }

        private void SetErrorMessage(string msg)
        {
            lblError.Text = msg;
            lblError.Visible = !(String.IsNullOrEmpty(msg.Trim()));

        }
        private void SetMode(StaffSearchMode m)
        {
            Mode = m;
            bool ShowResults = Mode == StaffSearchMode.Results;
            switch (Mode)
            {
                case StaffSearchMode.Search:
                    lblInstructions.Text = Instructions;
                    ResetSearchFields();
                    txtName.Visible = true;
                    ddlMinistry.Visible = true;
                    btnSubmit.Text = "Search";
                    btnSubmit.CommandName = "Search";
                    btnClear.Text = "Clear";
                    btnClear.CommandName = "Clear";
                    break;
                case StaffSearchMode.Results:
                    if (AllowMultipleSelections)
                        lblInstructions.Text = "Choose Staff Members and click Select.";
                    else
                        lblInstructions.Text = "Choose Staff Member and click Select.";
                    txtName.Visible = false;
                    ddlMinistry.Visible = false;
                    btnSubmit.Text = "Select";
                    btnSubmit.CommandName = "Select";
                    btnClear.Text = "Search Again";
                    btnClear.CommandName = "SearchAgain";
                    btnCancel.Focus();
                   
                    break;
                default:
                    break;
            }

            pnlSearch.Visible = Mode == StaffSearchMode.Search;
            pnlResults.Visible = Mode == StaffSearchMode.Results;
          
        }

        private Dictionary<string, string> ValidatePreReqs()
        {
            Dictionary<string, string> ValErrors = new Dictionary<string, string>();

            if (String.IsNullOrEmpty(ParentPersonControlID))
                ValErrors.Add("ParentPersonControlID", "The Parent form's Person ID control ID must be provided.");
            if(String.IsNullOrEmpty(ParentRefreshButtonID))
                ValErrors.Add("ParentRefreshButtonID", "The parent form's Refresh Button ID must be provided.");
            if(MinistryAreaAttributeID <= 0)
                ValErrors.Add("MinistryAreaAttributeID", "Ministry Area Attribute ID must be greater than 0.");
            if(PositionAttributeID <= 0)
                ValErrors.Add("PositionAttributeID", "Position Attribute ID must be greater than 0.");

            return ValErrors;
        }

        private bool ValidateStaffFields()
        {
            bool isValid = true;
            if (String.IsNullOrEmpty(txtName.Text.Trim()) && ddlMinistry.SelectedIndex == 0)
            {
                SetErrorMessage("Either name or ministry must be selected.");
                isValid = false;
            }

            return isValid;
        }
        #endregion
    }

    internal enum StaffSearchMode
    {
        Search, 
        Results
    }
}