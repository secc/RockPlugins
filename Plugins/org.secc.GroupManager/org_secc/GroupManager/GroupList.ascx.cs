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
using System.Linq;
using System.Web.UI.WebControls;
using org.secc.GroupManager;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI.Controls;

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
            if ( CurrentPerson == null )
            {
                return;
            }

            try
            {
                Guid? groupTypeGuid = GetAttributeValue( "GroupType" ).AsGuidOrNull();
                Guid? groupGuid = GetAttributeValue( "Group" ).AsGuidOrNull();
                var requireLeader = GetAttributeValue( "LeadersOnly" ).AsBoolean();
                var doForward = GetAttributeValue( "ForwardOnOne" ).AsBoolean();

                if ( groupTypeGuid == null && groupGuid == null )
                {
                    DisplayNotConfigured();
                    return;
                }

                RockContext rockContext = new RockContext();

                var groupService = new GroupService( rockContext );

                var qry = groupService.Queryable().AsNoTracking();

                if ( groupTypeGuid != null )
                {
                    qry = qry.Where( g => g.GroupType.Guid == groupTypeGuid );
                }

                if ( groupGuid != null )
                {
                    //Check parent groups, grand parent groups and great grandparent groups
                    qry = qry.Where( g => g.ParentGroup.Guid == groupGuid
                                        || g.ParentGroup.ParentGroup.Guid == groupGuid
                                        || g.ParentGroup.ParentGroup.ParentGroup.Guid == groupGuid);
                }

                qry = qry.Where( g => g.IsActive && !g.IsArchived );

                qry = qry.Where( g => g.Members.Any( gm => gm.PersonId == CurrentPersonId && gm.GroupMemberStatus == GroupMemberStatus.Active ) );

                if ( requireLeader )
                {
                    qry = qry.Where( g => g.Members.Any( gm => gm.PersonId == CurrentPersonId && gm.GroupRole.IsLeader ) );
                }

                var groups = qry.ToList();

                if ( !groups.Any() )
                {
                    return;
                }

                if ( doForward && !UserCanAdministrate && groups.Count == 1 )
                {
                    LoadGroup( groups.FirstOrDefault() );

                }

                foreach ( var group in groups.OrderBy( g => g.Name ) )
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
            catch ( Exception ex )
            {
                LogException( new Exception( "Group List Exception:", ex ) );
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
    }
}
