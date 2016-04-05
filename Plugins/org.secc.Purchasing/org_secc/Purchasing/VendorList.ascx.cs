using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock.Web.UI;
using Rock;
using org.secc.Purchasing;
using Rock.Web.UI.Controls;
using org.secc.Purchasing.DataLayer;
using Rock.Model;
using Rock.Attribute;
using System.ComponentModel;


namespace RockWeb.Plugins.org_secc.Purchasing
{
    [DisplayName("Vendor List")]
    [Category("Purchasing")]
    [Description("List all vendors in the SECC Purchasing system.")]

    [LinkedPage("Vendor Detail Page", "Vendor Detail Page", true)]
    [BooleanField("Active Only By Default", "Only show active vendors by default.", true)]
    public partial class VendorList : RockBlock
    {
        private bool? mCanEdit = null;
        public Guid VendorDetailPageSetting { get { return GetAttributeValue("VendorDetailPage").AsGuid(); } }

        public bool ActiveOnlyByDefaultSetting { get { return GetAttributeValue("ActiveOnlyByDefault").AsBoolean(); } }

        public static IQueryable<int> vendorsUsed = null;

        //public static IQueryable<int> vendorsInRequisitions {
        //    get {
        //        PurchasingContext Context = ContextHelper.GetArenaContext();
        //        return Context.RequisitionDatas.Where(r => r.pref_vendor_id.HasValue && r.pref_vendor_id > 0)
        //            .Select(r => r.pref_vendor_id.Value).Distinct();
        //    }
        //}

        //public static IQueryable<int> vendorsInPOs {
        //    get {
        //        PurchasingContext Context = ContextHelper.GetArenaContext();
        //        return Context.PurchaseOrderDatas.Where(r => r.vendor_id > 0)
        //            .Select(r => r.vendor_id).Distinct();
        //    }
        //}

        private bool CanEdit
        {
            get
            {
                return UserCanEdit;
            }
        }

        protected override void OnInit(EventArgs e)
        {
            InitializeComponent();
            base.OnInit(e);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                ResetFilter();
                LoadVendors();
                lbNewVendor.Visible = CanEdit;
            }
        }

        protected void btnApplyFilter_Click(object sender, EventArgs e)
        {
            LoadVendors();
        }

        protected void btnClearFilter_Click(object sender, EventArgs e)
        {
            ResetFilter();
            LoadVendors();
        }

        protected void lbNewVendor_Click(object sender, EventArgs e)
        {
            NavigateToPage(this.VendorDetailPageSetting, null);
        }

        #region Private
        private void InitializeComponent()
        {
            dgVendors.RowCommand += new GridViewCommandEventHandler(dgVendors_ItemCommand);
            dgVendors.GridRebind += new GridRebindEventHandler(dgVendors_ReBind);
            dgVendors.RowDeleted += new GridViewDeletedEventHandler(dgVendors_DeleteCommand);
        }

        private void ResetFilter()
        {
            txtFilterVendorName.Text = String.Empty;
            chkIncludeInactive.Checked = !ActiveOnlyByDefaultSetting;
        }


        void dgVendors_ItemCommand(object sender,  GridViewCommandEventArgs e)
        {
            
        }

        void dgVendors_ReBind(object sender, EventArgs e)
        {
            LoadVendors();
        }

        private void dgVendors_DeleteCommand(object sender, GridViewDeletedEventArgs e) {
            int vendorId = -1;
            Vendor currentVendor;

            /*if (int.TryParse(e.Item.Cells[0].Text, out vendorId) && vendorId > 0 && (currentVendor = new Vendor(vendorId)) != null) {
                if (!IsVendorUsed(currentVendor.VendorID))
                    Vendor.Delete(vendorId, "system");
                //Doesn't work
                //else
                //    ScriptManager.RegisterStartupScript(updVendorList, updVendorList.GetType(), Guid.NewGuid().ToString(), string.Format("alert('The vendor '{0}' cannot" +
                //        " be deleted because it is connected to a requisition or a PO');", currentVendor.VendorName), true);

                LoadVendors();
            }*/
        }

        private void ConfigureDataGrid()
        {
            dgVendors.Visible = true;
            dgVendors.ItemType = "Vendor";
            /*dgVendors.AddEnabled = false;
            /*dgVendors.DeleteEnabled = CurrentModule.Permissions.Allowed(OperationType.Edit, CurrentUser);  //false;
            dgVendors.DisableDeleteText = "You cannot delete the vendor '#VendorName#' because there are POs or requisitions that use it. You can only deactivate it.";
            dgVendors.DisableDeleteColumn = "DisableDelete";
            dgVendors.DeleteConfirmationText = @"Are you sure you want to delete the vendor '#VendorName#'?\n\nThis delete is irreversible!";
            */
            dgVendors.AllowSorting = true;
            /*dgVendors.MoveEnabled = false;
            dgVendors.SourceTableName = "cust_secc_purch_vendor";
            dgVendors.SourceTableKeyColumnName = "vendor_id";
            dgVendors.SourceTableOrderColumnName = "vendor_name";
            dgVendors.EditEnabled = false;
            dgVendors.MergeEnabled = false;
            dgVendors.MailEnabled = false;
            dgVendors.ExportEnabled = true;
            dgVendors.NoResultText = "No Vendors found"*/;

        }
        private void LoadVendors()
        {
            ConfigureDataGrid();
            var vendors = Vendor.LoadVendors(!chkIncludeInactive.Checked);
            if (!string.IsNullOrEmpty(txtFilterVendorName.Text)) {
                vendors = vendors.Where(v => v.VendorName.ToLower().Contains(txtFilterVendorName.Text.ToLower())).ToList();
            }
            dgVendors.DataSource = vendors;
            dgVendors.DataBind();
        }

        /// <summary>Is vendor used in a PO or a requisition?</summary>
        /// <param name="vendorsInRequisitions">Vendors who are linked from any requisition</param>
        /// <param name="vendor">Vendor to check</param>
        private static bool IsVendorUsed(int vendorId) {
            return Vendor.LoadVendors(false).Where(v => v.VendorID == vendorId && 
                (v.Requisitions.Count > 0 || v.PurchaseOrders.Count > 0)).Any();
        }

        #endregion

    }

}