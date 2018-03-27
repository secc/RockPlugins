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
    public partial class StaffPicker : UserControl
    {
        #region Properties

        private bool mAllowMultipleSelections;
        public bool AllowMultipleSelections
        {
            get
            {
                return mAllowMultipleSelections;
            }
            set
            {
                mAllowMultipleSelections = value;
            }
        }

        private Guid? mMinistryAreaAttributeGuid;
        public Guid? MinistryAreaAttributeGuid
        {
            get
            {
                return mMinistryAreaAttributeGuid;
            }
            set
            {
                mMinistryAreaAttributeGuid = value;
            }
        }

        private Guid? mPositionAttributeGuid;
        public Guid? PositionAttributeGuid
        {
            get
            {
                return mPositionAttributeGuid;
            }
            set
            {
                mPositionAttributeGuid = value;
            }
        }

        private string mInstructions;
        public string Instructions
        {
            get
            {
                return mInstructions;
            }

            set
            {
                mInstructions = value;
            }
        }

        private string mTitle;
        public string Title
        {
            get
            {
                return mTitle;
            }

            set
            {
                mTitle = value;
            }
        }

        private string mDefaultLabel;
        public string DefaultLabel
        {
            get
            {
                return mDefaultLabel;
            }
            set
            { 
                mDefaultLabel = value;
            }
        }

        private bool mShowPersonDetailLink;
        public bool ShowPersonDetailLink
        {
            get
            {
                return mShowPersonDetailLink;
            }
            set
            {
                mShowPersonDetailLink = value;
            }
        }

        private StaffPickerMode mMode;
        private StaffPickerMode Mode
        {
            get
            {
                return mMode;
            }
            set
            {
                mMode = value;
            }
        }
        private PersonAlias mStaffPerson = null;
        public PersonAlias StaffPerson
        {
            get
            {
                if (mStaffPerson != null && mStaffPerson.Id == StaffPersonAliasId)
                {
                    return mStaffPerson;
                }
                else if (StaffPersonAliasId.HasValue) { 
                    PersonAliasService personAliasService = new PersonAliasService(new Rock.Data.RockContext());
                    mStaffPerson = personAliasService.Get(StaffPersonAliasId.Value);
                    return mStaffPerson;
                }
                return null;
            }
            set
            {
                mStaffPerson = value;
                if (value != null)
                {
                    StaffPersonAliasId = mStaffPerson.Id;
                    lblRequesterName.Text = mStaffPerson.Person.FullName;
                }
                else
                {
                    StaffPersonAliasId = null;
                    lblRequesterName.Text = DefaultLabel;
                }
            }
        }

        public int? StaffPersonAliasId
        {
            get
            {
                return hdnPersonAliasId.Value.AsIntegerOrNull();
            }
            set
            {
                hdnPersonAliasId.Value = value.ToString();
            }
        }

        public bool mUserCanEdit;
        public Boolean UserCanEdit
        {
            get
            {
                return mUserCanEdit;
            }
            set
            {
                mUserCanEdit = value;
                btnChangeRequester.Visible = value;
            }
        }

        private bool mShowPhoto;
        private bool ShowPhoto
        {
            get
            {
                return mShowPhoto;
            }
        }



        #endregion 

        #region Page Events

        protected override void OnInit(EventArgs e)
        {
            
            base.OnInit(e);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (StaffPerson != null)
            {
                lblRequesterName.Text = StaffPerson.Person.FullName;
                btnRemoveRequester.Visible = true;
            } else {
                lblRequesterName.Text = DefaultLabel;
                btnRemoveRequester.Visible = false;
            }
        }

        protected void btnSubmit_Click(object sender, EventArgs e)
        {
                SearchStaff();
            /*string CmdName = ((HtmlAnchor)sender).CommandName;

            switch (CmdName.ToLower())
            {
                case "search":
                    SearchStaff();
                    break;
                case "select":
                    SelectStaff();
                    break;
            }*/
        }

        protected void btnRemoveRequester_Click( object sender, EventArgs e )
        {
            StaffPerson = null;
            btnRemoveRequester.Visible = false;
        }

        protected void btnCancel_click(object sender, EventArgs e)
        {
            mpStaffSearch.Hide();
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
        // Defines the Click event.
        public event EventHandler Select;

        #endregion

        #region Public
        public void Show()
        {

            Dictionary<string, string> ValErrors = ValidatePreReqs();
            if (ValErrors.Count > 0)
                throw new org.secc.Purchasing.RequisitionNotValidException("Staff Search Properties are not valid.", ValErrors);
            BindMinistryAreaList();
            SetMode(StaffPickerMode.Search);
            mpStaffSearch.Show();
            dgSearchResults.Visible = false;
            
        }
        #endregion

        #region Private
        private void BindMinistryAreaList()
        {
            ddlMinistry.Items.Clear();
            ddlMinistry.DataValueField = "Guid";
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
            dgSearchResults.Visible = true;
            InitStaffMemberList();
            dgSearchResults.DataSource = GetStaffMemberTable();
            dgSearchResults.DataBind();
        }

        private List<DefinedValue> GetMinistryAreas()
        {
            List<DefinedValue> MinistryAreas = new List<DefinedValue>();
            DefinedValueService definedValueService = new DefinedValueService(new Rock.Data.RockContext());
            AttributeService attributeService = new AttributeService(new Rock.Data.RockContext());
            Rock.Model.Attribute ministryArea = attributeService.Get(MinistryAreaAttributeGuid.Value);

            return definedValueService.GetByDefinedTypeId(ministryArea.AttributeQualifiers.Where(aq => aq.Key == "definedtype").Select(aq => aq.Value).FirstOrDefault().AsInteger()).ToList();
        }

        private string GetSelectedStaffPersonIDs()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            string Delim = ",";
            foreach (GridViewRow item in dgSearchResults.Rows)
            {
                if (item.RowType == DataControlRowType.DataRow)
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
            Guid? MinistryLUID;
            string Name1 = String.Empty;
            string Name2 = String.Empty;

            AttributeService attributeService = new AttributeService(new Rock.Data.RockContext());
            int ministryAreaAttributeID = 0;
            int positionAttributeID = 0;
            if (MinistryAreaAttributeGuid.HasValue) {
                ministryAreaAttributeID = attributeService.Get(MinistryAreaAttributeGuid.Value).Id;
            }
            if (PositionAttributeGuid.HasValue)
            {
                positionAttributeID = attributeService.Get(PositionAttributeGuid.Value).Id;
            }
            MinistryLUID = ddlMinistry.SelectedValue.AsGuidOrNull();

            if (ParseName(out PersonID, out Name1, out Name2))
            {
                if (PersonID > 0)
                    dt = new StaffMemberData().GetStaffMembersDTByPersonID(ministryAreaAttributeID, positionAttributeID, PersonID);
                else
                {
                    if (MinistryLUID.HasValue)
                        dt = new StaffMemberData().GetStaffMembersDTByNameMinistry(ministryAreaAttributeID, positionAttributeID, Name1, Name2, MinistryLUID);
                    else
                        dt = new StaffMemberData().GetStaffMembersDTByName(ministryAreaAttributeID, positionAttributeID, Name1, Name2);
                }
            }
            else
            {
                dt = new StaffMemberData().GetStaffMembersDTByMinistryID(ministryAreaAttributeID, positionAttributeID, MinistryLUID);
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
            SetMode(StaffPickerMode.Results);
        }

        private void SetErrorMessage(string msg)
        {
            lblError.Text = msg;
            lblError.Visible = !(String.IsNullOrEmpty(msg.Trim()));
            error.Visible = !(String.IsNullOrEmpty(msg.Trim()));

        }
        private void SetMode(StaffPickerMode m)
        {
            Mode = m;
            bool ShowResults = Mode == StaffPickerMode.Results;
            switch (Mode)
            {
                case StaffPickerMode.Search:
                    lblInstructions.Text = Instructions;
                    ResetSearchFields();
                    break;
                case StaffPickerMode.Results:
                    if (AllowMultipleSelections)
                        lblInstructions.Text = "Choose Staff Members and click Select.";
                    else
                        lblInstructions.Text = "Choose Staff Member and click Select.";
                   
                    break;
                default:
                    break;
            }
          
        }

        private Dictionary<string, string> ValidatePreReqs()
        {
            Dictionary<string, string> ValErrors = new Dictionary<string, string>();
            if(!MinistryAreaAttributeGuid.HasValue)
                ValErrors.Add("MinistryAreaAttributeGuid", "Ministry Area Attribute Guid must have a value.");
            if (!PositionAttributeGuid.HasValue)
                ValErrors.Add("PositionAttributeGuid", "Position Attribute Guid must have a value.");

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

        protected void SelectButton_Click(object sender, RowEventArgs e)
        {

            PersonAliasService personAliasService = new PersonAliasService(new Rock.Data.RockContext());
            StaffPerson = personAliasService.Get(e.RowKeyValue.ToString().AsInteger());
            lblRequesterName.Text = StaffPerson.Person.FullName;
            mpStaffSearch.Hide();
            btnRemoveRequester.Visible = true;
            EventHandler handler = this.Select;
            if (handler != null)
            {
                handler(this, e);
            }
        }
        protected void btnChangeRequester_Click(object sender, EventArgs e)
        {
            Show();
            //ShowStaffSelector("Select Requester", ihPersonList.ClientID, btnRefresh.ClientID);

        }
        #endregion

    }

    internal enum StaffPickerMode
    {
        Search, 
        Results
    }
}