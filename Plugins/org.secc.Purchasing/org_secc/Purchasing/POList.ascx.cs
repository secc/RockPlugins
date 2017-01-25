using System;
using System.ComponentModel;
using System.Collections.Generic;
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

    [DisplayName("Purchase Order List")]
    [Category("SECC > Purchasing")]
    [Description("Lists/filters all Purchase Orders.")]

    [LinkedPage("Purchase Order Detail Page", "Purchase Order Detail Page", true)]
    [AttributeField(Rock.SystemGuid.EntityType.PERSON, "Ministry Area Person Attribute", "The person attribute that stores the user's Ministry Area.", false, false, null, "Staff Selector")]
    [AttributeField(Rock.SystemGuid.EntityType.PERSON, "Position Person Attribute", "The person attribute that stores the user's job position.", false, false, null, "Staff Selector")]

    public partial class POList : RockBlock
    {

        private string PersonSettingKeyPrefix = "POList";
        #region Module Settings
        public string PurchaseOrderDetailPageSetting { get {

            if (!String.IsNullOrEmpty(GetAttributeValue("PurchaseOrderDetailPage")))
            {
                PageService pageService = new PageService(new Rock.Data.RockContext());
                if ( GetAttributeValue( "PurchaseOrderDetailPage" ).AsGuidOrNull() != null && pageService.Get( GetAttributeValue( "PurchaseOrderDetailPage" ).AsGuid() ) != null)
                {
                    return "~/page/" + pageService.Get( GetAttributeValue( "PurchaseOrderDetailPage" ).AsGuid() ).Id;

                }
            }
            return null; 
        } }


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
                BindPOTypeCheckboxList();
                BindVendors();
                LoadUserFilterSettings();
                BindPOGrid();
                lbAddPO.Visible = CanUserCreatePurchaseOrder();                
            }
            dgPurchaseOrders.GridRebind += dgPurchaseOrders_Rebind;
        }

        protected void btnFilterApply_Click(object sender, EventArgs e)
        {
            SaveUserFilterSettings();
            BindPOGrid();
        }

        protected void btnFilterClear_Click(object sender, EventArgs e)
        {
            ResetFilters();
        }

        protected void dgPurchaseOrders_ItemCommand(object sender, DataGridCommandEventArgs e)
        {

        }

        protected void dgPurchaseOrders_Rebind(object sender, EventArgs e)
        {
            BindPOGrid();
        }
        protected void btnFilterSubmittedBySelect_Click(object sender, EventArgs e)
        {
            ShowStaffSearch();
        }

        protected void lbAddPO_Click(object sender, EventArgs e)
        {
            RedirectToAddPO();
        }

        protected void lbRemoveOrderedBy_Click(object sender, EventArgs e)
        {
            ClearOrderedByFilter();
        }

        #endregion

        #region Private 
        private void BindStatusCheckboxList()
        {
            cbListStatus.DataSource = PurchaseOrder.GetPurchaseOrderStatuses(true).OrderBy(x => x.Order);
            cbListStatus.DataValueField = "Id";
            cbListStatus.DataTextField = "Value";
            cbListStatus.DataBind();
        }

        private void BindPOGrid()
        {
            ConfigurePOGrid();
            
            DataTable dt = new DataTable();

            dt.Columns.AddRange( new DataColumn[] {
                new DataColumn("PurchaseOrderID", typeof(int)),
                new DataColumn("VendorName", typeof(string)),
                new DataColumn("POType", typeof(string)),
                new DataColumn("Status", typeof(string)),
                new DataColumn("ItemDetails", typeof(int)),
                new DataColumn("TotalPayments", typeof(string)),
                new DataColumn("NoteCount", typeof(int)),
                new DataColumn("AttachmentCount", typeof(int))
            } );


            if ( GetUserPreferences( PersonSettingKeyPrefix ).Count() > 0 )
            {
                var POListItems = PurchaseOrder.GetPurchaseOrderList( BuildFilter() );


                SortProperty sortProperty = dgPurchaseOrders.SortProperty;
                if ( sortProperty != null )
                {
                    if ( sortProperty.Direction == SortDirection.Ascending )
                    {
                        POListItems = POListItems.OrderBy( r => r.GetType().GetProperty( sortProperty.Property ).GetValue( r ) ).ToList();
                    }
                    else
                    {
                        POListItems = POListItems.OrderByDescending( r => r.GetType().GetProperty( sortProperty.Property ).GetValue( r ) ).ToList();
                    }
                }
                else
                {
                    POListItems = POListItems.OrderByDescending( p => p.PurchaseOrderID ).ToList();
                }

                foreach ( var po in POListItems )
                {
                    DataRow dr = dt.NewRow();
                    dr["PurchaseOrderID"] = po.PurchaseOrderID;
                    dr["VendorName"] = po.VendorName;
                    dr["POType"] = po.POType;
                    dr["Status"] = po.Status;
                    dr["ItemDetails"] = po.ItemDetailCount;
                    dr["TotalPayments"] = string.Format( "{0:c}", po.TotalPayments );
                    dr["NoteCount"] = po.NoteCount;
                    dr["AttachmentCount"] = po.AttachmentCount;

                    dt.Rows.Add( dr );
                }

            }

            dgPurchaseOrders.DataSource = dt;
            dgPurchaseOrders.DataBind();
        }

        private void BindPOTypeCheckboxList()
        {
            cbListType.DataSource = PurchaseOrder.GetPurchaseOrderTypes(true).OrderBy(x => x.Order);
            cbListType.DataValueField = "Id";
            cbListType.DataTextField = "Value";
            cbListType.DataBind();
        }

        private void BindVendors()
        {
            ddlVendor.DataSource = Vendor.LoadVendors(false).OrderBy(x => x.VendorName);
            ddlVendor.DataValueField = "VendorID";
            ddlVendor.DataTextField = "VendorName";
            ddlVendor.DataBind();

            ddlVendor.Items.Insert(0, new ListItem("--All--", "0"));
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

            System.Text.StringBuilder TypeSB = new System.Text.StringBuilder();
            foreach (ListItem item in cbListType.Items)
            {
                if (item.Selected)
                    TypeSB.Append(item.Value + ",");
            }
            TypeSB.Append("0");
            Filter.Add("TypeLUID", TypeSB.ToString());

            int PONumber = 0;
            if (int.TryParse(txtPONumber.Text, out PONumber))
                Filter.Add("PONumber", PONumber.ToString());

            int VendorID = 0;
            if (int.TryParse(ddlVendor.SelectedValue, out VendorID) && VendorID > 0)
                Filter.Add("VendorID", VendorID.ToString());

            if (txtOrderDate.LowerValue.HasValue)
                Filter.Add("OrderedOnStart", txtOrderDate.LowerValue.Value.ToShortDateString());

            if (txtOrderDate.UpperValue.HasValue)
                Filter.Add("OrderedOnEnd", txtOrderDate.UpperValue.Value.ToShortDateString());

            if (ucStaffPicker.StaffPersonAliasId.HasValue) {
                Filter.Add("OrderedByID", ucStaffPicker.StaffPersonAliasId.Value.ToString());
            }

            Filter.Add("ShowInactive", chkShowInactive.Checked.ToString());

            return Filter;

        }

        private bool CanUserCreatePurchaseOrder()
        {
            return UserCanEdit;
        }

        private void ClearFilters()
        {
            foreach (ListItem item in cbListStatus.Items)
            {
                item.Selected = false;
            }

            foreach (ListItem item in cbListType.Items)
            {
                item.Selected = false;
            }

            txtOrderDate.LowerValue = null;
            txtOrderDate.UpperValue = null;
            txtPONumber.Text = String.Empty;
            ddlVendor.SelectedIndex = 0;
            ClearOrderedByFilter();
            chkShowInactive.Checked = false;

        }

        private void ClearOrderedByFilter()
        {
            ucStaffPicker.StaffPerson = null;
        }

        private void ConfigurePOGrid()
        {
            dgPurchaseOrders.Visible = true;
            dgPurchaseOrders.ItemType = "Items";
            dgPurchaseOrders.AllowSorting = true;
        }

        private void LoadUserFilterSettings()
        {
            foreach (ListItem item in cbListStatus.Items)
            {
                bool IsSelected = false;
                string KeyName = string.Format("{0}_Status_{1}", PersonSettingKeyPrefix, item.Value);
                bool.TryParse(GetUserPreference(KeyName), out IsSelected);
                item.Selected = IsSelected;
            }

            foreach (ListItem item in cbListType.Items)
	        {
                bool IsSelected = false;
                string KeyName = string.Format("{0}_Type_{1}", PersonSettingKeyPrefix, item.Value);
                bool.TryParse(GetUserPreference(KeyName), out IsSelected);
                item.Selected = IsSelected;
	        }

            DateTime OrderedOnStart;
            DateTime.TryParse(GetUserPreference(string.Format("{0}_OrderedOnStart", PersonSettingKeyPrefix)), out OrderedOnStart);
            if (OrderedOnStart > DateTime.MinValue)
                txtOrderDate.LowerValue = OrderedOnStart;

            DateTime OrderedOnEnd;
            DateTime.TryParse(GetUserPreference(string.Format("{0}_OrderedOnEnd", PersonSettingKeyPrefix)), out OrderedOnEnd);
            if (OrderedOnEnd > DateTime.MinValue)
                txtOrderDate.UpperValue = OrderedOnEnd;

            int OrderedByPersonID = 0;
            int.TryParse(GetUserPreference(string.Format("{0}_OrderedBy", PersonSettingKeyPrefix)), out OrderedByPersonID);

            int PONumber = 0;
            int.TryParse(GetUserPreference(string.Format("{0}_PONumber", PersonSettingKeyPrefix)), out PONumber);
            if (PONumber > 0)
                txtPONumber.Text = PONumber.ToString();

            int VendorID = 0;
            int.TryParse(GetUserPreference(string.Format("{0}_VendorID", PersonSettingKeyPrefix)), out VendorID);
            if (VendorID > 0 && ddlVendor.Items.FindByValue(VendorID.ToString()) != null)
                ddlVendor.SelectedValue = VendorID.ToString();


            if (OrderedByPersonID > 0)
            {
                PersonAliasService personAliasService = new PersonAliasService(new Rock.Data.RockContext());
                Person OrderedByPerson = personAliasService.Get(OrderedByPersonID).Person;

                if (OrderedByPerson.PrimaryAliasId > 0) { 
                    ucStaffPicker.StaffPerson = OrderedByPerson.PrimaryAlias;
                }
            }

            bool ShowInactive = false;
            bool.TryParse(GetUserPreference(string.Format("{0}_ShowInactive", PersonSettingKeyPrefix)), out ShowInactive);
            chkShowInactive.Checked = ShowInactive;

        }

        private void RedirectToAddPO()
        {
            Response.Redirect(PurchaseOrderDetailPageSetting);
        }

        private void ResetFilters()
        {
            ClearFilters();
            SaveUserFilterSettings();
            BindPOGrid();
        }

        private void SaveUserFilterSettings()
        {
            foreach (ListItem item in cbListStatus.Items)
            {
                SetUserPreference(string.Format("{0}_Status_{1}", PersonSettingKeyPrefix, item.Value), item.Selected.ToString());
            }

            foreach (ListItem item in cbListType.Items)
            {
                SetUserPreference(string.Format("{0}_Type_{1}", PersonSettingKeyPrefix, item.Value), item.Selected.ToString());
            }

            if (txtOrderDate.LowerValue.HasValue)
                SetUserPreference(string.Format("{0}_OrderedOnStart", PersonSettingKeyPrefix), txtOrderDate.LowerValue.Value.ToShortDateString());
            else
                SetUserPreference(string.Format("{0}_OrderedOnStart", PersonSettingKeyPrefix), String.Empty);

            if (txtOrderDate.UpperValue.HasValue)
                SetUserPreference(string.Format("{0}_OrderedOnEnd", PersonSettingKeyPrefix), txtOrderDate.UpperValue.Value.ToShortDateString());
            else
                SetUserPreference(string.Format("{0}_OrderedOnEnd", PersonSettingKeyPrefix), String.Empty);

            if (ucStaffPicker.StaffPersonAliasId.HasValue)
                SetUserPreference(string.Format("{0}_OrderedBy", PersonSettingKeyPrefix), ucStaffPicker.StaffPersonAliasId.Value.ToString());
            else
                SetUserPreference(string.Format("{0}_OrderedBy", PersonSettingKeyPrefix), String.Empty);

            int PONumber = 0;
            int.TryParse(txtPONumber.Text, out PONumber);
            if (PONumber > 0)
                SetUserPreference(string.Format("{0}_PONumber", PersonSettingKeyPrefix), PONumber.ToString());
            else
                SetUserPreference(string.Format("{0}_PONumber", PersonSettingKeyPrefix), String.Empty);

            int VendorID = 0;
            int.TryParse(ddlVendor.SelectedValue, out VendorID);
            SetUserPreference(string.Format("{0}_VendorID", PersonSettingKeyPrefix), VendorID.ToString());

            SetUserPreference(string.Format("{0}_ShowInactive", PersonSettingKeyPrefix), chkShowInactive.Checked.ToString());

        }

        private void ShowStaffSearch()
        {
            ucStaffPicker.Show();
        }

        protected override void CreateChildControls()
        {
            base.CreateChildControls();
            ucStaffPicker.MinistryAreaAttributeGuid = MinistryAreaAttributeIDSetting;
            ucStaffPicker.PositionAttributeGuid = PositionAttributeIDSetting;
        }
        #endregion
    }
}