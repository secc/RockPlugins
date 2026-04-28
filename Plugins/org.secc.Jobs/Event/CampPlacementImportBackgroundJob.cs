// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License should be included with this file.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System;
using System.Linq;
using Quartz;
using Rock;
using Rock.Data;
using Rock.Model;

namespace org.secc.Jobs.Event
{
    [DisallowConcurrentExecution]
    public class CampPlacementImportBackgroundJob : IJob
    {
        public void Execute( IJobExecutionContext context )
        {
            var runId = context.JobDetail.JobDataMap.GetInt( "RunId" );

            try
            {
                CampPlacementImportRunner.Run( runId );
            }
            catch ( Exception ex )
            {
                using ( var rockContext = new RockContext() )
                {
                    rockContext.Database.ExecuteSqlCommand( @"
UPDATE [_org_secc_CampPlacementImportRun]
SET [Status] = @status,
    [StatusMessage] = @statusMessage,
    [CompletedDateTime] = GETDATE()
WHERE [Id] = @runId",
                        new System.Data.SqlClient.SqlParameter( "@status", 3 ),
                        new System.Data.SqlClient.SqlParameter( "@statusMessage", ex.Message ),
                        new System.Data.SqlClient.SqlParameter( "@runId", runId ) );
                }

                throw;
            }
        }
    }
}