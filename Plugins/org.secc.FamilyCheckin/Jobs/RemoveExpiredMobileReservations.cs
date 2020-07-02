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
using Quartz;
using Rock;

namespace org.secc.FamilyCheckin
{
    [DisallowConcurrentExecution]
    public class RemoveExpiredMobileReservations : IJob
    {
        /// <summary>
        /// Executes the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        public void Execute( IJobExecutionContext context )
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;

            var records = MobileCheckinRecordCache.All()
                .Where( r => !r.ExpirationDateTime.HasValue || r.ExpirationDateTime < Rock.RockDateTime.Now
                || ( r.CreatedDateTime.HasValue && r.CreatedDateTime < Rock.RockDateTime.Today ) )
                .ToList();

            foreach ( var record in records )
            {
                MobileCheckinRecordCache.CancelReservation( record, true );
            }

            context.Result = $"Removed {records.Count} expired records.";
        }
    }
}
