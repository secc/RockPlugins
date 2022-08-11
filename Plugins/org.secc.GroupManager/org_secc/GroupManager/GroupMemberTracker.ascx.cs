using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;  
using org.secc.FamilyCheckin.Cache;
using org.secc.GroupManager;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_secc.GroupManager
{

    [DisplayName( "Group Member Tracker" )]
    [Category( "SECC > Groups" )]
    [Description( "Allows a group leader confirm that a member attended after checking in." )]

    [BooleanField( "Display Group Leaders", "Show Group Leaders in attendee List", false )]
    [BooleanField( "Add unassigned people", "Allows user to confirm unassigned people who came to their group.", false )]
    public partial class GroupMemberTracker : GroupManagerBlock
    {
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
        }

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
            pnlMain.Visible = false;

            if ( CurrentGroup == null || CurrentPerson == null)
            {
                NavigateToHomePage();
                return;
            }

            bool canTakeAttendance = CurrentGroup.IsAuthorized( Rock.Security.Authorization.MANAGE_MEMBERS, CurrentPerson ) ||
                CurrentGroup.IsAuthorized( Rock.Security.Authorization.EDIT, CurrentPerson );

            if(!canTakeAttendance)
            {
                DisplayNotAuthorizedMessage();
                return;
            }

            pnlMain.Visible = true;

            if ( !IsPostBack )
            {
                LoadGroupMembers();
            }
        }

        private void DisplayNotAuthorizedMessage()
        {
            nbMain.Text = "<strong><<i class='far fa-exclamation-triangle'></i> Not Authorized</strong> You are not authorized to view this group.";
            nbMain.Visible = true;

        }

        private void LoadGroupMembers()
        {
            var rockContext = new RockContext();
            var mobilePhoneDV = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() );

            var mobilePhoneQry = new PhoneNumberService( rockContext ).Queryable().AsNoTracking()
                .Where( m => m.NumberTypeValueId == mobilePhoneDV.Id && !m.IsUnlisted );

            var includeLeaders = GetAttributeValue( "DisplayGroupLeaders" ).AsBoolean();

            var groupMembersQry = CurrentGroupMembers
                .GroupJoin( mobilePhoneQry, gm => gm.PersonId, mp => mp.PersonId,
                    ( gm, mp ) => new { GroupMember = gm, MobilePhone = mp.DefaultIfEmpty() } );

            if(!includeLeaders)
            {
                groupMembersQry = groupMembersQry.Where( gm => !gm.GroupMember.GroupRole.IsLeader );
            }

            var gm1 = groupMembersQry.ToList();

            var groupMembers = groupMembersQry
                .Select( gm => new GroupMemberInfo
                {
                    PersonId = gm.GroupMember.PersonId,
                    GroupMemberId = gm.GroupMember.Id,
                    FirstName = gm.GroupMember.Person.FirstName,
                    LastName = gm.GroupMember.Person.LastName,
                    MobilePhone = gm.MobilePhone.FirstOrDefault() != null ? gm.MobilePhone.FirstOrDefault().NumberFormatted : String.Empty,
                    PhotoUrl = gm.GroupMember.Person.PhotoUrl,
                    AttendanceStateValue = "Checked Out"
                } )
                .OrderBy( gm => gm.NameReversed )
                .ToList();

            rGroupMember.DataSource = groupMembers;
            rGroupMember.DataBind();

        }

        protected void rGroupMember_ItemDataBound( object sender, System.Web.UI.WebControls.RepeaterItemEventArgs e )
        {
            var dataItem = e.Item.DataItem as GroupMemberInfo;

            var hlCheckinStatus = e.Item.FindControl( "hlCheckinStatus" ) as HighlightLabel;
            var lPhoneNumber = e.Item.FindControl( "lPhoneNumber" ) as RockLiteral;
            var lIcons = e.Item.FindControl( "lIcons" ) as RockLiteral;

            lPhoneNumber.Visible = dataItem.MobilePhone.IsNotNullOrWhiteSpace();
            lPhoneNumber.Text = dataItem.MobilePhone;
            lIcons.Visible = false;


            switch ( dataItem.State )
            {
                case AttendanceState.InRoom:
                    hlCheckinStatus.Text = "Checked In";
                    hlCheckinStatus.CssClass = "label-success pull-right";
                    break;
                case AttendanceState.EnRoute:
                    hlCheckinStatus.Text = "Enroute";
                    hlCheckinStatus.CssClass = "label-danger pull-right";
                    break;
                default:
                    hlCheckinStatus.Text = "Checked Out";
                    hlCheckinStatus.CssClass = "label-default pull-right";
                    break;
            }

        }

        class GroupMemberInfo
        {
            public int PersonId { get; set; }
            public int? GroupMemberId { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public AttendanceState? State { get; set; } = AttendanceState.CheckedOut;
            public string AttendanceStateValue { get; set; } 
            public string MobilePhone { get; set; }
            public string PhotoUrl { get; set; }

            public string Name
            {
                get
                {
                    return $"{FirstName} {LastName}";
                }
            }

            public string NameReversed
            {
                get
                {
                    return $"{LastName}, {FirstName}";
                }
            }
        }
    }
}