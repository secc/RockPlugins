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
            var scheduleService = new ScheduleService( rockContext );
            var groupLocationService = new GroupLocationService( rockContext );
            var deactivatedGroupLocationSchedules = dvDeactivated.Select( dv => dv.Value.Split( '|' ) )
                .Select( s => new
                {
                    GroupLocation = groupLocationService.Get( s[0].AsInteger() ),
                    Schedule = scheduleService.Get( s[1].AsInteger() ),
                } ).ToList();

            //add schedules back
            foreach ( var groupLocationSchedule in deactivatedGroupLocationSchedules )
            {
                if ( !groupLocationSchedule.GroupLocation.Schedules.Contains( groupLocationSchedule.Schedule ) )
                {
                    groupLocationSchedule.GroupLocation.Schedules.Add( groupLocationSchedule.Schedule );
                }
            }
            //Remove defined values
            foreach ( var value in dvDeactivated )
            {
                definedValueService.Delete( value );
                Rock.Web.Cache.DefinedValueCache.Remove( value.Id );
            }

            //clear defined type cache
            Rock.Web.Cache.DefinedTypeCache.Remove( dtDeactivated.Id );

            rockContext.SaveChanges();

            //clear caches
            CheckinKioskTypeCache.Clear();
            KioskDeviceHelpers.Clear();
            OccurrenceCache.Clear();
            AttendanceCache.Clear();

            context.Result = string.Format( "Finished at {0}. Reset {1} GroupScheduleLocations.", Rock.RockDateTime.Now, deactivatedGroupLocationSchedules.Count );
        }
    }
}
