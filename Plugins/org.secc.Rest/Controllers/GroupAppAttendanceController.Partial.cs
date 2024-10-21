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
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Http;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Rest;

namespace org.secc.Rest.Controllers
{
    public partial class GroupAppAttendanceController : ApiControllerBase
    {
        private readonly RockContext _context;

        public GroupAppAttendanceController()
        {
            _context = new RockContext();
        }

        /// <summary>
        /// Get all the attendance records for a group based on the occurrence date
        /// </summary>
        /// <param name="groupId"></param>        
        /// <param name="occurrenceDate"></param>
        /// <returns></returns>
        [HttpGet]
        [System.Web.Http.Route( "api/GroupApp/Attendance/{groupId}/{occurrenceDate}" )]
        public IHttpActionResult GetGroupAttendance( int groupId, DateTime occurrenceDate )
        {
            var currentUser = UserLoginService.GetCurrentUser();
            if ( currentUser == null )
                return StatusCode( HttpStatusCode.Unauthorized );

            var group = new GroupService( _context ).Get( groupId );
            if ( group == null )
                return NotFound();

            if ( !group.IsAuthorized( Rock.Security.Authorization.VIEW, currentUser.Person ) )
                return StatusCode( HttpStatusCode.Forbidden );

            var attendees = new AttendanceService( _context )
                .Queryable()
                .Where(
                    a => a.Occurrence.GroupId == group.Id &&
                    a.Occurrence.OccurrenceDate == occurrenceDate &&
                    a.DidAttend == true
                )
                .Select( a => new
                {
                    a.PersonAlias.PersonId,
                    a.OccurrenceId,
                    a.Id
                } )
                .ToList();

            var attendeePersonIds = attendees.Select( a => a.PersonId ).ToList();

            var groupMembers = new GroupMemberService( _context )
                .Queryable()
                .Where( gm => gm.GroupId == group.Id && attendeePersonIds.Contains( gm.Person.Id ) )
                .Select( gm => new
                {
                    gm.PersonId,
                    gm.Id
                } )
                .ToList();

            var occurrences = new AttendanceOccurrenceService( _context )
            .Queryable()
            .Where( o => o.GroupId == group.Id && o.OccurrenceDate == occurrenceDate )
            .ToList();

            var finalResult = from attendee in attendees
                              join groupMember in groupMembers on attendee.PersonId equals groupMember.PersonId
                              join occurrence in occurrences on attendee.OccurrenceId equals occurrence.Id
                              select new
                              {
                                  AttendanceId = attendee.Id,
                                  GroupMemberId = groupMember.Id,
                                  attendee.OccurrenceId,
                                  DidNotMeet = occurrence.DidNotOccur
                              };

            if ( !finalResult.Any() )
            {
                var didNotMeetValue = occurrences.FirstOrDefault()?.DidNotOccur ?? false;
                return Ok( new { DidNotMeet = didNotMeetValue } );
            }

            return Ok( finalResult );
        }

        /// <summary>
        /// Post an attendance record with a groupId, occurrenceDate, and groupMemberId
        /// </summary>
        /// <returns>IHttpActionResult</returns>
        [HttpPost]
        [System.Web.Http.Route( "api/GroupApp/Attendance/" )]
        public IHttpActionResult PostGroupAttendance( int groupId, DateTime occurrenceDate, int groupMemberId, int? scheduleId = null, int? locationId = null )
        {
            var currentUser = UserLoginService.GetCurrentUser();
            if ( currentUser == null )
                return StatusCode( HttpStatusCode.Unauthorized );

            var group = new GroupService( _context ).Get( groupId );
            if ( group == null )
                return NotFound();

            if ( !group.IsAuthorized( Rock.Security.Authorization.EDIT, currentUser.Person ) || !group.IsAuthorized( Rock.Security.Authorization.MANAGE_MEMBERS, currentUser.Person ) )
                return StatusCode( HttpStatusCode.Forbidden );

            locationId = locationId.HasValue ? locationId.Value : group.GroupLocations?.FirstOrDefault()?.Location?.Id;

            var groupMember = new GroupMemberService( _context ).Get( groupMemberId );

            var attendanceService = new AttendanceService( _context );
            var personAliasService = new PersonAliasService( _context );

            var attendanceData = attendanceService
                .Queryable( "PersonAlias" )
                .Where( a => a.Occurrence.GroupId == groupId && a.Occurrence.LocationId == locationId && a.Occurrence.OccurrenceDate == occurrenceDate );

            if ( scheduleId.HasValue )
            {
                attendanceData = attendanceData.Where( a => a.Occurrence.ScheduleId == scheduleId );
            }

            var attendanceItem = attendanceData.Where( a => a.PersonAlias.PersonId == groupMember.Person.Id )
                                .FirstOrDefault();
            if ( attendanceItem == null )
            {
                var attendancePerson = new PersonService( _context ).Get( groupMember.Person.Id );
                if ( attendancePerson != null && attendancePerson.PrimaryAliasId.HasValue )
                {
                    attendanceItem = attendanceService.AddOrUpdate( attendancePerson.PrimaryAliasId.Value, occurrenceDate, groupId, locationId, scheduleId, group.CampusId );
                }
            }

            if ( attendanceItem != null )
            {
                attendanceItem.DidAttend = true;
                attendanceItem.Note = "Checked in via Group App";
                attendanceItem.ModifiedByPersonAliasId = currentUser.Person.PrimaryAlias.Id;
                if ( attendanceItem.CreatedByPersonAliasId == null )
                {
                    attendanceItem.CreatedByPersonAliasId = currentUser.Person.PrimaryAlias.Id;
                }
            }

            _context.SaveChanges();

            return Ok();
        }

        /// <summary>
        /// Endpoint to mark "We Did Not Meet"
        /// </summary>
        /// <returns>IHttpActionResult</returns>
        [HttpPost]
        [System.Web.Http.Route( "api/GroupApp/Attendance/DidNotMeet" )]
        public IHttpActionResult PostDidNotMeet( int groupId, DateTime occurrenceDate, int? scheduleId = null, int? locationId = null, bool didNotMeet = true )
        {
            var currentUser = UserLoginService.GetCurrentUser();
            if ( currentUser == null )
                return StatusCode( HttpStatusCode.Unauthorized );

            var group = new GroupService( _context ).Get( groupId );
            if ( group == null )
                return NotFound();

            if ( !group.IsAuthorized( Rock.Security.Authorization.EDIT, currentUser.Person ) || !group.IsAuthorized( Rock.Security.Authorization.MANAGE_MEMBERS, currentUser.Person ) )
                return StatusCode( HttpStatusCode.Forbidden );

            locationId = locationId ?? ( group.GroupLocations.Any() ? ( int? ) group.GroupLocations
                        .Where( l => l.GroupLocationTypeValueId == 19 || l.GroupLocationTypeValueId == 209 )
                        .Select( l => l.LocationId ).FirstOrDefault()
                    : null );

            scheduleId = scheduleId ?? group.ScheduleId ?? null;

            var attendanceOccurrenceService = new AttendanceOccurrenceService( _context );
            var occurrence = attendanceOccurrenceService.GetOrAdd( occurrenceDate, groupId, locationId, scheduleId );
            occurrence.DidNotOccur = didNotMeet;

            if ( ( bool ) !didNotMeet )
            {
                _context.SaveChanges();

                return Ok();
            }

            var attendanceService = new AttendanceService( _context );


            var attendanceData = attendanceService
                .Queryable( "PersonAlias" )
                .Where( a => a.Occurrence.GroupId == groupId && a.Occurrence.LocationId == locationId && a.Occurrence.OccurrenceDate == occurrenceDate );


            if ( scheduleId.HasValue )
            {
                attendanceData = attendanceData.Where( a => a.Occurrence.ScheduleId == scheduleId );
            }

            var attendanceItems = attendanceData.ToList();

            foreach ( var attendanceItem in attendanceItems )
            {
                attendanceItem.DidAttend = false;
                attendanceItem.Note = "Group did not meet";
                attendanceItem.ModifiedByPersonAliasId = currentUser.Person.PrimaryAlias.Id;
                if ( attendanceItem.CreatedByPersonAliasId == null )
                {
                    attendanceItem.CreatedByPersonAliasId = currentUser.Person.PrimaryAlias.Id;
                }
            }

            _context.SaveChanges();

            return Ok();
        }

        /// <summary>
        /// Returns the possible schedules for the provided group ID
        /// </summary>
        /// <param name="groupId">The group ID</param>
        /// <returns>List<Group></returns> 
        [HttpGet]
        [System.Web.Http.Route( "api/GroupApp/AttendanceSchedules/{groupId}" )]
        public IHttpActionResult GetAttendanceSchedules( int groupId )
        {
            var currentUser = UserLoginService.GetCurrentUser();
            if ( currentUser == null )
                return StatusCode( HttpStatusCode.Unauthorized );

            var group = new GroupService( _context ).Get( groupId );
            if ( group == null )
                return NotFound();

            if ( !group.IsAuthorized( Rock.Security.Authorization.VIEW, currentUser.Person ) )
                return StatusCode( HttpStatusCode.Forbidden );

            if ( group.Schedule == null )
                return NotFound();

            var groupScheduleOccurrences = GetListOfOccurrences( group );

            return Ok( groupScheduleOccurrences );
        }

        private List<GroupScheduleOccurence> GetListOfOccurrences( Group group )
        {
            RockContext rockContext = _context;
            var startDate = RockDateTime.Today.AddYears( -1 );
            var enddate = RockDateTime.Today.AddDays( 1 );

            var existingOccurrences = new AttendanceOccurrenceService( rockContext )
                .Queryable()
                .Include( o => o.Schedule )
                .AsNoTracking()
                .Where( g => g.GroupId == group.Id )
                .Where( o => o.OccurrenceDate >= startDate && o.OccurrenceDate < enddate )
                .ToList();

            var occurrences = new List<GroupScheduleOccurence>();

            if ( group.Schedule != null )
            {
                if ( group.Schedule.ScheduleType == ScheduleType.Custom || group.Schedule.ScheduleType == ScheduleType.Named )
                {
                    var previousScheduleDates = group.Schedule.GetScheduledStartTimes( startDate, enddate )
                        .OrderByDescending( o => o )
                        .Take( 50 )
                        .ToList();

                    occurrences.AddRange( previousScheduleDates
                        .Select( p => new GroupScheduleOccurence
                        {
                            OccurrenceDate = p,
                            GroupId = group.Id,
                            ScheduleId = group.ScheduleId,
                            LocationId = group.GroupLocations.Any() ? ( int? ) group.GroupLocations.Select( l => l.LocationId ).FirstOrDefault() : null,
                            StartDateTime = group.Schedule.GetNextStartDateTime( p ),
                            DidNotMeet = false // Default value
                        } )
                        .ToList() );

                    foreach ( var occurrence in existingOccurrences )
                    {
                        var selectedOccurrence = occurrences
                            .Where( o => o.OccurrenceDate.Date == occurrence.OccurrenceDate.Date )
                            .Where( o => o.ScheduleId == occurrence.ScheduleId )
                            .FirstOrDefault();

                        if ( selectedOccurrence != null )
                        {
                            selectedOccurrence.OccurrenceId = occurrence.Id;
                            if ( !selectedOccurrence.LocationId.HasValue )
                            {
                                selectedOccurrence.LocationId = occurrence.LocationId;
                            }
                            selectedOccurrence.DidNotMeet = occurrence.DidNotOccur; // Update DidNotMeet property
                        }
                    }
                }
                else if ( group.Schedule.ScheduleType == ScheduleType.Weekly )
                {
                    var lastSchedule = RockDateTime.Today;
                    while ( lastSchedule.DayOfWeek != group.Schedule.WeeklyDayOfWeek )
                    {
                        lastSchedule = lastSchedule.AddDays( -1 );
                    }

                    while ( lastSchedule > startDate )
                    {
                        occurrences.Add( new GroupScheduleOccurence
                        {
                            OccurrenceDate = lastSchedule,
                            GroupId = group.Id,
                            ScheduleId = group.ScheduleId,
                            LocationId = group.GroupLocations.Any() ? ( int? ) group.GroupLocations.Select( l => l.LocationId ).FirstOrDefault() : null,
                            StartDateTime = lastSchedule.Add( group.Schedule.WeeklyTimeOfDay ?? new TimeSpan( 0, 0, 0 ) ),
                            DidNotMeet = false // Default value
                        } );

                        lastSchedule = lastSchedule.AddDays( -7 );
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
                            selectedOccurrence.DidNotMeet = occurrence.DidNotOccur; // Update DidNotMeet property
                        }
                    }
                }
            }

            var tmpOccurrence = existingOccurrences
                .Where( o => !occurrences.Select( o1 => o1.OccurrenceId ).Contains( o.Id ) )
                .ToList();

            foreach ( var occ in tmpOccurrence )
            {
                var summary = new GroupScheduleOccurence
                {
                    OccurrenceId = occ.Id,
                    GroupId = occ.GroupId,
                    LocationId = occ.LocationId,
                    ScheduleId = occ.ScheduleId,
                    OccurrenceDate = occ.OccurrenceDate,
                    DidNotMeet = occ.DidNotOccur // Update DidNotMeet property
                };

                if ( occ.Schedule == null )
                {
                    summary.StartDateTime = occ.OccurrenceDate;
                }
                else
                {
                    if ( !occ.Schedule.IsActive )
                    {
                        occ.Schedule.IsActive = true;
                    }
                    var tmpStartDate = occ.Schedule.GetNextStartDateTime( occ.OccurrenceDate );

                    if ( tmpStartDate.HasValue )
                    {
                        summary.StartDateTime = tmpStartDate.Value;
                    }
                    else if ( occ.Schedule.WeeklyTimeOfDay.HasValue )
                    {
                        summary.StartDateTime = occ.OccurrenceDate.Add( occ.Schedule.WeeklyTimeOfDay.Value );
                    }
                    else
                    {
                        summary.StartDateTime = occ.OccurrenceDate;
                    }
                }
                occurrences.Add( summary );
            }

            return occurrences;
        }
    }

    internal class GroupScheduleOccurence
    {
        public int? OccurrenceId { get; set; }
        public int? GroupId { get; set; }
        public int? LocationId { get; set; }
        public int? ScheduleId { get; set; }
        public DateTime OccurrenceDate { get; set; }
        public DateTime? StartDateTime { get; set; }
        public Boolean? DidNotMeet { get; set; }

        public override string ToString()
        {
            return $"OccurrenceId: {OccurrenceId}, GroupId: {GroupId}, LocationId: {LocationId}, ScheduleId: {ScheduleId}, OccurrenceDate: {OccurrenceDate}, StartDateTime: {StartDateTime}, DidNotMeet: {DidNotMeet}";
        }
    }
}
