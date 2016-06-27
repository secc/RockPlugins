// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
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
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock;
using Rock.Attribute;
using Rock.Constants;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI;
using org.secc.FamilyCheckin.Model;
using org.secc.FamilyCheckin.Data;
using System.Data.Entity;

namespace RockWeb.Plugins.org_secc.FamilyCheckin
{
    [DisplayName( "Kiosk Type Detail" )]
    [Category( "SECC > Check-in" )]
    [Description( "Displays the details of the given device." )]
    public partial class KioskTypeDetail : RockBlock, IDetailBlock
    {
        #region Properties

        private Dictionary<int, string> Locations
        {
            get
            {
                var locations = ViewState["Locations"] as Dictionary<int, string>;
                if ( locations == null )
                {
                    locations = new Dictionary<int, string>();
                    ViewState["Locations"] = locations;
                }
                return locations;
            }
            set
            {
                ViewState["Locations"] = value;
            }
        }

        private Dictionary<int, string> Schedules
        {
            get
            {
                var schedules = ViewState["Schedules"] as Dictionary<int, string>;
                if ( schedules == null )
                {
                    schedules = new Dictionary<int, string>();
                    ViewState["Schedules"] = schedules;
                }
                return schedules;
            }
            set
            {
                ViewState["Schedules"] = value;
            }
        }

        #endregion

        #region Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.AddConfigurationUpdateTrigger( upnlDevice );

            gLocations.DataKeyNames = new string[] { "Id" };
            gLocations.Actions.ShowAdd = true;
            gLocations.Actions.AddClick += gLocations_AddClick;
            gLocations.GridRebind += gLocations_GridRebind;
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            nbDuplicateDevice.Visible = false;

            if ( !Page.IsPostBack )
            {
                ShowDetail( PageParameter( "KioskTypeId" ).AsInteger() );
            }

            if ( hfAddLocationId.Value.AsIntegerOrNull().HasValue )
            {
                mdLocationPicker.Show();
            }

        }

        #endregion

        #region Events


        /// <summary>
        /// Handles the Click event of the btnSave control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void btnSave_Click( object sender, EventArgs e )
        {
            Device Device = null;

            var rockContext = new RockContext();
            var deviceService = new DeviceService( rockContext );
            var attributeService = new AttributeService( rockContext );
            var locationService = new LocationService( rockContext );

            int DeviceId = int.Parse( hfDeviceId.Value );

            if ( DeviceId != 0 )
            {
                Device = deviceService.Get( DeviceId );
            }

            if ( Device == null )
            {
                // Check for existing
                var existingDevice = deviceService.Queryable()
                    .Where( d => d.Name == tbName.Text )
                    .FirstOrDefault();
                if ( existingDevice != null )
                {
                    nbDuplicateDevice.Text = string.Format( "A device already exists with the name '{0}'. Please use a different device name.", existingDevice.Name );
                    nbDuplicateDevice.Visible = true;
                }
                else
                {
                    Device = new Device();
                    deviceService.Add( Device );
                }
            }

            if ( Device != null )
            {
                Device.Name = tbName.Text;
                Device.Description = tbDescription.Text;

                if ( !Device.IsValid || !Page.IsValid )
                {
                    // Controls will render the error messages
                    return;
                }

                // Remove any deleted locations
                foreach ( var location in Device.Locations
                    .Where( l =>
                        !Locations.Keys.Contains( l.Id ) )
                    .ToList() )
                {
                    Device.Locations.Remove( location );
                }

                // Add any new locations
                var existingLocationIDs = Device.Locations.Select( l => l.Id ).ToList();
                foreach ( var location in locationService.Queryable()
                    .Where( l =>
                        Locations.Keys.Contains( l.Id ) &&
                        !existingLocationIDs.Contains( l.Id ) ) )
                {
                    Device.Locations.Add( location );
                }

                rockContext.SaveChanges();

                Rock.CheckIn.KioskDevice.Flush( Device.Id );

                NavigateToParentPage();
            }
        }

        /// <summary>
        /// Handles the Click event of the btnCancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void btnCancel_Click( object sender, EventArgs e )
        {
            NavigateToParentPage();
        }

        /// <summary>
        /// Handles the ServerValidate event of the cvIpAddress control.
        /// </summary>
        /// <param name="source">The source of the event.</param>
        /// <param name="args">The <see cref="ServerValidateEventArgs"/> instance containing the event data.</param>
        protected void cvIpAddress_ServerValidate( object source, ServerValidateEventArgs args )
        {
            args.IsValid = VerifyUniqueIpAddress();
        }

        /// <summary>
        /// Handles when the device type selection is changed.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void ddlDeviceType_SelectedIndexChanged( object sender, EventArgs e )
        {

        }

        /// <summary>
        /// Handles when the Print To selection is changed.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void ddlPrintTo_SelectedIndexChanged( object sender, EventArgs e )
        {

        }

        protected void gLocations_AddClick( object sender, EventArgs e )
        {
            hfAddLocationId.Value = "0";
            mdLocationPicker.Show();
        }

        protected void gLocations_Delete( object sender, Rock.Web.UI.Controls.RowEventArgs e )
        {
            if ( Locations.ContainsKey( e.RowKeyId ) )
            {
                Locations.Remove( e.RowKeyId );
            }
            BindLocations();
            BindSchedules();
        }

        protected void gLocations_GridRebind( object sender, EventArgs e )
        {
            BindLocations();
        }

        protected void btnAddLocation_Click( object sender, EventArgs e )
        {
            // Add the location (ignore if they didn't pick one, or they picked one that already is selected)
            var location = new LocationService( new RockContext() ).Get( locationPicker.SelectedValue.AsInteger() );
            if ( location != null )
            {
                string path = location.Name;
                var parentLocation = location.ParentLocation;
                while ( parentLocation != null )
                {
                    path = parentLocation.Name + " > " + path;
                    parentLocation = parentLocation.ParentLocation;
                }
                Locations.Add( location.Id, path );
            }

            BindLocations();
            BindSchedules();

            hfAddLocationId.Value = string.Empty;
            mdLocationPicker.Hide();
        }

        #endregion

        #region Methods


        /// <summary>
        /// Shows the edit.
        /// </summary>
        /// <param name="DeviceId">The device identifier.</param>
        public void ShowDetail( int KioskTypeId )
        {
            pnlDetails.Visible = true;
            KioskType KioskType = null;

            var checkinContext = new FamilyCheckinContext();

            if ( !KioskTypeId.Equals( 0 ) )
            {
                KioskType = new KioskTypeService( checkinContext ).Get( KioskTypeId );
                lActionTitle.Text = ActionTitle.Edit( KioskType.FriendlyTypeName ).FormatAsHtmlTitle();
            }

            if ( KioskType == null )
            {
                KioskType = new KioskType { Id = 0 };
                lActionTitle.Text = ActionTitle.Add( KioskType.FriendlyTypeName ).FormatAsHtmlTitle();
            }

            hfDeviceId.Value = KioskType.Id.ToString();

            tbName.Text = KioskType.Name;
            tbDescription.Text = KioskType.Description;

            Locations = new Dictionary<int, string>();
            foreach ( var location in KioskType.Locations )
            {
                string path = location.Name;
                var parentLocation = location.ParentLocation;
                while ( parentLocation != null )
                {
                    path = parentLocation.Name + " > " + path;
                    parentLocation = parentLocation.ParentLocation;
                }
                Locations.Add( location.Id, path );
            }
            BindDropDownList();
            BindLocations();
            BindSchedules();


            // render UI based on Authorized and IsSystem
            bool readOnly = false;

            nbEditModeMessage.Text = string.Empty;
            if ( !IsUserAuthorized( Authorization.EDIT ) )
            {
                readOnly = true;
                nbEditModeMessage.Text = EditModeMessage.ReadOnlyEditActionNotAllowed( KioskType.FriendlyTypeName );
            }

            if ( readOnly )
            {
                lActionTitle.Text = ActionTitle.View( KioskType.FriendlyTypeName );
                btnCancel.Text = "Close";
            }

            tbName.ReadOnly = readOnly;
            tbDescription.ReadOnly = readOnly;

            btnSave.Visible = !readOnly;
        }

        private void BindDropDownList()
        {
            RockContext rockContext = new RockContext();
            GroupTypeService groupTypeService = new GroupTypeService( rockContext );
            Guid templateTypeGuid = Rock.SystemGuid.DefinedValue.GROUPTYPE_PURPOSE_CHECKIN_TEMPLATE.AsGuid();
            ddlTemplates.DataSource = groupTypeService
                .Queryable().AsNoTracking()
                .Where( t =>
                    t.GroupTypePurposeValue != null &&
                    t.GroupTypePurposeValue.Guid == templateTypeGuid )
                .OrderBy( t => t.Name )
                .Select( t => new
                {
                    t.Name,
                    t.Id
                } )
                .ToList();
            ddlTemplates.DataBind();
        }

        /// <summary>
        /// Verifies the ip address is unique.
        /// </summary>
        private bool VerifyUniqueIpAddress()
        {
            bool isValid = true;
            int currentDeviceId = int.Parse( hfDeviceId.Value );
            return isValid;
        }



        private void BindLocations()
        {
            gLocations.DataSource = Locations
                .OrderBy( l => l.Value )
                .Select( l => new
                {
                    Id = l.Key,
                    LocationPath = l.Value
                } )
                .ToList();
            gLocations.DataBind();
        }

        private void BindSchedules()
        {
            gSchedules.DataSource = Schedules
                .OrderBy( l => l.Value )
                .Select( l => new
                {
                    Id = l.Key,
                    Name = l.Value
                } )
                .ToList();
            gSchedules.DataBind();
        }

        #endregion


        protected void ddlTemplates_SelectedIndexChanged( object sender, EventArgs e )
        {
            BindGroupTypes();
        }

        private void BindGroupTypes()
        {
            var groupTypeIds = new List<string>();
            foreach ( ListItem item in cblPrimaryGroupTypes.Items )
            {
                if ( item.Selected )
                {
                    groupTypeIds.Add( item.Value );
                }
            }

            BindGroupTypes( groupTypeIds.AsDelimited( "," ) );
        }

        private void BindGroupTypes( string selectedValues )
        {
            cblPrimaryGroupTypes.Items.Clear();
            var selectedItems = selectedValues.Split( ',' );
            var templateGroupTypeId = ddlTemplates.SelectedValue.AsInteger();
            GroupTypeService groupTypeService = new GroupTypeService( new RockContext() );
            var templateGroupType = groupTypeService.Get( templateGroupTypeId );
            var primary = templateGroupType.ChildGroupTypes.Where(gt => gt.TakesAttendance).Select(gt => new { Id = gt.Id, Name = gt.Name } ).ToList();
            cblPrimaryGroupTypes.DataSource = primary;
            cblPrimaryGroupTypes.DataBind();
        }

        protected void gSchedules_Delete( object sender, Rock.Web.UI.Controls.RowEventArgs e )
        {

        }
    }
}