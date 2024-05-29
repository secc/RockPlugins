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

            var finalResult = from attendee in attendees
                              join groupMember in groupMembers
                              on attendee.PersonId equals groupMember.PersonId
                              select new
                              {
                                  AttendanceId = attendee.Id,
                                  GroupMemberId = groupMember.Id,
                                  attendee.OccurrenceId
                              };

            var resultList = finalResult.ToList();

            return Ok( resultList );
        }

        /// <summary>
        /// Post an attendance record with a groupId, occurrenceDate, and groupMemberId
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [System.Web.Http.Route( "api/GroupApp/Attendance/" )]
        public IHttpActionResult PostGroupAttendance( int groupId, DateTime occurrenceDate, int groupMemberId, int? scheduleId = null, int? locationId = null)
        {
            var currentUser = UserLoginService.GetCurrentUser();
            if ( currentUser == null )
                return StatusCode( HttpStatusCode.Unauthorized );

            var group = new GroupService( _context ).Get( groupId );
            if ( group == null )
                return NotFound();

            if ( !group.IsAuthorized( Rock.Security.Authorization.EDIT, currentUser.Person ) || !group.IsAuthorized( Rock.Security.Authorization.MANAGE_MEMBERS, currentUser.Person ))
                return StatusCode( HttpStatusCode.Forbidden );

            scheduleId = scheduleId.HasValue ? scheduleId.Value : group.ScheduleId;
            locationId = locationId.HasValue ? locationId.Value : group.GroupLocations?.FirstOrDefault()?.Id;

            var groupMember = new GroupMemberService( _context ).Get( groupMemberId );

            var attendanceService = new AttendanceService( _context );
            var personAliasService = new PersonAliasService( _context );

            var attendanceData = attendanceService
                        .Queryable( "PersonAlias" )
                        .Where( a => a.Occurrence.GroupId == groupId && a.Occurrence.LocationId == locationId && a.Occurrence.ScheduleId == scheduleId && a.Occurrence.OccurrenceDate == occurrenceDate );

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
    }
}
