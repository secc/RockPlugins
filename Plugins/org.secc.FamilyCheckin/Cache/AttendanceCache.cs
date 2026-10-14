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

        // The bulk load of cache misses in All(). Callers that arrive while a load runs share its result.
        private static readonly SingleFlight<LoadResult> _loadFlight = new SingleFlight<LoadResult>();

        // Upper bound on the number of ids per Contains() query when bulk-loading cache misses.
        private const int BulkLoadChunkSize = 500;

        public static List<AttendanceCache> All()
        {
            var allKeys = AllKeys( () => KeyFactory() );

            // Warm path: every key is cached. The list is sized to the key count so it is one allocation that
            // stays under the 85 KB large-object threshold up to ~10,600 keys; an unsized list doubles past it
            // (16,384 slots, 128 KB) at Sunday volumes. CheckinMonitor calls All() once per room row every 10 s.
            var allItems = new List<AttendanceCache>( allKeys.Count );
            foreach ( var key in allKeys )
            {
                var item = RockCache.Get( key ) as AttendanceCache;
                if ( item == null )
                {
                    return LoadAll( allKeys );
                }

                allItems.Add( item );
            }

            return allItems;
        }

        /// <summary>
        /// The cold path of <see cref="All"/>: at least one key is not cached. Collects what is cached, loads
        /// the rest in bulk and returns the items in key order.
        /// </summary>
        private static List<AttendanceCache> LoadAll( IReadOnlyList<string> allKeys )
        {
            var items = new AttendanceCache[allKeys.Count];
            var missing = CollectCachedItems( allKeys, items, Enumerable.Range( 0, allKeys.Count ) );

            // Two passes at most: if we joined another caller's load, pick up what it cached and load the rest.
            // Anything still missing after that is left out of this call and loaded by the next one.
            for ( int attempt = 0; attempt < 2 && missing.Any(); attempt++ )
            {
                var missingKeys = missing.Select( i => allKeys[i] ).ToList();
                bool joined;
                var result = _loadFlight.Run( () => LoadMissing( missingKeys, allKeys.Count ), out joined );

                if ( joined )
                {
                    missing = CollectCachedItems( allKeys, items, missing );
                    continue;
                }

                // Removing publishes a message and may reload the key list, so it runs after the load is released.
                foreach ( var staleKey in result.StaleKeys )
                {
                    Remove( staleKey, () => KeyFactory() );
                }

                var indexByKey = missing.ToDictionary( i => allKeys[i] );
                foreach ( var item in result.Items )
                {
                    int index;
                    if ( indexByKey.TryGetValue( QualifiedKey( item.Id ), out index ) )
                    {
                        items[index] = item;
                    }
                }

                break;
            }

            var allItems = new List<AttendanceCache>( allKeys.Count );
            foreach ( var item in items )
            {
                if ( item != null )
                {
                    allItems.Add( item );
                }
            }

            return allItems;
        }

        /// <summary>
        /// Fills <paramref name="items"/> at the given indexes with the items already in the cache and returns
        /// the indexes that are not cached, without triggering any load.
        /// </summary>
        private static List<int> CollectCachedItems( IReadOnlyList<string> keys, AttendanceCache[] items, IEnumerable<int> indexes )
        {
            var missing = new List<int>();
            foreach ( var index in indexes )
            {
                var item = RockCache.Get( keys[index] ) as AttendanceCache;
                if ( item != null )
                {
                    items[index] = item;
                }
                else
                {
                    missing.Add( index );
                }
            }

            return missing;
        }

        private class LoadResult
        {
            public List<AttendanceCache> Items { get; } = new List<AttendanceCache>();

            // Keys to drop from the key list: no matching attendance, or not a valid attendance key.
            public List<string> StaleKeys { get; } = new List<string>();
        }

        /// <summary>
        /// Loads the attendances for the given cache keys in as few queries as possible and caches each one
        /// unless a newer update was cached while the load ran. Returns the items now in the cache and the
        /// keys that should be removed.
        /// </summary>
        private static LoadResult LoadMissing( List<string> qualifiedKeys, int totalKeyCount )
        {
            var result = new LoadResult();

            var ids = new HashSet<int>();
            foreach ( var qualifiedKey in qualifiedKeys )
            {
                var id = KeyFromQualifiedKey( qualifiedKey ).AsInteger();
                if ( id > 0 )
                {
                    ids.Add( id );
                }

                // A key that is not exactly "AttendanceCache:<id>" can never be cached under its own name.
                // Drop it; the canonical key is added when the item is cached.
                if ( id <= 0 || QualifiedKey( id ) != qualifiedKey )
                {
                    result.StaleKeys.Add( qualifiedKey );
                }
            }

            if ( !ids.Any() )
            {
                return result;
            }

            // Take the stamp and build the occurrence lookup before reading attendance, so a cold occurrence
            // cache does not widen the gap between reading the rows and caching them.
            var readStamp = BeginLoad();
            try
            {
                var occurrences = OccurrenceCache.CreateLookup();
                var foundIds = new HashSet<int>();

                foreach ( var attendance in QueryAttendances( ids, totalKeyCount ) )
                {
                    foundIds.Add( attendance.Id );
                    var item = LoadByAttendance( attendance, occurrences );
                    if ( item != null )
                    {
                        item = AddLoaded( QualifiedKey( item.Id ), item, readStamp, () => KeyFactory() );
                        if ( item != null )
                        {
                            result.Items.Add( item );
                        }
                    }
                }

                result.StaleKeys.AddRange( ids.Where( id => !foundIds.Contains( id ) ).Select( id => QualifiedKey( id ) ) );
            }
            finally
            {
                EndLoad();
            }

            return result;
        }

        /// <summary>
        /// Returns the attendances with the given ids. When most keys are missing (a cold cache), one query
        /// over today's attendance is cheaper than many id lists; otherwise the ids are queried in chunks.
        /// </summary>
        private static List<Attendance> QueryAttendances( HashSet<int> ids, int totalKeyCount )
        {
            RockContext rockContext = new RockContext();
            AttendanceService attendanceService = new AttendanceService( rockContext );
            var query = attendanceService.Queryable( "PersonAlias.Person,Occurrence,AttendanceCode" ).AsNoTracking();

            if ( ids.Count * 2 >= totalKeyCount )
            {
                var today = RockDateTime.Today;
                return query
                    .Where( a => a.CreatedDateTime != null && a.CreatedDateTime >= today )
                    .ToList()
                    .Where( a => ids.Contains( a.Id ) )
                    .ToList();
            }

            var attendances = new List<Attendance>();
            var idList = ids.ToList();
            for ( int i = 0; i < idList.Count; i += BulkLoadChunkSize )
            {
                var chunk = idList.Skip( i ).Take( BulkLoadChunkSize ).ToList();
                attendances.AddRange( query.Where( a => chunk.Contains( a.Id ) ).ToList() );
            }

            return attendances;
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
            return LoadByAttendance( attendance, OccurrenceCache.CreateLookup() );
        }

        // Per-item mapping shared by the single-item (Get/AddOrUpdate) and bulk (All) load paths so they cannot drift.
        private static AttendanceCache LoadByAttendance( Attendance attendance, OccurrenceCache.Lookup occurrences )
        {
            //We don't want to cache attendances created before today.
            if ( !attendance.CreatedDateTime.HasValue || attendance.CreatedDateTime < Rock.RockDateTime.Today )
            {
                Remove( attendance.Id );
                return null;
            }

            Person person;
            if ( attendance.PersonAliasId != null && attendance.PersonAlias?.Person == null )
            {
                PersonAliasService personAliasService = new PersonAliasService( new RockContext() );
                person = personAliasService.GetPersonNoTracking( attendance.PersonAliasId ?? 0 );
            }
            else
            {
                person = attendance.PersonAlias?.Person;
            }


            var attendanceCache = new AttendanceCache
            {
                Id = attendance.Id,
                OccurrenceAccessKey = occurrences.Find( attendance.Occurrence )?.AccessKey ?? "None",
                Code = attendance.AttendanceCode?.Code,
                CreatedDateTime = attendance.CreatedDateTime,
                StartDateTime = attendance.StartDateTime,
                EndDateTime = attendance.EndDateTime,
                PersonId = person?.Id ?? 0,
                PersonName = person?.FullName ?? "Unnamed Person",
                GroupId = attendance.Occurrence.GroupId,
                LocationId = attendance.Occurrence.LocationId,
                ScheduleId = attendance.Occurrence.ScheduleId,
                IsVolunteer = occurrences.IsVolunteerGroup( attendance.Occurrence.GroupId ),
                IsChildren = occurrences.IsChildrenGroup( attendance.Occurrence.GroupId ),
                WithParent = false
            };

            //Sometimes we need to look up the attendance code
            if ( attendanceCache.Code.IsNullOrWhiteSpace() && attendance.AttendanceCodeId.HasValue )
            {
                RockContext rockContext = new RockContext();
                AttendanceCodeService attendanceCodeService = new AttendanceCodeService( rockContext );
                var code = attendanceCodeService.GetNoTracking( attendance.AttendanceCodeId.Value );
                if ( code != null )
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

                // Attendance without a person is cached as PersonId 0 ("Unnamed Person").
                var personId = attendance.PersonAlias?.PersonId ?? 0;
                if ( personId != attendanceCache.PersonId )
                {
                    errors.Add( $"Error: Attendance Cache (Id:{attendance.Id}) Desync: PersonId - DB:{personId} - Cache:{attendanceCache.PersonId}" );
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
