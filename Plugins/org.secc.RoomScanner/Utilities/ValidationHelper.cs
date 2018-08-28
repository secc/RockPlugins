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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using org.secc.RoomScanner.Models;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.RoomScanner.Utilities
{
    public static class ValidationHelper
    {
        private static Dictionary<string, string> settings = GlobalAttributesCache.Value( "RoomScannerSettings" ).AsDictionary();
        private static int allowedGroupId = settings["AllowedGroupId"].AsInteger();
        private static int subroomLocationTypeId = settings["SubroomLocationType"].AsInteger();

        public static Person TestPin( RockContext rockContext, string pin )
        {
            UserLoginService userLoginService = new UserLoginService( rockContext );
            GroupMemberService groupMemberService = new GroupMemberService( rockContext );
            var user = userLoginService.GetByUserName( pin );
            if ( user != null )
            {
                var personId = user.PersonId ?? 0;
                var groupMember = groupMemberService.GetByGroupIdAndPersonId( allowedGroupId, personId ).ToList();
                if ( groupMember.Any() )
                {
                    return user.Person;
                }
            }
            return null;
        }

        public static bool IsSubRoom( Location location )
        {
            return location?.LocationTypeValueId == subroomLocationTypeId;
        }

        public static bool LocationsFull( List<Attendance> attendancesToMove, int locationId, List<int> volunteerGroupIds, RockContext rockContext )
        {
            LocationService locationService = new LocationService( rockContext );
            AttendanceService attendanceService = new AttendanceService( rockContext );
            var location = locationService.Get( locationId );

            if ( location == null )
            {
                return true;
            }

            foreach ( var attendance in attendancesToMove )
            {
                var attendances = attendanceService.Queryable()
                    .Where( a => a.LocationId == locationId
                         && a.ScheduleId == attendance.ScheduleId
                         && a.EndDateTime == null
                         && a.StartDateTime >= Rock.RockDateTime.Today
                        ).ToList();
                var threshold = location.FirmRoomThreshold ?? 0;

                //check to see if the room is going over
                if ( attendances.Count() >= threshold )
                {
                    return true;
                }

                //Now check if checking in a kid there is "kid room"
                if ( !attendance.Group.GetAttributeValue( "IsVolunteer" ).AsBoolean() )
                {
                    threshold = Math.Min( location.SoftRoomThreshold ?? 0, threshold ); //lowest threshold
                    var kidAttendances = attendances.Where( a => !volunteerGroupIds.Contains( a.GroupId ?? 0 ) ); //remove volunters
                    if ( kidAttendances.Count() >= threshold )
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static Attendance GetAttendeeAttendance( Request req, RockContext rockContext )
        {
            Attendance attendeeAttendance = null;
            AttendanceService attendanceService = new AttendanceService( rockContext );
            var attendanceGuidGuid = req.AttendanceGuid.AsGuidOrNull();
            if ( attendanceGuidGuid != null )
            {
                attendeeAttendance = attendanceService.Get( attendanceGuidGuid ?? new Guid() );
            }
            else
            {
                UserLoginService userLoginService = new UserLoginService( rockContext );
                var user = userLoginService.GetByUserName( req.AttendanceGuid );
                if ( user != null )
                {
                    attendeeAttendance = attendanceService.Queryable().Where( a => a.PersonAlias.PersonId == user.PersonId
                                                        && Rock.RockDateTime.Today == attendeeAttendance.StartDateTime.Date )
                        .FirstOrDefault();
                }
            }
            return attendeeAttendance;
        }

        public static IQueryable<Attendance> GetAttendancesForAttendee( RockContext rockContext, Attendance attendeeAttendance )
        {
            AttendanceService attendanceService = new AttendanceService( rockContext );
            var today = Rock.RockDateTime.Today;
            var tomorrow = today.AddDays( 1 );
            return attendanceService.Queryable()
               .Where( a => a.PersonAliasId == attendeeAttendance.PersonAliasId
                        && a.StartDateTime >= today
                        && a.StartDateTime < tomorrow
                        && a.EndDateTime == null );
        }

        public static IQueryable<Attendance> GetActiveAttendances( RockContext rockContext, Attendance attendeeAttendance, Location exclusionLocation )
        {
            return GetAttendancesForAttendee( rockContext, attendeeAttendance )
                .Where( a => a.DidAttend == true
                && a.EndDateTime == null
                && a.LocationId != exclusionLocation.Id
                && a.LocationId != exclusionLocation.ParentLocationId );
        }
    }
}
