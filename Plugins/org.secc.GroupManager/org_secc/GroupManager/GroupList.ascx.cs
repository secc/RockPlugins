using Rock.Data;
using Rock.Model;
using Rock.Web.UI;
using System;
using System.ComponentModel;
using System.Linq;
using System.Web.UI.WebControls;
using Rock.Web.UI.Controls;
using System.Collections.Generic;
using Rock;
using Rock.Attribute;
using org.secc.GroupManager;
using System.Web;

namespace RockWeb.Plugins.org_secc.GroupManager
{
    [DisplayName( "Group List" )]
    [Category( "SECC > Groups" )]
    [Description( "Presents members of group in roster format." )]

    //Settings
    [GroupTypeField( "GroupType", "GroupType groups to display.", false )]
    [GroupField( "Group", "Group to display child groups", false )]
    [LinkedPage( "Next Page", "The next page in which " )]
    [TextField( "Theme", "Theme to switch to on page load.", false )]
    [BooleanField( "ForwardOnOne", "Should you forward if there is only one option?", false )]

    public partial class GroupList : GroupManagerBlock
    {
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            if ( !Page.IsPostBack )
            {
                ClearThemeCookie();
            }
        }

        protected override void OnLoad( EventArgs e )
        {
            List<Group> groups = new List<Group>();
            Guid? groupTypeGuid = GetAttributeValue( "GroupType" ).AsGuidOrNull();
            Guid? groupGuid = GetAttributeValue( "Group" ).AsGuidOrNull();
            RockContext rockContext = new RockContext();

            if ( groupGuid != null && groupTypeGuid != null )
            {
                List<int> groupIds = GetChildGroupIds( rockContext );

                groups.AddRange( new GroupTypeService( rockContext ).Queryable()
                    .Where( gt => gt.Guid == groupTypeGuid )
                    .SelectMany( gt => gt.Groups )
                    .Where( g => groupIds.Contains( g.Id ) )
                    );
            }
            else if ( groupGuid != null )
            {
                List<int> groupIds = GetChildGroupIds( rockContext );
                groups.AddRange( new GroupService( rockContext ).Queryable()
                    .Where( g => groupIds.Contains( g.Id ) )
                    );
            }
            else if ( groupTypeGuid != null )
            {
                groups.AddRange( new GroupTypeService( rockContext ).Queryable()
                    .Where( gt => gt.Guid == groupTypeGuid )
                    .SelectMany( gt => gt.Groups )
                    );
            }
            else
            {
                DisplayNotConfigured();
            }

            var requireLeader = GetAttributeValue( "LeadersOnly" ).AsBoolean();
            var doForward = GetAttributeValue( "ForwardOnOne" ).AsBoolean();
            if ( doForward && !UserCanAdministrate )
            {
                var groupList = new List<Group>();
                if ( requireLeader )
                {
                    groupList = groups.Where( g => g.Members.Where( m => m.PersonId == CurrentPerson.Id && m.GroupRole.IsLeader ).Any() ).ToList();
                }
                else
                {
                    groupList = groups.Where( g => g.Members.Where( m => m.PersonId == CurrentPerson.Id ).Any() ).ToList();
                }
                if ( groupList.Count() == 1 )
                {
                    LoadGroup( groupList.FirstOrDefault() );
                    return;
                }
            }

            foreach ( var group in groups.OrderBy( g => g.Name ) )
            {
                var groupMember = group.Members.Where( m => m.PersonId == CurrentPerson.Id ).FirstOrDefault();
                if ( !requireLeader
                    || ( groupMember != null && groupMember.GroupRole.IsLeader ) )
                {
                    LinkButton lbGroup = new LinkButton()
                    {
                        Text = group.Name,
                        ID = group.Guid.ToString()
                    };
                    lbGroup.Click += ( s, ee ) => LoadGroup( group );
                    phContent.Controls.Add( lbGroup );
                }
            }
        }

        private void LoadGroup( Group group )
        {
            SetUserPreference( "CurrentGroupManagerGroup", group.Id.ToString() );
            var theme = GetAttributeValue( "Theme" );
            if ( !string.IsNullOrWhiteSpace( theme ) )
            {
                SetThemeCookie( theme );
            }
            NavigateToLinkedPage( "NextPage" );
        }

        private void DisplayNotConfigured()
        {
            NotificationBox nbError = new NotificationBox()
            {
                NotificationBoxType = NotificationBoxType.Warning,
                Text = "Block not configured."
            };
            phContent.Controls.Add( nbError );

        }

        private List<int> GetChildGroupIds( RockContext rockContext )
        {
            var childGroupIds = ( List<int> ) GetCacheItem( BlockId.ToString() );
            if ( childGroupIds != null && childGroupIds.Any() )
            {
                return childGroupIds;
            }

            var parentGroup = new GroupService( rockContext ).Get( GetAttributeValue( "Group" ).AsGuid() );
            if ( parentGroup != null )
            {
                childGroupIds = GetChildGroups( parentGroup ).Select( g => g.Id ).ToList();
                AddCacheItem( BlockId.ToString(), childGroupIds, 300 );
                return childGroupIds;
            }
            return new List<int>();
        }

        private List<Group> GetChildGroups( Group group )
        {
            List<Group> childGroups = group.Groups.ToList();
            List<Group> grandChildGroups = new List<Group>();
            foreach ( var childGroup in childGroups )
            {
                grandChildGroups.AddRange( GetChildGroups( childGroup ) );
            }
            childGroups.AddRange( grandChildGroups );
            return childGroups;
        }
    }
}
