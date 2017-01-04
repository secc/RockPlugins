// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
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
using System.Data;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Blocks.Reporting.Children
{
    /// <summary>
    /// Block to execute a sql command and display the result (if any).
    /// </summary>
    [DisplayName( "Breakout Group Attendance" )]
    [Category( "SECC > Reporting > Children" )]
    [Description( "A filterable and sortable list of breakout groups." )]

    [GroupTypeField( "Breakout Group Type", "GroupType of the Breakout Groups" )]
    [TextField( "Schedule IDs", "Coma separated list of the Ids of schedules." )]
    public partial class BreakoutGroupAttendance : RockBlock
    {

        List<Schedule> schedules;
        RockContext rockContext;
        List<Dictionary<int, int>> scheduleValueDicts;
        Dictionary<int, DateTime> firstAttendanceDate;
        Dictionary<int, DateTime> lastAttendanceDate;

        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            gBreakoutGroups.Actions.ShowCommunicate = true;
        }
        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            var scheduleIds = GetAttributeValue( "ScheduleIDs" );
            rockContext = new RockContext();
            schedules = new ScheduleService( rockContext )
                .GetByIds( GetAttributeValue( "ScheduleIDs" )
                    .Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries )
                    .Select( int.Parse )
                    .ToList()
            ).ToList();

            if ( !Page.IsPostBack )
            {
                BindGrid();
            }
        }

        private void BindGrid()
        {
            var groupTypeGuid = GetAttributeValue( "BreakoutGroupType" );
            if ( string.IsNullOrWhiteSpace( groupTypeGuid ) )
            {
                return;
            }

            var groupType = new GroupTypeService( rockContext ).Get( groupTypeGuid.AsGuid() );
            if ( groupType == null )
            {
                return;
            }

            List<BreakoutGroupMember> data = new List<BreakoutGroupMember>();

            BindFilter( groupType.Groups, schedules );

            var selectedGroups = new List<int>();
            foreach ( ListItem item in cblGroups.Items )
            {
                if ( item.Selected )
                {
                    selectedGroups.Add( item.Value.AsInteger() );
                }
            }

            var breakoutGroupMembers = new GroupService( rockContext )
                .GetByIds( selectedGroups )
                .SelectMany( g => g.Members )
                .Select( gm => new BreakoutGroupMember
                {
                    group = gm.Group,
                    Name = gm.Person.LastName + ", " + gm.Person.NickName,
                    person = gm.Person,
                    Gender = gm.Person.Gender,
                    Birthdate = gm.Person.BirthDate,
                    Breakout = gm.Group.Name
                }
                )
                .OrderBy( a => a.Breakout )
                .ThenBy( a => a.Name )
                .ToList();

            var selectedSchedules = new List<string>();
            scheduleValueDicts = new List<Dictionary<int, int>>();

            var personIds = breakoutGroupMembers.Select( bgm => bgm.person.Id ).ToList();
            var attendanceService = new AttendanceService( rockContext );

            var lower = drRange.LowerValue;
            var upper = drRange.UpperValue;
            if ( upper != null )
            {
                upper = upper.Value.AddDays( 1 );
            }

            //Build dictionaries containing the values for the attendance count to be added on row bind
            foreach ( ListItem item in cblSchedules.Items )
            {
                if ( item.Selected )
                {
                    selectedSchedules.Add( item.Value );
                    var scheduleId = item.Value.AsInteger();

                    var attendanceQry = attendanceService.Queryable()
                        .Where( a => personIds.Contains( a.PersonAlias.PersonId ) && a.ScheduleId == scheduleId );

                    if ( upper != null )
                    {
                        attendanceQry = attendanceQry.Where( a => a.StartDateTime <= upper );
                    }
                    if ( lower != null )
                    {
                        attendanceQry = attendanceQry.Where( a => a.StartDateTime >= lower );
                    }
                    var groupby = attendanceQry.GroupBy( a => a.PersonAlias.PersonId )
                        .Select( g => new { Key = g.Key, Value = g.Count() } ).ToDictionary( kvp => kvp.Key, kvp => kvp.Value );
                    scheduleValueDicts.Add( groupby );
                }
            }

            //Get first and last attendance dates
            List<int> selectedSchedulesInt = selectedSchedules.Select( ss => ss.AsInteger() ).ToList();

            var attendanceDateQry = attendanceService.Queryable()
                .Where( a => personIds.Contains(a.PersonAlias.PersonId) && a.ScheduleId != null && selectedSchedulesInt.Contains( a.ScheduleId ?? 0 ) );

            if ( upper != null )
            {
                attendanceDateQry = attendanceDateQry.Where( a => a.StartDateTime <= upper );
            }
            if ( lower != null )
            {
                attendanceDateQry = attendanceDateQry.Where( a => a.StartDateTime >= lower );
            }

            var attendanceDateSelect = attendanceDateQry
                .Select( a => new { Key = a.PersonAlias.PersonId, Value = a.StartDateTime } );

            firstAttendanceDate = attendanceDateSelect
                .OrderBy( a => a.Value )
                .DistinctBy( a => a.Key )
                .ToDictionary( a => a.Key, a => a.Value );

            lastAttendanceDate = attendanceDateSelect
                .OrderByDescending( a => a.Value )
                .DistinctBy( a => a.Key )
                .ToDictionary( a => a.Key, a => a.Value );

            //remove dynamically added columns if any exist
            while ( gBreakoutGroups.Columns.Count > 8 )
            {
                gBreakoutGroups.Columns.RemoveAt( 8 );
            }

            foreach ( var schedule in schedules )
            {
                if ( selectedSchedules.Contains( schedule.Id.ToString() ) )
                {
                    var field = new RockLiteralField();
                    field.HeaderText = schedule.Name;
                    gBreakoutGroups.Columns.Add( field );
                }
            }

            gBreakoutGroups.DataSource = breakoutGroupMembers;
            gBreakoutGroups.DataBind();
        }

        private void BindFilter( ICollection<Group> groups, ICollection<Schedule> schedules )
        {

            //Group checkbox
            cblGroups.Items.Clear();
            foreach ( var group in groups )
            {
                ListItem listItem = new ListItem()
                {
                    Value = group.Id.ToString(),
                    Text = group.Name
                };
                cblGroups.Items.Add( listItem );
            }
            var groupPreference = GetUserPreference( BlockCache.Guid.ToString() + "Group" )
                .Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries )
                .ToList();

            //Pre-select based on user preference
            if ( groupPreference.Any() )
            {
                foreach ( ListItem item in cblGroups.Items )
                {
                    if ( groupPreference.Contains( item.Value ) )
                    {
                        item.Selected = true;
                    }
                }
            }
            else
            {
                foreach ( ListItem item in cblGroups.Items )
                {
                    item.Selected = true;
                }
            }

            //Schedule checkbox
            cblSchedules.Items.Clear();
            foreach ( var schedule in schedules )
            {
                ListItem listItem = new ListItem()
                {
                    Value = schedule.Id.ToString(),
                    Text = schedule.Name
                };
                cblSchedules.Items.Add( listItem );
            }
            var schedulePreference = GetUserPreference( BlockCache.Guid.ToString() + "Schedule" )
                .Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries )
                .ToList();

            //Pre-select based on user preference
            if ( schedulePreference.Any() )
            {
                foreach ( ListItem item in cblSchedules.Items )
                {
                    if ( schedulePreference.Contains( item.Value ) )
                    {
                        item.Selected = true;
                    }
                }
            }
            else
            {
                foreach ( ListItem item in cblSchedules.Items )
                {
                    item.Selected = true;
                }
            }

            //DateRange
            var lower = GetUserPreference( BlockCache.Guid.ToString() + "Lower" );
            var upper = GetUserPreference( BlockCache.Guid.ToString() + "Upper" );

            if ( !string.IsNullOrWhiteSpace( lower ) && !string.IsNullOrWhiteSpace( upper ) )
            {
                drRange.UpperValue = DateTime.Parse( upper );
                drRange.LowerValue = DateTime.Parse( lower );
            }


        }

        protected void gBreakoutGroups_RowDataBound( object sender, GridViewRowEventArgs e )
        {
            if ( e.Row.RowType == DataControlRowType.DataRow )
            {
                var total = 0;
                var attendanceService = new AttendanceService( rockContext ).Queryable();
                BreakoutGroupMember bgm = ( BreakoutGroupMember ) e.Row.DataItem;
                e.Row.Cells[4].Text = bgm.person.GradeFormatted;

                var schedulePreference = GetUserPreference( BlockCache.Guid.ToString() + "Schedule" )
                .Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries )
                .ToList();

                var i = 0;

                if ( firstAttendanceDate.ContainsKey( bgm.person.Id ) )
                {
                    e.Row.Cells[5].Text = firstAttendanceDate[bgm.person.Id].ToString( "MM/dd/yyyy " );
                }

                if ( lastAttendanceDate.ContainsKey( bgm.person.Id ) )
                {
                    e.Row.Cells[6].Text = lastAttendanceDate[bgm.person.Id].ToString( "MM/dd/yyyy " );
                }

                if ( schedulePreference.Any() )
                {
                    foreach ( var schedule in schedules )
                    {
                        if ( !schedulePreference.Contains( schedule.Id.ToString() ) )
                        {
                            continue;
                        }

                        var attendance = 0;

                        if ( scheduleValueDicts[i].ContainsKey(bgm.person.Id) )
                        {
                            attendance = scheduleValueDicts[i][bgm.person.Id];
                        }


                        total += attendance;
                        e.Row.Cells[8 + i].Text = attendance.ToString();
                        i++;
                    }
                }
                e.Row.Cells[7].Text = total.ToString();
            }
        }

        protected void fBreakoutGroups_ApplyFilterClick( object sender, EventArgs e )
        {
            var groupPreferences = new List<string>();

            foreach ( ListItem item in cblGroups.Items )
            {
                if ( item.Selected )
                {
                    groupPreferences.Add( item.Value );
                }
            }
            SetUserPreference( BlockCache.Guid.ToString() + "Group", string.Join( ",", groupPreferences ) );

            var schedulePreferences = new List<string>();

            foreach ( ListItem item in cblSchedules.Items )
            {
                if ( item.Selected )
                {
                    schedulePreferences.Add( item.Value );
                }
            }
            SetUserPreference( BlockCache.Guid.ToString() + "Schedule", string.Join( ",", schedulePreferences ) );

            if ( drRange.LowerValue != null && drRange.UpperValue != null )
            {
                SetUserPreference( BlockCache.Guid.ToString() + "Lower", drRange.LowerValue.ToString() );
                SetUserPreference( BlockCache.Guid.ToString() + "Upper", drRange.UpperValue.ToString() );
            }

            BindGrid();
        }
    }

    class BreakoutGroupMember
    {
        public Rock.Model.Group group { get; set; }
        public Person person { get; set; }
        public string Breakout { get; set; }
        public string Name { get; set; }
        public DateTime? Birthdate { get; set; }
        public Gender Gender { get; set; }
        public string Grade { get; set; }
    }
}