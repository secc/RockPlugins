// <copyright>
// Copyright 2013 by the Spark Development Network
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
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
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.MergeTemplates;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_secc.GroupManager
{
    [DisplayName( "Group Manager Attendance" )]
    [Category( "Groups" )]
    [Description( "Lists the group members for a specific occurrence datetime and allows selecting if they attended or not." )]

    [BooleanField( "Allow Add Date", "Should block support adding new attendance dates outside of the group's configured schedule and group type's exclusion dates?", true, "", 0 )]
    [BooleanField( "Allow Add Person", "Should block support adding new attendee ( Requires that person has rights to search for new person )?", false, "", 1 )]
    [MergeTemplateField( "Attendance Roster Template", "", false )]
    [TextField("Count Label", "Label for count field.", true, "Head Count:")]
    [BooleanField("Auto Count", "Auto count membership.", false)]
    public partial class GroupManagerAttendance : RockBlock
    {
        #region Private Variables

        private RockContext _rockContext = null;
        private Group _group = null;
        private bool _canEdit = false;
        private bool _allowAddDate = false;
        private ScheduleOccurrence _occurrence = null;
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

            int groupId = PageParameter( "GroupId" ).AsInteger();
            _group = new GroupService( _rockContext )
                .Queryable( "GroupType,Schedule" ).AsNoTracking()
                .FirstOrDefault( g => g.Id == groupId );

            if ( _group != null && _group.IsAuthorized( Authorization.EDIT, CurrentPerson ) )
            {
                lHeading.Text = _group.Name + " Attendance";
                _canEdit = true;
            }

            _allowAddDate = GetAttributeValue( "AllowAddDate" ).AsBoolean();
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            _occurrence = GetOccurrence();

            if ( !Page.IsPostBack )
            {
                pnlDetails.Visible = _canEdit;

                if ( _canEdit )
                {
                    BindDropDown();
                    ShowDetails();
                    tbCount.Label = GetAttributeValue( "CountLabel" );
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
                if ( _attendees != null )
                {
                    foreach ( var item in lvMembers.Items )
                    {
                        var hfMember = item.FindControl( "hfMember" ) as HiddenField;
                        var cbMember = item.FindControl( "cbMember" ) as CheckBox;

                        if ( hfMember != null && cbMember != null )
                        {
                            int personId = hfMember.ValueAsInt();

                            var attendance = _attendees.Where( a => a.PersonId == personId ).FirstOrDefault();
                            if ( attendance != null )
                            {
                                attendance.Attended = cbMember.Checked;
                            }
                        }
                    }
                }
            }

            if ( GetAttributeValue( "AutoCount" ).AsBoolean() )
            {
                ScriptManager.RegisterStartupScript( this, this.GetType(), "AutoCount", "doCount = true;", true );
            }
        }

        private void BindDropDown()
        {
            RockContext rockContext = new RockContext();
            AttendanceService attendanceService = new AttendanceService( rockContext );
            var schedules = attendanceService.Queryable().Where( a => a.GroupId == _group.Id ).DistinctBy( s => s.StartDateTime ).Select( s => s.StartDateTime ).ToList();
            ddlPastOccurrences.Items.Clear();
            if ( _occurrence != null )
            {
                ddlPastOccurrences.Items.Add( new ListItem( "Create New", "0" ) );
            }

            foreach ( var schedule in schedules )
            {
                ddlPastOccurrences.Items.Add( new ListItem( schedule.ToStringSafe(), schedule.ToStringSafe() ) );
            }
            if ( _occurrence != null )
            {
                ddlPastOccurrences.SelectedValue = _occurrence.Date.ToStringSafe();
            }
        }

        protected override object SaveViewState()
        {
            ViewState["Attendees"] = _attendees;
            return base.SaveViewState();
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
            if ( _group != null && _occurrence != null )
            {
                var rockContext = new RockContext();
                var attendanceService = new AttendanceService( rockContext );
                var personAliasService = new PersonAliasService( rockContext );

                DateTime startDate = _occurrence.Date;

                var existingAttendees = attendanceService
                    .Queryable( "PersonAlias" )
                    .Where( a =>
                        a.GroupId == _group.Id &&
                        a.LocationId == _occurrence.LocationId &&
                        a.ScheduleId == _occurrence.ScheduleId &&
                        a.StartDateTime == startDate );

                // If did not meet was selected and this was a manually entered occurrence (not based on a schedule/location)
                // then just delete all the attendance records instead of tracking a 'did not meet' value
                if ( cbDidNotMeet.Checked && !_occurrence.ScheduleId.HasValue )
                {
                    foreach ( var attendance in existingAttendees )
                    {
                        attendanceService.Delete( attendance );
                    }
                }
                else
                {
                    if ( cbDidNotMeet.Checked )
                    {
                        // If the occurrence is based on a schedule, set the did not meet flags
                        foreach ( var attendance in existingAttendees )
                        {
                            attendance.DidAttend = null;
                            attendance.DidNotOccur = true;
                        }
                    }

                    foreach ( var attendee in _attendees )
                    {
                        var attendance = existingAttendees
                            .Where( a => a.PersonAlias.PersonId == attendee.PersonId )
                            .FirstOrDefault();

                        if ( attendance == null )
                        {
                            int? personAliasId = personAliasService.GetPrimaryAliasId( attendee.PersonId );
                            if ( personAliasId.HasValue )
                            {
                                attendance = new Attendance();
                                attendance.GroupId = _group.Id;
                                attendance.ScheduleId = _group.ScheduleId;
                                attendance.PersonAliasId = personAliasId;
                                attendance.StartDateTime = _occurrence.Date;
                                attendance.ScheduleId = _occurrence.ScheduleId;
                                attendanceService.Add( attendance );
                            }
                        }

                        if ( attendance != null )
                        {
                            if ( cbDidNotMeet.Checked )
                            {
                                attendance.DidAttend = null;
                                attendance.DidNotOccur = true;
                            }
                            else
                            {
                                attendance.DidAttend = attendee.Attended;
                                attendance.DidNotOccur = null;
                            }
                        }
                    }
                }

                Note note = GetNoteForAttendanceDate( rockContext, true );

                note.Text = tbNotes.Text;

                note.SetAttributeValue( "HeadCount", tbCount.Text );
                note.SaveAttributeValues( rockContext );

                rockContext.SaveChanges();

                Response.Redirect( Request.Url.AbsolutePath + "?GroupId=" + _group.Id.ToString() + "&Date=" + _occurrence.Date );
            }
        }

        private Note GetNoteForAttendanceDate( RockContext rockContext, bool createNew = false )
        {
            NoteTypeService noteTypeService = new NoteTypeService( rockContext );
            NoteType noteType = noteTypeService.Queryable().FirstOrDefault( nt => nt.Guid == new Guid( "FFFC3644-60CD-4D14-A714-E8DCC202A0E1" ) );

            NoteService noteService = new NoteService( rockContext );
            var notes = noteService.Queryable().Where( n => n.NoteType.Guid == noteType.Guid && n.EntityId == _group.Id ).ToList();
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
                if ( dateString != null && _occurrence != null && parseDate == _occurrence.Date )
                {
                    return note;
                }
            }
            if ( createNew )
            {
                //Create new note if one does not exist.
                Note newNote = new Note();
                newNote.NoteType = noteType;
                newNote.EntityId = _group.Id;
                noteService.Add( newNote );
                rockContext.SaveChanges();
                newNote.LoadAttributes();
                newNote.SetAttributeValue( "NoteDate", _occurrence.Date );
                newNote.SaveAttributeValues( rockContext );

                rockContext.SaveChanges();
                return newNote;
            }
            return null;
        }

        protected void ppAddPerson_SelectPerson( object sender, EventArgs e )
        {
            if ( ppAddPerson.PersonId.HasValue )
            {
                if ( !_attendees.Any( a => a.PersonId == ppAddPerson.PersonId.Value ) )
                {
                    var Person = new PersonService( new RockContext() ).Get( ppAddPerson.PersonId.Value );
                    if ( Person != null )
                    {
                        var attendee = new GroupAttendanceAttendee();
                        attendee.PersonId = Person.Id;
                        attendee.NickName = Person.NickName;
                        attendee.LastName = Person.LastName;
                        attendee.Attended = true;
                        _attendees.Add( attendee );
                        BindAttendees();
                    }
                }
            }
        }

        /// <summary>
        /// Handles the ItemCommand event of the lvPendingMembers control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ListViewCommandEventArgs"/> instance containing the event data.</param>
        protected void lvPendingMembers_ItemCommand( object sender, ListViewCommandEventArgs e )
        {
            if ( _group != null && e.CommandName == "Add" )
            {
                int personId = e.CommandArgument.ToString().AsInteger();

                var rockContext = new RockContext();

                foreach ( var groupMember in new GroupMemberService( rockContext )
                    .GetByGroupIdAndPersonId( _group.Id, personId ) )
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
        /// Gets the occurrence items.
        /// </summary>
        private ScheduleOccurrence GetOccurrence()
        {
            DateTime? occurrenceDate = PageParameter( "Date" ).AsDateTime();

            List<int> locationIds = new List<int>();

            List<int> scheduleIds = new List<int>();


            if ( Page.IsPostBack && _allowAddDate )
            {
                if ( dpOccurrenceDate.Visible && dpOccurrenceDate.SelectedDate.HasValue )
                {
                    occurrenceDate = dpOccurrenceDate.SelectedDate + tpOccurrenceTime.SelectedTime;
                }

            }

            if ( occurrenceDate.HasValue )
            {
                // Try to find the selected occurrence based on group's schedule
                if ( _group != null )
                {
                    // Get all the occurrences for this group, and load the attendance so we can show Attendance Count
                    var occurrence = new ScheduleService( _rockContext )
                        .GetGroupOccurrences( _group, occurrenceDate.Value.Date, occurrenceDate.Value.AddDays( 1 ),
                            locationIds, scheduleIds, true )
                        .OrderBy( o => o.Date )
                        .FirstOrDefault();

                    if ( occurrence != null )
                    {
                        if ( occurrenceDate.Value.Date != occurrence.Date.Date )
                        {
                            occurrence.ScheduleId = null;
                            occurrence.ScheduleName = string.Empty;
                            occurrence.Date = occurrenceDate.Value;
                        } else
                        {
                            // Just make sure the date matches exactly what we are looking for
                            occurrence.Date = occurrenceDate.Value;
                        }
                        return occurrence;
                    }
                }

                // If an occurrence date was included, but no occurrence was found with that date, and new 
                // occurrences can be added, create a new one
                if ( _allowAddDate )
                {
                    DateTime startDateTime = occurrenceDate.Value.Date.Add( tpOccurrenceTime.SelectedTime.Value );
                    return new ScheduleOccurrence( startDateTime, tpOccurrenceTime.SelectedTime.Value );
                }
            }

            return null;
        }

        /// <summary>
        /// Loads the dropdowns.
        /// </summary>


        private void BindSchedules( int? locationId )
        {
            var schedules = new Dictionary<int, string> { { 0, "" } };

            if ( _group != null && locationId.HasValue )
            {
                _group.GroupLocations
                    .Where( l => l.LocationId == locationId.Value )
                    .SelectMany( l => l.Schedules )
                    .OrderBy( s => s.Name )
                    .ToList()
                    .ForEach( s => schedules.AddOrIgnore( s.Id, s.Name ) );
            }
        }

        /// <summary>
        /// Binds the group members grid.
        /// </summary>
        protected void ShowDetails()
        {
            bool existingOccurrence = _occurrence != null;

            if ( !existingOccurrence && !_allowAddDate )
            {
                nbNotice.Heading = "No Occurrences";
                nbNotice.Text = "<p>There are currently not any active occurrences for selected group to take attendance for.</p>";
                nbNotice.NotificationBoxType = NotificationBoxType.Warning;
                nbNotice.Visible = true;

                pnlDetails.Visible = false;
            }
            else
            {
                if ( existingOccurrence )
                {
                    lOccurrenceDate.Visible = true;
                    lOccurrenceDate.Text = _occurrence.Date.ToShortDateString();

                    lOccurrenceTime.Visible = true;
                    lOccurrenceTime.Text = _occurrence.Date.ToShortTimeString();

                    dpOccurrenceDate.Visible = false;

                    tpOccurrenceTime.Visible = false;
                }
                else
                {
                    lOccurrenceDate.Visible = false;
                    dpOccurrenceDate.Visible = true;
                    dpOccurrenceDate.SelectedDate = RockDateTime.Today;

                    lOccurrenceTime.Visible = false;
                    tpOccurrenceTime.Visible = true;
                    if ( _group != null && _group.Schedule != null && _group.Schedule.WeeklyTimeOfDay != null )
                    {
                        tpOccurrenceTime.SelectedTime = _group.Schedule.WeeklyTimeOfDay;
                    }
                    else
                    {
                        tpOccurrenceTime.SelectedTime = RockDateTime.Now.TimeOfDay;
                    }

                }

                lMembers.Text = _group.GroupType.GroupMemberTerm.Pluralize();
                lPendingMembers.Text = "Pending " + lMembers.Text;

                List<int> attendedIds = new List<int>();

                // Load the attendance for the selected occurrence
                if ( existingOccurrence )
                {
                    cbDidNotMeet.Checked = _occurrence.DidNotOccur;

                    // Get the list of people who attended
                    attendedIds = new ScheduleService( _rockContext ).GetAttendance( _group, _occurrence )
                        .Where( a => a.DidAttend.HasValue && a.DidAttend.Value )
                        .Select( a => a.PersonAlias.PersonId )
                        .Distinct()
                        .ToList();
                }

                ppAddPerson.Visible = GetAttributeValue( "AllowAddPerson" ).AsBoolean();

                // Get the group members
                var groupMemberService = new GroupMemberService( _rockContext );

                // Add any existing active members not on that list
                var unattendedIds = groupMemberService
                    .Queryable().AsNoTracking()
                    .Where( m =>
                        m.GroupId == _group.Id &&
                        m.GroupMemberStatus == GroupMemberStatus.Active &&
                        !attendedIds.Contains( m.PersonId ) )
                    .Select( m => m.PersonId )
                    .ToList();

                // Bind the attendance roster
                _attendees = new PersonService( _rockContext )
                    .Queryable().AsNoTracking()
                    .Where( p => attendedIds.Contains( p.Id ) || unattendedIds.Contains( p.Id ) )
                    .Select( p => new GroupAttendanceAttendee()
                    {
                        PersonId = p.Id,
                        NickName = p.NickName,
                        LastName = p.LastName,
                        Attended = attendedIds.Contains( p.Id )
                    } )
                    .ToList();
                BindAttendees();

                // Bind the pending members
                var pendingMembers = groupMemberService
                    .Queryable().AsNoTracking()
                    .Where( m =>
                        m.GroupId == _group.Id &&
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

                RockContext rockContext = new RockContext();
                Note note = GetNoteForAttendanceDate( rockContext );

                if ( note != null )
                {
                    tbNotes.Text = note.Text;
                    tbCount.Text = note.GetAttributeValue( "HeadCount" );
                }

            }

        }

        private void BindAttendees()
        {
            lvMembers.DataSource = _attendees.OrderBy( a => a.LastName ).ThenBy( a => a.NickName ).ToList();
            lvMembers.DataBind();

            ppAddPerson.PersonId = Rock.Constants.None.Id;
            ppAddPerson.PersonName = "Add New Attendee";
        }

        protected void RegisterScript()
        {
            string script = string.Format( @"

    Sys.Application.add_load(function () {{

        if ($('#{0}').is(':checked')) {{
            $('div.js-roster').hide();
        }}

        $('#{0}').click(function () {{
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


        protected void ddlPastOccurrences_SelectionChanged( object sender, EventArgs e )
        {
            Response.Redirect( Request.Url.AbsolutePath + "?GroupId=" + _group.Id.ToString() + "&Date=" + ddlPastOccurrences.SelectedValue );
        }
    }
}