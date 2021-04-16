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
    public class OccurrenceCache : ItemCache<OccurrenceCache>
    {
        private OccurrenceCache()
        {
        }

        public override string ToString()
        {
            return $"{( IsActive ? "Active" : "Inactive" ) }{( IsVolunteer ? "Volunteer" : "" )}: {GroupName} in {LocationName} at {ScheduleName}";
        }

        private const string _AllRegion = "AllItems";

        private static readonly string KeyPrefix = typeof( OccurrenceCache ).Name;
        private static string AllKey => $"{KeyPrefix}:{AllString}";

        [DataMember]
        //I'm using an access key because we may need to load this data before the actual occurrence is made
        public string AccessKey { get; set; }

        [DataMember]
        public int GroupId { get; set; }

        [DataMember]
        public string GroupName { get; set; }

        [DataMember]
        public int GroupOrder { get; set; }

        [DataMember]
        public int GroupLocationId { get; set; }

        [DataMember]
        public int GroupLocationOrder { get; set; }

        [DataMember]
        public int LocationId { get; set; }

        [DataMember]
        public string LocationName { get; set; }

        [DataMember]
        public int ScheduleId { get; set; }

        [DataMember]
        public string ScheduleName { get; set; }

        [DataMember]
        public TimeSpan ScheduleStartTime { get; set; }

        [DataMember]
        public int GroupTypeId { get; set; }

        [DataMember]
        public bool IsActive { get; set; }

        [DataMember]
        public int? RoomRatio { get; set; }

        [DataMember]
        public bool IsVolunteer { get; set; }

        [DataMember]
        public bool IsChildren { get; set; }

        [DataMember]
        public int? SoftRoomThreshold { get; set; }

        [DataMember]
        public int? FirmRoomThreshold { get; set; }

        public List<AttendanceCache> Attendances { get => AttendanceCache.GetByOccurrenceKey( this.AccessKey ); }

        public bool IsFull
        {
            get
            {
                if ( !SoftRoomThreshold.HasValue || !FirmRoomThreshold.HasValue )
                {
                    return true; //No data --free pass.
                }

                var capacity = FirmRoomThreshold.Value;
                var attendanceCount = 0;

                if ( !IsVolunteer )
                {
                    capacity = Math.Min( FirmRoomThreshold.Value, SoftRoomThreshold.Value );
                }

                if ( IsVolunteer )
                {
                    attendanceCount = AttendanceCache
                        .All()
                        .Where( a => a.LocationId == LocationId
                                  && a.ScheduleId == ScheduleId
                                  && a.AttendanceState != AttendanceState.CheckedOut )
                        .Count();
                }
                else
                {
                    attendanceCount = AttendanceCache
                      .All()
                      .Where( a => a.LocationId == LocationId
                                && a.ScheduleId == ScheduleId
                                && !a.IsVolunteer
                                && a.AttendanceState != AttendanceState.CheckedOut )
                      .Count();
                }

                //If we are within 2 of full, cache is too slow to be reliablly accurate
                //.. to the database!!
                if ( attendanceCount < capacity && attendanceCount >= capacity - 2 )
                {
                    var attendanceService = new AttendanceService( new RockContext() ).Queryable().AsNoTracking();

                    if ( IsVolunteer )
                    {
                        attendanceCount = attendanceService.Where( a =>
                                      a.EndDateTime == null
                                      && a.Occurrence.ScheduleId == ScheduleId
                                      && a.Occurrence.LocationId == LocationId
                                      && a.StartDateTime >= Rock.RockDateTime.Today )
                            .Count();
                    }
                    else
                    {
                        var childGroupIds = All()
                            .Where( o => o.LocationId == LocationId
                                         && o.ScheduleId == ScheduleId
                                         && !o.IsVolunteer )
                            .Select( o => o.GroupId )
                            .ToList();
                        attendanceCount = attendanceService.Where( a =>
                                      a.EndDateTime == null
                                      && a.Occurrence.ScheduleId == ScheduleId
                                      && a.Occurrence.LocationId == LocationId
                                      && childGroupIds.Contains( a.Occurrence.GroupId ?? 0 )
                                      && a.StartDateTime >= Rock.RockDateTime.Today )
                            .Count();

                    }
                }

                if ( attendanceCount >= capacity )
                {
                    return true;
                }

                return false;
            }
        }


        public static List<OccurrenceCache> All()
        {
            var allKeys = RockCacheManager<List<string>>.Instance.Cache.Get( AllKey, _AllRegion );
            if ( allKeys == null )
            {
                allKeys = GetOrAddKeys( () => AllKeys() );
                if ( allKeys == null )
                {
                    return new List<OccurrenceCache>();
                }
            }

            var allItems = new List<OccurrenceCache>();

            foreach ( var key in allKeys.ToList() )
            {
                var value = Get( key );
                if ( value != null )
                {
                    allItems.Add( value );
                }
            }

            return allItems;

        }

        public static OccurrenceCache Get( int groupId, int locationId, int scheduleId )
        {
            return All().Where( o => o.GroupId == groupId && o.LocationId == locationId && o.ScheduleId == scheduleId ).FirstOrDefault();
        }

        public static OccurrenceCache Get( string accessKey )
        {
            return GetOrAddExisting( accessKey, () => LoadByAccessKey( accessKey ) );
        }

        private static OccurrenceCache LoadByAccessKey( string accessKey )
        {
            var keys = accessKey.SplitDelimitedValues();

            if ( keys.Length < 2 )
            {
                return null;
            }

            var groupLocationId = keys[0].AsInteger();
            var scheduleId = keys[1].AsInteger();
            RockContext rockContext = new RockContext();
            GroupLocationService groupLocationService = new GroupLocationService( rockContext );
            ScheduleService scheduleService = new ScheduleService( rockContext );

            var groupLocation = groupLocationService.Queryable( "Group,Location" ).Where( gl => gl.Id == groupLocationId ).FirstOrDefault();
            var schedule = scheduleService.Get( scheduleId );

            if ( groupLocation == null || schedule == null )
            {
                return null;
            }

            var volAttribute = AttributeCache.Get( Constants.VOLUNTEER_ATTRIBUTE_GUID.AsGuid() );
            AttributeValueService attributeValueService = new AttributeValueService( new RockContext() );
            var volunteerGroupIds = attributeValueService.Queryable().AsNoTracking()
                .Where( av => av.AttributeId == volAttribute.Id && av.Value == "True" ).Select( av => av.EntityId.Value ).ToList();
            var childrenGroupIds = attributeValueService.Queryable().AsNoTracking()
                .Where( av => av.AttributeId == volAttribute.Id && av.Value == "False" ).Select( av => av.EntityId.Value ).ToList();


            OccurrenceCache occurrenceCache = new OccurrenceCache
            {
                AccessKey = accessKey,
                GroupTypeId = groupLocation.Group.GroupTypeId,
                GroupId = groupLocation.GroupId,
                GroupName = groupLocation.Group.Name,
                GroupOrder = groupLocation.Group.Order,
                GroupLocationId = groupLocation.Id,
                GroupLocationOrder = groupLocation.Order,
                LocationId = groupLocation.LocationId,
                LocationName = groupLocation.Location.Name,
                ScheduleId = scheduleId,
                ScheduleName = schedule.Name,
                ScheduleStartTime = schedule.StartTimeOfDay,
                SoftRoomThreshold = groupLocation.Location.SoftRoomThreshold,
                FirmRoomThreshold = groupLocation.Location.FirmRoomThreshold,
                IsActive = groupLocation.Schedules.Select( s => s.Id ).Contains( scheduleId ),
                IsVolunteer = volunteerGroupIds.Contains( groupLocation.GroupId ),
                IsChildren = childrenGroupIds.Contains( groupLocation.GroupId ),
            };

            var location = groupLocation.Location;
            location.LoadAttributes();
            occurrenceCache.RoomRatio = location.GetAttributeValue( Constants.LOCATION_ATTRIBUTE_ROOM_RATIO ).AsIntegerOrNull();

            return occurrenceCache;
        }

        public static List<OccurrenceCache> GetVolunteerOccurrences()
        {
            return All().Where( o => o.IsVolunteer ).ToList();
        }

        public static List<OccurrenceCache> GetNonVolunteerOccurrences()
        {
            return All().Where( o => !o.IsVolunteer ).ToList();
        }

        public static List<OccurrenceCache> GetChildrenOccurrences()
        {
            return All().Where( o => o.IsChildren ).ToList();
        }

        public static List<OccurrenceCache> GetNonChildrenOccurrences()
        {
            return All().Where( o => !o.IsChildren ).ToList();
        }

        public static List<OccurrenceCache> GetUnlabledOccurrences()
        {
            return All().Where( o => !o.IsChildren && !o.IsVolunteer ).ToList();
        }

        [Obsolete( "Use GetChildrenOccurrences" )]
        public static List<OccurrenceCache> GetChildOccurrences()
        {
            return GetChildrenOccurrences();
        }

        internal static OccurrenceCache GetByOccurrence( AttendanceOccurrence occurrence )
        {
            var candidate = All()
                .Where( o => o.GroupId == occurrence.GroupId
                        && o.LocationId == occurrence.LocationId
                        && o.ScheduleId == occurrence.ScheduleId )
                .FirstOrDefault();
            return candidate;
        }

        private static List<string> AllKeys()
        {
            RockContext rockContext = new RockContext();

            var groupTypeIds = GroupTypeCache.All()
                .Where( gt => gt.GroupTypePurposeValueId == DefinedValueCache.GetId( Rock.SystemGuid.DefinedValue.GROUPTYPE_PURPOSE_CHECKIN_TEMPLATE.AsGuid() ) )
                .SelectMany( gt => gt.ChildGroupTypes )
                .Select( gt => gt.Id ).ToList();

            var groupService = new GroupService( rockContext );

            var gls = groupService.Queryable().AsNoTracking()
                .Where( g => g.IsActive && !g.IsArchived && groupTypeIds.Contains( g.GroupTypeId ) )
                .SelectMany( g => g.GroupLocations )
                .Where( gl => gl.Schedules.Any() )
                .Select( gl => new
                {
                    GroupLocation = gl,
                    Schedules = gl.Schedules
                } )
                .ToList();

            var keys = new List<string>();

            foreach ( var gl in gls )
            {
                foreach ( var schedule in gl.Schedules )
                {
                    keys.Add( string.Format( "{0}|{1}", gl.GroupLocation.Id, schedule.Id ) );
                }
            }


            var dtDisabled = DefinedTypeCache.Get( Constants.DEFINED_TYPE_DISABLED_GROUPLOCATIONSCHEDULES );
            foreach ( var dvInstance in dtDisabled.DefinedValues )
            {
                if ( keys.Contains( dvInstance.Value ) )
                {
                    RemoveDisabledGroupLocationSchedule( dvInstance );
                }
                else
                {
                    keys.Add( dvInstance.Value );
                }
            }

            return keys;
        }

        private static void RemoveDisabledGroupLocationSchedule( DefinedValueCache definedValueCache )
        {
            using ( RockContext _rockContext = new RockContext() )
            {
                var definedValueService = new DefinedValueService( _rockContext );
                var definedValue = definedValueService.Get( definedValueCache.Id );
                if ( definedValue == null )
                {
                    return;
                }

                definedValueService.Delete( definedValue );
                _rockContext.SaveChanges();
                DefinedValueCache.Clear();
            }
        }

        public static void AddOrUpdate( OccurrenceCache occurrenceCache )
        {
            UpdateCacheItem( occurrenceCache.AccessKey, occurrenceCache, TimeSpan.MaxValue );
        }


        public static void Verify( ref List<string> errors )
        {
            //Load Fresh Values
            var freshKeys = AllKeys();
            var freshCaches = new List<OccurrenceCache>();
            foreach ( var key in freshKeys )
            {
                var cache = OccurrenceCache.LoadByAccessKey( key );
                if ( cache != null )
                {
                    freshCaches.Add( cache );
                }
                else
                {
                    errors.Add( $"Could not load cache by access key {key}" );
                }
            }

            All();
            var currentKeys = RockCacheManager<List<string>>.Instance.Cache.Get( AllKey, _AllRegion );

            var missingKeys = freshKeys.Except( currentKeys ).ToList();
            if ( missingKeys.Any() )
            {
                foreach ( var key in missingKeys )
                {
                    errors.Add( $"Missing key: {key}." );
                }
                errors.Add( "Restored Missing Keys" );
                RockCacheManager<List<string>>.Instance.Cache.AddOrUpdate( AllKey, _AllRegion, freshKeys, ( x ) => freshKeys );
            }

            foreach ( var freshCache in freshCaches )
            {
                var cache = Get( freshCache.AccessKey );
                var heal = false;

                if ( cache.FirmRoomThreshold != freshCache.FirmRoomThreshold )
                {
                    heal = true;
                    errors.Add( $"Occurrence Cache missmatch (Key:{cache.AccessKey} Property:FirmRoomThreshold) Cache: {cache.FirmRoomThreshold} Actual:{freshCache.FirmRoomThreshold})" );
                }

                if ( cache.GroupId != freshCache.GroupId )
                {
                    heal = true;
                    errors.Add( $"Occurrence Cache missmatch (Key:{cache.AccessKey} Property:GroupId) Cache: {cache.GroupId} Actual:{freshCache.GroupId})" );
                }

                if ( cache.GroupLocationId != freshCache.GroupLocationId )
                {
                    heal = true;
                    errors.Add( $"Occurrence Cache missmatch (Key:{cache.AccessKey} Property:GroupLocationId) Cache: {cache.GroupLocationId} Actual:{freshCache.GroupLocationId})" );
                }

                if ( cache.GroupLocationOrder != freshCache.GroupLocationOrder )
                {
                    heal = true;
                    errors.Add( $"Occurrence Cache missmatch (Key:{cache.AccessKey} Property:GroupLocationOrder) Cache: {cache.GroupLocationOrder} Actual:{freshCache.GroupLocationOrder})" );
                }

                if ( cache.GroupName != freshCache.GroupName )
                {
                    heal = true;
                    errors.Add( $"Occurrence Cache missmatch (Key:{cache.AccessKey} Property:GroupName) Cache: {cache.GroupName} Actual:{freshCache.GroupName})" );
                }

                if ( cache.GroupOrder != freshCache.GroupOrder )
                {
                    heal = true;
                    errors.Add( $"Occurrence Cache missmatch (Key:{cache.AccessKey} Property:GroupOrder) Cache: {cache.GroupOrder} Actual:{freshCache.GroupOrder})" );
                }

                if ( cache.GroupTypeId != freshCache.GroupTypeId )
                {
                    heal = true;
                    errors.Add( $"Occurrence Cache missmatch (Key:{cache.AccessKey} Property:GroupTypeId) Cache: {cache.GroupTypeId} Actual:{freshCache.GroupTypeId})" );
                }

                if ( cache.IsActive != freshCache.IsActive )
                {
                    heal = true;
                    errors.Add( $"Occurrence Cache missmatch (Key:{cache.AccessKey} Property:IsActive) Cache: {cache.IsActive} Actual:{freshCache.IsActive})" );
                }

                if ( cache.IsFull != freshCache.IsFull )
                {
                    heal = true;
                    errors.Add( $"Occurrence Cache missmatch (Key:{cache.AccessKey} Property:IsFull) Cache: {cache.IsFull} Actual:{freshCache.IsFull})" );
                }

                if ( cache.IsVolunteer != freshCache.IsVolunteer )
                {
                    heal = true;
                    errors.Add( $"Occurrence Cache missmatch (Key:{cache.AccessKey} Property:IsVolunteer) Cache: {cache.IsVolunteer} Actual:{freshCache.IsVolunteer})" );
                }

                if ( cache.LocationId != freshCache.LocationId )
                {
                    heal = true;
                    errors.Add( $"Occurrence Cache missmatch (Key:{cache.AccessKey} Property:LocationId) Cache: {cache.LocationId} Actual:{freshCache.LocationId})" );
                }

                if ( cache.LocationName != freshCache.LocationName )
                {
                    heal = true;
                    errors.Add( $"Occurrence Cache missmatch (Key:{cache.AccessKey} Property:LocationName) Cache: {cache.LocationName} Actual:{freshCache.LocationName})" );
                }

                if ( cache.RoomRatio != freshCache.RoomRatio )
                {
                    heal = true;
                    errors.Add( $"Occurrence Cache missmatch (Key:{cache.AccessKey} Property:RoomRatio) Cache: {cache.RoomRatio} Actual:{freshCache.RoomRatio})" );
                }

                if ( cache.ScheduleId != freshCache.ScheduleId )
                {
                    heal = true;
                    errors.Add( $"Occurrence Cache missmatch (Key:{cache.AccessKey} Property:ScheduleId) Cache: {cache.ScheduleId} Actual:{freshCache.ScheduleId})" );
                }

                if ( cache.ScheduleId != freshCache.ScheduleId )
                {
                    heal = true;
                    errors.Add( $"Occurrence Cache missmatch (Key:{cache.AccessKey} Property:ScheduleId) Cache: {cache.ScheduleId} Actual:{freshCache.ScheduleId})" );
                }

                if ( cache.ScheduleId != freshCache.ScheduleId )
                {
                    heal = true;
                    errors.Add( $"Occurrence Cache missmatch (Key:{cache.AccessKey} Property:ScheduleId) Cache: {cache.ScheduleId} Actual:{freshCache.ScheduleId})" );
                }

                if ( cache.ScheduleName != freshCache.ScheduleName )
                {
                    heal = true;
                    errors.Add( $"Occurrence Cache missmatch (Key:{cache.AccessKey} Property:ScheduleName) Cache: {cache.ScheduleName} Actual:{freshCache.ScheduleName})" );
                }

                if ( cache.ScheduleStartTime != freshCache.ScheduleStartTime )
                {
                    heal = true;
                    errors.Add( $"Occurrence Cache missmatch (Key:{cache.AccessKey} Property:ScheduleStartTime) Cache: {cache.ScheduleStartTime} Actual:{freshCache.ScheduleStartTime})" );
                }

                if ( cache.SoftRoomThreshold != freshCache.SoftRoomThreshold )
                {
                    heal = true;
                    errors.Add( $"Occurrence Cache missmatch (Key:{cache.AccessKey} Property:SoftRoomThreshold) Cache: {cache.SoftRoomThreshold} Actual:{freshCache.SoftRoomThreshold})" );
                }

                if ( heal )
                {
                    UpdateCacheItem( freshCache.AccessKey, freshCache, TimeSpan.MaxValue );
                }
            }
        }
    }
}
