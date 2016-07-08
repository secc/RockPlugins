using System;
using System.Linq;

using Quartz;
using Rock.Attribute;
using Rock;
using Rock.Model;
using Rock.Data;

namespace org.secc.FamilyCheckin
{

    [DefinedTypeField("Disabled GroupLocationSchedules", "Defined type which the disabled GroupLocationSchedules are saved", true)]

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
            var definedTypeGuid = dataMap.GetString( "DisabledGroupLocationSchedules" ).AsGuidOrNull();
            if ( definedTypeGuid == null )
            {
                return;
            }
            var definedTypeService = new DefinedTypeService( rockContext );
            var definedValueService = new DefinedValueService( rockContext );
            var dtDeactivated = definedTypeService.Get( definedTypeGuid ?? new Guid());
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
            foreach (var groupLocationSchedule in deactivatedGroupLocationSchedules )
            {
                if ( !groupLocationSchedule.GroupLocation.Schedules.Contains( groupLocationSchedule.Schedule ) )
                {
                    groupLocationSchedule.GroupLocation.Schedules.Add( groupLocationSchedule.Schedule );
                }
            }
            //Remove defined values
            foreach (var value in dvDeactivated )
            {
                definedValueService.Delete( value );
                Rock.Web.Cache.DefinedValueCache.Flush( value.Id );
            }

            //clear defined type cache
            Rock.Web.Cache.DefinedTypeCache.Flush( dtDeactivated.Id );

            rockContext.SaveChanges();

            context.Result = string.Format("Finished at {0}. Reset {1} GroupScheduleLocations.", Rock.RockDateTime.Now, deactivatedGroupLocationSchedules.Count);
        }
    }
}
