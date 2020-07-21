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

        public static List<OccurrenceCache> GetChildOccurrences()
        {
            return All().Where( o => !o.IsVolunteer ).ToList();
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
            keys.AddRange( dtDisabled.DefinedValues.Select( dv => dv.Value ) );

            return keys;
        }

        public static void AddOrUpdate( OccurrenceCache occurrenceCache )
        {
            UpdateCacheItem( occurrenceCache.AccessKey, occurrenceCache, TimeSpan.MaxValue );
        }
    }
}
