using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;
using Newtonsoft.Json;
using Rock.Web.UI;
using System.Data.Entity;

namespace RockWeb.Plugins.org_secc.GroupManager
{

    [DisplayName( "Group Recommit" )]
    [Category( "Groups" )]
    [Description( "Allows a person to sign up to lead a new group, or copy one they are already a leader of." )]

    //Settings
    [GroupTypeField( "Group Type", "Type of group to copy or generate.", true, "", "", 0 )]
    [GroupField( "Group Parent", "If selected only groups under this group will be used.", true, "", "", 1 )]
    [GroupField( "Destination Group", "Location to place new groups.", true, "", "", 2 )]
    [BooleanField( "Show Description", "Option to toggle if the group description is to be shown for editing", true, "", 3 )]
    [TextField( "Save Text", "Text to display on save button", true, "Sign Up To Lead Group", "", 4 )]
    [GroupRoleField( "", "Group Role", "Group role that the user will be saved as. You will need to select the group type before selecting the group role.", true, "", "", 5 )]
    [CodeEditorField("Success Text", "Text to display to user upon successfully creating new group.", CodeEditorMode.Text,CodeEditorTheme.Rock,
        200,true, "You have successfully signed up to lead a group.", "",5)]
    [CodeEditorField( "Login Text", "Text to display when user account cannot be determined", CodeEditorMode.Text, CodeEditorTheme.Rock,
        200, true, "We're sorry we could not find your account in our system. Please log-in to continue.", "", 6 )]
    [CodeEditorField( "Multiple Groups Text", "Text to display when too many groups are found to make recomitment a possiblity.", CodeEditorMode.Text, CodeEditorTheme.Rock,
        200, true, "We found multiple groups matched to you. Please contact your leader to help you create your groups for this cycle.", "", 7 )]
    [CodeEditorField( "Destination Group Text", "Text to display when it is suspected that the user has already had a group made.", CodeEditorMode.Text, CodeEditorTheme.Rock,
        200, true, "A group has already been created for you. If you think this is in error, or you would like to create another group please contact your leader.", "", 8 )]

    public partial class GroupRecommit : RockBlock
    {
        private Person _person;
        private Group _baseGroup;
        private Group _group;
        private GroupTypeCache _groupType;
        private RockContext _rockContext;

        #region Base Control Methods

        protected override object SaveViewState()
        {
            var jsonSetting = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                ContractResolver = new Rock.Utility.IgnoreUrlEncodedKeyContractResolver()
            };

            ViewState["Group"] = JsonConvert.SerializeObject( _group, Formatting.None, jsonSetting );

            return base.SaveViewState();
        }

        protected override void LoadViewState( object savedState )
        {
            base.LoadViewState( savedState );

            var json = ViewState["Group"] as string;
            if ( string.IsNullOrWhiteSpace( json ) )
            {
                _group = new Group();
            }
            else
            {
                _group = JsonConvert.DeserializeObject<Group>( json ) ?? new Group();
            }
        }


        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            rblSchedule.BindToEnum<ScheduleType>();

            if ( _rockContext == null )
            {
                _rockContext = new RockContext();
            }

            gMembers.GridRebind += gMembers_GridRebind;
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
            LoadPerson();
            if ( _person == null )
            {
                ShowMessage( GetAttributeValue("LoginText"), "Information");
                return;
            }

            if ( string.IsNullOrWhiteSpace( GetAttributeValue( "GroupType" ) )
                || string.IsNullOrWhiteSpace( GetAttributeValue( "DestinationGroup" ) ) )
            {
                ShowMessage( "Block not configured. Please configure to use.", "Configuration Error", "panel panel-danger" );
                return;
            }

            _groupType = GroupTypeCache.Read( GetAttributeValue( "GroupType" ).AsGuid() );

            if ( !IsDestinationAvailable() )
            {
                ShowMessage( GetAttributeValue("DestinationGroupText"));
                return;
            }

            var groups = LoadGroups();
            //if we load the groups and there are too many we need to stop because the logic needs a person
            if ( groups.Count() > 1 )
            {
                ShowMessage(GetAttributeValue( "Multiple Groups Text" ));
                return;
            }

            _baseGroup = groups.FirstOrDefault();


            if ( !Page.IsPostBack )
            {
                //Create/copy group and fill it full of properties and attributes
                HydrateGroup();
                LoadControls();
            }
            else if ( _group != null )
            {
                Rock.Attribute.Helper.AddEditControls( _group, phAttributes, false );
            }
        }

        private void HydrateGroup()
        {
            _group = new Group() { GroupTypeId = _groupType.Id };
            if ( _baseGroup != null )
            {
                ltTitle.Text = "Recommit To Lead Group";
                _baseGroup.LoadAttributes();

                //Copy group
                _group.CopyPropertiesFrom( _baseGroup );
                _group.CopyAttributesFrom( _baseGroup );

                //Copy schedule
                var schedule = new Schedule();

                if ( _baseGroup.Schedule != null )
                {
                    schedule.CopyPropertiesFrom( _baseGroup.Schedule );
                    _group.Schedule = schedule;
                }

                //Copy group location
                var location = new Location();
                if ( _baseGroup.GroupLocations.Any() )
                {
                    location.CopyPropertiesFrom( _baseGroup.GroupLocations.First().Location );
                    _group.GroupLocations.Add( new GroupLocation() { Location = location } );
                }

                pnlMembers.Visible = true;
                _group.Members = _baseGroup.Members;
            }
            else
            {
                ltTitle.Text = "Lead New Group";
                _group.LoadAttributes();
            }

            SaveViewState();
        }

        private void gMembers_GridRebind( object sender, EventArgs e )
        {
            BindGrid();
        }

        private void BindGrid()
        {
            var members = _group.Members
                    .Where( gm => gm.PersonId != _person.Id )
                    .OrderByDescending( gm => gm.GroupRole.IsLeader )
                    .DistinctBy( gm => gm.PersonId )
                    .ToList();
            gMembers.DataSource = members;
            gMembers.DataBind();
        }

        private void LoadControls()
        {
            tbName.Text = _group.Name;

            //Load schedule control
            if ( _groupType.AllowedScheduleTypes != ScheduleType.None )
            {
                rblSchedule.Items.Clear();

                ListItem liNone = new ListItem( "None", "0" );
                liNone.Selected = _group != null && ( _group.Schedule == null || _group.Schedule.ScheduleType == ScheduleType.None );
                rblSchedule.Items.Add( liNone );

                if ( _groupType != null && ( _groupType.AllowedScheduleTypes & ScheduleType.Weekly ) == ScheduleType.Weekly )
                {
                    ListItem li = new ListItem( "Weekly", "1" );
                    li.Selected = _group != null && _group.Schedule != null && _group.Schedule.ScheduleType == ScheduleType.Weekly;
                    rblSchedule.Items.Add( li );
                }

                if ( _groupType != null && ( _groupType.AllowedScheduleTypes & ScheduleType.Custom ) == ScheduleType.Custom )
                {
                    ListItem li = new ListItem( "Custom", "2" );
                    li.Selected = _group != null && _group.Schedule != null && _group.Schedule.ScheduleType == ScheduleType.Custom;
                    rblSchedule.Items.Add( li );
                }

                if ( _groupType != null && ( _groupType.AllowedScheduleTypes & ScheduleType.Named ) == ScheduleType.Named )
                {
                    ListItem li = new ListItem( "Named", "4" );
                    li.Selected = _group != null && _group.Schedule != null && _group.Schedule.ScheduleType == ScheduleType.Named;
                    rblSchedule.Items.Add( li );
                }
                if ( _group.Schedule != null )
                {
                    //Load values into schedule controls
                    switch ( _group.Schedule.ScheduleType )
                    {
                        case ScheduleType.None:
                            break;
                        case ScheduleType.Weekly:
                            dowWeekly.SelectedDayOfWeek = _group.Schedule.WeeklyDayOfWeek;
                            timeWeekly.Text = _group.Schedule.WeeklyTimeOfDay.ToString();
                            rblSchedule.SelectedValue = "1";
                            break;
                        case ScheduleType.Custom:
                            sbSchedule.iCalendarContent = _group.Schedule.iCalendarContent;
                            rblSchedule.SelectedValue = "2";
                            break;
                        case ScheduleType.Named:
                            spSchedule.SetValue( _group.ScheduleId );
                            rblSchedule.SelectedValue = "4";
                            break;
                        default:
                            break;
                    }
                    UpdateScheduleDisplay();
                }
                BindGrid();
            }
            else
            {
                rblSchedule.Visible = false;
            }


            //Load location
            GroupLocationPickerMode groupTypeModes = _groupType.LocationSelectionMode;
            if ( groupTypeModes != GroupLocationPickerMode.None )
            {
                // Set the location picker modes allowed based on the group type's allowed modes
                LocationPickerMode modes = LocationPickerMode.None;
                if ( ( groupTypeModes & GroupLocationPickerMode.Named ) == GroupLocationPickerMode.Named )
                {
                    modes = modes | LocationPickerMode.Named;
                }

                if ( ( groupTypeModes & GroupLocationPickerMode.Address ) == GroupLocationPickerMode.Address )
                {
                    modes = modes | LocationPickerMode.Address;
                }

                if ( ( groupTypeModes & GroupLocationPickerMode.Point ) == GroupLocationPickerMode.Point )
                {
                    modes = modes | LocationPickerMode.Point;
                }

                if ( ( groupTypeModes & GroupLocationPickerMode.Polygon ) == GroupLocationPickerMode.Polygon )
                {
                    modes = modes | LocationPickerMode.Polygon;
                }
                lopAddress.AllowedPickerModes = modes;
            }

            var groupLocation = _group.GroupLocations.FirstOrDefault();
            if ( groupLocation != null )
            {
                lopAddress.Location = groupLocation.Location;
            }
            Rock.Attribute.Helper.AddEditControls( _group, phAttributes, true );

            tbDescription.Visible = GetAttributeValue( "ShowDescription" ).AsBoolean();
            tbDescription.Text = _group.Description;
            btnSave.Text = GetAttributeValue( "SaveText" );
        }
        private List<Group> LoadGroups()
        {
            var parent = GetAttributeValue( "GroupParent" );
            return LoadGroups( GetAttributeValue( "GroupParent" ).AsGuidOrNull() );
        }

        private List<Group> LoadGroups( Guid? groupGuid )
        {
            var groupRole = new GroupTypeRoleService( _rockContext ).Get( GetAttributeValue( "GroupRole" ).AsGuid() );
            if ( groupRole == null )
            {
                return new List<Group>();
            }
            var groupService = new GroupService( _rockContext );
            IQueryable<Group> groupQry = groupService.Queryable( "GroupLocations.Location" );

            //Sort by group parent if option set
            if ( groupGuid != null )
            {
                var availableGroupIds = ( List<int> ) GetCacheItem( groupGuid.ToString() );

                if ( availableGroupIds == null || !availableGroupIds.Any())
                {
                    availableGroupIds = GetChildGroups( groupGuid ?? new Guid(), groupService ).Select( g => g.Id ).ToList();
                    AddCacheItem( groupGuid.ToString(), availableGroupIds );
                }

                groupQry = groupQry.Where( g => g.IsActive && g.GroupTypeId == _groupType.Id && availableGroupIds.Contains( g.Id ) );
            }
            else
            {
                //else just get the available groups of type
                groupQry = groupQry.Where( g => g.IsActive && g.GroupTypeId == _groupType.Id );
            }

            return groupQry.Where( g => g.Members.Where( gm => gm.GroupRoleId == groupRole.Id && gm.PersonId==_person.Id ).Any() ).ToList();
        }

        private List<Group> GetChildGroups( Guid groupGuid, GroupService groupService )
        {
            List<Group> childGroups = new List<Group>();
            var group = groupService.Get( groupGuid );
            childGroups.AddRange( group.Groups );
            List<Group> grandChildGroups = new List<Group>();
            foreach ( var childGroup in childGroups )
            {
                grandChildGroups.AddRange( GetChildGroups( childGroup.Guid, groupService ) );
            }
            childGroups.AddRange( grandChildGroups );
            return childGroups;
        }

        private void LoadPerson()
        {
            var personAliasId = PageParameter( "PersonAliasId" ).AsInteger();
            var personId = PageParameter( "PersonId" ).AsInteger();
            var urlEncodedKey = PageParameter( "UrlEncodedKey" );

            if ( personId != 0 )
            {
                _person = new PersonService( _rockContext ).Get( personId );
            }

            if ( personAliasId != 0 && _person == null )
            {
                _person = new PersonAliasService( _rockContext ).Get( personAliasId ).Person;
            }

            if ( !string.IsNullOrWhiteSpace( urlEncodedKey ) && _person == null )
            {
                _person = new PersonService( _rockContext ).GetByUrlEncodedKey( urlEncodedKey );
            }

            if ( _person == null && CurrentPerson != null )
            {
                _person = CurrentPerson;
            }
        }
        #endregion

        private void ShowMessage( string message, string header="Information", string cssClass="panel panel-warning" )
        {
            pnlMain.Visible = false;
            pnlInfo.Visible = true;
            ltHeading.Text = header;
            ltBody.Text = message;
            pnlInfo.CssClass = cssClass;
        }

        protected void btnSave_Click( object sender, EventArgs e )
        {
            if ( _group == null )
            {
                ShowMessage( "There was an issue with the viewstate. Please reload and try again. If the problem perssits contact an administrator.", "Error", "panel panel-danger");
                return;
            }

            if ( !IsDestinationAvailable() )
            {
                ShowMessage( "There has already been a group created for you. If you think this is in error, or you would like to create another group please contact your leader" );
                return;

            }
            GroupService groupService = new GroupService( _rockContext );
            //Add basic information
            Group group = new Group() { GroupTypeId = _groupType.Id };

            // Find the parent group
            Group parentGroup = groupService.Get( GetAttributeValue( "DestinationGroup" ).AsGuid() );
            // If we don't find it, this will go into the parent level.
            group.ParentGroupId = parentGroup.Id;

            foreach ( Group childGroup in parentGroup.Groups )
            {
                if ( _baseGroup != null )
                {
                    // If we find a group with the same name as the old child's, we've found where this should land
                    if ( _baseGroup.ParentGroup.Name == childGroup.Name )
                    {
                        group.ParentGroupId = childGroup.Id;
                    }
                        group.Name = tbName.Text;
                }
                else
                {
                    // If this is a new group, drop them in "Other"
                    if ( "Other" == childGroup.Name )
                    {
                        group.ParentGroupId = childGroup.Id;
                    }
                    var zip = "No Zip";
                    if (lopAddress.Location!=null && !string.IsNullOrWhiteSpace( lopAddress.Location.PostalCode ) )
                    {
                        zip = lopAddress.Location.PostalCode;
                    }
                    group.Name = string.Format( "{0} [{1}]", tbName.Text,  zip  );
                }
            }

            
            group.Description = tbDescription.Text;

            group.CreatedByPersonAliasId = _person.PrimaryAliasId;
            groupService.Add( group );

            //Add location
            if ( lopAddress.Location != null && lopAddress.Location.Id != 0 )
            {
                group.GroupLocations.Add( new GroupLocation() { LocationId = lopAddress.Location.Id } );
            }

            //Add Schedule
            if ( _groupType.AllowedScheduleTypes != ScheduleType.None )
            {
                switch ( rblSchedule.SelectedValueAsEnum<ScheduleType>() )
                {
                    case ScheduleType.None:
                        group.ScheduleId = null;
                        break;
                    case ScheduleType.Weekly:
                        var weeklySchedule = new Schedule() { WeeklyDayOfWeek = dowWeekly.SelectedDayOfWeek, WeeklyTimeOfDay = timeWeekly.SelectedTime };
                        group.Schedule = weeklySchedule;
                        break;
                    case ScheduleType.Custom:
                        var customSchedule = new Schedule() { iCalendarContent = sbSchedule.iCalendarContent };
                        group.Schedule = customSchedule;
                        break;
                    case ScheduleType.Named:
                        if ( spSchedule.SelectedValue.AsInteger() != 0 )
                        {
                            group.ScheduleId = spSchedule.SelectedValue.AsInteger();
                        }
                        break;
                    default:
                        break;
                }
            }

            //Add Group Members
            GroupMemberService groupMemberService = new GroupMemberService( _rockContext );
            var groupRoleId = new GroupTypeRoleService( _rockContext ).Get( GetAttributeValue( "GroupRole" ).AsGuid() ).Id;
            group.Members.Add( new GroupMember() { PersonId = _person.Id, GroupRoleId = groupRoleId } );

            if ( pnlMembers.Visible && gMembers.SelectedKeys.Count() != 0 )
            {
                foreach ( int groupMemberId in gMembers.SelectedKeys )
                {
                    var groupMember = groupMemberService.Get( groupMemberId );
                    group.Members.Add( new GroupMember() { PersonId = groupMember.PersonId, GroupRoleId = groupMember.GroupRoleId } );
                }
            }


            //Save attributes
            _rockContext.SaveChanges();
            group.LoadAttributes();
            Rock.Attribute.Helper.GetEditValues( phAttributes, group );
            group.SaveAttributeValues();

            //Update cache
            var availableGroupIds = ( List<int> ) GetCacheItem( GetAttributeValue("DestinationGroup") );
            availableGroupIds.Add( group.Id );
            AddCacheItem(GetAttributeValue("DestinationGroup"), availableGroupIds );
            ShowMessage(GetAttributeValue("SuccessText"), "Thank you!", "panel panel-success" );
        }

        protected void rblSchedule_SelectedIndexChanged( object sender, EventArgs e )
        {
            UpdateScheduleDisplay();
        }

        private void UpdateScheduleDisplay()
        {
            dowWeekly.Visible = false;
            timeWeekly.Visible = false;
            spSchedule.Visible = false;
            sbSchedule.Visible = false;

            if ( !string.IsNullOrWhiteSpace( rblSchedule.SelectedValue ) )
            {
                switch ( rblSchedule.SelectedValueAsEnum<ScheduleType>() )
                {
                    case ScheduleType.None:
                        {
                            break;
                        }

                    case ScheduleType.Weekly:
                        {
                            dowWeekly.Visible = true;
                            timeWeekly.Visible = true;
                            break;
                        }

                    case ScheduleType.Custom:
                        {
                            sbSchedule.Visible = true;
                            break;
                        }

                    case ScheduleType.Named:
                        {
                            spSchedule.Visible = true;
                            break;
                        }
                }
            }
        }

        private bool IsDestinationAvailable()
        {
            //Check to make sure there is no other group in destination where user is of selected group role
            var destinationGuid = GetAttributeValue( "DestinationGroup" ).AsGuidOrNull();
            if ( destinationGuid != null )
            {
                return LoadGroups( destinationGuid ).Count() == 0;
            }
            return false;
        }
    }
}
