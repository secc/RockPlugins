// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License should be included with this file.
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
using org.secc.FamilyCheckin.Cache;
using org.secc.RoomScanner.Models;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.RoomScanner.Utilities
{
    public static class ValidationHelper
    {
        private const string SettingsAttributeKey = "RoomScannerSettings";

        // Settings are read per call (GlobalAttributesCache is already cached) rather than in static
        // initializers, so a missing or invalid setting fails only the current request instead of
        // poisoning the type with a TypeInitializationException until the app pool recycles.
        // A key that is present but blank means that feature is not configured (null), the same
        // outcome as the old "".AsInteger() == 0: no override group and no subroom location type.
        private static int? allowedGroupId => GetSettingId( "AllowedGroupId" );
        private static int? subroomLocationTypeId => GetSettingId( "SubroomLocationType" );

        private static int? GetSettingId( string key )
        {
            Dictionary<string, string> settings = GlobalAttributesCache.Value( SettingsAttributeKey ).AsDictionary();
            string value;
            if ( !settings.TryGetValue( key, out value ) )
            {
                throw new InvalidOperationException( string.Format( "RoomScanner is not configured: missing {0}[{1}]", SettingsAttributeKey, key ) );
            }

            if ( string.IsNullOrWhiteSpace( value ) )
            {
                return null;
            }

            var id = value.AsIntegerOrNull();
            if ( !id.HasValue )
            {
                throw new InvalidOperationException( string.Format( "RoomScanner is not configured: {0}[{1}] is not a valid integer", SettingsAttributeKey, key ) );
            }

            return id;
        }

        public static Person TestPin( RockContext rockContext, string pin )
        {
            // Read the setting before looking up the user so a configuration error fails every PIN
            // the same way and the response does not reveal whether the username exists.
            var groupId = allowedGroupId;
            if ( !groupId.HasValue )
            {
                return null;
            }

            UserLoginService userLoginService = new UserLoginService( rockContext );
            GroupMemberService groupMemberService = new GroupMemberService( rockContext );
            var user = userLoginService.GetByUserName( pin );
            if ( user != null )
            {
                var personId = user.PersonId ?? 0;
                var groupMember = groupMemberService.GetByGroupIdAndPersonId( groupId.Value, personId ).ToList();
                if ( groupMember.Any() )
                {
                    return user.Person;
                }
            }
            return null;
        }

        public static bool IsSubRoom( Location location )
        {
            var typeId = subroomLocationTypeId;
            return typeId.HasValue && location?.LocationTypeValueId == typeId.Value;
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

            // A null threshold means the room has no limit (same as core check-in manager).
            // Capacity is checked against the current occupancy plus the batch being moved,
            // and rows from the batch that are already in the room are not counted twice.
            var attendanceIdsToMove = attendancesToMove.Select( a => a.Id ).ToList();
            foreach ( var attendance in attendancesToMove )
            {
                var attendances = attendanceService.Queryable()
                    .Where( a => a.Occurrence.LocationId == locationId
                         && a.Occurrence.ScheduleId == attendance.Occurrence.ScheduleId
                         && a.EndDateTime == null
                         && a.StartDateTime >= Rock.RockDateTime.Today
                        ).ToList()
                    .Where( a => !attendanceIdsToMove.Contains( a.Id ) )
                    .ToList();
                var moving = attendancesToMove.Where( a => a.Occurrence.ScheduleId == attendance.Occurrence.ScheduleId ).ToList();

                //check to see if the room is going over
                if ( location.FirmRoomThreshold.HasValue
                    && attendances.Count + moving.Count > location.FirmRoomThreshold.Value )
                {
                    return true;
                }

                //Now check if checking in a kid there is "kid room"
                //(the firm threshold was already applied above, so only the soft threshold is left to check)
                if ( location.SoftRoomThreshold.HasValue
                    && !volunteerGroupIds.Contains( attendance.Occurrence.GroupId ?? 0 ) )
                {
                    var kidAttendances = attendances.Where( a => !volunteerGroupIds.Contains( a.Occurrence.GroupId ?? 0 ) ); //remove volunters
                    var kidsMoving = moving.Where( a => !volunteerGroupIds.Contains( a.Occurrence.GroupId ?? 0 ) );
                    if ( kidAttendances.Count() + kidsMoving.Count() > location.SoftRoomThreshold.Value )
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
                    var today = Rock.RockDateTime.Today;
                    var tomorrow = today.AddDays( 1 );
                    attendeeAttendance = attendanceService.Queryable().Where( a => a.PersonAlias.PersonId == user.PersonId
                                                        && a.StartDateTime >= today
                                                        && a.StartDateTime < tomorrow )
                        // Prefer an open attendance so a closed earlier row (checked out or moved from)
                        // is not used as the attendee's current record.
                        .OrderBy( a => a.EndDateTime == null ? 0 : 1 )
                        .ThenByDescending( a => a.StartDateTime )
                        .ThenByDescending( a => a.Id )
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
                && a.Occurrence.LocationId != exclusionLocation.Id
                && a.Occurrence.LocationId != exclusionLocation.ParentLocationId );
        }

        internal static AttendanceOccurrence GetOccurrenceForLocation( RockContext rockContext, Location location )
        {
            if ( IsSubRoom( location ) )
            {
                location = location.ParentLocation;
            }

            AttendanceOccurrenceService attendanceOccurrenceService = new AttendanceOccurrenceService( rockContext );
            var occurrences = attendanceOccurrenceService.Queryable()
                .Where( o => o.OccurrenceDate == RockDateTime.Now.Date && o.LocationId == location.Id ).ToList();
            var volunteerOccurrences = occurrences.Where( o => OccurrenceCache.GetVolunteerOccurrences().Select( oc => oc.GroupId ).Contains( o.GroupId ?? 0 ) ).FirstOrDefault();
            if ( volunteerOccurrences != null )
            {
                return volunteerOccurrences;
            }
            return null;
        }
    }
}
