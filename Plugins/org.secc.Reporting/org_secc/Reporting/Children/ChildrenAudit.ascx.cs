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
using System.Data;
using System.Linq;
using System.Web.UI;
using org.secc.FamilyCheckin.Utilities;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Blocks.Reporting.Children
{
    /// <summary>
    /// Block to execute a sql command and display the result (if any).
    /// </summary>
    [DisplayName( "Children Audit" )]
    [Category( "SECC > Reporting > Children" )]
    [Description( "Tool to display audit information of children's ministry" )]

    public partial class ChildrenAudit : RockBlock
    {
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            gReport.GridRebind += RebindGrid;
        }

        private void RebindGrid( object sender, GridRebindEventArgs e )
        {
            BindGrid();
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
                dpDate.SelectedDate = GetBlockUserPreference( "Date" ).AsDateTime();

                RockContext rockContext = new RockContext();
                LocationService locationService = new LocationService( rockContext );
                var locationId = GetBlockUserPreference( "Location" ).AsInteger();
                lpLocation.Location = locationService.Get( locationId );

                ScheduleService scheduleService = new ScheduleService( rockContext );
                var schedule = scheduleService.Get( GetBlockUserPreference( "Schedule" ).AsInteger() );
                if ( schedule != null )
                {
                    spSchedule.SetValue( schedule );
                }
            }
        }

        private void BindGrid()
        {
            nbNotification.Visible = false;
            RockContext rockContext = new RockContext();
            AttendanceOccurrenceService attendanceOccurrenceService = new AttendanceOccurrenceService( rockContext );
            AttendanceService attendanceService = new AttendanceService( rockContext );
            LocationService locationService = new LocationService( rockContext );
            ScheduleService scheduleService = new ScheduleService( rockContext );

            var location = lpLocation.Location;
            if ( location == null || dpDate.SelectedDate == null )
            {
                return;
            }

            //Set the name of the export
            List<string> locationNameList = new List<string>();
            var locationForExport = location;

            while ( true )
            {
                if ( locationForExport == null )
                {
                    break;
                }
                locationNameList.Add( locationForExport.Name );
                locationForExport = locationForExport.ParentLocation;
            }

            locationNameList.Reverse();
            gReport.ExportTitleName = dpDate.SelectedDate.Value.ToString( "MM/dd/yyyy" ) +
                "  -- ";

            if ( tgChildLocations.Checked )
            {
                gReport.ExportTitleName += " Child Locations of: ";
            }
            gReport.ExportTitleName += string.Join( " > ", locationNameList );
            var schedule = scheduleService.Get( spSchedule.SelectedValueAsId() ?? 0 );
            if ( schedule != null )
            {
                gReport.ExportTitleName += " (" + schedule.Name + ")";
            }

            var fileName = dpDate.SelectedDate.Value.ToString( "MM.dd.yyyy" ) +
                ( tgChildLocations.Checked ? "_ChildLocationsOf_" : "_" ) +
                location.Name;
            if ( schedule != null )
            {
                fileName += "_" + schedule.Name;
            }

            gReport.ExportFilename = fileName;



            //Get child locations if needed
            var locationIds = new List<int> { location.Id };
            if ( tgChildLocations.Checked )
            {
                List<Location> childLocations = GetChildLocations( location );
                locationIds.AddRange( childLocations.Select( l => l.Id ) );
            }

            var attendanceOccurrencesQry = attendanceOccurrenceService
                .Queryable()
                .Where( ao => ao.OccurrenceDate == ( dpDate.SelectedDate ?? RockDateTime.Now ) )
                .Where( ao => locationIds.Contains( ao.LocationId ?? 0 ) )
                .Where( ao => ao.Group != null && ao.Schedule != null );


            var scheduleId = spSchedule.SelectedValueAsId();
            if ( scheduleId != null )
            {
                attendanceOccurrencesQry = attendanceOccurrencesQry.Where( ao => ao.ScheduleId == scheduleId );
            }

            var attendanceOccurrences = attendanceOccurrencesQry
                .Select( ao => ao.Id )
                .ToList();

            var attendances = attendanceService.Queryable( "PersonAlias, PersonAlias.Person, Occurrence, Occurrence.Group, Occurrence.Location, Occurrence.Schedule, Device" )
                .Where( a => attendanceOccurrences.Contains( a.OccurrenceId ) )
                .ToList();

            if ( !attendances.Any() )
            {
                nbNotification.Visible = true;
                nbNotification.Text = "Could not find any attendances with these parameters";
                return;
            }

            var records = new List<AttendanceRecord>();
            var volunteerGroupIds = KioskCountUtility.GetVolunteerGroupIds();

            foreach ( var attendance in attendances )
            {
                var record = new AttendanceRecord
                {
                    PersonId = attendance.PersonAlias.Person.Id,
                    PersonName = attendance.PersonAlias.Person.FullNameReversed,
                    Age = attendance.PersonAlias.Person.Age,
                    Schedule = attendance.Occurrence.Schedule.Name,
                    Location = attendance.Occurrence.Location.Name,
                    Group = attendance.Occurrence.Group.Name,
                    CheckInTime = attendance.CreatedDateTime,
                    EntryTime = attendance.StartDateTime,
                    ExitTime = attendance.EndDateTime,
                    DidAttend = attendance.DidAttend,
                    IsVolunteer = volunteerGroupIds.Contains( attendance.Occurrence.GroupId ?? 0 ),
                    Device = attendance.Device != null ? attendance.Device.Name : "Room Scanner"
                };

                if ( attendance.ForeignId != null )
                {
                    var subLocation = locationService.Get( attendance.ForeignId ?? 0 );
                    if ( subLocation != null )
                    {
                        record.SubLocation = subLocation.Name;
                    }
                }

                if ( record.CheckInTime >= record.EntryTime && record.Device != "Room Scanner" )
                {
                    record.EntryTime = null;
                }

                records.Add( record );
            }

            records = records
                .OrderBy( r => r.CheckInTime )
                .ToList();

            gReport.DataSource = records;
            gReport.DataBind();

        }

        private List<Location> GetChildLocations( Location parentLocation )
        {
            List<Location> locations = new List<Location>();
            locations.AddRange( parentLocation.ChildLocations );
            var intermediateLocations = new List<Location>();
            foreach ( var location in locations )
            {
                //Recursion!!
                intermediateLocations.AddRange( GetChildLocations( location ) );
            }
            locations.AddRange( intermediateLocations );
            return locations;
        }

        protected void btnGo_Click( object sender, EventArgs e )
        {
            SetBlockUserPreference( "Date", dpDate.SelectedDate.ToString() );
            SetBlockUserPreference( "Location", lpLocation.Location.Id.ToString() );
            SetBlockUserPreference( "Schedule", spSchedule.SelectedValue );
            BindGrid();
        }

        public class AttendanceRecord
        {
            public int PersonId { get; set; }
            public string PersonName { get; set; }
            public int? Age { get; set; }
            public string Location { get; set; }
            public string Schedule { get; set; }
            public string Group { get; set; }
            public DateTime? CheckInTime { get; set; }
            public DateTime? EntryTime { get; set; }
            public DateTime? ExitTime { get; set; }
            public bool? DidAttend { get; set; }
            public bool IsVolunteer { get; set; }
            public string Device { get; set; }
            public string SubLocation { get; set; }
        }


        protected void gReport_RowSelected( object sender, RowEventArgs e )
        {
            var date = dpDate.SelectedDate;
            if ( date == null )
            {
                return;
            }

            RockContext rockContext = new RockContext();
            PersonService personService = new PersonService( rockContext );
            HistoryService historyService = new HistoryService( rockContext );
            var person = personService.Get( ( int ) e.RowKeyValue );
            if ( person == null )
            {
                return;
            }

            var dayOf = date.Value.Date;
            var nextDay = dayOf.AddDays( 1 );

            int personEntityTypeId = EntityTypeCache.Get( Rock.SystemGuid.EntityType.PERSON.AsGuid() ).Id;
            var history = historyService
                .Queryable()
                .Where(
                h => h.EntityTypeId == personEntityTypeId
                && h.EntityId == person.Id
                && h.CategoryId == 4
                && h.CreatedDateTime >= dayOf
                && h.CreatedDateTime < nextDay
                ).ToList();

            //This is a hack.
            //The HTML grid field doesn't render html on the first column for some reason.
            //This inserts a non-HTML item so the later items will be rendered correctly.
            history.Insert( 0, new History() { Summary = "Summary:" } );

            gHistory.DataSource = history;
            gHistory.DataBind();

            mdModal.Title = string.Format( "{0} data for {1}", person.FullName.ToPossessive(), dayOf.ToString( "MM/dd/yyyy" ) );
            mdModal.Show();

        }
    }
}