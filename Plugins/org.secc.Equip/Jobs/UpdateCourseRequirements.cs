
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Reflection;
using org.secc.Equip.Helpers;
using org.secc.Equip.Model;
using Quartz;
using Quartz.Impl.Matchers;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.Equip.Jobs

{
    /// <summary>
    /// Job to keep a heartbeat of the job process so we know when the jobs stop working
    /// </summary>
    [DisallowConcurrentExecution]
    public class UpdateCourseRequirements : IJob
    {
        public UpdateCourseRequirements() { }

        public virtual void Execute( IJobExecutionContext context )
        {
            using ( RockContext rockContext = new RockContext() )
            {
                CourseRequirementService courseRequirementService = new CourseRequirementService( rockContext );

                var courseRequirements = courseRequirementService.Queryable().AsNoTracking().ToList();
                foreach ( var courseRequirement in courseRequirements )
                {
                    CourseRequirementHelper.UpdateCourseRequirementStatuses( courseRequirement );
                }

            }
        }
    }
}