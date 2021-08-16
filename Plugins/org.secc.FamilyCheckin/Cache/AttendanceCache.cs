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
using System.Data.Entity;
using System.Linq;
using System.Runtime.Serialization;
using org.secc.FamilyCheckin.Utilities;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.FamilyCheckin.Cache
{
    [DataContract]
    [Serializable]
    public class AttendanceCache : CheckinCache<AttendanceCache>
    {
        private AttendanceCache()
        {
        }

        public override string ToString()
        {
            var occurrence = OccurrenceCache.Get( OccurrenceAccessKey );
            return $"{PersonName} {AttendanceState} {occurrence?.GroupName ?? "Unknown Group"} in {occurrence?.LocationName ?? "Unknown Location"} at  {occurrence?.ScheduleName ?? "Unknown Schedule"}";
        }

        private const string _AllRegion = "AllItems";

        private static readonly string KeyPrefix = typeof( AttendanceCache ).Name;
        private static string AllKey => $"{KeyPrefix}:{AllString}";

        [DataMember]
        public string OccurrenceAccessKey { get; set; }

        [DataMember]
        public int Id { get; set; }

        [DataMember]
        public int PersonId { get; set; }

        [DataMember]
        public string PersonName { get; set; }

        [DataMember]
        public int? GroupId { get; set; }

        [DataMember]
        public int? LocationId { get; set; }

        [DataMember]
        public int? ScheduleId { get; set; }

        [DataMember]
        public string Code { get; set; }

        [DataMember]
        public DateTime? CreatedDateTime { get; set; }

        [DataMember]
        public DateTime? StartDateTime { get; set; }

        [DataMember]
        public DateTime? EndDateTime { get; set; }

        [DataMember]
        public AttendanceState AttendanceState { get; set; }

        [DataMember]
        public bool IsVolunteer { get; set; }

        [DataMember]
        public bool IsChildren { get; set; }

        [DataMember]
        public bool WithParent { get; set; }


        public bool CanClose
        {
            get
            {
                if ( AttendanceState != AttendanceState.MobileReserve )
                {
                    return true;
                }

                //You can't close a mobile check-in that we've promised the user would be active for some time
                var mobileCheckinRecord = MobileCheckinRecordCache.GetByAttendanceId( Id );
                if ( mobileCheckinRecord == null )
                {
                    return true;
                }
                return !mobileCheckinRecord.ReservedUntilDateTime.HasValue || mobileCheckinRecord.ReservedUntilDateTime < Rock.RockDateTime.Now;

            }
        }

        public static List<AttendanceCache> All()
        {
            var allKeys = AllKeys( () => KeyFactory() );

            var allItems = new List<AttendanceCache>();

            foreach ( var key in allKeys.ToList() )
            {
                var value = GetFromQualifiedKey( key );
                if ( value != null )
                {
                    allItems.Add( value );
                }
            }

            return allItems;
        }

        public static AttendanceCache Get( int id )
        {
            return Get( id.ToString() );
        }

        public static AttendanceCache GetFromQualifiedKey( string qualifiedKey )
        {
            return Get( qualifiedKey, () => ItemFactory( KeyFromQualifiedKey( qualifiedKey ) ), () => KeyFactory() );
        }

        public static AttendanceCache Get( string key )
        {
            return Get( QualifiedKey( key ), () => ItemFactory( key ), () => KeyFactory() );
        }

        internal static List<AttendanceCache> GetByOccurrenceKey( string accessKey )
        {
            return All().Where( a => a.OccurrenceAccessKey == accessKey ).ToList();
        }

        public static void SetWithParent( int personId )
        {
            var attendanceIds = All()
                .Where( a => a.PersonId == personId && a.AttendanceState == AttendanceState.InRoom )
                .Select( a => a.Id )
                .ToList();

            RockContext rockContext = new RockContext();
            AttendanceService attendanceService = new AttendanceService( rockContext );
            var attendances = attendanceService.Queryable()
                .Where( a => attendanceIds.Contains( a.Id ) )
                .ToList();
            attendances.ForEach( a => a.QualifierValueId = DefinedValueCache.GetId( Constants.DEFINED_VALUE_ATTENDANCE_STATUS_WITH_PARENT.AsGuid() ) );
            rockContext.SaveChanges();

            attendances.ForEach( a => AddOrUpdate( a ) );
        }

        public static void RemoveWithParent( int personId )
        {
            var attendanceIds = All()
                .Where( a => a.PersonId == personId && a.WithParent )
                .Select( a => a.Id )
                .ToList();

            RockContext rockContext = new RockContext();
            AttendanceService attendanceService = new AttendanceService( rockContext );
            var attendances = attendanceService.Queryable()
                .Where( a => attendanceIds.Contains( a.Id ) )
                .ToList();
            attendances.ForEach( a => a.QualifierValueId = null );
            rockContext.SaveChanges();

            attendances.ForEach( a => AddOrUpdate( a ) );
        }

        private static AttendanceCache ItemFactory( string key )
        {
            return LoadById( key.AsInteger() );
        }

        private static AttendanceCache LoadById( int id )
        {
            RockContext rockContext = new RockContext();
            AttendanceService attendanceService = new AttendanceService( rockContext );
            var attendance = attendanceService.Get( id );

            if ( attendance == null )
            {
                Remove( id );
                return null;
            }

            return LoadByAttendance( attendance );
        }

        public static void Remove( int id )
        {
            Remove( QualifiedKey( id ), () => KeyFactory() );
        }


        private static AttendanceCache LoadByAttendance( Attendance attendance )
        {
            //We don't want to cache attendances created before today.
            if ( !attendance.CreatedDateTime.HasValue || attendance.CreatedDateTime < Rock.RockDateTime.Today )
            {
                Remove( attendance.Id );
                return null;
            }

            var attendanceCache = new AttendanceCache
            {
                Id = attendance.Id,
                OccurrenceAccessKey = OccurrenceCache.GetByOccurrence( attendance.Occurrence )?.AccessKey ?? "None",
                Code = attendance.AttendanceCode?.Code,
                CreatedDateTime = attendance.CreatedDateTime,
                StartDateTime = attendance.StartDateTime,
                EndDateTime = attendance.EndDateTime,
                PersonId = attendance.PersonAlias.PersonId,
                PersonName = attendance.PersonAlias.Person.FullName,
                GroupId = attendance.Occurrence.GroupId,
                LocationId = attendance.Occurrence.LocationId,
                ScheduleId = attendance.Occurrence.ScheduleId,
                IsVolunteer = OccurrenceCache.GetVolunteerOccurrences().Select( o => o.GroupId ).Contains( attendance.Occurrence.GroupId ?? 0 ),
                IsChildren = OccurrenceCache.GetChildrenOccurrences().Select( o => o.GroupId ).Contains( attendance.Occurrence.GroupId ?? 0 ),
                WithParent = false
            };

            //Sometimes we need to look up the attendance code
            if ( attendanceCache.Code.IsNullOrWhiteSpace() && attendance.AttendanceCodeId.HasValue )
            {
                RockContext rockContext = new RockContext();
                AttendanceCodeService attendanceCodeService = new AttendanceCodeService( rockContext );
                var code = attendanceCodeService.GetNoTracking( attendance.AttendanceCodeId.Value );
                if (code != null )
                {
                    attendanceCache.Code = code.Code;
                }
            }


            if ( attendance.EndDateTime.HasValue ) //End date means checked out
            {
                attendanceCache.AttendanceState = AttendanceState.CheckedOut;
            }
            else //has not been checked out yet
            {
                if ( attendance.DidAttend == false && attendance.QualifierValueId.HasValue )
                {
                    attendanceCache.AttendanceState = AttendanceState.MobileReserve;
                }
                else if ( attendance.DidAttend == true )
                {
                    attendanceCache.AttendanceState = AttendanceState.InRoom;
                    if ( attendance.QualifierValueId == DefinedValueCache.GetId( Constants.DEFINED_VALUE_ATTENDANCE_STATUS_WITH_PARENT.AsGuid() ) )
                    {
                        attendanceCache.WithParent = true;
                    }
                }
                else
                {
                    attendanceCache.AttendanceState = AttendanceState.EnRoute;
                }
            }


            return attendanceCache;
        }

        public static bool IsWithParent( int personId )
        {
            return All().Where( a => a.PersonId == personId && a.WithParent ).Any();
        }

        private static List<string> KeyFactory()
        {
            RockContext rockContext = new RockContext();
            AttendanceService attendanceService = new AttendanceService( rockContext );
            var keys = attendanceService.Queryable().AsNoTracking()
                .Where( a => a.CreatedDateTime != null && a.CreatedDateTime >= RockDateTime.Today )
                .Select( a => a.Id.ToString() )
                .ToList();

            return keys;
        }

        public static List<AttendanceCache> GetByLocationAndSchedule( Location location, Schedule schedule, bool activeOnly = true )
        {
            return GetByLocationIdAndScheduleId( location.Id, schedule.Id, activeOnly );
        }

        public static List<AttendanceCache> GetByLocationIdAndScheduleId( int locationId, int scheduleId, bool activeOnly = true )
        {
            var attendances = All()
                .Where( a => a.LocationId == locationId && a.ScheduleId == scheduleId );

            if ( activeOnly )
            {
                attendances = attendances.Where( a => a.AttendanceState != AttendanceState.CheckedOut );
            }

            return attendances.ToList();
        }

        public static List<AttendanceCache> GetByLocationId( int locationId, bool activeOnly = true )
        {
            var attendances = All().Where( a => a.LocationId == locationId );

            if ( activeOnly )
            {
                attendances = attendances.Where( a => a.AttendanceState != AttendanceState.CheckedOut );
            }

            return attendances.ToList();
        }

        public static void AddOrUpdate( Attendance attendance )
        {
            var attendanceCache = LoadByAttendance( attendance );
            if ( attendanceCache != null )
            {
                AddOrUpdate( QualifiedKey( attendanceCache.Id ), attendanceCache, () => KeyFactory() );
            }
        }

        public static void Clear()
        {
            Clear( () => KeyFactory() );
        }

        #region Verification
        public static void Verify( ref List<string> errors )
        {
            RockContext rockContext = new RockContext();
            AttendanceService attendanceService = new AttendanceService( rockContext );
            var attendances = attendanceService.Queryable( "Occurrence" )
                .Where( a => a.CreatedDateTime.HasValue && a.CreatedDateTime >= Rock.RockDateTime.Today )
                .ToList();
            var attendanceCaches = All();

            if ( attendances.Count != attendanceCaches.Count )
            {
                var recordIds = attendances.Select( r => r.Id );
                var cacheIds = attendanceCaches.Select( r => r.Id );
                var missingCacheIds = recordIds.Except( cacheIds ).ToList();
                var extraCacheIds = cacheIds.Except( recordIds ).ToList();
                foreach ( var id in missingCacheIds )
                {
                    errors.Add( $"Warning: Attendance Cache missing from All(): {id}" );
                }

                foreach ( var id in extraCacheIds )
                {
                    errors.Add( $"Error: Extraneous Attendance Cache: {id}" );
                }
            }

            foreach ( var attendance in attendances )
            {
                var attendanceCache = AttendanceCache.Get( attendance.Id );
                if ( attendanceCache == null )
                {
                    errors.Add( "Error: Attendance Cache missing for Attendance Id: " + attendance.Id.ToString() );
                    continue;
                }

                AttendanceState attendanceState = AttendanceState.CheckedOut;
                bool withParent = false;

                if ( attendance.EndDateTime.HasValue ) //End date means checked out
                {
                    attendanceState = AttendanceState.CheckedOut;
                }
                else //has not been checked out yet
                {
                    if ( attendance.DidAttend == false && attendance.QualifierValueId.HasValue )
                    {
                        attendanceState = AttendanceState.MobileReserve;
                    }
                    else if ( attendance.DidAttend == true )
                    {
                        attendanceState = AttendanceState.InRoom;
                        if ( attendance.QualifierValueId == DefinedValueCache.GetId( Constants.DEFINED_VALUE_ATTENDANCE_STATUS_WITH_PARENT.AsGuid() ) )
                        {
                            withParent = true;
                        }
                    }
                    else
                    {
                        attendanceState = AttendanceState.EnRoute;
                    }
                }

                if ( attendance.Occurrence.GroupId != attendanceCache.GroupId )
                {
                    errors.Add( $"Error: Attendance Cache (Id:{attendance.Id}) Desync: GroupId - DB:{attendance.Occurrence.GroupId} - Cache:{attendanceCache.GroupId}" );
                }

                if ( attendance.PersonAlias.PersonId != attendanceCache.PersonId )
                {
                    errors.Add( $"Error: Attendance Cache (Id:{attendance.Id}) Desync: PersonId - DB:{attendance.PersonAlias.PersonId} - Cache:{attendanceCache.PersonId}" );
                }

                if ( attendance.Occurrence.LocationId != attendanceCache.LocationId )
                {
                    errors.Add( $"Error: Attendance Cache (Id:{attendance.Id}) Desync: LocationId - DB:{attendance.Occurrence.LocationId} - Cache:{attendanceCache.LocationId}" );
                }

                if ( attendance.Occurrence.ScheduleId != attendanceCache.ScheduleId )
                {
                    errors.Add( $"Error: Attendance Cache (Id:{attendance.Id}) Desync: ScheduleId - DB:{attendance.Occurrence.ScheduleId} - Cache:{attendanceCache.ScheduleId}" );
                }

                if ( attendanceState != attendanceCache.AttendanceState )
                {
                    errors.Add( $"Error: Attendance Cache (Id:{attendance.Id}) Desync: attendanceState - DB:{attendanceState} - Cache:{attendanceCache.AttendanceState}" );
                }

                if ( withParent != attendanceCache.WithParent )
                {
                    errors.Add( $"Error: Attendance Cache (Id:{attendance.Id}) Desync: WithParent - DB:{withParent} - Cache:{attendanceCache.WithParent}" );
                }
            }
        }
        #endregion
    }

    public enum AttendanceState
    {
        MobileReserve = 0,
        EnRoute = 1,
        InRoom = 2,
        CheckedOut = 3
    }
}
