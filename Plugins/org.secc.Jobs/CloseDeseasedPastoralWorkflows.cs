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
//
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Jobs;
using Rock.Model;

namespace org.secc.Jobs
{
    [WorkflowTypeField( "Hospital Admission Workflow", "", false, true, "", "Workflows" )]
    [WorkflowTypeField( "Nursing Home Resident Workflow", "", false, true, "", "Workflows" )]
    [WorkflowTypeField( "Homebound Person Workflow", "", false, true, "", "Workflows" )]

    public class CloseDeseasedPastoralWorkflows : RockJob
    {
        /// <summary>
        /// Written (forced, regardless of the workflow type's logging level) to every workflow this job
        /// closes. It records what the job closed, and a workflow that already has it was closed by this
        /// job and then reopened by staff, so it is not closed again.
        /// </summary>
        private const string ClosedLogText = "Closed by CloseDeseasedPastoralWorkflows: person is deceased.";

        private static readonly string[] PersonAttributeKeys = { "PersonToVisit", "HomeboundPerson" };

        public override void Execute()
        {
            var workflowTypeGuids = new[]
                {
                    GetAttributeValue( "HospitalAdmissionWorkflow" ).AsGuidOrNull(),
                    GetAttributeValue( "NursingHomeResidentWorkflow" ).AsGuidOrNull(),
                    GetAttributeValue( "HomeboundPersonWorkflow" ).AsGuidOrNull()
                }
                .Where( g => g.HasValue )
                .Select( g => g.Value )
                .ToList();

            List<int> workflowIdsToClose;

            using ( var rockContext = new RockContext() )
            {
                var workflowTypeIds = new WorkflowTypeService( rockContext ).Queryable()
                    .Where( wt => workflowTypeGuids.Contains( wt.Guid ) )
                    .Select( wt => wt.Id )
                    .ToList();

                var workflowTypeIdsAsStrings = workflowTypeIds.Select( id => id.ToString() ).ToList();

                var attributeIds = new AttributeService( rockContext ).Queryable()
                    .Where( a => a.EntityTypeQualifierColumn == "WorkflowTypeId"
                        && workflowTypeIdsAsStrings.Contains( a.EntityTypeQualifierValue )
                        && PersonAttributeKeys.Contains( a.Key ) )
                    .Select( a => a.Id )
                    .ToList();

                // Only the workflow Id and the person attribute value (a PersonAlias Guid) are needed.
                var candidates = new WorkflowService( rockContext ).Queryable().AsNoTracking()
                    .Where( w => workflowTypeIds.Contains( w.WorkflowTypeId )
                        && w.Status == "Active"
                        && w.CompletedDateTime == null )
                    .Join( new AttributeValueService( rockContext ).Queryable().AsNoTracking()
                            .Where( av => attributeIds.Contains( av.AttributeId ) ),
                        w => w.Id,
                        av => av.EntityId.Value,
                        ( w, av ) => new { WorkflowId = w.Id, av.Value } )
                    .ToList()
                    .Select( c => new { c.WorkflowId, PersonAliasGuid = c.Value.AsGuidOrNull() } )
                    .Where( c => c.PersonAliasGuid.HasValue )
                    .ToList();

                var personAliasGuids = candidates.Select( c => c.PersonAliasGuid.Value ).Distinct().ToList();

                var deceasedPersonAliasGuids = new HashSet<Guid>( new PersonAliasService( rockContext ).Queryable().AsNoTracking()
                    .Where( pa => personAliasGuids.Contains( pa.Guid ) && pa.Person.IsDeceased )
                    .Select( pa => pa.Guid )
                    .ToList() );

                workflowIdsToClose = candidates
                    .Where( c => deceasedPersonAliasGuids.Contains( c.PersonAliasGuid.Value ) )
                    .Select( c => c.WorkflowId )
                    .Distinct()
                    .ToList();

                // Leave alone workflows this job already closed once that staff have since reopened.
                var reopenedWorkflowIds = rockContext.Set<WorkflowLog>().AsNoTracking()
                    .Where( l => workflowIdsToClose.Contains( l.WorkflowId ) && l.LogText == ClosedLogText )
                    .Select( l => l.WorkflowId )
                    .Distinct()
                    .ToList();

                workflowIdsToClose = workflowIdsToClose.Except( reopenedWorkflowIds ).ToList();
            }

            // Re-fetch and save each workflow in its own context so one failure does not stop the rest.
            var closedWorkflowIds = new List<int>();
            var notSavedCount = 0;
            var errorCount = 0;

            foreach ( var workflowId in workflowIdsToClose )
            {
                try
                {
                    using ( var writeContext = new RockContext() )
                    {
                        // Re-check eligibility: the workflow may have been completed since it was selected.
                        var workflow = new WorkflowService( writeContext ).Queryable()
                            .FirstOrDefault( w => w.Id == workflowId && w.Status == "Active" && w.CompletedDateTime == null );

                        if ( workflow == null )
                        {
                            continue;
                        }

                        // Not Workflow.MarkComplete(): it re-stamps CompletedDateTime on activities that were
                        // already completed, which would overwrite their original completion times.
                        foreach ( var activity in workflow.Activities.Where( a => !a.CompletedDateTime.HasValue ) )
                        {
                            activity.MarkComplete();
                        }

                        workflow.CompletedDateTime = RockDateTime.Now;
                        workflow.Status = "Completed";
                        workflow.AddLogEntry( ClosedLogText, true );

                        // SaveChanges returns 0 without throwing when a workflow trigger cancels the save.
                        if ( writeContext.SaveChanges() > 0 )
                        {
                            closedWorkflowIds.Add( workflowId );
                        }
                        else
                        {
                            notSavedCount++;
                        }
                    }
                }
                catch ( Exception ex )
                {
                    ExceptionLogService.LogException( new Exception( string.Format( "CloseDeseasedPastoralWorkflows could not close workflow {0}.", workflowId ), ex ) );
                    errorCount++;
                }
            }

            var result = string.Format( "Closed {0} workflows.", closedWorkflowIds.Count );

            if ( closedWorkflowIds.Any() )
            {
                result += string.Format( " Workflow Ids: {0}.", string.Join( ", ", closedWorkflowIds ) );
            }

            if ( notSavedCount > 0 )
            {
                result += string.Format( " {0} not saved (save cancelled).", notSavedCount );
            }

            if ( errorCount > 0 )
            {
                result += string.Format( " {0} failed (see Exception Log).", errorCount );
                throw new RockJobWarningException( result );
            }

            Result = result;
        }
    }
}
