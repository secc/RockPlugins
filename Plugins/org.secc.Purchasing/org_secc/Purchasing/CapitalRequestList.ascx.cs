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
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using org.secc.Purchasing;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;


namespace RockWeb.Plugins.org_secc.Purchasing
{
    [DisplayName( "Capital Request List" )]
    [Category( "SECC > Purchasing" )]
    [Description( "Lists all capital requests." )]
    [LinkedPage( "Capital Request Detail Page", "Page that shows the details of a selected capital request.", true )]
    [DefinedTypeField( "Ministry Area Lookup Type", "The Lookup Type that contains the ministry lookup values.", true )]
    [DefinedTypeField( "Location Lookup Type", "The lookup Type that contains the Location lookup values. If no value is selected, the Location filter will not be available.", true )]
    [SecurityRoleField( "Location Admin Group", "Security role that contains location administrators.  These users will be able to see all requisitions for their location.", false )]
    [AttributeField( Rock.SystemGuid.EntityType.PERSON, "Ministry Area Person Attribute", "The person attribute that stores the user's Ministry Area.", key: "MinistryAttribute" )]
    [AttributeField( Rock.SystemGuid.EntityType.PERSON, "SECC Location Person Attribute", "The person attribute that stores the user's SECC Location.", key: "LocationAttribute" )]
    public partial class CapitalRequestList : RockBlock
    {
        private string PersonSettingKey = "CapitalRequestList";
        #region Module Settings


        public string CapitalRequestDetailPageSetting
        {
            get
            {
                if ( !String.IsNullOrEmpty( GetAttributeValue( "CapitalRequestDetailPage" ) ) )
                {
                    PageService pageService = new PageService( new Rock.Data.RockContext() );
                    return "~/page/" + pageService.Get( new Guid( GetAttributeValue( "CapitalRequestDetailPage" ) ) ).Id;
                }
                return null;
            }
        }

        public Guid? LocationLookupTypeIdSetting
        {
            get
            {
                return GetAttributeValue( "LocationLookupType" ).AsGuidOrNull();
            }
        }

        #endregion

        #region Module Events
        protected void Page_Load( object sender, EventArgs e )
        {
            if ( !Page.IsPostBack )
            {
                LoadFilters();
                LoadUserPreferences();
                LoadCapitalRequests();
            }
            gRequestList.GridRebind += grdCapitalRequests_ReBind;
        }

        protected void btnFilterApply_Click( object sender, EventArgs e )
        {
            SaveUserPreferences();
            LoadCapitalRequests();
        }
        protected void btnFilterReset_Click( object sender, EventArgs e )
        {
            LoadUserPreferences();
            LoadCapitalRequests();
        }



        protected void btnRequesterChange_Click( object sender, EventArgs e )
        {
            /*ucStaffSearch.Title = "Requester Select";
            ucStaffSearch.ParentPersonControlID = hfRequesterSearchResults.ClientID;
            ucStaffSearch.ParentRefreshButtonID = btnRequesterSet.ClientID;
            ucStaffSearch.MinistryAreaAttributeID = MinistryAreaAttributeIDSetting;
            ucStaffSearch.PositionAttributeID = PositionAttributeIDSetting;
            ucStaffSearch.Show();*/
        }

        protected void lbRequesterRemove_Click( object sender, EventArgs e )
        {
            //SetRequesterFilter( 0 );
        }

        protected void lbCreateCapitalRequest_Click( object sender, EventArgs e )
        {
            NavigateToPage( new Guid( GetAttributeValue( "CapitalRequestDetailPage" ) ), null );
        }
        #endregion


        #region Private Methods

        private Dictionary<string, string> BuildFilters()
        {
            CurrentPerson.LoadAttributes();
            Dictionary<string, string> filter = new Dictionary<string, string>();

            filter.Add( "PersonId", CurrentPerson.Id.ToString() );
            filter.Add( "UserId", CurrentUser.UserName );
            CurrentPerson.LoadAttributes();
            if ( GetAttributeValue( "MinistryAttribute" ).AsGuidOrNull() != null )
            {

                List<Guid> ministryGuids = CurrentPerson.AttributeValues[AttributeCache.Get( GetAttributeValue( "MinistryAttribute" ).AsGuid() ).Key].Value.Split( ',' ).AsGuidList();
                var ministryValues = DefinedValueCache.All().Where( dv => ministryGuids.Contains( dv.Guid ) );
                if ( ministryValues.Count() > 0 )
                {
                    filter.Add( "MinistryIds", ministryValues.Select( mv => mv.Id.ToString() ).JoinStrings( "," ) );
                }
            }
            if ( GetAttributeValue( "LocationAttribute" ).AsGuidOrNull() != null )
            {
                var attributeValue = CurrentPerson.AttributeValues[AttributeCache.Get( GetAttributeValue( "LocationAttribute" ).AsGuid() ).Key];
                if ( attributeValue != null && !string.IsNullOrEmpty( attributeValue.Value ) )
                {
                    filter.Add( "MyLocationId", DefinedValueCache.Get( attributeValue.Value ).Id.ToString() );
                }
            }
            //filter.Add( "FinanceApprover", UserIsFinanceApprover().ToString() );

            if ( LocationLookupTypeIdSetting != null && UserCanEdit )
            {
                filter.Add( "LocationId", ddlSCCLocation.SelectedValue );
            }

            System.Text.StringBuilder statusFilter = new System.Text.StringBuilder();
            foreach ( ListItem item in cblStatus.Items )
            {
                if ( item.Selected )
                {
                    statusFilter.AppendFormat( "{0},", item.Value );
                }
            }
            statusFilter.Append( "0" );

            filter.Add( "StatusLUID", statusFilter.ToString() );

            filter.Add( "RequestingMinistry", ddlMinistry.SelectedValue );
            filter.Add( "Requester", requester.PersonAliasId.ToString() );
            filter.Add( "GLAccount", txtGLAccount.Text );
            filter.Add( "FiscalYear", ddlFiscalYear.SelectedValue );

            foreach ( ListItem item in cblShow.Items )
            {
                filter.Add( string.Format( "Show_{0}", item.Value ), item.Selected.ToString() );
            }

            return filter;

        }
        private void ConfigureCapitalRequestGrid()
        {
            /*

                //grdCapitalRequests.Visible = false;
                HyperLinkColumn linkColumn = (HyperLinkColumn)grdCapitalRequests.Columns[1];
                linkColumn.DataNavigateUrlFormatString = GetDetailsPageRootUrl() + "&CER={0}";

                grdCapitalRequests.ItemType = "Items";
                grdCapitalRequests.ItemBgColor = CurrentPortalPage.Setting( "ItemBgColor", string.Empty, false );
                grdCapitalRequests.ItemAltBgColor = CurrentPortalPage.Setting( "ItemAltBgColor", string.Empty, false );
                grdCapitalRequests.ItemMouseOverColor = CurrentPortalPage.Setting( "ItemMouseOverColor", string.Empty, false );
                grdCapitalRequests.AllowSorting = true;
                grdCapitalRequests.AllowPaging = true;
                grdCapitalRequests.ExportEnabled = true;
                grdCapitalRequests.NoResultText = "No Capital Requests Found";
                grdCapitalRequests.MergeEnabled = false;
                grdCapitalRequests.EditEnabled = false;
                grdCapitalRequests.MailEnabled = false;
                grdCapitalRequests.AddEnabled = false;
                grdCapitalRequests.DeleteEnabled = false;
                grdCapitalRequests.SourceTableKeyColumnName = "CapitalRequestId";
            }

            private string GetDetailsPageRootUrl()
            {
                return string.Format( "~/default.aspx?page={0}", CapitalRequestDetailPageSetting );
            */
        }

        private void LoadCapitalRequests()
        {
            //ConfigureCapitalRequestGrid();

            Dictionary<string, string> filterValues = BuildFilters();
            List<CapitalRequestListItem> requests = CapitalRequest.GetCapitalRequestList( filterValues );

            SortProperty sortProperty = gRequestList.SortProperty;
            // Check User Preferences to see if we have a pre-existing sort property
            if ( sortProperty == null )
            {
                sortProperty = new SortProperty();
                sortProperty.Direction = GetUserPreference( string.Format( "{0}_Sort_Direction", PersonSettingKey ) ) == "ASC" ? SortDirection.Ascending : SortDirection.Descending;
                sortProperty.Property = GetUserPreference( string.Format( "{0}_Sort_Column", PersonSettingKey ) );
                if ( string.IsNullOrEmpty( sortProperty.Property ) )
                {
                    sortProperty.Property = "CapitalRequestId";
                }
            }
            if ( sortProperty != null )
            {
                if ( sortProperty.Direction == SortDirection.Ascending )
                {
                    requests = requests.OrderBy( r => r.GetType().GetProperty( sortProperty.Property ).GetValue( r ) ).ToList();
                }
                else
                {
                    requests = requests.OrderByDescending( r => r.GetType().GetProperty( sortProperty.Property ).GetValue( r ) ).ToList();
                }
                SetUserPreference( string.Format( "{0}_Sort_Direction", PersonSettingKey ), sortProperty.DirectionString );
                SetUserPreference( string.Format( "{0}_Sort_Column", PersonSettingKey ), sortProperty.Property );
            }
            else
            {
                requests = requests.OrderByDescending( r => r.CapitalRequestId ).ToList();
            }

            DataTable capRequestDT = new DataTable();
            capRequestDT.Columns.Add( "CapitalRequestId", typeof( string ) );
            capRequestDT.Columns.Add( "ProjectName", typeof( string ) );
            capRequestDT.Columns.Add( "RequestingMinistry", typeof( string ) );
            capRequestDT.Columns.Add( "RequesterName", typeof( string ) );
            capRequestDT.Columns.Add( "RequesterNameLastFirst", typeof( string ) );
            capRequestDT.Columns.Add( "FullAccountNumber", typeof( string ) );
            capRequestDT.Columns.Add( "ProjectId", typeof( string ) );
            capRequestDT.Columns.Add( "Status", typeof( string ) );
            capRequestDT.Columns.Add( "RequisitionCount", typeof( int ) );
            capRequestDT.Columns.Add( "TotalCharges", typeof( decimal ) );
            capRequestDT.Columns.Add( "Active", typeof( bool ) );

            foreach ( var request in requests )
            {
                capRequestDT.Rows.Add(
                        request.CapitalRequestId,
                        request.ProjectName,
                        request.RequestingMinistry,
                        request.RequesterName,
                        request.RequesterNameLastFirst,
                        request.FullAccountNumber,
                        request.ProjectId,
                        request.Status,
                        request.RequisitionCount,
                        request.TotalCharges,
                        request.Active
                    );
            }

            gRequestList.DataSource = capRequestDT;
            gRequestList.DataBind();


            gRequestList.Actions.ShowAdd = UserCanEdit;
            gRequestList.Actions.AddButton.Text = "<i class=\"fa fa-plus\"></i>&nbsp;Add Capital Request";
            gRequestList.Actions.AddClick += lbCreateCapitalRequest_Click;
            gRequestList.Actions.ShowExcelExport = false;
            gRequestList.Actions.ShowMergeTemplate = false;

            if ( sortProperty != null )
            {
                foreach ( var column in gRequestList.Columns )
                {
                    var dcf = column as DataControlField;
                    if ( dcf != null && dcf.SortExpression == sortProperty.Property )
                    {
                        gRequestList.HeaderRow.Cells[gRequestList.Columns.IndexOf( dcf )].AddCssClass( sortProperty.Direction.ToString().ToLower() );
                        break;
                    }
                }
                if ( gRequestList.SortProperty == null )
                {
                    gRequestList.Sort( sortProperty.Property, sortProperty.Direction );
                }
            }

            //grdCapitalRequests.DataSource = capRequestDT;
            //grdCapitalRequests.DataBind();

            //grdCapitalRequests.Visible = requests.Count > 0;
        }

        private void LoadStatusList()
        {
            cblStatus.Items.Clear();
            var statusLookups = DefinedTypeCache.Get( new Guid( CapitalRequest.STATUS_LOOKUP_TYPE_GUID ) );

            cblStatus.DataSource = statusLookups.DefinedValues.Where( l => l.IsActive ).OrderBy( l => l.Order );
            cblStatus.DataValueField = "Id";
            cblStatus.DataTextField = "Value";
            cblStatus.DataBind();
        }
        private void LoadFilters()
        {
            LoadStatusList();
            LoadLocationList();
            LoadMinistryList();
            LoadFiscalYearList();
            //SetRequesterFilter( 0 );

            CurrentPerson.LoadAttributes();
            ddlMinistry.Visible = UserCanEdit || CurrentPerson.AttributeValues[AttributeCache.Get( GetAttributeValue( "MinistryAttribute" ).AsGuid() ).Key].Value.Split( ',' ).Count() > 0;
            ddlSCCLocation.Visible = UserCanEdit;
            requester.Visible = UserCanEdit;
            txtGLAccount.Visible = UserCanEdit;
            ddlFiscalYear.Visible = UserCanEdit;

            SetRequestedByAllOption( UserCanEdit );

            Group locationAdminGroup = new GroupService( new RockContext() ).Get( GetAttributeValue( "LocationAdminGroup" ).AsGuid() );

            if ( !( UserCanEdit || ( locationAdminGroup != null && locationAdminGroup.ActiveMembers().Where( m => m.PersonId == CurrentPerson.Id ).Any() ) ) )
            {
                cblShow.Items.Remove( cblShow.Items.FindByValue( "Location" ) );
            }

        }

        private void LoadFiscalYearList()
        {
            ddlFiscalYear.Items.Insert( 0, new ListItem( "All", DateTime.MinValue.ToString() ) );

            for ( int i = 0; i <= 10; i++ )
            {
                string date = RockDateTime.New( RockDateTime.Now.Year - i, 1, 1 ).ToString();
                ddlFiscalYear.Items.Insert( 0, new ListItem( ( RockDateTime.Now.Year - i ).ToString(), date.ToString() ) );
            }
        }

        private void LoadLocationList()
        {
            ddlSCCLocation.Items.Clear();

            if ( LocationLookupTypeIdSetting.HasValue )
            {
                ddlSCCLocation.DataSource = DefinedTypeCache.Get( LocationLookupTypeIdSetting.Value ).DefinedValues
                                                .Where( l => l.IsActive )
                                                .Select( l => new { l.Id, l.Value } )
                                                .OrderBy( l => l.Value );
                ddlSCCLocation.DataValueField = "Id";
                ddlSCCLocation.DataTextField = "Value";
                ddlSCCLocation.DataBind();

                ddlSCCLocation.Items.Insert( 0, new ListItem( "All", "0" ) );

            }
        }

        private void LoadMinistryList()
        {
            ddlMinistry.Items.Clear();

            if ( String.IsNullOrEmpty( GetAttributeValue( "MinistryAreaLookupType" ) ) )
                return;

            IEnumerable<DefinedValueCache> ministries;
            if ( !UserCanEdit )
            {
                CurrentPerson.LoadAttributes();
                var ministryGuids = CurrentPerson.AttributeValues[AttributeCache.Get( GetAttributeValue( "MinistryAttribute" ).AsGuid() ).Key].Value.Split( ',' ).AsGuidList();
                ministries = DefinedValueCache.All().Where( dv => ministryGuids.Contains( dv.Guid ) ).OrderBy( l => l.Value );
            }
            else
            {
                ministries = DefinedTypeCache.Get( GetAttributeValue( "MinistryAreaLookupType" ).AsGuid() ).DefinedValues
                                    .Where( l => l.IsActive )
                                    .OrderBy( l => l.Value );
            }

            ddlMinistry.DataSource = ministries;
            ddlMinistry.DataValueField = "Id";
            ddlMinistry.DataTextField = "Value";
            ddlMinistry.DataBind();

            ddlMinistry.Items.Insert( 0, new ListItem( "All", "0" ) );
        }

        private void LoadUserPreferences()
        {
            foreach ( ListItem item in cblStatus.Items )
            {
                bool isSelected = false;
                string keyName = string.Format( "{0}_Status_{1}", PersonSettingKey, item.Value );
                bool.TryParse( GetUserPreference( keyName ), out isSelected );
                item.Selected = isSelected;
            }

            int requestingMinistry = 0;
            int.TryParse( GetUserPreference( string.Format( "{0}_RequestingMinistry", PersonSettingKey ) ), out requestingMinistry );
            ddlMinistry.SelectedValue = requestingMinistry.ToString();

            if ( LocationLookupTypeIdSetting != null )
            {
                int locationLUID = 0;
                int.TryParse( GetUserPreference( string.Format( "{0}_Location", PersonSettingKey ) ), out locationLUID );
                ddlSCCLocation.SelectedValue = locationLUID.ToString();
            }

            int requesterId = 0;
            int.TryParse( GetUserPreference( string.Format( "{0}_Requester", PersonSettingKey ) ), out requesterId );
            /*Person requester = new Person( requesterId, true );
            SetRequesterFilter( requester.PersonID );*/

            txtGLAccount.Text = GetUserPreference( string.Format( "{0}_GLAccount", PersonSettingKey ) );

            DateTime fyStartDate;
            DateTime.TryParse( GetUserPreference( string.Format( "{0}_FiscalYear", PersonSettingKey ) ), out fyStartDate );

            ddlFiscalYear.SelectedValue = fyStartDate.ToString();

            foreach ( ListItem item in cblShow.Items )
            {
                bool isSelected = false;
                string keyName = string.Format( "{0}_Show_{1}", PersonSettingKey, item.Value );
                bool.TryParse( GetUserPreference( keyName ), out isSelected );
                item.Selected = isSelected;

            }

        }

        private void SaveUserPreferences()
        {
            foreach ( ListItem item in cblStatus.Items )
            {
                SetUserPreference( string.Format( "{0}_Status_{1}", PersonSettingKey, item.Value ), item.Selected.ToString() );
            }

            SetUserPreference( string.Format( "{0}_RequestingMinistry", PersonSettingKey ), ddlMinistry.SelectedValue );

            SetUserPreference( string.Format( "{0}_Requester", PersonSettingKey ), requester.PersonAliasId.ToString() );

            SetUserPreference( string.Format( "{0}_GLAccount", PersonSettingKey ), txtGLAccount.Text );

            if ( LocationLookupTypeIdSetting != null )
            {
                SetUserPreference( string.Format( "{0}_Location", PersonSettingKey ), ddlSCCLocation.SelectedValue );
            }

            DateTime selectedFYStartDate;

            DateTime.TryParse( ddlFiscalYear.SelectedValue, out selectedFYStartDate );

            if ( selectedFYStartDate > DateTime.MinValue )
            {
                SetUserPreference( string.Format( "{0}_FiscalYear", PersonSettingKey ), selectedFYStartDate.ToString() );
            }
            else
            {
                SetUserPreference( string.Format( "{0}_FiscalYear", PersonSettingKey ), String.Empty );
            }

            foreach ( ListItem item in cblShow.Items )
            {
                SetUserPreference( string.Format( "{0}_Show_{1}", PersonSettingKey, item.Value ), item.Selected.ToString() );
            }


        }

        private void SetRequestedByAllOption( bool isVisible )
        {
            var allItem = cblStatus.Items.FindByValue( "All" );

            if ( isVisible )
            {
                if ( allItem == null )
                {
                    cblShow.Items.Add( new ListItem( "All", "All" ) );
                }
            }
            else
            {
                if ( allItem != null )
                {
                    cblShow.Items.Remove( allItem );
                }
            }
        }

        /*
        private void SetRequesterFilter( int personId )
        {
            var requester = new Person( personId );

            if ( requester.PersonID <= 0 )
            {
                hfRequesterId.Value = "0";
                lRequesterName.Text = "(All)";
                lbRequesterRemove.Visible = false;
            }
            else
            {
                hfRequesterId.Value = requester.PersonID.ToString();
                lRequesterName.Text = requester.FullName;
                lbRequesterRemove.Visible = true;
            }
        }

        private bool UserCanEdit()
        {
            return CurrentModule.Permissions.Allowed( Arena.Security.OperationType.Edit, CurrentUser );
        }

        private bool UserIsFinanceApprover()
        {
            string viewStateKey = string.Format( "{0}_UserIsFinanceApprover", CurrentModule.ModuleInstanceID );

            if ( ViewState[viewStateKey] != null )
            {
                return (bool)ViewState[viewStateKey];
            }

            bool isApprover = false;
			
            if ( new ProfileMemberCollection( FinanceApproverTagSetting ).Where( p => p.PersonID == CurrentPerson.PersonID ).Count() > 0 )
            {
                var proMember = new ProfileMember( FinanceApproverTagSetting, CurrentPerson.PersonID );

                if(proMember.Status.LookupID == new Lookup(Arena.Core.SystemLookup.TagMemberStatus_Connected).LookupID)
                {
                    isApprover = true;
                }

            }

            ViewState[viewStateKey] = isApprover;

            return isApprover;
        }

    */
        #endregion

        #region Capital Request Grid Events

        protected void grdCapitalRequests_ReBind( object sender, EventArgs e )
        {
            LoadCapitalRequests();

        }

        #endregion
    }

}