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
        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

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

            var selectedGroups = new List<string>();
            foreach ( ListItem item in cblGroups.Items )
            {
                if ( item.Selected )
                {
                    selectedGroups.Add( item.Value );
                }
            }

            foreach ( var group in groupType.Groups )
            {
                if ( selectedGroups.Contains( group.Id.ToString() ) )
                {
                    foreach ( var member in group.Members )
                    {
                        data.Add( new BreakoutGroupMember { group = group, person = member.Person } );
                    }
                }
            }

            var selectedSchedules = new List<string>();
            foreach ( ListItem item in cblSchedules.Items )
            {
                if ( item.Selected )
                {
                    selectedSchedules.Add( item.Value );
                }
            }

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

            var source = data
                 .Select( d =>
                  new BreakoutGroupMember()
                  {
                      group = d.group,
                      person = d.person,
                      Breakout = d.group.Name,
                      Name = string.Format( "{0}, {1}", d.person.LastName, d.person.NickName ),
                      Birthdate = d.person.BirthDate.ToString(),
                      Gender = d.person.Gender.ToString(),
                      Grade = d.person.GradeFormatted
                  }
                 )
                 .OrderBy( a => a.Breakout )
                 .ThenBy( a => a.Name );

            gBreakoutGroups.DataSource = source;
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
                var cell = 8;

                var schedulePreference = GetUserPreference( BlockCache.Guid.ToString() + "Schedule" )
                .Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries )
                .ToList();

                var lower = drRange.LowerValue;
                var upper = drRange.UpperValue;
                if ( upper != null )
                {
                    upper = upper.Value.AddDays( 1 );
                }

                foreach ( var schedule in schedules )
                {
                    if ( !schedulePreference.Any() || schedulePreference.Contains( schedule.Id.ToString() ) )
                    {
                        var attendance = attendanceService
                            .Where( a =>
                                a.ScheduleId == schedule.Id
                                && a.PersonAlias.PersonId == bgm.person.Id
                                && a.DidAttend == true );

                        if ( upper != null && lower != null )
                        {
                            attendance = attendance.Where( a => a.StartDateTime >= lower && a.StartDateTime <= upper );
                        }
                        attendance = attendance.OrderBy( a => a.StartDateTime );
                        if ( attendance.Any() )
                        {
                            e.Row.Cells[5].Text = attendance
                                .First()
                                .StartDateTime
                                .ToString( "MM/dd/yyyy " );
                            e.Row.Cells[6].Text = attendance.ToList()
                                .Last()
                                .StartDateTime
                                .ToString( "MM/dd/yyyy " );
                        }

                        total += attendance.Count();
                        e.Row.Cells[cell].Text = attendance.Count().ToString();
                        cell++;
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
        public string Birthdate { get; set; }
        public string Gender { get; set; }
        public string Grade { get; set; }
    }
}