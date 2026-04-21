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
using System.Linq;
using System.Text;
using Quartz;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Jobs;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.Finance.Jobs
{
    [WorkflowTypeField( "Statement Generator Workflow", "", false, true, "", "Workflow" )]
    [TextField( "Workflow Activity Name", "The name of the PDF generation activity within the Statement Generator Workflow", true )]
    [DisallowConcurrentExecution]
    public class ProcessGivingStatements : RockJob
    {
        public ProcessGivingStatements()
        {
        }

        public override void Execute()
        {
            int workflowsProcessed = 0;
            int workflowErrors = 0;
            int workflowExceptions = 0;
            var processingErrors = new List<string>();
            var exceptionMsgs = new List<string>();

            var statementGeneratorWorkflowGuid = GetAttributeValue( "StatementGeneratorWorkflow" ).AsGuidOrNull();
            var workflowActivityName = GetAttributeValue( "WorkflowActivityName" );

            if ( statementGeneratorWorkflowGuid != null && !string.IsNullOrWhiteSpace( workflowActivityName ) )
            {
                var statementGeneratorWorkflowType = WorkflowTypeCache.Get( statementGeneratorWorkflowGuid.Value );
                if ( statementGeneratorWorkflowType != null )
                {
                    var activityType = statementGeneratorWorkflowType.ActivityTypes
                        .FirstOrDefault( at => at.Name == workflowActivityName );

                    if ( activityType != null )
                    {
                        foreach ( var workflowId in new WorkflowService( new RockContext() ).GetActive()
                            .Where( w => w.WorkflowTypeId == statementGeneratorWorkflowType.Id && w.Status == "Pending" )
                            .Select( w => w.Id )
                            .ToList() )
                        {
                            try
                            {
                                var rockContext = new RockContext();
                                var workflowService = new WorkflowService( rockContext );
                                var workflow = workflowService.Queryable().FirstOrDefault( a => a.Id == workflowId );

                                if ( workflow != null )
                                {
                                    var workflowType = workflow.WorkflowTypeCache;
                                    if ( workflowType != null )
                                    {
                                        try
                                        {
                                            WorkflowActivity.Activate( activityType, workflow, rockContext );

                                            var errorMessages = new List<string>();
                                            var processed = workflowService.Process( workflow, out errorMessages );

                                            if ( processed )
                                            {
                                                workflowsProcessed++;
                                            }
                                            else
                                            {
                                                workflowErrors++;
                                                processingErrors.Add(
                                                    string.Format(
                                                        "{0} [{1}] - {2} [{3}]: {4}",
                                                        workflowType.Name,
                                                        workflowType.Id,
                                                        workflow.Name,
                                                        workflow.Id,
                                                        errorMessages.AsDelimited( ", " ) ) );
                                            }
                                        }
                                        catch ( Exception ex )
                                        {
                                            var workflowDetails = string.Format(
                                                "{0} [{1}] - {2} [{3}]",
                                                workflowType.Name,
                                                workflowType.Id,
                                                workflow.Name,
                                                workflow.Id );

                                            exceptionMsgs.Add( workflowDetails + ": " + ex.Message );
                                            throw new Exception( "Exception occurred processing workflow: " + workflowDetails, ex );
                                        }
                                    }
                                }
                            }
                            catch ( Exception ex )
                            {
                                ExceptionLogService.LogException( ex, null );
                                workflowExceptions++;
                            }
                        }
                    }
                }
            }

            var resultMsg = new StringBuilder();
            resultMsg.AppendFormat( "{0} workflows processed", workflowsProcessed );

            if ( workflowErrors > 0 )
            {
                resultMsg.AppendFormat( ", {0} workflows reported an error", workflowErrors );
            }

            if ( workflowExceptions > 0 )
            {
                resultMsg.AppendFormat( ", {0} workflows caused an exception", workflowExceptions );
            }

            if ( processingErrors.Any() )
            {
                resultMsg.Append( Environment.NewLine + processingErrors.AsDelimited( Environment.NewLine ) );
            }

            if ( exceptionMsgs.Any() )
            {
                throw new Exception(
                    "One or more exceptions occurred processing workflows..." +
                    Environment.NewLine +
                    exceptionMsgs.AsDelimited( Environment.NewLine ) );
            }

            Result = resultMsg.ToString();
        }
    }
}