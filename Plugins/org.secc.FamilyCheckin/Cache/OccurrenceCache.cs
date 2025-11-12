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
using Rock.Logging;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.FamilyCheckin.Cache
{
    [DataContract]
    [Serializable]
    public class OccurrenceCache : CheckinCache<OccurrenceCache>
    {
        private OccurrenceCache()
        {
        }

        public override string ToString()
        {
            return $"{( IsActive ? "Active" : "Inactive" )}{( IsVolunteer ? "Volunteer" : "" )}: {GroupName} in {LocationName} at {ScheduleName}";
        }

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

        public static void Clear()
        {
            RockLogger.Log.Information( "OTHER", "OccurrenceCache.Clear: Clearing all occurrence cache entries" );
            Clear( () => KeyFactory() );
            RockLogger.Log.Information( "OTHER", "OccurrenceCache.Clear: Successfully cleared all occurrence cache entries" );
        }

        public bool IsFull
        {
            get
            {
                if ( !SoftRoomThreshold.HasValue || !FirmRoomThreshold.HasValue )
                {
                    RockLogger.Log.Debug( "OTHER", "OccurrenceCache.IsFull: No thresholds set for AccessKey: {AccessKey} - returning true (free pass)", AccessKey );
                    return true; //No data --free pass.
                }

                var capacity = FirmRoomThreshold.Value;
                var attendanceCount = 0;

                if ( !IsVolunteer )
                {
                    capacity = Math.Min( FirmRoomThreshold.Value, SoftRoomThreshold.Value );
                    RockLogger.Log.Debug( "OTHER", "OccurrenceCache.IsFull: Non-volunteer occurrence {AccessKey} - using minimum capacity: {Capacity}", 
                        AccessKey, capacity );
                }
                else
                {
                    RockLogger.Log.Debug( "OTHER", "OccurrenceCache.IsFull: Volunteer occurrence {AccessKey} - using firm capacity: {Capacity}", 
                        AccessKey, capacity );
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

                RockLogger.Log.Debug( "OTHER", "OccurrenceCache.IsFull: Initial attendance count from cache for {AccessKey}: {AttendanceCount}/{Capacity}", 
                    AccessKey, attendanceCount, capacity );

                //If we are within 2 of full, cache is too slow to be reliablly accurate
                //.. to the database!!
                if ( attendanceCount < capacity && attendanceCount >= capacity - 2 )
                {
                    RockLogger.Log.Information( "OTHER", "OccurrenceCache.IsFull: Near capacity threshold ({AttendanceCount}/{Capacity}) - checking database for accurate count", 
                        attendanceCount, capacity );

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

                    RockLogger.Log.Information( "OTHER", "OccurrenceCache.IsFull: Database attendance count for {AccessKey}: {AttendanceCount}/{Capacity}", 
                        AccessKey, attendanceCount, capacity );
                }

                var isFull = attendanceCount >= capacity;
                RockLogger.Log.Debug( "OTHER", "OccurrenceCache.IsFull: Final result for {AccessKey}: IsFull={IsFull} ({AttendanceCount}/{Capacity})", 
                    AccessKey, isFull, attendanceCount, capacity );

                return isFull;
            }
        }


        public static List<OccurrenceCache> All()
        {
            RockLogger.Log.Debug( "OTHER", "OccurrenceCache.All: Retrieving all occurrence cache entries" );
            
            var allKeys = AllKeys( () => KeyFactory() );
            RockLogger.Log.Debug( "OTHER", "OccurrenceCache.All: Found {KeyCount} keys", allKeys.Count );

            var allItems = new List<OccurrenceCache>();

            foreach ( var key in allKeys.ToList() )
            {
                var value = GetFromQualifiedKey( key );
                if ( value != null )
                {
                    allItems.Add( value );
                }
                else
                {
                    RockLogger.Log.Warning( "OTHER", "OccurrenceCache.All: Could not load cache entry for key: {Key}", key );
                }
            }

            RockLogger.Log.Debug( "OTHER", "OccurrenceCache.All: Successfully retrieved {ItemCount} occurrence cache entries", allItems.Count );
            return allItems;
        }

        public static OccurrenceCache Get( int groupId, int locationId, int scheduleId )
        {
            RockLogger.Log.Debug( "OTHER", "OccurrenceCache.Get: Retrieving occurrence for GroupId: {GroupId}, LocationId: {LocationId}, ScheduleId: {ScheduleId}", 
                groupId, locationId, scheduleId );
            
            var occurrence = All().Where( o => o.GroupId == groupId && o.LocationId == locationId && o.ScheduleId == scheduleId ).FirstOrDefault();
            
            if ( occurrence != null )
            {
                RockLogger.Log.Debug( "OTHER", "OccurrenceCache.Get: Found occurrence with AccessKey: {AccessKey}", occurrence.AccessKey );
            }
            else
            {
                RockLogger.Log.Warning( "OTHER", "OccurrenceCache.Get: No occurrence found for GroupId: {GroupId}, LocationId: {LocationId}, ScheduleId: {ScheduleId}", 
                    groupId, locationId, scheduleId );
            }
            
            return occurrence;
        }

        public static OccurrenceCache Get( string accessKey )
        {
            RockLogger.Log.Debug( "OTHER", "OccurrenceCache.Get: Retrieving occurrence by AccessKey: {AccessKey}", accessKey );
            var occurrence = Get( QualifiedKey( accessKey ), () => LoadByAccessKey( accessKey ), () => KeyFactory() );
            
            if ( occurrence != null )
            {
                RockLogger.Log.Debug( "OTHER", "OccurrenceCache.Get: Successfully retrieved occurrence for AccessKey: {AccessKey}", accessKey );
            }
            else
            {
                RockLogger.Log.Warning( "OTHER", "OccurrenceCache.Get: Could not retrieve occurrence for AccessKey: {AccessKey}", accessKey );
            }
            
            return occurrence;
        }

        public static OccurrenceCache GetFromQualifiedKey( string qualifiedKey )
        {
            RockLogger.Log.Debug( "OTHER", "OccurrenceCache.GetFromQualifiedKey: Retrieving occurrence by QualifiedKey: {QualifiedKey}", qualifiedKey );
            return Get( qualifiedKey, () => LoadByAccessKey( KeyFromQualifiedKey( qualifiedKey ) ), () => KeyFactory() );
        }

        private static OccurrenceCache LoadByAccessKey( string accessKey )
        {
            RockLogger.Log.Information( "OTHER", "OccurrenceCache.LoadByAccessKey started for AccessKey: {AccessKey}", accessKey );
            
            var keys = accessKey.SplitDelimitedValues();

            if ( keys.Length < 2 )
            {
                RockLogger.Log.Warning( "OTHER", "OccurrenceCache.LoadByAccessKey: Invalid AccessKey format (expected 2 parts, got {PartCount}): {AccessKey}", 
                    keys.Length, accessKey );
                return null;
            }

            var groupLocationId = keys[0].AsInteger();
            var scheduleId = keys[1].AsInteger();
            
            RockLogger.Log.Debug( "OTHER", "OccurrenceCache.LoadByAccessKey: Parsed AccessKey - GroupLocationId: {GroupLocationId}, ScheduleId: {ScheduleId}", 
                groupLocationId, scheduleId );
            
            RockContext rockContext = new RockContext();
            GroupLocationService groupLocationService = new GroupLocationService( rockContext );
            ScheduleService scheduleService = new ScheduleService( rockContext );

            var groupLocation = groupLocationService.Queryable( "Group,Location" ).Where( gl => gl.Id == groupLocationId ).FirstOrDefault();
            var schedule = scheduleService.Get( scheduleId );

            if ( groupLocation == null || schedule == null )
            {
                RockLogger.Log.Warning( "OTHER", "OccurrenceCache.LoadByAccessKey: Could not load entities - GroupLocation: {GroupLocationFound}, Schedule: {ScheduleFound}", 
                    groupLocation != null, schedule != null );
                return null;
            }

            RockLogger.Log.Debug( "OTHER", "OccurrenceCache.LoadByAccessKey: Found GroupLocation {GroupLocationId} ({GroupName}) and Schedule {ScheduleId} ({ScheduleName})", 
                groupLocationId, groupLocation.Group.Name, scheduleId, schedule.Name );

            var volAttribute = AttributeCache.Get( Constants.VOLUNTEER_ATTRIBUTE_GUID.AsGuid() );
            AttributeValueService attributeValueService = new AttributeValueService( new RockContext() );
            var volunteerGroupIds = attributeValueService.Queryable().AsNoTracking()
                .Where( av => av.AttributeId == volAttribute.Id && av.Value == "True" ).Select( av => av.EntityId.Value ).ToList();
            var childrenGroupIds = attributeValueService.Queryable().AsNoTracking()
                .Where( av => av.AttributeId == volAttribute.Id && av.Value == "False" ).Select( av => av.EntityId.Value ).ToList();

            RockLogger.Log.Debug( "OTHER", "OccurrenceCache.LoadByAccessKey: Loaded {VolunteerCount} volunteer groups and {ChildrenCount} children groups", 
                volunteerGroupIds.Count, childrenGroupIds.Count );

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

            RockLogger.Log.Information( "OTHER", "OccurrenceCache.LoadByAccessKey: Successfully created occurrence - AccessKey: {AccessKey}, IsActive: {IsActive}, IsVolunteer: {IsVolunteer}, IsChildren: {IsChildren}, RoomRatio: {RoomRatio}", 
                accessKey, occurrenceCache.IsActive, occurrenceCache.IsVolunteer, occurrenceCache.IsChildren, occurrenceCache.RoomRatio );

            return occurrenceCache;
        }

        public static List<OccurrenceCache> GetVolunteerOccurrences()
        {
            RockLogger.Log.Debug( "OTHER", "OccurrenceCache.GetVolunteerOccurrences: Retrieving volunteer occurrences" );
            var occurrences = All().Where( o => o.IsVolunteer ).ToList();
            RockLogger.Log.Debug( "OTHER", "OccurrenceCache.GetVolunteerOccurrences: Found {Count} volunteer occurrences", occurrences.Count );
            return occurrences;
        }

        public static List<OccurrenceCache> GetNonVolunteerOccurrences()
        {
            RockLogger.Log.Debug( "OTHER", "OccurrenceCache.GetNonVolunteerOccurrences: Retrieving non-volunteer occurrences" );
            var occurrences = All().Where( o => !o.IsVolunteer ).ToList();
            RockLogger.Log.Debug( "OTHER", "OccurrenceCache.GetNonVolunteerOccurrences: Found {Count} non-volunteer occurrences", occurrences.Count );
            return occurrences;
        }

        public static List<OccurrenceCache> GetChildrenOccurrences()
        {
            RockLogger.Log.Debug( "OTHER", "OccurrenceCache.GetChildrenOccurrences: Retrieving children occurrences" );
            var occurrences = All().Where( o => o.IsChildren ).ToList();
            RockLogger.Log.Debug( "OTHER", "OccurrenceCache.GetChildrenOccurrences: Found {Count} children occurrences", occurrences.Count );
            return occurrences;
        }

        public static List<OccurrenceCache> GetNonChildrenOccurrences()
        {
            RockLogger.Log.Debug( "OTHER", "OccurrenceCache.GetNonChildrenOccurrences: Retrieving non-children occurrences" );
            var occurrences = All().Where( o => !o.IsChildren ).ToList();
            RockLogger.Log.Debug( "OTHER", "OccurrenceCache.GetNonChildrenOccurrences: Found {Count} non-children occurrences", occurrences.Count );
            return occurrences;
        }

        public static List<OccurrenceCache> GetUnlabledOccurrences()
        {
            RockLogger.Log.Debug( "OTHER", "OccurrenceCache.GetUnlabledOccurrences: Retrieving unlabeled occurrences" );
            var occurrences = All().Where( o => !o.IsChildren && !o.IsVolunteer ).ToList();
            RockLogger.Log.Debug( "OTHER", "OccurrenceCache.GetUnlabledOccurrences: Found {Count} unlabeled occurrences", occurrences.Count );
            return occurrences;
        }

        [Obsolete( "Use GetChildrenOccurrences" )]
        public static List<OccurrenceCache> GetChildOccurrences()
        {
            return GetChildrenOccurrences();
        }

        internal static OccurrenceCache GetByOccurrence( AttendanceOccurrence occurrence )
        {
            RockLogger.Log.Debug( "OTHER", "OccurrenceCache.GetByOccurrence: Searching for occurrence - GroupId: {GroupId}, LocationId: {LocationId}, ScheduleId: {ScheduleId}", 
                occurrence.GroupId, occurrence.LocationId, occurrence.ScheduleId );
            
            var candidate = All()
                .Where( o => o.GroupId == occurrence.GroupId
                        && o.LocationId == occurrence.LocationId
                        && o.ScheduleId == occurrence.ScheduleId )
                .FirstOrDefault();
            
            if ( candidate != null )
            {
                RockLogger.Log.Debug( "OTHER", "OccurrenceCache.GetByOccurrence: Found occurrence with AccessKey: {AccessKey}", candidate.AccessKey );
            }
            else
            {
                RockLogger.Log.Warning( "OTHER", "OccurrenceCache.GetByOccurrence: No occurrence found for GroupId: {GroupId}, LocationId: {LocationId}, ScheduleId: {ScheduleId}", 
                    occurrence.GroupId, occurrence.LocationId, occurrence.ScheduleId );
            }
            
            return candidate;
        }

        private static List<string> KeyFactory()
        {
            RockLogger.Log.Information( "OTHER", "OccurrenceCache.KeyFactory: Starting key generation" );
            
            RockContext rockContext = new RockContext();

            var groupTypeIds = GroupTypeCache.All()
                .Where( gt => gt.GroupTypePurposeValueId == DefinedValueCache.GetId( Rock.SystemGuid.DefinedValue.GROUPTYPE_PURPOSE_CHECKIN_TEMPLATE.AsGuid() ) )
                .SelectMany( gt => gt.ChildGroupTypes )
                .Select( gt => gt.Id ).ToList();

            RockLogger.Log.Debug( "OTHER", "OccurrenceCache.KeyFactory: Found {GroupTypeCount} check-in group types", groupTypeIds.Count );

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

            RockLogger.Log.Debug( "OTHER", "OccurrenceCache.KeyFactory: Found {GroupLocationCount} active group locations with schedules", gls.Count );

            var keys = new List<string>();

            foreach ( var gl in gls )
            {
                foreach ( var schedule in gl.Schedules )
                {
                    var key = string.Format( "{0}|{1}", gl.GroupLocation.Id, schedule.Id );
                    keys.Add( key );
                    RockLogger.Log.Verbose( "OTHER", "OccurrenceCache.KeyFactory: Added key: {Key} (GroupLocation: {GroupLocationId}, Schedule: {ScheduleId})", 
                        key, gl.GroupLocation.Id, schedule.Id );
                }
            }

            RockLogger.Log.Information( "OTHER", "OccurrenceCache.KeyFactory: Generated {KeyCount} keys from active group location schedules", keys.Count );

            var dtDisabled = DefinedTypeCache.Get( Constants.DEFINED_TYPE_DISABLED_GROUPLOCATIONSCHEDULES );
            
            if ( dtDisabled == null )
            {
                RockLogger.Log.Warning( "OTHER", "OccurrenceCache.KeyFactory: Could not load disabled GroupLocationSchedules defined type" );
            }
            else
            {
                RockLogger.Log.Debug( "OTHER", "OccurrenceCache.KeyFactory: Processing {DisabledCount} disabled GroupLocationSchedule entries", 
                    dtDisabled.DefinedValues.Count );

                var disabledKeysProcessed = 0;
                var disabledKeysRemoved = 0;
                var disabledKeysAdded = 0;

                foreach ( var dvInstance in dtDisabled.DefinedValues )
                {
                    disabledKeysProcessed++;
                    
                    if ( keys.Contains( dvInstance.Value ) )
                    {
                        RockLogger.Log.Information( "OTHER", "OccurrenceCache.KeyFactory: Removing disabled key that is now active: {Key}", dvInstance.Value );
                        RemoveDisabledGroupLocationSchedule( dvInstance.Id );
                        disabledKeysRemoved++;
                    }
                    else
                    {
                        RockLogger.Log.Debug( "OTHER", "OccurrenceCache.KeyFactory: Adding disabled key: {Key}", dvInstance.Value );
                        keys.Add( dvInstance.Value );
                        disabledKeysAdded++;
                    }
                }

                RockLogger.Log.Information( "OTHER", "OccurrenceCache.KeyFactory: Processed disabled entries - Total: {Total}, Removed (reactivated): {Removed}, Added (still disabled): {Added}", 
                    disabledKeysProcessed, disabledKeysRemoved, disabledKeysAdded );
            }

            RockLogger.Log.Information( "OTHER", "OccurrenceCache.KeyFactory: Completed key generation - Total keys: {KeyCount}", keys.Count );

            return keys;
        }

        private static void RemoveDisabledGroupLocationSchedule( int definedValueId )
        {
            RockLogger.Log.Information( "OTHER", "OccurrenceCache.RemoveDisabledGroupLocationSchedule: Removing disabled DefinedValue {DefinedValueId}", definedValueId );
            
            using ( RockContext _rockContext = new RockContext() )
            {
                var definedValueService = new DefinedValueService( _rockContext );
                var definedValue = definedValueService.Get( definedValueId );
                if ( definedValue == null )
                {
                    RockLogger.Log.Warning( "OTHER", "OccurrenceCache.RemoveDisabledGroupLocationSchedule: DefinedValue {DefinedValueId} not found", definedValueId );
                    return;
                }

                var value = definedValue.Value;
                var definedTypeId = definedValue.DefinedTypeId;
                
                RockLogger.Log.Debug( "OTHER", "OccurrenceCache.RemoveDisabledGroupLocationSchedule: Deleting DefinedValue {DefinedValueId} with value: {Value}", 
                    definedValueId, value );
                
                definedValueService.Delete( definedValue );
                _rockContext.SaveChanges();

                RockLogger.Log.Debug( "OTHER", "OccurrenceCache.RemoveDisabledGroupLocationSchedule: Clearing DefinedValue and DefinedType caches" );
                
                // Clear both caches to ensure consistency
                DefinedValueCache.Remove( definedValueId );
                DefinedTypeCache.Remove( definedTypeId );
                
                RockLogger.Log.Information( "OTHER", "OccurrenceCache.RemoveDisabledGroupLocationSchedule: Successfully removed disabled DefinedValue {DefinedValueId} ({Value})", 
                    definedValueId, value );
            }
        }

        public static void AddOrUpdate( OccurrenceCache occurrenceCache )
        {
            RockLogger.Log.Information( "OTHER", "OccurrenceCache.AddOrUpdate: Updating occurrence with AccessKey: {AccessKey}, IsActive: {IsActive}", 
                occurrenceCache.AccessKey, occurrenceCache.IsActive );
            
            AddOrUpdate( QualifiedKey( occurrenceCache.AccessKey ), occurrenceCache, () => KeyFactory() );
            
            RockLogger.Log.Information( "OTHER", "OccurrenceCache.AddOrUpdate: Successfully updated occurrence for AccessKey: {AccessKey}", 
                occurrenceCache.AccessKey );
        }


        public static void Verify( ref List<string> errors )
        {
            RockLogger.Log.Information( "OTHER", "OccurrenceCache.Verify: Starting cache verification" );
            
            //Load Fresh Values
            var freshKeys = KeyFactory();
            RockLogger.Log.Information( "OTHER", "OccurrenceCache.Verify: Generated {FreshKeyCount} fresh keys", freshKeys.Count );
            
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
                    var errorMsg = $"Could not load cache by access key {key}";
                    errors.Add( errorMsg );
                    RockLogger.Log.Error( "OTHER", "OccurrenceCache.Verify: {ErrorMessage}", errorMsg );
                }
            }

            RockLogger.Log.Information( "OTHER", "OccurrenceCache.Verify: Loaded {FreshCacheCount} fresh cache entries", freshCaches.Count );

            All();
            var currentKeys = AllKeys( () => KeyFactory() );
            RockLogger.Log.Information( "OTHER", "OccurrenceCache.Verify: Found {CurrentKeyCount} current cache keys", currentKeys.Count );

            var missingKeys = freshKeys.Except( currentKeys.Select( k => KeyFromQualifiedKey( k ) ) ).ToList();
            if ( missingKeys.Any() )
            {
                RockLogger.Log.Warning( "OTHER", "OccurrenceCache.Verify: Found {MissingKeyCount} missing keys in cache", missingKeys.Count );
                
                foreach ( var key in missingKeys )
                {
                    var errorMsg = $"Missing key: {key}.";
                    errors.Add( errorMsg );
                    RockLogger.Log.Warning( "OTHER", "OccurrenceCache.Verify: {ErrorMessage}", errorMsg );
                }
                errors.Add( "Restored Missing Keys" );
                RockLogger.Log.Information( "OTHER", "OccurrenceCache.Verify: Missing keys will be restored on next access" );
            }
            else
            {
                RockLogger.Log.Information( "OTHER", "OccurrenceCache.Verify: No missing keys found" );
            }

            var mismatchCount = 0;
            var healedCount = 0;

            foreach ( var freshCache in freshCaches )
            {
                var cache = Get( freshCache.AccessKey );
                var heal = false;

                if ( cache?.FirmRoomThreshold != freshCache.FirmRoomThreshold )
                {
                    heal = true;
                    mismatchCount++;
                    var errorMsg = $"Occurrence Cache missmatch (Key:{cache.AccessKey} Property:FirmRoomThreshold) Cache: {cache.FirmRoomThreshold} Actual:{freshCache.FirmRoomThreshold})";
                    errors.Add( errorMsg );
                    RockLogger.Log.Warning( "OTHER", "OccurrenceCache.Verify: {ErrorMessage}", errorMsg );
                }

                if ( cache.GroupId != freshCache.GroupId )
                {
                    heal = true;
                    mismatchCount++;
                    var errorMsg = $"Occurrence Cache missmatch (Key:{cache.AccessKey} Property:GroupId) Cache: {cache.GroupId} Actual:{freshCache.GroupId})";
                    errors.Add( errorMsg );
                    RockLogger.Log.Warning( "OTHER", "OccurrenceCache.Verify: {ErrorMessage}", errorMsg );
                }

                if ( cache.GroupLocationId != freshCache.GroupLocationId )
                {
                    heal = true;
                    mismatchCount++;
                    var errorMsg = $"Occurrence Cache missmatch (Key:{cache.AccessKey} Property:GroupLocationId) Cache: {cache.GroupLocationId} Actual:{freshCache.GroupLocationId})";
                    errors.Add( errorMsg );
                    RockLogger.Log.Warning( "OTHER", "OccurrenceCache.Verify: {ErrorMessage}", errorMsg );
                }

                if ( cache.GroupLocationOrder != freshCache.GroupLocationOrder )
                {
                    heal = true;
                    mismatchCount++;
                    var errorMsg = $"Occurrence Cache missmatch (Key:{cache.AccessKey} Property:GroupLocationOrder) Cache: {cache.GroupLocationOrder} Actual:{freshCache.GroupLocationOrder})";
                    errors.Add( errorMsg );
                    RockLogger.Log.Warning( "OTHER", "OccurrenceCache.Verify: {ErrorMessage}", errorMsg );
                }

                if ( cache.GroupName != freshCache.GroupName )
                {
                    heal = true;
                    mismatchCount++;
                    var errorMsg = $"Occurrence Cache missmatch (Key:{cache.AccessKey} Property:GroupName) Cache: {cache.GroupName} Actual:{freshCache.GroupName})";
                    errors.Add( errorMsg );
                    RockLogger.Log.Warning( "OTHER", "OccurrenceCache.Verify: {ErrorMessage}", errorMsg );
                }

                if ( cache.GroupOrder != freshCache.GroupOrder )
                {
                    heal = true;
                    mismatchCount++;
                    var errorMsg = $"Occurrence Cache missmatch (Key:{cache.AccessKey} Property:GroupOrder) Cache: {cache.GroupOrder} Actual:{freshCache.GroupOrder})";
                    errors.Add( errorMsg );
                    RockLogger.Log.Warning( "OTHER", "OccurrenceCache.Verify: {ErrorMessage}", errorMsg );
                }

                if ( cache.GroupTypeId != freshCache.GroupTypeId )
                {
                    heal = true;
                    mismatchCount++;
                    var errorMsg = $"Occurrence Cache missmatch (Key:{cache.AccessKey} Property:GroupTypeId) Cache: {cache.GroupTypeId} Actual:{freshCache.GroupTypeId})";
                    errors.Add( errorMsg );
                    RockLogger.Log.Warning( "OTHER", "OccurrenceCache.Verify: {ErrorMessage}", errorMsg );
                }

                if ( cache.IsActive != freshCache.IsActive )
                {
                    heal = true;
                    mismatchCount++;
                    var errorMsg = $"Occurrence Cache missmatch (Key:{cache.AccessKey} Property:IsActive) Cache: {cache.IsActive} Actual:{freshCache.IsActive})";
                    errors.Add( errorMsg );
                    RockLogger.Log.Warning( "OTHER", "OccurrenceCache.Verify: {ErrorMessage}", errorMsg );
                }

                if ( cache.IsFull != freshCache.IsFull )
                {
                    heal = true;
                    mismatchCount++;
                    var errorMsg = $"Occurrence Cache missmatch (Key:{cache.AccessKey} Property:IsFull) Cache: {cache.IsFull} Actual:{freshCache.IsFull})";
                    errors.Add( errorMsg );
                    RockLogger.Log.Warning( "OTHER", "OccurrenceCache.Verify: {ErrorMessage}", errorMsg );
                }

                if ( cache.IsVolunteer != freshCache.IsVolunteer )
                {
                    heal = true;
                    mismatchCount++;
                    var errorMsg = $"Occurrence Cache missmatch (Key:{cache.AccessKey} Property:IsVolunteer) Cache: {cache.IsVolunteer} Actual:{freshCache.IsVolunteer})";
                    errors.Add( errorMsg );
                    RockLogger.Log.Warning( "OTHER", "OccurrenceCache.Verify: {ErrorMessage}", errorMsg );
                }

                if ( cache.LocationId != freshCache.LocationId )
                {
                    heal = true;
                    mismatchCount++;
                    var errorMsg = $"Occurrence Cache missmatch (Key:{cache.AccessKey} Property:LocationId) Cache: {cache.LocationId} Actual:{freshCache.LocationId})";
                    errors.Add( errorMsg );
                    RockLogger.Log.Warning( "OTHER", "OccurrenceCache.Verify: {ErrorMessage}", errorMsg );
                }

                if ( cache.LocationName != freshCache.LocationName )
                {
                    heal = true;
                    mismatchCount++;
                    var errorMsg = $"Occurrence Cache missmatch (Key:{cache.AccessKey} Property:LocationName) Cache: {cache.LocationName} Actual:{freshCache.LocationName})";
                    errors.Add( errorMsg );
                    RockLogger.Log.Warning( "OTHER", "OccurrenceCache.Verify: {ErrorMessage}", errorMsg );
                }

                if ( cache.RoomRatio != freshCache.RoomRatio )
                {
                    heal = true;
                    mismatchCount++;
                    var errorMsg = $"Occurrence Cache missmatch (Key:{cache.AccessKey} Property:RoomRatio) Cache: {cache.RoomRatio} Actual:{freshCache.RoomRatio})";
                    errors.Add( errorMsg );
                    RockLogger.Log.Warning( "OTHER", "OccurrenceCache.Verify: {ErrorMessage}", errorMsg );
                }

                if ( cache.ScheduleId != freshCache.ScheduleId )
                {
                    heal = true;
                    mismatchCount++;
                    var errorMsg = $"Occurrence Cache missmatch (Key:{cache.AccessKey} Property:ScheduleId) Cache: {cache.ScheduleId} Actual:{freshCache.ScheduleId})";
                    errors.Add( errorMsg );
                    RockLogger.Log.Warning( "OTHER", "OccurrenceCache.Verify: {ErrorMessage}", errorMsg );
                }

                if ( cache.ScheduleName != freshCache.ScheduleName )
                {
                    heal = true;
                    mismatchCount++;
                    var errorMsg = $"Occurrence Cache missmatch (Key:{cache.AccessKey} Property:ScheduleName) Cache: {cache.ScheduleName} Actual:{freshCache.ScheduleName})";
                    errors.Add( errorMsg );
                    RockLogger.Log.Warning( "OTHER", "OccurrenceCache.Verify: {ErrorMessage}", errorMsg );
                }

                if ( cache.ScheduleStartTime != freshCache.ScheduleStartTime )
                {
                    heal = true;
                    mismatchCount++;
                    var errorMsg = $"Occurrence Cache missmatch (Key:{cache.AccessKey} Property:ScheduleStartTime) Cache: {cache.ScheduleStartTime} Actual:{freshCache.ScheduleStartTime})";
                    errors.Add( errorMsg );
                    RockLogger.Log.Warning( "OTHER", "OccurrenceCache.Verify: {ErrorMessage}", errorMsg );
                }

                if ( cache.SoftRoomThreshold != freshCache.SoftRoomThreshold )
                {
                    heal = true;
                    mismatchCount++;
                    var errorMsg = $"Occurrence Cache missmatch (Key:{cache.AccessKey} Property:SoftRoomThreshold) Cache: {cache.SoftRoomThreshold} Actual:{freshCache.SoftRoomThreshold})";
                    errors.Add( errorMsg );
                    RockLogger.Log.Warning( "OTHER", "OccurrenceCache.Verify: {ErrorMessage}", errorMsg );
                }

                if ( heal )
                {
                    healedCount++;
                    RockLogger.Log.Information( "OTHER", "OccurrenceCache.Verify: Healing cache entry for AccessKey: {AccessKey}", freshCache.AccessKey );
                    AddOrUpdate( freshCache );
                }
            }

            RockLogger.Log.Information( "OTHER", "OccurrenceCache.Verify: Verification complete - Total Mismatches: {MismatchCount}, Healed Entries: {HealedCount}", 
                mismatchCount, healedCount );
        }
    }
}
