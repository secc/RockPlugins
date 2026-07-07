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
using Rock;
using Rock.Jobs;

namespace org.secc.FamilyCheckin
{
    public class RemoveExpiredMobileReservations : RockJob
    {
        /// <summary>
        /// Executes the job.
        /// </summary>
        public override void Execute()
        {
            var records = MobileCheckinRecordCache.All()
                .Where( r => !r.ExpirationDateTime.HasValue || r.ExpirationDateTime < Rock.RockDateTime.Now
                || ( r.CreatedDateTime.HasValue && r.CreatedDateTime < Rock.RockDateTime.Today ) )
                .ToList();

            foreach ( var record in records )
            {
                MobileCheckinRecordCache.CancelReservation( record, true );
            }

            Result = $"Removed {records.Count} expired records.";
        }
    }
}
