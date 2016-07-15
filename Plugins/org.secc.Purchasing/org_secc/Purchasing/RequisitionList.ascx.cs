using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;
using org.secc.Purchasing;

namespace RockWeb.Plugins.org_secc.Purchasing
{
    [DisplayName("Requisition List")]
    [Category("SECC > Purchasing")]
    [Description("Lists all requisitions.")]
    [LinkedPage("Requisition Detail Page", "Requisition Detail Page.", true)]
    [AttributeField(Rock.SystemGuid.EntityType.PERSON, "Ministry Location Person Attribute", "The person attribute that stores the user's Location.", false, false, null, "Staff Selector")]
    [AttributeField(Rock.SystemGuid.EntityType.PERSON, "Ministry Area Person Attribute", "The person attribute that stores the user's Ministry Area.", false, false, null, "Staff Selector")]
    [AttributeField(Rock.SystemGuid.EntityType.PERSON, "Position Person Attribute", "The person attribute that stores the user's job position.", false, false, null, "Staff Selector")]
    public partial class RequisitionList : RockBlock
    {
        private string PersonSettingsKeyPrefix = "RequisitionList";
        private DefinedTypeService definedTypeService = new DefinedTypeService(new RockContext());
        #region Module Settings

        public String RequisitionDetailPageSetting { 
            get {
                if (!String.IsNullOrEmpty(GetAttributeValue("RequisitionDetailPage")))
                {
                    PageService pageService = new PageService(new Rock.Data.RockContext());
                    return "~/page/"+pageService.Get(new Guid(GetAttributeValue("RequisitionDetailPage"))).Id;
                }
                return null;
            }
        }

        public Guid MinistryLocationAttributeIDSetting
        {
            get
            {
                return GetAttributeValue("MinistryLocationPersonAttribute").AsGuid();
            }
        }

        public Guid MinistryAreaAttributeIDSetting
        {
            get
            {
                return GetAttributeValue("MinistryAreaPersonAttribute").AsGuid();
            }
        }

        public Guid PositionAttributeIDSetting
        {
            get
            {
                return GetAttributeValue("PositionPersonAttribute").AsGuid();
            }
        }

        #endregion

        #region Page Events
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                BindStatusCheckboxList();
                BindLocationList();
                BindMinistryList();
                BindRequisitionTypeCheckboxList();
                LoadFilterOptions(UserCanEdit);
                LoadUserFilterSettings();
                BindRequisitionGrid();
            }
            dgRequisitions.GridRebind += dgRequisitions_ReBind;
        }

        protected void btnFilterSubmittedBySelect_Click(object sender, EventArgs e)
        {
            ShowStaffSearch();
        }

        protected void btnFilterApply_Click(object sender, EventArgs e)
        {
            SaveUserFilterSettings();
            BindRequisitionGrid();
        }

        protected void btnFilterClear_Click(object sender, EventArgs e)
        {
            ResetFilters();
        }
        protected void dgRequisitions_ItemCommand(object source, DataGridCommandEventArgs e)
        {
           
        }

        protected void dgRequisitions_ReBind(object sender, EventArgs e)
        {
            BindRequisitionGrid();
        }

        protected void dgRequisitions_AddItem(object sender, EventArgs e)
        {
            RedirectToRequisitionDetail();
        }

        protected void lbCreateRequisition_Click(object sender, EventArgs e)
        {
            RedirectToRequisitionDetail();
        }

        protected void lbRemoveSubmittedBy_Click(object sender, EventArgs e)
        {
            ClearSubmittedByFilter();
        }

        #endregion

        #region Private
        private void BindLocationList()
        {
            AttributeService attributeService = new AttributeService(new RockContext());

            Rock.Model.Attribute locationAttribute = attributeService.Get(MinistryLocationAttributeIDSetting);
            
            if ( locationAttribute != null )
            {
                ddlLocation.Items.Clear();
                ddlLocation.DataSource = definedTypeService.Get( locationAttribute.AttributeQualifiers.Where( aq => aq.Key == "definedtype" ).FirstOrDefault().Value.AsInteger() ).DefinedValues.OrderBy( l => l.Order );
                ddlLocation.DataTextField = "Value";
                ddlLocation.DataValueField = "Id";
                ddlLocation.DataBind();

                ddlLocation.Items.Insert( 0, new ListItem( "<All>", "0" ) );
                ddlLocation.SelectedValue = "0";
            }
        }
        private void BindMinistryList()
        {
            AttributeService attributeService = new AttributeService(new RockContext());

            Rock.Model.Attribute ministryAttribute = attributeService.Get(MinistryAreaAttributeIDSetting);

            if (ministryAttribute != null)
            {
                
                ddlMinistry.Items.Clear();
                ddlMinistry.DataSource = definedTypeService.Get( ministryAttribute.AttributeQualifiers.Where( aq => aq.Key == "definedtype" ).FirstOrDefault().Value.AsInteger() ).DefinedValues.OrderBy(l => l.Value);
                ddlMinistry.DataTextField = "Value";
                ddlMinistry.DataValueField = "Id";
                ddlMinistry.DataBind();

                ddlMinistry.Items.Insert(0, new ListItem("<All>", "0"));
                ddlMinistry.SelectedValue = "0";
            }
        }

        private void BindRequisitionTypeCheckboxList()
        {
            cbListType.Items.Clear();
            cbListType.DataSource = Requisition.GetRequisitionTypes(true).OrderBy(t => t.Order);
            cbListType.DataTextField = "Value";
            cbListType.DataValueField = "Id";
            cbListType.DataBind();
        }

        private void BindStatusCheckboxList()
        {
            cbListStatus.Items.Clear();
            cbListStatus.DataSource = Requisition.GetStatuses(true);
            cbListStatus.DataTextField = "Value";
            cbListStatus.DataValueField = "Id";
            cbListStatus.DataBind();

        }

        private void BindRequisitionGrid()
        {
            ConfigureRequisitionGrid();
            List<RequisitionListItem> Requisitions = GetRequisitions();

            SortProperty sortProperty = dgRequisitions.SortProperty;
            if ( sortProperty != null )
            {
                if ( sortProperty.Direction == SortDirection.Ascending )
                {
                    Requisitions = Requisitions.OrderBy( r => r.GetType().GetProperty(sortProperty.Property).GetValue(r)  ).ToList();
                } else
                {
                    Requisitions = Requisitions.OrderByDescending( r => r.GetType().GetProperty( sortProperty.Property ).GetValue( r ) ).ToList();
                }
            }
            else
            {
                Requisitions = Requisitions.OrderByDescending( r => r.DateSubmitted ).ToList();
            }

            DataTable dt = new DataTable( "Requisitions" );

            dt.Columns.AddRange(
                    new DataColumn[] {
                        new DataColumn("RequisitionID", typeof(int)),
                        new DataColumn("Title", typeof(string)),
                        new DataColumn("RequesterID", typeof(int)),
                        new DataColumn("Requester_Last_First", typeof(string)),
                        new DataColumn("Status", typeof(string)),
                        new DataColumn("RequisitionType", typeof(string)),
                        new DataColumn("ItemCount", typeof(int)),
                        new DataColumn("NoteCount", typeof(int)),
                        new DataColumn("AttachmentCount", typeof(int)),
                        new DataColumn("DateSubmitted", typeof(DateTime)),
                        new DataColumn("IsApproved", typeof(bool)),
                        new DataColumn("IsAccepted", typeof(bool))
                    } );

            foreach ( RequisitionListItem item in Requisitions )
            {
                DataRow dr = dt.NewRow();

                dr["RequisitionID"] = item.RequisitionID;
                dr["Title"] = item.Title;
                dr["RequesterID"] = item.RequesterID;
                dr["Requester_Last_First"] = item.RequesterLastFirst;
                dr["Status"] = item.Status;
                dr["RequisitionType"] = item.RequisitionType;
                dr["ItemCount"] = item.ItemCount;
                dr["NoteCount"] = item.NoteCount;
                dr["AttachmentCount"] = item.AttachmentCount;
                if ( item.DateSubmitted != null )
                    dr["DateSubmitted"] = item.DateSubmitted;
                dr["IsApproved"] = item.IsApproved;
                dr["IsAccepted"] = item.IsAccepted;

                dt.Rows.Add( dr );
            }


            dgRequisitions.DataSource = dt;
            dgRequisitions.DataBind();

        }

        private Dictionary<string, string> BuildFilter()
        {
            Dictionary<string, string> Filter = new Dictionary<string, string>();

            System.Text.StringBuilder StatusSB = new System.Text.StringBuilder();

            foreach (ListItem item in cbListStatus.Items)
            {
                if (item.Selected)
                    StatusSB.Append(item.Value + ",");
            }
            StatusSB.Append("0");
            Filter.Add("StatusLUID", StatusSB.ToString());

            System.Text.StringBuilder RequisitionTypeSB = new System.Text.StringBuilder();
            foreach (ListItem item in cbListType.Items)
            {
                if (item.Selected)
                    StatusSB.Append(item.Value + ",");
            }
            StatusSB.Append("0");
            Filter.Add("TypeLUID", StatusSB.ToString());

            foreach (ListItem item in cbShow.Items)
            {
                Filter.Add(string.Format("Show_{0}", item.Value), item.Selected.ToString());
            }

            int poNumber;
            if (int.TryParse(txtPONumber.Text, out poNumber) && poNumber > 0)
            {
                Filter.Add("PONumber", poNumber.ToString());
            }

            if (txtFilterSubmitted.LowerValue.HasValue) {
                Filter.Add("SubmitOnStart", txtFilterSubmitted.LowerValue.Value.ToShortDateString());
            }

            if (txtFilterSubmitted.UpperValue.HasValue)
            {
                Filter.Add("SubmitOnEnd", txtFilterSubmitted.UpperValue.Value.ToShortDateString());
            }

            if (hfFilterSubmittedBy.Visible && hfFilterSubmittedBy.PersonAliasId.HasValue)
                Filter.Add("RequesterID", hfFilterSubmittedBy.PersonAliasId.ToString());

            int MinistryID = 0;
            if (ddlMinistry.Visible && int.TryParse(ddlMinistry.SelectedValue, out MinistryID))
                Filter.Add("MinistryLUID", MinistryID.ToString());

            int LocationID = 0;
            if (ddlLocation.Visible && int.TryParse(ddlLocation.SelectedValue, out LocationID))
                Filter.Add( "LocationLUID", LocationID.ToString() );

            bool ShowInactive = false;
            if (chkShowInactive.Visible)
                ShowInactive = chkShowInactive.Checked;

            Filter.Add("ShowInactive", ShowInactive.ToString());


            Filter.Add("PersonID", CurrentPerson.Id.ToString());
            Filter.Add("UserName", CurrentUser.UserName);
            AttributeService attributeService = new AttributeService(new RockContext());
            Rock.Model.Attribute ministryAttribute = attributeService.Get(MinistryAreaAttributeIDSetting);
            if (ministryAttribute != null)
            {
                DefinedValue ministryValue = org.secc.Purchasing.Helpers.Person.GetMyMinistryLookup(CurrentPerson.Id, ministryAttribute.Key);
                if (ministryValue != null) {
                    Filter.Add("MyMinistryID", ministryValue.Id.ToString());
                }
            }

            Rock.Model.Attribute locationAttribute = attributeService.Get(MinistryAreaAttributeIDSetting);
            if (locationAttribute != null)
            {
                DefinedValue locationValue = org.secc.Purchasing.Helpers.Person.GetMyMinistryLookup(CurrentPerson.Id, locationAttribute.Key);
                if (locationValue != null)
                {
                    Filter.Add( "MyLocationID", locationValue.Id.ToString());
                }
            }
            return Filter;

        }

        private void ClearFilters()
        {
            foreach (ListItem item in cbListStatus.Items)
            {
                if (item.Selected)
                    item.Selected = false;
            }

            foreach (ListItem item in cbShow.Items)
            {
                if (item.Text == "Me")
                    item.Selected = true;
                else
                    item.Selected = false;
            }

            txtFilterSubmitted.LowerValue = null;
            txtFilterSubmitted.UpperValue = null;

            ClearSubmittedByFilter();

            chkShowInactive.Checked = false;

            ddlMinistry.SelectedValue = "0";
            ddlLocation.SelectedValue = "0";
        }

        private void ClearSubmittedByFilter()
        {
            hfFilterSubmittedBy.PersonId = null;
            /*lblFilterSubmittedBy.Text = "(any)";
            lbRemoveSubmittedBy.Visible = false;
            */
        }

        private void ConfigureRequisitionGrid()
        {
            dgRequisitions.Visible = true;

            dgRequisitions.ItemType = "Items";
            dgRequisitions.AllowSorting = true;
            /*dgRequisitions.ItemBgColor = CurrentPortalPage.Setting("ItemBgColor", string.Empty, false);
            dgRequisitions.ItemAltBgColor = CurrentPortalPage.Setting("ItemAltBgColor", string.Empty, false);
            dgRequisitions.ItemMouseOverColor = CurrentPortalPage.Setting("ItemMouseOverColor", string.Empty, false);
            dgRequisitions.NoResultText = "No Requisitions Found";
            dgRequisitions.MergeEnabled = false;
            dgRequisitions.EditEnabled = false;
            dgRequisitions.MailEnabled = false;
            dgRequisitions.AddEnabled = false;
            dgRequisitions.DeleteEnabled = false;
            dgRequisitions.SourceTableKeyColumnName = "RequisitionID";*/
        }

        private List<RequisitionListItem> GetRequisitions()
        {

            List<RequisitionListItem> Requisitions = new List<RequisitionListItem>();


            if (GetUserPreferences("").Where(s => s.Key.Contains(PersonSettingsKeyPrefix)).Count() > 0)
            {
                Requisitions.AddRange(Requisition.GetRequisitionList(BuildFilter()));
            }

            return Requisitions;
        }

        private void LoadFilterOptions(bool isEditor)
        {
            if(isEditor)
            {
                if(cbShow.Items.FindByValue("All") == null)
                    cbShow.Items.Add(new ListItem("All", "All"));
            }
            else
            {
                if (cbShow.Items.FindByValue("All") != null)
                    cbShow.Items.Remove(cbShow.Items.FindByValue("All"));
            }



            hfFilterSubmittedBy.Visible = isEditor;
            chkShowInactive.Visible = isEditor;
            ddlMinistry.Visible = isEditor;
            ddlLocation.Visible = isEditor;
        }

        private void LoadUserFilterSettings()
        {
            foreach (ListItem item in cbListStatus.Items)
	        {
                bool IsSelected = false;
                string KeyName = string.Format("{0}_Status_{1}", PersonSettingsKeyPrefix, item.Value);
                bool.TryParse(GetUserPreference(KeyName), out IsSelected);
                item.Selected = IsSelected;
	        }

            foreach(ListItem item in cbShow.Items)
            {
                bool IsSelected = false;
                string KeyName = string.Format("{0}_RequisitionsBy_{1}", PersonSettingsKeyPrefix, item.Value);
                bool.TryParse(GetUserPreference(KeyName), out IsSelected);

                item.Selected = IsSelected;
            }

            foreach (ListItem item in cbListType.Items)
            {
                bool IsSelected = false;
                string KeyName = string.Format("{0}_RequisitionType_{1}", PersonSettingsKeyPrefix, item.Value);
                bool.TryParse(GetUserPreference(KeyName), out IsSelected);
                item.Selected = IsSelected;
            }

            int poNumber;
            if (int.TryParse(GetUserPreference(string.Format("{0}_PONumber", PersonSettingsKeyPrefix)), out poNumber) && poNumber > 0)
            {
                txtPONumber.Text = poNumber.ToString();
            }
            else
            {
                txtPONumber.Text = String.Empty;  
            }

            DateTime SubmittedOnStart;
            DateTime.TryParse(GetUserPreference(string.Format("{0}_SubmittedOnStart", PersonSettingsKeyPrefix)), out SubmittedOnStart);
            if (SubmittedOnStart > DateTime.MinValue)
                txtFilterSubmitted.LowerValue = SubmittedOnStart;

            DateTime SubmittedOnEnd;
            DateTime.TryParse(GetUserPreference(string.Format("{0}_SubmittedOnEnd", PersonSettingsKeyPrefix)), out SubmittedOnEnd);
            if (SubmittedOnEnd > DateTime.MinValue)
                txtFilterSubmitted.UpperValue = SubmittedOnEnd;

            if (hfFilterSubmittedBy.Visible)
            {
                int RequesterID = 0;
                int.TryParse(GetUserPreference(string.Format("{0}_RequesterID", PersonSettingsKeyPrefix)), out RequesterID);
                if (RequesterID > 0)
                {
                    hfFilterSubmittedBy.PersonId = RequesterID;
                    /*
                    PersonAliasService personAliasService = new PersonAliasService(new RockContext());
                    lblFilterSubmittedBy.Text = personAliasService.Get(RequesterID).Person.FullName;
                    lbRemoveSubmittedBy.Visible = true;
                     */
                }
                else
                {
                    ClearSubmittedByFilter();
                }
            }

            if (ddlMinistry.Visible)
            {
                int MinistryID = 0;
                int.TryParse(GetUserPreference(string.Format("{0}_MinistryLUID", PersonSettingsKeyPrefix)), out MinistryID);

                if (MinistryID > 0)
                {
                    if (ddlMinistry.Items.FindByValue(MinistryID.ToString()) != null)
                    {
                        ddlMinistry.SelectedValue = MinistryID.ToString();
                    }
                }
            }

            if ( ddlLocation.Visible )
            {
                int LocationID = 0;
                int.TryParse(GetUserPreference(string.Format( "{0}_LocationLUID", PersonSettingsKeyPrefix )), out LocationID );

                if ( LocationID > 0 )
                {
                    if ( ddlLocation.Items.FindByValue( LocationID.ToString() ) != null )
                    {
                        ddlLocation.SelectedValue = LocationID.ToString();
                    }
                }
            }
            

            if (chkShowInactive.Visible)
            {
                bool ShowInactive = false;
                bool.TryParse(GetUserPreference(string.Format("{0}_ShowInactive", PersonSettingsKeyPrefix)), out ShowInactive);
                chkShowInactive.Checked = ShowInactive;
            }
        }

        private void RedirectToRequisitionDetail()
        {
            NavigateToPage(new Guid(GetAttributeValue("RequisitionDetailPage")), null);
        }

        private void ResetFilters()
        {
            ClearFilters();
            LoadUserFilterSettings();
            BindRequisitionGrid();
        }

        private void SaveUserFilterSettings()
        {
            foreach (ListItem item in cbListStatus.Items)
            {
                SetUserPreference(string.Format("{0}_Status_{1}", PersonSettingsKeyPrefix, item.Value), item.Selected.ToString());
            }

            foreach (ListItem item in cbShow.Items)
            {
                SetUserPreference(string.Format("{0}_RequisitionsBy_{1}", PersonSettingsKeyPrefix, item.Value), item.Selected.ToString());
            }

            foreach (ListItem item in cbListType.Items)
            {
                SetUserPreference(string.Format("{0}_RequisitionType_{1}", PersonSettingsKeyPrefix, item.Value), item.Selected.ToString());
            }

            DateTime SubmittedOnStart = DateTime.MinValue;
            if (txtFilterSubmitted.LowerValue.HasValue)
            {
                SubmittedOnStart = txtFilterSubmitted.LowerValue.Value;
            }
            if (SubmittedOnStart > DateTime.MinValue)
                SetUserPreference(string.Format("{0}_SubmittedOnStart", PersonSettingsKeyPrefix), SubmittedOnStart.ToShortDateString());
            else
                SetUserPreference(string.Format("{0}_SubmittedOnStart", PersonSettingsKeyPrefix), String.Empty);

            DateTime SubmittedOnEnd = DateTime.MinValue;
            if (txtFilterSubmitted.UpperValue.HasValue)
            {
                SubmittedOnEnd = txtFilterSubmitted.UpperValue.Value;
            }
            if (SubmittedOnEnd > DateTime.MinValue)
                SetUserPreference(string.Format("{0}_SubmittedOnEnd", PersonSettingsKeyPrefix), SubmittedOnEnd.ToShortDateString());
            else
                SetUserPreference(string.Format("{0}_SubmittedOnEnd", PersonSettingsKeyPrefix), String.Empty);

            int poNumber = 0;

            if (int.TryParse(txtPONumber.Text, out poNumber) && poNumber > 0)
            {
                SetUserPreference(string.Format("{0}_PONumber", PersonSettingsKeyPrefix), poNumber.ToString());
            }
            else
            {
                SetUserPreference(string.Format("{0}_PONumber", PersonSettingsKeyPrefix), String.Empty);
            }

            if (hfFilterSubmittedBy.Visible && hfFilterSubmittedBy.PersonAliasId.HasValue)
                SetUserPreference(string.Format("{0}_RequesterID", PersonSettingsKeyPrefix), hfFilterSubmittedBy.PersonAliasId.Value.ToString());
            else
                SetUserPreference(string.Format("{0}_RequesterID", PersonSettingsKeyPrefix), String.Empty);

            if (chkShowInactive.Visible)
                SetUserPreference(string.Format("{0}_ShowInactive", PersonSettingsKeyPrefix), chkShowInactive.Checked.ToString());
            else
                SetUserPreference(string.Format("{0}_ShowInactive", PersonSettingsKeyPrefix), "False");

            int LocationID = 0;
            if (ddlLocation.Visible)
            {
                int.TryParse( ddlLocation.SelectedValue, out LocationID );
            }

            if ( LocationID > 0 )
            {
                SetUserPreference(string.Format( "{0}_LocationLUID", PersonSettingsKeyPrefix ), LocationID.ToString());
            }
            else
            {
                SetUserPreference(string.Format( "{0}_LocationLUID", PersonSettingsKeyPrefix ), String.Empty);
            }

            int MinistryID = 0;
            if (ddlMinistry.Visible)
            {
                int.TryParse(ddlMinistry.SelectedValue, out MinistryID);
            }

            if (MinistryID > 0)
            {
                SetUserPreference(string.Format("{0}_MinistryLUID", PersonSettingsKeyPrefix), MinistryID.ToString());
            }
            else
            {
                SetUserPreference(string.Format("{0}_MinistryLUID", PersonSettingsKeyPrefix), String.Empty);
            }

        }

        private void ShowStaffSearch()
        {

            ucStaffSearch.MinistryAreaAttributeGuid = MinistryAreaAttributeIDSetting;
            ucStaffSearch.PositionAttributeGuid = PositionAttributeIDSetting;
            ucStaffSearch.ParentPersonControlID = hfFilterSubmittedBy.ClientID;
            //ucStaffSearch.ParentRefreshButtonID = btnFilterSubmittedByRefresh.ClientID;
            ucStaffSearch.Show();
        }

        #endregion
    }

}