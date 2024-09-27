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
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using org.secc.FamilyCheckin.Cache;
using org.secc.FamilyCheckin.Model;
using org.secc.FamilyCheckin.Utilities;
using Rock;
using Rock.Attribute;
using Rock.Constants;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.UI;

namespace RockWeb.Plugins.org_secc.FamilyCheckin
{
    [DisplayName( "Kiosk Type Detail" )]
    [Category( "SECC > Check-in" )]
    [Description( "Displays the details of the given device." )]

    [BooleanField("Show Medical Consent Skips",
        Description = "A flag indicating if the Medical Consent Skips funcationality should be displayed. Default is false.",
        ControlType = Rock.Field.Types.BooleanFieldType.BooleanControlType.Checkbox,
        DefaultBooleanValue = false,
        IsRequired = false,
        Order = 0,
        Key = AttributeKeys.ShowMedicalConsentSkips )]
    public partial class KioskTypeDetail : RockBlock, IDetailBlock
    {

        public class AttributeKeys
        {
            public const string ShowMedicalConsentSkips = "ShowMedicalConsentSkips";
        }

        #region Fields
        string AttributeKey_RequireMedicalConsent = "ShowMedicalConsent";
        string AttributeKey_MedicalConsentSkips = "MedicalConsentSkipsAllowed";
        #endregion

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

            if (GetAttributeValue( AttributeKeys.ShowMedicalConsentSkips ).AsBoolean())
            {
                cbRequireMedicalConsent.AutoPostBack = true;
                cbRequireMedicalConsent.CheckedChanged += cbRequireMedicalConsent_CheckedChanged;
            }
            else
            {
                tbMedicalConsentMaxSkips.Visible = false;
            }
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
                BindThemeDropDown();
                ShowDetail( PageParameter( "KioskTypeId" ).AsInteger() );
            }

            if ( hfAddLocationId.Value.AsIntegerOrNull().HasValue )
            {
                mdLocationPicker.Show();
            }
        }

        private void BindThemeDropDown()
        {
            DirectoryInfo di = new DirectoryInfo( this.Page.Request.MapPath( ResolveRockUrl( "~~" ) ) );
            foreach ( var themeDir in di.Parent.EnumerateDirectories().OrderBy( a => a.Name ) )
            {
                var checkinLayout = themeDir.GetFiles( "Checkin-Site.Master", SearchOption.AllDirectories );
                if ( checkinLayout.Length > 0 )
                {
                    ddlTheme.Items.Add( new ListItem( themeDir.Name, themeDir.Name.ToLower() ) );
                }
            }
            ddlTheme.Items.Insert( 0, new ListItem( "", "" ) );
        }

        #endregion

        #region Events


        protected void btnSave_Click( object sender, EventArgs e )
        {
            KioskType kioskType = null;

            var rockContext = new RockContext();
            var kioskTypeService = new KioskTypeService( rockContext );
            var attributeService = new AttributeService( rockContext );
            var locationService = new LocationService( rockContext );
            var scheduleService = new ScheduleService( rockContext );
            var groupTypeService = new GroupTypeService( rockContext );

            int kioskTypeId = int.Parse( hfKioskTypeId.Value );

            if ( kioskTypeId != 0 )
            {
                kioskType = kioskTypeService.Get( kioskTypeId );
            }

            if ( kioskType == null )
            {
                kioskType = new KioskType();
                kioskTypeService.Add( kioskType );

            }

            if ( kioskType != null )
            {
                kioskType.Name = tbName.Text;
                kioskType.Description = tbDescription.Text;
                kioskType.Message = tbMessage.Text;
                kioskType.IsMobile = cbIsMobile.Checked;
                kioskType.MinutesValid = tbMinutesValid.Text.AsIntegerOrNull();
                kioskType.GraceMinutes = tbGraceMinutes.Text.AsIntegerOrNull();
                kioskType.Theme = ddlTheme.SelectedValue;

                if ( !kioskType.IsValid || !Page.IsValid )
                {
                    // Controls will render the error messages
                    return;
                }

                // Remove any deleted locations
                foreach ( var location in kioskType.Locations
                    .Where( l =>
                        !Locations.Keys.Contains( l.Id ) )
                    .ToList() )
                {
                    kioskType.Locations.Remove( location );
                }

                // Remove any deleted schedules
                foreach ( var schedule in kioskType.Schedules
                    .Where( s =>
                        !Schedules.Keys.Contains( s.Id ) )
                    .ToList() )
                {
                    kioskType.Schedules.Remove( schedule );
                }

                // Add any new locations
                var existingLocationIDs = kioskType.Locations.Select( l => l.Id ).ToList();
                foreach ( var location in locationService.Queryable()
                    .Where( l =>
                        Locations.Keys.Contains( l.Id ) &&
                        !existingLocationIDs.Contains( l.Id ) ) )
                {
                    kioskType.Locations.Add( location );
                }

                // Add any new schedules
                var existingScheduleIDs = kioskType.Schedules.Select( s => s.Id ).ToList();
                foreach ( var schedule in scheduleService.Queryable()
                    .Where( s =>
                        Schedules.Keys.Contains( s.Id ) &&
                        !existingScheduleIDs.Contains( s.Id ) ) )
                {
                    kioskType.Schedules.Add( schedule );
                }

                //Save checkin template
                kioskType.CheckinTemplateId = ddlTemplates.SelectedValue.AsInteger();


                var GroupTypes = kioskType.GroupTypes;
                GroupTypes.Clear();

                foreach ( ListItem item in cblPrimaryGroupTypes.Items )
                {
                    if ( item.Selected )
                    {
                        GroupTypes.Add( groupTypeService.Get( item.Value.AsInteger() ) );
                    }
                }

                kioskType.CampusId = ddlCampus.SelectedCampusId;

                rockContext.SaveChanges();

                kioskType.LoadAttributes( rockContext );
                kioskType.SetAttributeValue( AttributeKey_RequireMedicalConsent, cbRequireMedicalConsent.Checked.ToString() );
                if(cbRequireMedicalConsent.Checked && tbMedicalConsentMaxSkips.Text.AsIntegerOrNull().HasValue)
                {
                    kioskType.SetAttributeValue( AttributeKey_MedicalConsentSkips, tbMedicalConsentMaxSkips.Text.AsInteger() );
                }
                else
                {
                    kioskType.SetAttributeValue( AttributeKey_MedicalConsentSkips, null );
                }
                kioskType.SaveAttributeValues( rockContext );

                rockContext.SaveChanges();

                KioskTypeCache.Remove( kioskType.Id );
                KioskTypeCache.Get( kioskType.Id );
                KioskDeviceHelpers.Clear( kioskType.GroupTypes.Select( gt => gt.Id ).ToList() );

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

        private void cbRequireMedicalConsent_CheckedChanged( object sender, EventArgs e )
        {
            tbMedicalConsentMaxSkips.Visible = cbRequireMedicalConsent.Checked;
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
        public void ShowDetail( int kioskTypeId )
        {
            pnlDetails.Visible = true;
            KioskType kioskType = null;

            var checkinContext = new RockContext();

            if ( !kioskTypeId.Equals( 0 ) )
            {
                kioskType = new KioskTypeService( checkinContext ).Get( kioskTypeId );
                lActionTitle.Text = ActionTitle.Edit( KioskType.FriendlyTypeName ).FormatAsHtmlTitle();
                kioskType.LoadAttributes( checkinContext );
            }

            if ( kioskType == null )
            {
                kioskType = new KioskType { Id = 0 };
                lActionTitle.Text = ActionTitle.Add( KioskType.FriendlyTypeName ).FormatAsHtmlTitle();
            }

            hfKioskTypeId.Value = kioskType.Id.ToString();

            tbName.Text = kioskType.Name;
            tbDescription.Text = kioskType.Description;
            tbMessage.Text = kioskType.Message;
            cbIsMobile.Checked = kioskType.IsMobile;
            tbGraceMinutes.Text = kioskType.GraceMinutes.ToString();
            tbMinutesValid.Text = kioskType.MinutesValid.ToString();

            ddlTheme.SelectedValue = kioskType.Theme;

            Locations = new Dictionary<int, string>();
            foreach ( var location in kioskType.Locations )
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
            foreach ( var schedule in kioskType.Schedules )
            {
                Schedules.Add( schedule.Id, schedule.Name );
            }

            BindDropDownList( kioskType );
            BindLocations();
            BindSchedules();

            if(kioskType.Id > 0)
            {
                cbRequireMedicalConsent.Checked = kioskType.GetAttributeValue( AttributeKey_RequireMedicalConsent ).AsBoolean();
                var skipsAllowed = kioskType.GetAttributeValue( AttributeKey_MedicalConsentSkips ).AsInteger();
                tbMedicalConsentMaxSkips.Visible = cbRequireMedicalConsent.Checked && GetAttributeValue(AttributeKeys.ShowMedicalConsentSkips).AsBoolean();
                tbMedicalConsentMaxSkips.Text = skipsAllowed >= 0 ? skipsAllowed.ToString() : string.Empty;

            }

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

            ddlCampus.SelectedCampusId = kioskType.CampusId;

            tbName.ReadOnly = readOnly;
            tbDescription.ReadOnly = readOnly;
            tbMedicalConsentMaxSkips.ReadOnly = readOnly;
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