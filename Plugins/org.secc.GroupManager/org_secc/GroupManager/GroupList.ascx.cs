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

    public partial class GroupList : GroupManagerBlock
    {
        protected override void OnInit( EventArgs e )
        {
            AllowLoadTheme = false;
            base.OnInit( e );
            ClearThemeCookie();
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
            foreach ( var group in groups.OrderBy( g => g.Name ) )
            {
                LinkButton lbGroup = new LinkButton()
                {
                    Text = group.Name,
                    ID = group.Guid.ToString(),
                    CssClass = "btn btn-block"
                };
                lbGroup.Click += ( s, ee ) => LoadGroup( group );
                phContent.Controls.Add( lbGroup );
            }
        }

        private void LoadGroup( Group group )
        {
            Session["CurrentGroupManagerGroup"] = group.Id;

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
