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
using System.Data.Entity;
using System.Linq;
using org.secc.Rise.Model;
using Quartz;
using Rock.Data;

namespace org.secc.Rise
{
    /// <summary>Syncs groups to Rise</summary>
    /// <seealso cref="Quartz.IJob" />
    [DisallowConcurrentExecution]
    public class SyncCourseCompletion : IJob
    {
        /// <summary>Executes the specified context.</summary>
        /// <param name="context">The context.</param>
        public void Execute( IJobExecutionContext context )
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;
            RockContext rockContext = new RockContext();
            CourseService courseService = new CourseService( rockContext );
            var skip = 0;

            Course course = courseService.Queryable().AsNoTracking().OrderBy( c => c.Id ).FirstOrDefault();

            while ( course != null )
            {
                skip++;

                course.SyncCompletions();

                course = courseService.Queryable().AsNoTracking().OrderBy( c => c.Id ).Skip( skip ).FirstOrDefault();
            }
            context.Result = $"Synced {skip} courses";
        }

    }
}