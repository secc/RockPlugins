using System;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;


namespace RockWeb.Plugins.org_secc.Cms
{

    [DisplayName( "Link List - Edit Users" )]
    [Description( "Add/Remove people who can update a specific link list." )]
    [Category( "SECC > CRM" )]

    [ContentChannelField( "Linked List Content Channel",
        Description = "The Linked List content channel",
        IsRequired = true,
        Order = 0,
        Key = "LinkedListChannel" )]
    [TextField( "Security Group Name Format",
        Description = "The text/name format used for the security role groups.",
        IsRequired = true,
        Order = 1,
        Key = "SecurityGroupNameFormat" )]
    public partial class LinkListEditUsers : RockBlock
    {

        #region Control Methods
        protected override void OnInit( EventArgs e )
        {
            gListUsers.Actions.AddButton.Visible = true;
            gListUsers.Actions.ShowExcelExport = false;
            gListUsers.ShowWorkflowOrCustomActionButtons = false;
            gListUsers.Actions.ShowMergePerson = false;
            gListUsers.Actions.ShowMergeTemplate = false;
            gListUsers.RowDataBound += gListUsers_RowDataBound;
            gListUsers.RowCommand += gListUsers_RowCommand;
            gListUsers.Actions.AddClick += gListUsers_AddClick;
            lbAddGroupMember.Click += lbAddGroupMember_Click;
            lbCancel.Click += lbCancel_Click;
            base.OnInit( e );
        }



        protected override void OnLoad( EventArgs e )
        {
            nbNotification.Text = string.Empty;
            nbNotification.Visible = false;
            LoadLinkList();
            base.OnLoad( e );
        }
        #endregion

        #region Events

        private void gListUsers_AddClick( object sender, EventArgs e )
        {
            ppGroupMember.SelectedValue = null;
            mdAddGroupMember.Show();
        }

        private void gListUsers_RowCommand( object sender, GridViewCommandEventArgs e )
        {
            if (e.CommandName.Equals( "RemoveMember", StringComparison.InvariantCultureIgnoreCase ))
            {
                var groupMemberId = e.CommandArgument.ToString().AsInteger();

                GroupMemberRemove( groupMemberId );
            }
        }

        private void gListUsers_RowDataBound( object sender, GridViewRowEventArgs e )
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                var groupMember = e.Row.DataItem as GroupMember;
                Label lName = e.Row.FindControl( "lName" ) as Label;
                LinkButton lbRemove = e.Row.FindControl( "lbRemove" ) as LinkButton;

                lName.Text = groupMember.Person.FullName;
                lbRemove.CommandArgument = groupMember.Id.ToString();
            }
        }


        private void lbAddGroupMember_Click( object sender, EventArgs e )
        {
            if (ppGroupMember.SelectedValue.HasValue)
            {
                GroupMemberAdd( ppGroupMember.SelectedValue.Value );
            }
            mdAddGroupMember.Hide();
        }


        private void lbCancel_Click( object sender, EventArgs e )
        {
            ppGroupMember.Visible = false;
            var groupId = hfSecurityGroupId.Value.AsInteger();
            LoadEditorList( groupId );
            mdAddGroupMember.Hide();
        }

        #endregion

        private Group GetListEditorsGroup( ContentChannelItem listItem )
        {
            var contentChannelItemET = EntityTypeCache.Get( typeof( ContentChannelItem ) );
            var securityRoleGT = GroupTypeCache.Get( Rock.SystemGuid.GroupType.GROUPTYPE_SECURITY_ROLE.AsGuid() );
            var securityGroupName = string.Format( GetAttributeValue( "SecurityGroupNameFormat" ), listItem.Title );
            using (var groupContext = new RockContext())
            {
                var groupService = new GroupService( groupContext );
                var authService = new AuthService( groupContext );

                var authQry = authService.Queryable()
                    .Where( a => a.EntityTypeId == contentChannelItemET.Id )
                    .Where( a => a.EntityId == listItem.Id )

                    .Where( a => a.Action.Equals( "edit", StringComparison.InvariantCultureIgnoreCase ) );

                Group authGroup = authQry
                    .Where( a => a.Group.Name.Equals( securityGroupName, StringComparison.InvariantCultureIgnoreCase ) )
                    .Where( a => a.Group.IsActive )
                    .Where( a => a.AllowOrDeny == "A" )
                    .OrderBy( a => a.Order )
                    .Select( a => a.Group )
                    .FirstOrDefault();

                if (authGroup == null)
                {
                    authGroup = groupService.Queryable()
                        .Where( g => g.GroupTypeId == securityRoleGT.Id )
                        .Where( g => g.IsActive && !g.IsArchived )
                        .Where( g => g.Name.Equals( securityGroupName, StringComparison.InvariantCultureIgnoreCase ) )
                        .OrderBy( g => g.Order )
                        .FirstOrDefault();

                    if (authGroup == null)
                    {
                        authGroup = new Group
                        {
                            IsSystem = true,
                            Name = securityGroupName,
                            GroupTypeId = securityRoleGT.Id,
                            IsPublic = false,
                            IsActive = true,
                            IsArchived = false,
                            IsSecurityRole = true,
                            AttendanceRecordRequiredForCheckIn = AttendanceRecordRequiredForCheckIn.ScheduleNotRequired,
                            DisableScheduleToolboxAccess = false,
                            SchedulingMustMeetRequirements = false,
                            DisableScheduling = false,
                        };

                        groupService.Add( authGroup );
                        groupContext.SaveChanges();
                    }

                    var auth = new Auth
                    {
                        EntityTypeId = contentChannelItemET.Id,
                        EntityId = listItem.Id,
                        Order = 0,
                        AllowOrDeny = "A",
                        SpecialRole = SpecialRole.None,
                        GroupId = authGroup.Id,

                    };

                    authService.Add( auth );
                    groupContext.SaveChanges();

                }

                return authGroup;
            }
        }

        private void LoadEditorList( int groupId )
        {
            using (var memberContext = new RockContext())
            {
                var groupMemberService = new GroupMemberService( memberContext );
                var members = groupMemberService.Queryable()
                    .Include( m => m.Person )
                    .Where( m => m.GroupId == groupId )
                    .Where( m => !m.IsArchived )
                    .Where( m => m.GroupMemberStatus == GroupMemberStatus.Active )
                    .OrderBy( m => m.Person.LastName )
                    .ThenBy( m => m.Person.NickName )
                    .ToList();

                gListUsers.DataSource = members;
                gListUsers.DataBind();

            }
        }

        private void LoadLinkList()
        {
            var contentChannelGuid = GetAttributeValue( "LinkedListChannel" ).AsGuid();
            var contentChannel = ContentChannelCache.Get( contentChannelGuid );

            if (contentChannel == null)
            {
                var message = "<strong><i class=\"fas fa-exclamation-triangle\"></i>Configuration Error</strong><br />Content Channel not configured.";
                ShowNotification( NotificationBoxType.Validation, message );
                return;
            }

            var contentChannelItemId = PageParameter( "listId" ).AsInteger();


            using (var rockContext = new RockContext())
            {
                var itemService = new ContentChannelItemService( rockContext );
                var contentItem = itemService.Get( contentChannelItemId );

                if (contentItem == null || contentItem.ContentChannelId != contentChannel.Id)
                {
                    var message = "<strong><i class=\"fas fa-exclamation-triangle\"></i>List Not Found<br />Link List Not found or is unavailable.";
                    ShowNotification( NotificationBoxType.Info, message );
                    return;
                }

                var canEdit = contentItem.IsAuthorized( Rock.Security.Authorization.EDIT, CurrentPerson );
                if (!canEdit)
                {
                    var message = "<strong><i class=\"fas fa-exclamation-triangle\"></i>Not Authorized</strong><br /> You do not have edit rights to this Link List.";
                    ShowNotification( NotificationBoxType.Danger, message );
                    return;
                }
                lListName.Text = contentItem.Title;
                hfContentChannelItemId.Value = contentItem.Id.ToString();
                var editorsGroup = GetListEditorsGroup( contentItem );
                LoadEditorList( editorsGroup.Id );


                hfSecurityGroupId.Value = editorsGroup.Id.ToString();
                pnlLinkList.Visible = true;
            }
        }

        private void GroupMemberAdd( int personId )
        {
            var groupId = hfSecurityGroupId.Value.AsInteger();

            using (var rockContext = new RockContext())
            {
                var defaultRoleId = GroupTypeCache.Get( Rock.SystemGuid.GroupType.GROUPTYPE_SECURITY_ROLE )
                    .DefaultGroupRoleId;
                var groupService = new GroupService( rockContext );
                var group = groupService.Get( groupId );
                var groupMemberService = new GroupMemberService( rockContext );
                groupMemberService.AddOrRestoreGroupMember( group, personId, defaultRoleId.Value );
            }

            LoadEditorList( groupId );

        }

        private void GroupMemberRemove( int groupMemberId )
        {
            var groupId = hfSecurityGroupId.Value.AsInteger();
            using (var rockContext = new RockContext())
            {
                var groupMemberService = new GroupMemberService( rockContext );
                var groupMember = groupMemberService.Get( groupMemberId );

                if (groupMember.GroupId == groupId)
                {
                    groupMemberService.Delete( groupMember );
                    rockContext.SaveChanges();
                }

                LoadEditorList( groupId );

            }
        }

        private void ShowNotification( NotificationBoxType boxType, string message )
        {
            nbNotification.NotificationBoxType = boxType;
            nbNotification.Text = message;
            nbNotification.Visible = true;
        }
    }
}