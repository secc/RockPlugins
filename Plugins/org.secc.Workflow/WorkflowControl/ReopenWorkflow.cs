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
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Workflow;

namespace org.secc.Workflow.WorkflowControl
{
    /// <summary>
    /// Reopens a completed workflow (other than the one running this action) and optionally activates an activity in it.
    /// </summary>
    [ActionCategory( "SECC > Workflow Control" )]
    [Description( "Reopens a completed workflow (clears CompletedDateTime) and optionally activates an activity in it. Works on a different workflow than the one running this action." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Reopen Workflow" )]

    [WorkflowTextOrAttribute( "Workflow", "Workflow Attribute", "The ID or Guid of the workflow to reopen. <span class='tip tip-lava'></span>", true, "", "", 0, "WorkflowReference",
        new string[] { "Rock.Field.Types.WorkflowFieldType", "Rock.Field.Types.TextFieldType", "Rock.Field.Types.IntegerFieldType" } )]
    [WorkflowTextOrAttribute( "Activity Type", "Activity Type Attribute", "Optional. The ID or Guid of an activity type on the target workflow's type to activate after reopening. Activate a long-lived activity (e.g. one with a form) first; if no activity stays active, processing re-completes the workflow. <span class='tip tip-lava'></span>", false, "", "", 1, "ActivityType",
        new string[] { "Rock.Field.Types.WorkflowActivityFieldType", "Rock.Field.Types.TextFieldType", "Rock.Field.Types.IntegerFieldType" } )]
    [BooleanField( "Only Activate If Reopened", "When Yes, the activity is activated only if this action reopened the workflow (it was completed). When No, the activity is always activated.", false, "", 2, "OnlyActivateIfReopened" )]
    [TextField( "Status", "The status to set on the target workflow when it is reopened. <span class='tip tip-lava'></span>", false, "Active", "", 3, "Status" )]
    [TextField( "Log Text", "The log entry written to the target workflow when it is reopened. Merge fields are the calling workflow's. <span class='tip tip-lava'></span>", false, "Re-activated by {{ Workflow.WorkflowType.Name }} workflow{% if Workflow.Id > 0 %} {{ Workflow.Id }}{% endif %}", "", 4, "LogText" )]
    [WorkflowAttribute( "Reopened Attribute", "Optional Boolean or Text attribute on this workflow. Set to True if this action reopened the target workflow, otherwise False.", false, "", "", 5, "ReopenedAttribute",
        new string[] { "Rock.Field.Types.BooleanFieldType", "Rock.Field.Types.TextFieldType" } )]
    [BooleanField( "Process Target Workflow", "Process the target workflow after saving so the activated activity runs now instead of waiting for the Process Workflows job.", true, "", 6, "ProcessWorkflow" )]
    [BooleanField( "Continue On Error", "Should processing continue even if this action fails?", false, "", 7, "ContinueOnError" )]
    public class ReopenWorkflow : ActionComponent
    {
        /// <summary>
        /// Executes the specified workflow.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="action">The action.</param>
        /// <param name="entity">The entity.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns></returns>
        public override bool Execute( RockContext rockContext, WorkflowAction action, Object entity, out List<string> errorMessages )
        {
            errorMessages = new List<string>();

            try
            {
                return Reopen( action, errorMessages );
            }
            catch ( Exception ex )
            {
                ExceptionLogService.LogException( ex );
                return Fail( action, errorMessages, ex.Message );
            }
        }

        private bool Reopen( WorkflowAction action, List<string> errorMessages )
        {
            var mergeFields = GetMergeFields( action );

            // Use new context so only changes made to the target workflow by this action are persisted
            using ( var newRockContext = new RockContext() )
            {
                var workflowService = new WorkflowService( newRockContext );

                var reference = GetAttributeValue( action, "WorkflowReference", true ).ResolveMergeFields( mergeFields ).Trim();
                Rock.Model.Workflow workflow = null;
                var referenceGuid = reference.AsGuidOrNull();
                var referenceInt = reference.AsIntegerOrNull();
                if ( referenceGuid.HasValue )
                {
                    workflow = workflowService.Get( referenceGuid.Value );
                }
                else if ( referenceInt.HasValue )
                {
                    workflow = workflowService.Get( referenceInt.Value );
                }

                if ( workflow == null )
                {
                    return Fail( action, errorMessages, "Workflow not found" );
                }

                if ( workflow.Id == action.Activity.Workflow.Id )
                {
                    return Fail( action, errorMessages, "Reopen Workflow cannot target the workflow it is running in. Use ReActivate Activity instead." );
                }

                if ( workflow.IsProcessing )
                {
                    return Fail( action, errorMessages, "Target workflow is currently being processed" );
                }

                // Resolve and validate the activity type before changing anything, so a bad
                // configuration never leaves the target reopened with nothing activated.
                WorkflowActivityTypeCache activityType = null;
                var activityReference = GetAttributeValue( action, "ActivityType", true ).ResolveMergeFields( mergeFields ).Trim();
                if ( activityReference.IsNotNullOrWhiteSpace() )
                {
                    var activityGuid = activityReference.AsGuidOrNull();
                    var activityId = activityReference.AsIntegerOrNull();
                    if ( activityGuid.HasValue )
                    {
                        activityType = WorkflowActivityTypeCache.Get( activityGuid.Value );
                    }
                    else if ( activityId.HasValue )
                    {
                        activityType = WorkflowActivityTypeCache.Get( activityId.Value );
                    }

                    if ( activityType == null )
                    {
                        return Fail( action, errorMessages, "Activity type not found" );
                    }

                    if ( activityType.WorkflowTypeId != workflow.WorkflowTypeId )
                    {
                        return Fail( action, errorMessages, "Activity type belongs to a different workflow type" );
                    }
                }

                bool reopened = false;
                if ( workflow.CompletedDateTime.HasValue )
                {
                    workflow.CompletedDateTime = null;
                    workflow.Status = GetAttributeValue( action, "Status" ).ResolveMergeFields( mergeFields );
                    workflow.AddLogEntry( GetAttributeValue( action, "LogText" ).ResolveMergeFields( mergeFields ), true );
                    reopened = true;
                    action.AddLogEntry( string.Format( "Reopened workflow {0} '{1}'", workflow.Id, workflow.Name ) );
                }
                else
                {
                    action.AddLogEntry( string.Format( "Workflow {0} is already active", workflow.Id ) );
                }

                // Must run after the reopen above, or Activate returns null for a completed workflow.
                bool activated = false;
                if ( activityType != null && ( reopened || !GetAttributeValue( action, "OnlyActivateIfReopened" ).AsBoolean() ) )
                {
                    if ( WorkflowActivity.Activate( activityType, workflow, newRockContext ) == null )
                    {
                        return Fail( action, errorMessages, string.Format( "Could not activate '{0}' on workflow {1}", activityType.Name, workflow.Id ) );
                    }

                    activated = true;
                    action.AddLogEntry( string.Format( "Activated '{0}' on workflow {1}", activityType.Name, workflow.Id ) );
                }

                newRockContext.SaveChanges();

                if ( GetAttributeValue( action, "ProcessWorkflow" ).AsBoolean() )
                {
                    if ( reopened && !activated )
                    {
                        // A reopened workflow has no active activities, so processing it would just complete it again.
                        action.AddLogEntry( string.Format( "Skipped processing workflow {0} because no activity was activated on it", workflow.Id ), true );
                    }
                    else
                    {
                        // Logged only: returning error messages would stop the calling workflow's
                        // processing, and the reopen/activate above is already saved.
                        List<string> processErrors;
                        workflowService.Process( workflow, out processErrors );
                        foreach ( var processError in processErrors )
                        {
                            action.AddLogEntry( string.Format( "Error processing workflow {0}: {1}", workflow.Id, processError ), true );
                        }

                        if ( reopened && workflow.CompletedDateTime.HasValue )
                        {
                            action.AddLogEntry( "Target workflow re-completed during processing because no activity remained active. Activate a long-lived activity (e.g. a form) first, or set Process Target Workflow = false.", true );
                        }
                    }
                }

                SetReopenedAttribute( action, reopened );
                return true;
            }
        }

        private bool Fail( WorkflowAction action, List<string> errorMessages, string message )
        {
            action.AddLogEntry( message, true );
            SetReopenedAttribute( action, false );

            if ( GetAttributeValue( action, "ContinueOnError" ).AsBoolean() )
            {
                return true;
            }

            // Any error message stops the calling workflow's processing, so only add one when not continuing.
            errorMessages.Add( message );
            return false;
        }

        private void SetReopenedAttribute( WorkflowAction action, bool reopened )
        {
            var attributeGuid = GetAttributeValue( action, "ReopenedAttribute" ).AsGuidOrNull();
            if ( attributeGuid.HasValue )
            {
                SetWorkflowAttributeValue( action, attributeGuid.Value, reopened ? "True" : "False" );
            }
        }
    }
}
