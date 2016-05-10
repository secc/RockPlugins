using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.IO;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;
using System.Web.UI.HtmlControls;
using Newtonsoft.Json;

namespace RockWeb.Plugins.org_secc.GroupManager
{

    [DisplayName( "Group Recommit" )]
    [Category( "Groups" )]
    [Description( "Allows a person to sign up to lead a new group, or copy one they are already a leader of." )]

    //Settings
    [GroupTypeField( "Group Type", "Type of group to copy or generate.", true, "", "", 0 )]
    [GroupField( "Group Parent", "If selected only groups under this group will be used.", false, "", "", 1 )]
    [GroupField( "Destination Group", "Location to place new groups.", true, "", "", 2 )]
    public partial class GroupRecommit : Rock.Web.UI.RockBlock
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
                ShowMessage( "We're sorry we could not find your person in our system.", NotificationBoxType.Danger );
                return;
            }

            if ( string.IsNullOrWhiteSpace( GetAttributeValue( "GroupType" ) )
                || string.IsNullOrWhiteSpace( GetAttributeValue( "DestinationGroup" ) ) )
            {
                ShowMessage( "Block not configured. Please configure to use." );
                return;
            }

            _groupType = GroupTypeCache.Read( GetAttributeValue( "GroupType" ).AsGuid() );

            var groups = LoadGroups();
            //if we load the groups and there are too many we need to stop because the logic needs a person
            if ( groups.Count() > 1 )
            {
                ShowMessage( "Your group structure needs a human's help. Please contact your leader to continue." );
                return;
            }

            _baseGroup = groups.FirstOrDefault();
            _baseGroup.LoadAttributes();

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
            _group = new Group();
            if ( _baseGroup != null )
            {
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

            }
            _group.ParentGroupId = new GroupService( _rockContext ).Get( GetAttributeValue( "DestinationGroup" ).AsGuid() ).Id;
            SaveViewState();
        }

        private void LoadControls()
        {
            tbName.Text = _group.Name;

            if ( _group.Schedule != null )
            {
                dowWeekly.SelectedDayOfWeek = _group.Schedule.WeeklyDayOfWeek;
                timeWeekly.Text = _group.Schedule.WeeklyTimeOfDay.ToString();
            }

            var groupLocation = _group.GroupLocations.FirstOrDefault();
            if ( groupLocation != null )
            {
                lopAddress.Location = groupLocation.Location;
            }
            Rock.Attribute.Helper.AddEditControls( _group, phAttributes, true );
        }

        private List<Group> LoadGroups()
        {
            if ( _rockContext == null )
            {
                _rockContext = new RockContext();
            }

            var groupService = new GroupService( _rockContext );
            IQueryable<Group> groupQry = groupService.Queryable( "GroupLocations.Location" );

            //Sort by group parent if option set
            if ( GetAttributeValue( "GroupParent" ).AsGuidOrNull() != null )
            {
                var availableGroupIds = ( List<int> ) GetCacheItem( "AvailableGroupIds" );

                if ( availableGroupIds == null )
                {
                    availableGroupIds = GetChildGroups( GetAttributeValue( "GroupParent" ).AsGuid(), groupService ).Select( g => g.Id ).ToList();
                    AddCacheItem( "AvailableGroupIds", availableGroupIds );
                }

                groupQry = groupQry.Where( g => g.IsActive && g.GroupType.Guid.Equals( _groupType.Guid ) && g.IsPublic && availableGroupIds.Contains( g.Id ) );
            }
            else
            {
                //else just get the available groups of type
                groupQry = groupQry.Where( g => g.IsActive && g.GroupType.Guid.Equals( _groupType.Guid ) && g.IsPublic );
            }


            return new GroupMemberService( _rockContext ).Queryable().Where( m => m.PersonId == _person.Id && m.GroupRole.IsLeader )
                .Join( groupQry, gm => gm.GroupId, g => g.Id, ( gm, g ) => g ).ToList();
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

            if ( _rockContext == null )
            {
                _rockContext = new RockContext();
            }

            var personAliasId = PageParameter( "PersonAlias" ).AsInteger();
            var personId = PageParameter( "Person" ).AsInteger();
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

        private void ShowMessage( string message, NotificationBoxType notificationBoxType = NotificationBoxType.Warning )
        {
            nbWarning.Text = message;
            nbWarning.NotificationBoxType = notificationBoxType;
            nbWarning.Visible = true;
        }
    }
}