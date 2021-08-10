﻿// <copyright>
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
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using org.secc.Purchasing;
using Rock;
using Rock.Attribute;
using Rock.Model;
using Rock.Web.UI;
using Rock.Web.UI.Controls;


namespace RockWeb.Plugins.org_secc.Purchasing
{
    [DisplayName( "Vendor List" )]
    [Category( "SECC > Purchasing" )]
    [Description( "List all vendors in the SECC Purchasing system." )]

    [LinkedPage( "Vendor Detail Page", "Vendor Detail Page", true )]
    [BooleanField( "Active Only By Default", "Only show active vendors by default.", true )]
    public partial class VendorList : RockBlock
    {
        private bool? mCanEdit = null;
        Rock.Model.Page mVendorPage = null;
        public Rock.Model.Page VendorDetailPageSetting
        {
            get
            {
                if ( mVendorPage == null )
                {
                    PageService pageService = new PageService( new Rock.Data.RockContext() );
                    mVendorPage = pageService.Get( GetAttributeValue( "VendorDetailPage" ).AsGuid() );
                }
                return mVendorPage;
            }
        }

        public bool ActiveOnlyByDefaultSetting { get { return GetAttributeValue( "ActiveOnlyByDefault" ).AsBoolean(); } }

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

        protected override void OnInit( EventArgs e )
        {
            InitializeComponent();
            base.OnInit( e );
        }

        protected void Page_Load( object sender, EventArgs e )
        {
            if ( !Page.IsPostBack )
            {
                ResetFilter();
                phNewVendor.Visible = CanEdit;
            };
            LoadVendors();
        }

        protected void btnApplyFilter_Click( object sender, EventArgs e )
        {
            LoadVendors();
        }

        protected void btnClearFilter_Click( object sender, EventArgs e )
        {
            ResetFilter();
            LoadVendors();
        }

        protected void lbNewVendor_Click( object sender, EventArgs e )
        {
            NavigateToPage( this.VendorDetailPageSetting.Guid, null );
        }

        #region Private
        private void InitializeComponent()
        {
            dgVendors.RowCommand += new GridViewCommandEventHandler( dgVendors_ItemCommand );
            dgVendors.GridRebind += new GridRebindEventHandler( dgVendors_ReBind );
        }

        private void ResetFilter()
        {
            txtFilterVendorName.Text = String.Empty;
            chkIncludeInactive.Checked = !ActiveOnlyByDefaultSetting;
        }


        void dgVendors_ItemCommand( object sender, GridViewCommandEventArgs e )
        {

        }

        void dgVendors_ReBind( object sender, EventArgs e )
        {
            LoadVendors();
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
            dgVendors.NoResultText = "No Vendors found"*/
            ;

        }
        private void LoadVendors()
        {
            ConfigureDataGrid();
            var vendors = Vendor.LoadVendors( !chkIncludeInactive.Checked );

            SortProperty sortProperty = dgVendors.SortProperty;
            if ( sortProperty != null )
            {
                if ( sortProperty.Direction == SortDirection.Ascending )
                {
                    vendors = vendors.OrderBy( r => r.GetType().GetProperty( sortProperty.Property ).GetValue( r ) ).ToList();
                }
                else
                {
                    vendors = vendors.OrderByDescending( r => r.GetType().GetProperty( sortProperty.Property ).GetValue( r ) ).ToList();
                }
            }
            else
            {
                vendors = vendors.OrderBy( v => v.VendorName ).ToList();
            }

            if ( !string.IsNullOrEmpty( txtFilterVendorName.Text ) )
            {
                vendors = vendors.Where( v => v.VendorName.ToLower().Contains( txtFilterVendorName.Text.ToLower() ) ).ToList();
            }
            dgVendors.DataSource = vendors;
            dgVendors.DataBind();
        }

        /// <summary>Is vendor used in a PO or a requisition?</summary>
        /// <param name="vendorsInRequisitions">Vendors who are linked from any requisition</param>
        /// <param name="vendor">Vendor to check</param>
        private static bool IsVendorUsed( int vendorId )
        {
            return Vendor.LoadVendors( false ).Where( v => v.VendorID == vendorId &&
                   ( v.Requisitions.Count > 0 || v.PurchaseOrders.Count > 0 ) ).Any();
        }

        #endregion

        protected void Delete_Click( object sender, RowEventArgs e )
        {
            int vendorId = e.RowKeyId;
            Vendor currentVendor;

            if ( vendorId > 0 && ( currentVendor = new Vendor( vendorId ) ) != null )
            {
                if ( !IsVendorUsed( currentVendor.VendorID ) )
                {
                    Vendor.Delete( vendorId, "system" );
                }
                else
                {
                    maAlert.Show(
                        string.Format( "The vendor {0} cannot be deleted because it is connected to a requisition or a PO",
                            currentVendor.VendorName ),
                            ModalAlertType.Warning );
                }
                LoadVendors();
            }
        }
        protected void Unnamed_Click( object sender, RowEventArgs e )
        {
            return;
        }
        protected void dgVendors_RowDataBound( object sender, GridViewRowEventArgs e )
        {
            if ( e.Row.RowType == DataControlRowType.DataRow || e.Row.RowType == DataControlRowType.Header )
            {
                e.Row.Controls[7].Visible = UserCanEdit;
                e.Row.Controls[8].Visible = UserCanEdit;
                var key = e.Row.Controls[0];

            }
            if ( e.Row.RowType == DataControlRowType.DataRow )
            {
                if ( ( ( Vendor ) e.Row.DataItem ).HasRequisitions )
                {
                    DataControlFieldCell cell = ( DataControlFieldCell ) e.Row.Controls[8]; // as DeleteField ).ButtonCssClass = "btn-disabled";
                    cell.CssClass = "disabled";
                }
            }
        }
    }

}