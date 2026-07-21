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
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Jobs;
using Rock.Model;

namespace org.secc.Jobs.Event
{
    [IntegerField(
        "Run Id",
        "Id of the _org_secc_CampPlacementImportRun row to process.",
        true, 0, "General", 0, "RunId" )]
    public class CampPlacementImportBackgroundJob : RockJob
    {
        public override void Execute()
        {
            int runId = GetAttributeValue( "RunId" ).AsInteger();

            if ( runId <= 0 )
            {
                throw new Exception( "Camp placement import job requires a valid RunId job attribute." );
            }

            try
            {
                CampPlacementImportRunner.Run( runId );
            }
            catch ( Exception ex )
            {
                // Log exception into rock to allow diagnostics.
                ExceptionLogService.LogException( ex, null );

                using ( var rockContext = new RockContext() )
                {
                    rockContext.Database.ExecuteSqlCommand( @"
UPDATE [_org_secc_CampPlacementImportRun]
SET [Status] = @status,
    [StatusMessage] = @statusMessage,
    [CompletedDateTime] = GETDATE()
WHERE [Id] = @runId",
                        new System.Data.SqlClient.SqlParameter( "@status", ( int ) ImportRunStatus.Failed ),
                        // Render standard Exception message to the user leaving traces off of the UI
                        new System.Data.SqlClient.SqlParameter( "@statusMessage", string.Format("Import Failed: {0}", ex.Message) ),
                        new System.Data.SqlClient.SqlParameter( "@runId", runId ) );
                }

                throw;
            }
        }
    }
}