using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Web.UI.WebControls;
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

    [IntegerField("Refresh Interval", "How often to refresh the results in milliseconds. Default is 15000 (15 sec)", false, 15000)]

    public partial class GroupMemberTracker : GroupManagerBlock
    {

        GroupOccurrence currentOccurrence = null;

        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            rGroupMember.ItemCommand += rGroupMember_ItemCommand;
        }



        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
            pnlMain.Visible = false;


            if ( CurrentGroup == null || CurrentPerson == null )
            {
                NavigateToHomePage();
                return;
            }

            var refreshInterval = GetAttributeValue( "RefreshInterval" ).AsInteger();
            tmrRefresh.Enabled = refreshInterval > 0;
            if ( refreshInterval > 0 )
            {
                tmrRefresh.Interval = refreshInterval;
            }

            bool canTakeAttendance = CurrentGroup.IsAuthorized( Rock.Security.Authorization.MANAGE_MEMBERS, CurrentPerson ) ||
                CurrentGroup.IsAuthorized( Rock.Security.Authorization.EDIT, CurrentPerson );

            if ( !canTakeAttendance )
            {
                DisplayNotAuthorizedMessage();
                return;
            }


            if ( !IsPostBack )
            {
                ddlFilter.SelectedValue = "1";
                LoadCheckinData();
            }
        }

        private void CheckinGroupMember( int? attendanceId )
        {
            using ( var rockContext = new RockContext() )
            {
                var attendanceService = new AttendanceService( rockContext );
                var attendance = attendanceService.Get( attendanceId.Value );

                if ( attendance != null )
                {
                    attendance.DidAttend = true;
                    rockContext.SaveChanges();
                }
            }
            LoadCheckinData();
        }

        private void DisplayNotAuthorizedMessage()
        {
            nbMain.Text = "<strong><<i class='far fa-exclamation-triangle'></i> Not Authorized</strong> You are not authorized to view this group.";
            nbMain.NotificationBoxType = NotificationBoxType.Warning;
            nbMain.Visible = true;

        }

        private void DisplayNoOccurrenceMessage()
        {
            nbMain.Text = "<strong><i class='far fa-exclamation-triangle'></i> No Check-ins</strong> No Check-ins found.";
            nbMain.NotificationBoxType = NotificationBoxType.Info;
            nbMain.Visible = true;
        }

        private void LoadCheckinData()
        {
            nbMain.Visible = false;
            LoadCurrentOccurrence();

            if ( currentOccurrence == default( GroupOccurrence ) )
            {
                DisplayNoOccurrenceMessage();
                pnlMain.Visible = false;
            }
            else
            {
                pnlMain.Visible = true;
                LoadGroupMembers();

            }

            tmrRefresh.Enabled = GetAttributeValue("RefreshInterval").AsInteger() > 0;
        }

        private void LoadCurrentOccurrence()
        {
            var today = RockDateTime.Now.Date;
            using ( var rockContext = new RockContext() )
            {
                currentOccurrence = new AttendanceOccurrenceService( rockContext )
                    .Queryable().AsNoTracking()
                    .Where( o => o.OccurrenceDate == today )
                    .Where( o => o.GroupId == CurrentGroup.Id )
                    .Select( o => new GroupOccurrence
                    {
                        Id = o.Id,
                        GroupId = o.GroupId,
                        ScheduleId = o.ScheduleId,
                        LocationId = o.LocationId,
                        OccurrenceDate = o.OccurrenceDate,
                        OccurrenceAttendees = o.Attendees.Select( a => new OccurrenceAttendeeItem
                        {
                            Id = a.Id,
                            PersonAliasId = a.PersonAliasId,
                            PersonId = a.PersonAlias != null ? a.PersonAlias.PersonId : ( int? ) null,
                            DidAttend = !a.DidAttend.HasValue ? false : a.DidAttend.Value,
                            StartDateTime = a.StartDateTime,
                            EndDateTime = a.EndDateTime
                        } )
                    } ).FirstOrDefault();


            }
        }


        private void LoadGroupMembers()
        {
            var rockContext = new RockContext();
            var mobilePhoneDV = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() );

            var mobilePhoneQry = new PhoneNumberService( rockContext ).Queryable().AsNoTracking()
                .Where( m => m.NumberTypeValueId == mobilePhoneDV.Id && !m.IsUnlisted );

            var includeLeaders = GetAttributeValue( "DisplayGroupLeaders" ).AsBoolean();


            var groupMembersQry = CurrentGroupMembers
                .GroupJoin( currentOccurrence.OccurrenceAttendees, gm => gm.PersonId, a => a.PersonId,
                    ( gm, o ) => new
                    {
                        Id = gm.Id,
                        PersonId = gm.PersonId,
                        Person = gm.Person,
                        GroupRole = gm.GroupRole,
                        AttendanceId = o.FirstOrDefault() == null ? ( int? ) null : o.FirstOrDefault().Id,
                        AttendanceState = o.FirstOrDefault() == null ? AttendanceState.CheckedOut : o.FirstOrDefault().AttendanceState
                    } )
                .GroupJoin( mobilePhoneQry, gm => gm.PersonId, mp => mp.PersonId,
                    ( gm, mp ) => new { GroupMember = gm, MobilePhone = mp.Select( p => p.NumberFormatted ).FirstOrDefault() } );

            if ( !includeLeaders )
            {
                groupMembersQry = groupMembersQry.Where( gm => !gm.GroupMember.GroupRole.IsLeader );
            }

            switch ( ddlFilter.SelectedValueAsInt() )
            {
                case 1:
                    groupMembersQry = groupMembersQry.Where( m => m.GroupMember.AttendanceState == AttendanceState.EnRoute );
                    break;
                case 2:
                    groupMembersQry = groupMembersQry.Where( m => m.GroupMember.AttendanceState == AttendanceState.InRoom );
                    break;
                default:
                    groupMembersQry = groupMembersQry.Where( m => m.GroupMember.AttendanceId.HasValue );
                    break;
            }

            var groupMembers = groupMembersQry
                .Select( gm => new GroupMemberInfo
                {
                    PersonId = gm.GroupMember.PersonId,
                    GroupMemberId = gm.GroupMember.Id,
                    FirstName = gm.GroupMember.Person.FirstName,
                    LastName = gm.GroupMember.Person.LastName,
                    MobilePhone = gm.MobilePhone,
                    PhotoUrl = gm.GroupMember.Person.PhotoUrl,
                    State = gm.GroupMember.AttendanceState,
                    AttendanceId = gm.GroupMember.AttendanceId
                } )
                .OrderBy( gm => gm.NameReversed )
                .ToList();

            rGroupMember.DataSource = groupMembers;
            rGroupMember.DataBind();

            if ( groupMembers.Count() == 0 )
            {
                var noResultsTemplate = "<h3 style='text-align:center;'><i class='far fa-exclamation-triangle'></i> No group members {0}.</h3>";
                var noResultsPart2 = String.Empty;

                switch ( ddlFilter.SelectedValue.AsInteger() )
                {
                    case 1:
                        noResultsPart2 = "are enroute";
                        break;
                    case 2:
                        noResultsPart2 = "have arrived";
                        break;
                    default:
                        noResultsPart2 = "have checked-in";
                        break;
                }

                nbNoResults.Visible = true;
                nbNoResults.Text = String.Format( noResultsTemplate, noResultsPart2 );
            }
            else
            {
                nbNoResults.Visible = false;
            }
        }

        protected void rGroupMember_ItemDataBound( object sender, System.Web.UI.WebControls.RepeaterItemEventArgs e )
        {
            var dataItem = e.Item.DataItem as GroupMemberInfo;

            var hlCheckinStatus = e.Item.FindControl( "hlCheckinStatus" ) as HighlightLabel;
            var lPhoneNumber = e.Item.FindControl( "lPhoneNumber" ) as RockLiteral;
            var lIcons = e.Item.FindControl( "lIcons" ) as RockLiteral;
            var btnCheckin = e.Item.FindControl( "btnCheckin" ) as BootstrapButton;

            lPhoneNumber.Label = dataItem.MobilePhone.IsNotNullOrWhiteSpace() ? "Mobile Phone" : String.Empty;
            lPhoneNumber.Text = dataItem.MobilePhone;
            lIcons.Visible = false;

            btnCheckin.Visible = dataItem.State == AttendanceState.EnRoute;
            btnCheckin.CommandArgument = dataItem.AttendanceId.HasValue ? dataItem.AttendanceId.ToString() : null;
            btnCheckin.CommandName = "CheckIn";

            switch ( dataItem.State )
            {
                case AttendanceState.InRoom:
                    hlCheckinStatus.Text = "Arrived";
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

        private void rGroupMember_ItemCommand( object source, RepeaterCommandEventArgs e )
        {
            tmrRefresh.Enabled = false;
            if ( e.CommandName == "CheckIn" )
            {
                CheckinGroupMember( e.CommandArgument.ToString().AsIntegerOrNull() );
            }
        }

        protected void ddlFilter_SelectionChanged( object sender, EventArgs e )
        {
            tmrRefresh.Enabled = false;
            LoadCheckinData();
        }

        protected void lbRefresh_Click( object sender, EventArgs e )
        {
            tmrRefresh.Enabled = false;
            LoadCheckinData();
        }

        protected void tmrRefresh_Tick( object sender, EventArgs e )
        {
            Console.WriteLine( Rock.RockDateTime.Now.ToString("T"));
            LoadCheckinData();
        }


        class GroupMemberInfo
        {
            public int PersonId { get; set; }
            public int? GroupMemberId { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public AttendanceState? State { get; set; } = AttendanceState.CheckedOut;
            public string AttendanceStateValue
            {
                get
                {
                    switch ( State )
                    {
                        case AttendanceState.InRoom:
                            return "Arrived";
                        case AttendanceState.EnRoute:
                            return "EnRoute";
                        default:
                            return "Checked Out";
                    }
                }
            }
            public string MobilePhone { get; set; }
            public string PhotoUrl { get; set; }
            public int? AttendanceId { get; set; }

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

        class GroupOccurrence
        {
            public int Id { get; set; }
            public int? GroupId { get; set; }
            public int? ScheduleId { get; set; }
            public int? LocationId { get; set; }
            public DateTime OccurrenceDate { get; set; }
            public IEnumerable<OccurrenceAttendeeItem> OccurrenceAttendees { get; set; }

        }

        class OccurrenceAttendeeItem
        {
            public int Id { get; set; }
            public int? PersonAliasId { get; set; }
            public int? PersonId { get; set; }
            public bool DidAttend { get; set; }
            public int? QualifierValueId { get; set; }
            public DateTime StartDateTime { get; set; }
            public DateTime? EndDateTime { get; set; }
            public AttendanceState AttendanceState
            {
                get
                {
                    if ( EndDateTime.HasValue )
                    {
                        return AttendanceState.CheckedOut;
                    }
                    if ( DidAttend == true )
                    {
                        return AttendanceState.InRoom;
                    }
                    return AttendanceState.EnRoute;


                }
            }
        }






    }
}