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
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock;
using Rock.Constants;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.UI;
using org.secc.FamilyCheckin.Model;
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

            gSchedules.DataKeyNames = new string[] { "Id" };
            gSchedules.Actions.ShowAdd = true;
            gSchedules.Actions.AddClick += gSchedules_AddClick;
            gSchedules.GridRebind += gSchedules_GridRebind;
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


        protected void btnSave_Click( object sender, EventArgs e )
        {
            KioskType KioskType = null;

            var rockContext = new RockContext();
            var kioskTypeService = new KioskTypeService( rockContext );
            var attributeService = new AttributeService( rockContext );
            var locationService = new LocationService( rockContext );
            var scheduleService = new ScheduleService( rockContext );
            var groupTypeService = new GroupTypeService( rockContext );

            int KioskTypeId = int.Parse( hfKioskTypeId.Value );

            if ( KioskTypeId != 0 )
            {
                KioskType = kioskTypeService.Get( KioskTypeId );
            }

            if ( KioskType == null )
            {
                KioskType = new KioskType();
                kioskTypeService.Add( KioskType );

            }

            if ( KioskType != null )
            {
                KioskType.Name = tbName.Text;
                KioskType.Description = tbDescription.Text;
                KioskType.Message = tbMessage.Text;

                if ( !KioskType.IsValid || !Page.IsValid )
                {
                    // Controls will render the error messages
                    return;
                }

                // Remove any deleted locations
                foreach ( var location in KioskType.Locations
                    .Where( l =>
                        !Locations.Keys.Contains( l.Id ) )
                    .ToList() )
                {
                    KioskType.Locations.Remove( location );
                }

                // Remove any deleted schedules
                foreach ( var schedule in KioskType.Schedules
                    .Where( s =>
                        !Schedules.Keys.Contains( s.Id ) )
                    .ToList() )
                {
                    KioskType.Schedules.Remove( schedule );
                }

                // Add any new locations
                var existingLocationIDs = KioskType.Locations.Select( l => l.Id ).ToList();
                foreach ( var location in locationService.Queryable()
                    .Where( l =>
                        Locations.Keys.Contains( l.Id ) &&
                        !existingLocationIDs.Contains( l.Id ) ) )
                {
                    KioskType.Locations.Add( location );
                }

                // Add any new schedules
                var existingScheduleIDs = KioskType.Schedules.Select( s => s.Id ).ToList();
                foreach ( var schedule in scheduleService.Queryable()
                    .Where( s =>
                        Schedules.Keys.Contains( s.Id ) &&
                        !existingScheduleIDs.Contains( s.Id ) ) )
                {
                    KioskType.Schedules.Add( schedule );
                }

                //Save checkin template
                KioskType.CheckinTemplateId = ddlTemplates.SelectedValue.AsInteger();


                var GroupTypes = KioskType.GroupTypes;
                GroupTypes.Clear();

                foreach ( ListItem item in cblPrimaryGroupTypes.Items )
                {
                    if ( item.Selected )
                    {
                        GroupTypes.Add( groupTypeService.Get( item.Value.AsInteger() ) );
                    }
                }

                rockContext.SaveChanges();

                Rock.CheckIn.KioskDevice.FlushAll();

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
        }

        protected void gLocations_GridRebind( object sender, EventArgs e )
        {
            BindSchedules();
        }

        protected void gSchedules_AddClick( object sender, EventArgs e )
        {
            schedulePicker.SetValue( 0 );
            mdSchedulepicker.Show();
        }

        protected void gSchedules_Delete( object sender, Rock.Web.UI.Controls.RowEventArgs e )
        {
            if ( Schedules.ContainsKey( e.RowKeyId ) )
            {
                Schedules.Remove( e.RowKeyId );
            }
            BindSchedules();
        }

        protected void gSchedules_GridRebind( object sender, EventArgs e )
        {
            BindSchedules();
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

            hfAddLocationId.Value = string.Empty;
            mdLocationPicker.Hide();
        }

        protected void mdSchedulepicker_SaveClick( object sender, EventArgs e )
        {
            var schedule = new ScheduleService( new RockContext() ).Get( schedulePicker.SelectedValue.AsInteger() );
            if ( schedule != null )
            {
                if ( !Schedules.ContainsKey( schedule.Id ) )
                {
                    Schedules.Add( schedule.Id, schedule.Name );
                }
            }
            BindSchedules();
            mdSchedulepicker.Hide();
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

            var checkinContext = new RockContext();

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

            hfKioskTypeId.Value = KioskType.Id.ToString();

            tbName.Text = KioskType.Name;
            tbDescription.Text = KioskType.Description;
            tbMessage.Text = KioskType.Message;

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

            Schedules = new Dictionary<int, string>();
            foreach ( var schedule in KioskType.Schedules )
            {
                Schedules.Add( schedule.Id, schedule.Name );
            }

            BindDropDownList( KioskType );
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

        private void BindDropDownList( KioskType kioskType = null )
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
            if ( kioskType != null )
            {
                ddlTemplates.SetValue( kioskType.CheckinTemplateId );
            }
            BindGroupTypes( kioskType );
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

        private void BindGroupTypes( KioskType kioskType = null )
        {

            var groupTypeIds = new List<string>();
            if ( kioskType != null )
            {
                groupTypeIds.AddRange( kioskType.GroupTypes.Select( gt => gt.Id.ToString() ).ToList() );
            }
            else
            {
                foreach ( ListItem item in cblPrimaryGroupTypes.Items )
                {
                    if ( item.Selected )
                    {
                        groupTypeIds.Add( item.Value );
                    }
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
            List<GroupType> primaryGroups = GetPrimaryGroupTypesFromTemplate( templateGroupType );
            cblPrimaryGroupTypes.DataSource = primaryGroups;
            cblPrimaryGroupTypes.DataBind();

            if ( selectedValues != string.Empty )
            {
                foreach ( string id in selectedValues.Split( ',' ) )
                {
                    ListItem item = cblPrimaryGroupTypes.Items.FindByValue( id );
                    if ( item != null )
                    {
                        item.Selected = true;
                    }
                }
            }
        }

        //finds all the lowest level group types
        //this only goes one level down which is good enough for me
        //not good enough for long term
        private List<GroupType> GetPrimaryGroupTypesFromTemplate( GroupType templateGroupType )
        {
            List<GroupType> primaryGroupTypes = new List<GroupType>();
            var primary = templateGroupType.ChildGroupTypes.Where( gt => gt.TakesAttendance );
            foreach ( var groupType in primary )
            {
                var children = groupType.ChildGroupTypes;
                if ( children.Any() )
                {
                    primaryGroupTypes.AddRange( children );
                    foreach ( var child in children )
                    {
                        if ( child.ChildGroupTypes.Any() )
                        {
                            primaryGroupTypes.AddRange( child.ChildGroupTypes.ToList() );
                        }
                        else
                        {
                            primaryGroupTypes.Add( child );
                        }
                    }
                }
                else
                {
                    primaryGroupTypes.Add( groupType );
                }
            }
            return primaryGroupTypes;
        }


    }
}