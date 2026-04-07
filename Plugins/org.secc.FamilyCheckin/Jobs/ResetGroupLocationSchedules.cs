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
using System.Collections.Generic;
using System.Linq;
using org.secc.FamilyCheckin.Cache;
using org.secc.FamilyCheckin.Utilities;
using Quartz;
using Rock;
using Rock.Data;
using Rock.Model;

namespace org.secc.FamilyCheckin
{
    [DisallowConcurrentExecution]
    public class ResetGroupLocationSchedules : IJob
    {
        /// <summary>
        /// Executes the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        public void Execute( IJobExecutionContext context )
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;

            var rockContext = new RockContext();

            var definedTypeService = new DefinedTypeService( rockContext );
            var definedValueService = new DefinedValueService( rockContext );
            var dtDeactivated = definedTypeService.Get( Constants.DEFINED_TYPE_DISABLED_GROUPLOCATIONSCHEDULES.AsGuid() );
            var dvDeactivated = dtDeactivated.DefinedValues.ToList();

            // Early exit if nothing to process
            if ( !dvDeactivated.Any() )
            {
                context.Result = "No deactivated GroupLocationSchedules to reset.";
                return;
            }

            var scheduleService = new ScheduleService( rockContext );
            var groupLocationService = new GroupLocationService( rockContext );
            var deactivatedGroupLocationSchedules = dvDeactivated
                .Select( dv => dv.Value.Split( '|' ) )
                .Select( s => new
                {
                    GroupLocation = groupLocationService.Get( s[0].AsInteger() ),
                    Schedule = scheduleService.Get( s[1].AsInteger() ),
                } )
                .Where( x => x.GroupLocation != null && x.Schedule != null )
                .ToList();

            // Track affected items for targeted cache invalidation
            var affectedAccessKeys = new List<string>();
            var affectedGroupTypeIds = new HashSet<int>();

            // Add schedules back
            foreach ( var groupLocationSchedule in deactivatedGroupLocationSchedules )
            {
                if ( !groupLocationSchedule.GroupLocation.Schedules.Contains( groupLocationSchedule.Schedule ) )
                {
                    groupLocationSchedule.GroupLocation.Schedules.Add( groupLocationSchedule.Schedule );
                }
                affectedAccessKeys.Add( string.Format( "{0}|{1}", groupLocationSchedule.GroupLocation.Id, groupLocationSchedule.Schedule.Id ) );
                affectedGroupTypeIds.Add( groupLocationSchedule.GroupLocation.Group.GroupTypeId );
            }

            // Remove defined values in batch (no individual cache removals)
            foreach ( var value in dvDeactivated )
            {
                definedValueService.Delete( value );
            }

            // Single database save for all changes
            rockContext.SaveChanges();

            // --- Targeted cache invalidation ---

            // 1. Clear the defined type cache once (covers all child defined values)
            Rock.Web.Cache.DefinedTypeCache.Remove( dtDeactivated.Id );

            // 2. Update only the affected occurrence cache entries instead of clearing all
            foreach ( var accessKey in affectedAccessKeys )
            {
                var occurrence = OccurrenceCache.Get( accessKey );
                if ( occurrence != null )
                {
                    occurrence.IsActive = true;
                    OccurrenceCache.AddOrUpdate( occurrence );
                }
            }

            // 3. Clear only kiosk types that reference the affected group types
            var affectedKioskTypeIds = CheckinKioskTypeCache.All()
                .Where( kt => kt.GroupTypeIds != null && kt.GroupTypeIds.Any( id => affectedGroupTypeIds.Contains( id ) ) )
                .Select( kt => kt.Id )
                .ToList();

            foreach ( var id in affectedKioskTypeIds )
            {
                CheckinKioskTypeCache.FlushItem( id );
            }

            // 4. Clear only kiosk devices for the affected group types
            KioskDeviceHelpers.Clear( affectedGroupTypeIds.ToList() );

            // NOTE: AttendanceCache is intentionally NOT cleared — attendance records
            // are unaffected by schedule re-activation.

            context.Result = string.Format( "Finished at {0}. Reset {1} GroupScheduleLocations.", Rock.RockDateTime.Now, deactivatedGroupLocationSchedules.Count );
        }
    }
}
