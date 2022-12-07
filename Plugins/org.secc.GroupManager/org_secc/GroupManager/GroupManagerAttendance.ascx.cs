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
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using org.secc.GroupManager;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_secc.GroupManager
{
    [DisplayName( "Group Manager Attendance" )]
    [Category( "Groups" )]
    [Description( "Lists the group members for a specific occurrence datetime and allows selecting if they attended or not." )]

    [BooleanField( "Show Filters", "Shows all filters so that the filter can be changed." )]

    public partial class GroupManagerAttendance : GroupManagerBlock
    {
        #region Private Variables

        private RockContext _rockContext = null;
        private bool _canEdit = false;
        private List<GroupAttendanceAttendee> _attendees;

        #endregion

        #region Control Methods

        protected override void LoadViewState( object savedState )
        {
            base.LoadViewState( savedState );
            _attendees = ViewState["Attendees"] as List<GroupAttendanceAttendee>;
        }
        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            RegisterScript();

            _rockContext = new RockContext();

            if ( CurrentGroup != null && CurrentGroup.IsAuthorized( Authorization.EDIT, CurrentPerson ) )
            {
                _canEdit = true;
            }
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack )
            {
                pnlDetails.Visible = _canEdit;

                if ( _canEdit )
                {

                    BindDropDown();
                    ShowDetails();

                    if ( CurrentGroupFilters.Any() )
                    {
                        pnlDidNotMeet.Visible = false;
                    }
                }
                else
                {
                    nbNotice.Heading = "Sorry";
                    nbNotice.Text = "<p>You're not authorized to update the attendance for the selected group.</p>";
                    nbNotice.NotificationBoxType = NotificationBoxType.Danger;
                    nbNotice.Visible = true;
                }
            }
            else
            {

            }

            if ( GetAttributeValue( "AutoCount" ).AsBoolean() )
            {
                ScriptManager.RegisterStartupScript( this, this.GetType(), "AutoCount", "doCount = true;", true );
            }
        }

        private void BindDropDown()
        {
            RockContext rockContext = new RockContext();
            var startDate = RockDateTime.Today.AddYears( -1 );
            var enddate = RockDateTime.Today.AddDays( 1 );

            var existingOccurrences = new AttendanceOccurrenceService( rockContext )
                .Queryable()
                .AsNoTracking()
                .Where( g => g.GroupId == CurrentGroup.Id )
                .Where( o => o.OccurrenceDate >= startDate && o.OccurrenceDate < enddate )
                .ToList();

            var occurrences = new List<GroupManagerOccurrenceSummary>();
            if(CurrentGroup.Schedule != null)
            {
                if(CurrentGroup.Schedule.ScheduleType == ScheduleType.Custom || CurrentGroup.Schedule.ScheduleType == ScheduleType.Named)
                {
                    var previousScheduleDates = CurrentGroup.Schedule.GetScheduledStartTimes( startDate, enddate )
                        .OrderByDescending( o => o )
                        .Take( 50 )
                        .ToList();


                        occurrences.AddRange( previousScheduleDates
                            .Select( p => new GroupManagerOccurrenceSummary
                            {
                                OccurrenceDate = p,
                                GroupId = CurrentGroup.Id,
                                ScheduleId = CurrentGroup.ScheduleId,
                                LocationId = CurrentGroup.GroupLocations.Any() ? ( int? ) CurrentGroup.GroupLocations.Select( l => l.LocationId ).FirstOrDefault() : null,
                                StartDateTime = p.Add( CurrentGroup.Schedule.StartTimeOfDay )
                            } )
                            .ToList() );


                    foreach ( var occurrence in existingOccurrences )
                    {
                        var selectedOccurrence = occurrences
                            .Where( o => o.OccurrenceDate == occurrence.OccurrenceDate )
                            .Where( o => o.LocationId == occurrence.LocationId )
                            .Where( o => o.ScheduleId == occurrence.ScheduleId )
                            .FirstOrDefault();

                        if ( selectedOccurrence != null )
                        {
                            selectedOccurrence.OccurrenceId = occurrence.Id;
                        }
                    }
                }
                else if(CurrentGroup.Schedule.ScheduleType == ScheduleType.Weekly)
                {
                    var lastSchedule = RockDateTime.Today;
                    while(lastSchedule.DayOfWeek != CurrentGroup.Schedule.WeeklyDayOfWeek)
                    {
                        lastSchedule = lastSchedule.AddDays( -1 );
                    }

                    while ( lastSchedule > startDate )
                    {
                        occurrences.Add( new GroupManagerOccurrenceSummary
                        {
                            OccurrenceDate = lastSchedule,
                            GroupId = CurrentGroup.Id,
                            ScheduleId = CurrentGroup.ScheduleId,
                            LocationId = CurrentGroup.GroupLocations.Any() ? (int?)CurrentGroup.GroupLocations.Select(l => l.LocationId).FirstOrDefault() : null,
                            StartDateTime = lastSchedule.Add( CurrentGroup.Schedule.WeeklyTimeOfDay ?? new TimeSpan( 0, 0, 0 ) )
                        } ) ;

                        lastSchedule  = lastSchedule.AddDays( -7 );
                    }

                    foreach ( var occurrence in existingOccurrences )
                    {
                        var selectedOccurrence = occurrences
                            .Where( o => o.OccurrenceDate == occurrence.OccurrenceDate )
                            .Where( o => o.LocationId == occurrence.LocationId )
                            .Where( o => o.ScheduleId == occurrence.ScheduleId )
                            .FirstOrDefault();
                        if ( selectedOccurrence != null )
                        {
                            selectedOccurrence.OccurrenceId = occurrence.Id;
                        }
                    }
                }
            }

            occurrences.AddRange( existingOccurrences
            .Where( o => !occurrences.Select( o1 => o1.OccurrenceId ).Contains( o.Id ) )
            .Select( o => new GroupManagerOccurrenceSummary
            {
                OccurrenceId = o.Id,
                GroupId = o.GroupId,
                LocationId = o.LocationId,
                ScheduleId = o.ScheduleId,
                OccurrenceDate = o.OccurrenceDate,
                StartDateTime = o.Schedule.GetNextStartDateTime( o.OccurrenceDate )
            } )
            .ToList() );

            ddlOccurence.DataSource = occurrences
                .OrderByDescending( o => o.StartDateTime )
                .Take( 50 )
                .Select( o => new
                {
                    Id = o.ToString(),
                    Name = $"{o.StartDateTime: MMM d, yyy h:mmtt}"
                } )
                .ToList();
            ddlOccurence.DataBind();

            //Drop down for filter values
            ddlFilter.Visible = ( GetAttributeValue( "ShowFilters" ).AsBoolean() && CurrentGroupFilters.Any() );
            if ( ddlFilter.Visible )
            {
                ddlFilter.DataSource = CurrentGroupFilterValues
                    .Select( s => new
                    {
                        Id = s,
                        Name = s,
                    }
                    );
                ddlFilter.DataBind();
                CurrentGroupMember.LoadAttributes();
                ddlFilter.SelectedValue = CurrentGroupMember.GetAttributeValue( CurrentGroupFilters.FirstOrDefault() );
            }
        }

    #endregion

    #region Events

    /// <summary>
    /// Handles the Click event of the lbSave control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
    protected void lbSave_Click( object sender, EventArgs e )
    {
        if ( CurrentGroup != null )
        {
            if ( ddlOccurence.SelectedValue.IsNotNullOrWhiteSpace() )
            {
                var values = ddlOccurence.SelectedValue.Split( "^".ToCharArray() )
                    .Select( o => new { Key = o.Split( ":".ToCharArray() )[0], Value = o.Split( ":".ToCharArray() )[1] } )
                    .ToDictionary( v => v.Key, v => v.Value );

                var groupId = values["G"].AsInteger();
                var locationId = values["L"].AsIntegerOrNull();
                var scheduleId = values["S"].AsIntegerOrNull();
                var occurrenceDate = new DateTime( values["D"].Substring( 0, 4 ).AsInteger(), values["D"].Substring( 4, 2 ).AsInteger(), values["D"].Substring( 6, 2 ).AsInteger() );


                var attendanceData = new AttendanceService( _rockContext )
                    .Queryable( "PersonAlias" )
                    .Where( a => a.Occurrence.GroupId == groupId && a.Occurrence.LocationId == locationId && a.Occurrence.ScheduleId == scheduleId && a.Occurrence.OccurrenceDate == occurrenceDate );


                var attendanceOccurenceService = new AttendanceOccurrenceService( _rockContext );
                if ( cbDidNotMeet.Checked == true )
                {
                    var occurrence = attendanceOccurenceService.Get( occurrenceDate, groupId, locationId, scheduleId );
                    if ( occurrence == null )
                    {
                        occurrence = new AttendanceOccurrence();
                        occurrence.OccurrenceDate = occurrenceDate;
                        occurrence.GroupId = groupId;
                        occurrence.ScheduleId = scheduleId;
                        occurrence.LocationId = locationId;
                        attendanceOccurenceService.Add( occurrence );
                    }
                    occurrence.DidNotOccur = true;
                    foreach ( var attendee in occurrence.Attendees )
                    {
                        attendee.DidAttend = false;
                    }
                }
                else
                {
                    var attendanceService = new AttendanceService( _rockContext );
                    var personAliasService = new PersonAliasService( _rockContext );

                    foreach ( var item in lvMembers.Items )
                    {
                        var hfMember = item.FindControl( "hfMember" ) as HiddenField;
                        var cbMember = item.FindControl( "cbMember" ) as HtmlInputCheckBox;
                        var personId = hfMember.Value.AsInteger();
                        var attendanceItem = attendanceData.Where( a => a.PersonAlias.PersonId == personId )
                            .FirstOrDefault();
                        if ( attendanceItem == null )
                        {
                            var attendancePerson = new PersonService( _rockContext ).Get( personId );
                            if ( attendancePerson != null && attendancePerson.PrimaryAliasId.HasValue )
                            {
                                attendanceItem = attendanceService.AddOrUpdate( attendancePerson.PrimaryAliasId.Value, occurrenceDate, groupId, locationId, scheduleId, CurrentGroup.CampusId );
                            }
                        }

                        if ( attendanceItem != null )
                        {
                            attendanceItem.DidAttend = cbMember.Checked;
                        }
                    }
                }
            }


            _rockContext.SaveChanges();
            nbNotice.Text = "Attendance Saved";
            nbNotice.NotificationBoxType = NotificationBoxType.Success;
            nbNotice.Visible = true;
            nbNotice.Dismissable = true;
        }
    }

    private Note GetNoteForAttendanceDate( RockContext rockContext, bool createNew = false )
    {
        NoteTypeService noteTypeService = new NoteTypeService( rockContext );
        NoteType noteType = noteTypeService.Queryable().FirstOrDefault( nt => nt.Guid == new Guid( "FFFC3644-60CD-4D14-A714-E8DCC202A0E1" ) );

        NoteService noteService = new NoteService( rockContext );
        var notes = noteService.Queryable().Where( n => n.NoteType.Guid == noteType.Guid && n.EntityId == CurrentGroup.Id ).ToList();
        foreach ( Note note in notes )
        {
            note.LoadAttributes();
            var dateString = note.GetAttributeValue( "NoteDate" );
            DateTime parseDate;
            try
            {
                parseDate = DateTime.Parse( dateString );
            }
            catch
            {
                continue;
            }
            //if ( dateString != null && _occurrence != null && parseDate == _occurrence.Date )
            //{
            //    return note;
            //}
        }
        if ( createNew )
        {
            //Create new note if one does not exist.
            Note newNote = new Note();
            newNote.NoteType = noteType;
            newNote.EntityId = CurrentGroup.Id;
            noteService.Add( newNote );
            rockContext.SaveChanges();
            newNote.LoadAttributes();
            //newNote.SetAttributeValue( "NoteDate", _occurrence.Date );
            newNote.SaveAttributeValues( rockContext );

            rockContext.SaveChanges();
            return newNote;
        }
        return null;
    }

    /// <summary>
    /// Handles the ItemCommand event of the lvPendingMembers control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="ListViewCommandEventArgs"/> instance containing the event data.</param>
    protected void lvPendingMembers_ItemCommand( object sender, ListViewCommandEventArgs e )
    {
        if ( CurrentGroup != null && e.CommandName == "Add" )
        {
            int personId = e.CommandArgument.ToString().AsInteger();

            var rockContext = new RockContext();

            foreach ( var groupMember in new GroupMemberService( rockContext )
                .GetByGroupIdAndPersonId( CurrentGroup.Id, personId ) )
            {
                if ( groupMember.GroupMemberStatus == GroupMemberStatus.Pending )
                {
                    groupMember.GroupMemberStatus = GroupMemberStatus.Active;
                }
            }

            rockContext.SaveChanges();

            ShowDetails();
        }
    }

    #endregion

    #region Internal Methods

    /// <summary>
    /// Binds the group members grid.
    /// </summary>
    protected void ShowDetails()
    {
        if ( CurrentGroup == null )
        {
            NavigateToHomePage();
            return;
        }

        nbNotice.Visible = false;
        lMembers.Text = CurrentGroup.GroupType.GroupMemberTerm.Pluralize();
        lPendingMembers.Text = "Pending " + lMembers.Text;

        if ( !String.IsNullOrWhiteSpace( ddlOccurence.SelectedValue ) )
        {
            var itemKey = ddlOccurence.SelectedValue.Split( "^".ToCharArray() )
                .ToList()
                .Select( v => new { Key = v.Split( ":".ToCharArray() )[0], Value = v.Split( ":".ToCharArray() )[1] } )
                .ToDictionary( v => v.Key, v => v.Value );

            var groupId = itemKey["G"].AsInteger();
            var locationId = itemKey["L"].AsIntegerOrNull();
            var scheduleId = itemKey["S"].AsIntegerOrNull();
            var occurrenceDate = new DateTime( itemKey["D"].Substring( 0, 4 ).AsInteger(), itemKey["D"].Substring( 4, 2 ).AsInteger(), itemKey["D"].Substring( 6, 2 ).AsInteger() );



            var attendanceData = new AttendanceService( _rockContext )
                .Queryable()
                .Where( a => a.Occurrence.GroupId == groupId && a.Occurrence.LocationId == locationId && a.Occurrence.ScheduleId == scheduleId && a.Occurrence.OccurrenceDate == occurrenceDate )
                .ToList();

            lvMembers.Items.Clear();

            List<GroupMember> groupMembers;

            if ( ddlFilter.Visible )
            {
                groupMembers = GetFilteredMembers( ddlFilter.SelectedValue );
            }
            else
            {
                groupMembers = CurrentGroupMembers;
            }

            var items = groupMembers
                .Where( gm => gm.GroupMemberStatus == GroupMemberStatus.Active )
                .OrderBy( gm => gm.Person.LastName )
                .ThenBy( gm => gm.Person.NickName )
                .Select( m => new
                {
                    PersonId = m.PersonId.ToString(),
                    FullName = m.ToString(),
                    Active = ( attendanceData.Where( a => a.PersonAlias.PersonId == m.PersonId ).Any()
                     && ( attendanceData.Where( a => a.PersonAlias.PersonId == m.PersonId ).FirstOrDefault().DidAttend ?? false ) ) ? "active" : "",
                    Attended = ( attendanceData.Where( a => a.PersonAlias.PersonId == m.PersonId ).Any()
                     && ( attendanceData.Where( a => a.PersonAlias.PersonId == m.PersonId ).FirstOrDefault().DidAttend ?? false ) )
                }
                )
                .ToList();

            lvMembers.DataSource = items;
            lvMembers.DataBind();

            AttendanceOccurrenceService attendanceOccurenceService = new AttendanceOccurrenceService( _rockContext );
            var occurrence = attendanceOccurenceService.Get( occurrenceDate, groupId, locationId, scheduleId );

            cbDidNotMeet.Checked = (
                   ( attendanceData.Where( a => a.DidAttend == true ).Count() <= 0
                   && attendanceData.Where( a => a.Occurrence.DidNotOccur == true ).Count() > 0 )
                   || ( occurrence != null && occurrence.DidNotOccur == true )
                   );
            if ( cbDidNotMeet.Checked )
            {
                lbDidNotMeet.AddCssClass( "active" );
            }
        }


        GroupMemberService groupMemberService = new GroupMemberService( _rockContext );
        // Bind the pending members
        var pendingMembers = groupMemberService
            .Queryable().AsNoTracking()
            .Where( m =>
                m.GroupId == CurrentGroup.Id &&
                m.GroupMemberStatus == GroupMemberStatus.Pending )
            .OrderBy( m => m.Person.LastName )
            .ThenBy( m => m.Person.NickName )
            .Select( m => new
            {
                Id = m.PersonId,
                FullName = m.Person.NickName + " " + m.Person.LastName
            } )
            .ToList();

        pnlPendingMembers.Visible = pendingMembers.Any();
        lvPendingMembers.DataSource = pendingMembers;
        lvPendingMembers.DataBind();
    }

    protected void RegisterScript()
    {
        string script = string.Format( @"

    Sys.Application.add_load(function () {{

        if ($('#{0}').is(':checked')) {{
            $('div.js-roster').hide();
        }}

        $('#{0}').change(function () {{
            if ($(this).is(':checked')) {{
                $('div.js-roster').hide('fast');
                updateCount();
            }} else {{
                $('div.js-roster').show('fast');
                updateCount();
            }}
        }});

        $('.js-add-member').click(function ( e ) {{
            e.preventDefault();
            var $a = $(this);
            var memberName = $(this).parent().find('span').html();
            Rock.dialogs.confirm('Add ' + memberName + ' to your group?', function (result) {{
                if (result) {{
                    window.location = $a.prop('href');                    
                }}
            }});
        }});

    }});

", cbDidNotMeet.ClientID );

        ScriptManager.RegisterStartupScript( cbDidNotMeet, cbDidNotMeet.GetType(), "group-attendance-detail", script, true );
    }

    #endregion

    #region Helper Classes

    [Serializable]
    public class GroupAttendanceAttendee
    {
        /// <summary>
        /// Gets or sets the person identifier.
        /// </summary>
        /// <value>
        /// The person identifier.
        /// </value>
        public int PersonId { get; set; }

        /// <summary>
        /// Gets or sets the name of the nick.
        /// </summary>
        /// <value>
        /// The name of the nick.
        /// </value>
        public string NickName { get; set; }

        /// <summary>
        /// Gets or sets the last name.
        /// </summary>
        /// <value>
        /// The last name.
        /// </value>
        public string LastName { get; set; }

        /// <summary>
        /// Gets or sets the full name.
        /// </summary>
        /// <value>
        /// The full name.
        /// </value>
        public string FullName
        {
            get { return NickName + " " + LastName; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="GroupAttendanceAttendee"/> is attended.
        /// </summary>
        /// <value>
        ///   <c>true</c> if attended; otherwise, <c>false</c>.
        /// </value>
        public bool Attended { get; set; }
    }

    #endregion

    protected void ddlOccurence_SelectedIndexChanged( object sender, EventArgs e )
    {
        ShowDetails();
    }

    protected void ddlFilter_SelectedIndexChanged( object sender, EventArgs e )
    {
        ShowDetails();
    }
}

public class GroupManagerOccurrenceSummary
{
    public int? OccurrenceId { get; set; }
    public int? GroupId { get; set; }
    public int? LocationId { get; set; }
    public int? ScheduleId { get; set; }
    public DateTime OccurrenceDate { get; set; }
    public DateTime? StartDateTime { get; set; }

    public override string ToString()
    {
        return $"G:{GroupId}^L:{LocationId}^S:{ScheduleId}^D:{OccurrenceDate:yyyyMMdd}";
    }
}
}