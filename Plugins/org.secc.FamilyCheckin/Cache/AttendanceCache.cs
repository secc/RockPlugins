using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Rock;
using Rock.Constants;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.FamilyCheckin.Cache
{
    [DataContract]
    [Serializable]
    public class AttendanceCache : ItemCache<AttendanceCache>
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

        public bool CanClose
        {
            get
            {
                if (AttendanceState != AttendanceState.MobileReserve )
                {
                    return true;
                }

                //You can't close a mobile check-in that we've promised the user would be active for some time
                var mobileCheckinRecord = MobileCheckinRecordCache.GetByAttendanceId( Id );
                if (mobileCheckinRecord == null )
                {
                    return true;
                }
                return !mobileCheckinRecord.ReservedUntilDateTime.HasValue || mobileCheckinRecord.ReservedUntilDateTime < Rock.RockDateTime.Now;

            }
        }

        public static List<AttendanceCache> All()
        {
            var allKeys = RockCacheManager<List<string>>.Instance.Cache.Get( AllKey, _AllRegion );
            if ( allKeys == null )
            {
                allKeys = GetOrAddKeys( () => AllKeys() );
                if ( allKeys == null )
                {
                    return new List<AttendanceCache>();
                }
            }

            var allItems = new List<AttendanceCache>();

            foreach ( var key in allKeys.ToList() )
            {
                var value = Get( key );
                if ( value != null )
                {
                    allItems.Add( value );
                }
            }

            return allItems.Where( a => a.CreatedDateTime.HasValue && a.CreatedDateTime.Value >= Rock.RockDateTime.Today ).ToList();

        }

        internal static List<AttendanceCache> GetByOccurrenceKey( string accessKey )
        {
            return All().Where( a => a.OccurrenceAccessKey == accessKey ).ToList();
        }

        public static AttendanceCache Get( int id )
        {
            return GetOrAddExisting( id.ToString(), () => LoadById( id ), TimeSpan.FromHours( 6 ) );
        }

        private static AttendanceCache Get( string key )
        {
            return GetOrAddExisting( key, () => LoadById( key.AsInteger() ), TimeSpan.FromHours( 6 ) );
        }

        private static AttendanceCache LoadById( int id )
        {
            RockContext rockContext = new RockContext();
            AttendanceService attendanceService = new AttendanceService( rockContext );
            var attendance = attendanceService.Get( id );
            return LoadByAttendance( attendance );
        }

        private static AttendanceCache LoadByAttendance( Attendance attendance )
        {
            //We don't want to cache attendances created before today.
            if ( !attendance.CreatedDateTime.HasValue || attendance.CreatedDateTime < Rock.RockDateTime.Today )
            {
                return null;
            }

            var attendanceCache = new AttendanceCache
            {
                Id = attendance.Id,
                OccurrenceAccessKey = OccurrenceCache.GetByOccurrence( attendance.Occurrence )?.AccessKey ?? "None",
                Code = attendance.AttendanceCode.Code,
                CreatedDateTime = attendance.CreatedDateTime,
                StartDateTime = attendance.StartDateTime,
                EndDateTime = attendance.EndDateTime,
                PersonId = attendance.PersonAlias.PersonId,
                PersonName = attendance.PersonAlias.Person.FullName,
                GroupId = attendance.Occurrence.GroupId,
                LocationId = attendance.Occurrence.LocationId,
                ScheduleId = attendance.Occurrence.ScheduleId,
                IsVolunteer = OccurrenceCache.GetVolunteerOccurrences().Select( o => o.GroupId ).Contains( attendance.Occurrence.GroupId ?? 0 )
            };

            if ( attendance.QualifierValueId.HasValue )
            {
                attendanceCache.AttendanceState = AttendanceState.MobileReserve;
            }
            else if ( !attendance.EndDateTime.HasValue )
            {
                if ( attendance.DidAttend == true )
                {
                    attendanceCache.AttendanceState = AttendanceState.InRoom;
                }
                else
                {
                    attendanceCache.AttendanceState = AttendanceState.EnRoute;
                }
            }
            else
            {
                attendanceCache.AttendanceState = AttendanceState.CheckedOut;
            }

            return attendanceCache;
        }

        private static List<string> AllKeys()
        {
            RockContext rockContext = new RockContext();
            AttendanceService attendanceService = new AttendanceService( rockContext );
            var keys = attendanceService.Queryable().AsNoTracking()
                .Where( a => a.StartDateTime > RockDateTime.Today )
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
            UpdateCacheItem( attendance.Id.ToString(), attendanceCache, TimeSpan.FromHours( 6 ) );
        }
    }

    public enum AttendanceState
    {
        MobileReserve = 0,
        EnRoute = 1,
        InRoom = 2,
        CheckedOut = 3
    }
}
