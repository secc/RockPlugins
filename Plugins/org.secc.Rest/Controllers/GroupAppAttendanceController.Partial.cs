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
    }
}
