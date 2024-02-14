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
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Workflow;

namespace org.secc.Workflow.WorkflowAttributes
{

    /// <summary>
    /// This workflow action copies the attribute values from one workflow into another
    /// </summary>
    [ActionCategory( "SECC > Workflow Attributes" )]
    [Description( "Copy attribute values from one workflow to another." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Copy Attributes From Workflow" )]

    [WorkflowTextOrAttribute( "Source Workflow", "Source Workflow Attribute", "The ID or Guid of the workflow that attributes are being copied from", true, key: "SourceWorkflowReference" )]
    [WorkflowTextOrAttribute( "Target Workflow", "Target Workflow Attribute", "The ID or Guid of the workflow that attributes are being copied to", true, key: "TargetWorkflowReference" )]
    [KeyValueListField( "Workflow Attribute Key", "Used to match the attribute keys of the source workflow to the keys of the target workflow. The target workflow will inherit the attribute values of the keys provided.", false, key: "WorkflowAttributeKey", keyPrompt: "Source Attribute", valuePrompt: "Target Attribute" )]

    class CopyAttributesFromWorkflow : ActionComponent
    {

        public override bool Execute( RockContext rockContext, WorkflowAction action, Object entity, out List<string> errorMessages )
        {

            errorMessages = new List<string>();


            /// <summary>
            /// Instantiating a WorkflowService for the source & target workflows
            /// </summary>
            //Setting up to use the source workflow based on its Guid or Id
            var sourceWorkflowReference = GetAttributeValue( action, "SourceWorkflowReference", true );
            Rock.Model.Workflow sourceWorkflow = null;
            if ( sourceWorkflowReference.AsGuidOrNull() != null )
            {
                var sourceWorkflowReferenceGuid = sourceWorkflowReference.AsGuid();
                sourceWorkflow = new WorkflowService( rockContext ).Queryable()
                    .Where( w => w.Guid == sourceWorkflowReferenceGuid )
                    .FirstOrDefault();
            }
            else if ( sourceWorkflowReference.AsIntegerOrNull() != null )
            {
                var sourceWorkflowReferenceInt = sourceWorkflowReference.AsInteger();
                sourceWorkflow = new WorkflowService( rockContext ).Queryable()
                   .Where( w => w.Id == sourceWorkflowReferenceInt )
                   .FirstOrDefault();
            }
            else
            {
                action.AddLogEntry( "Invalid Source Workflow Property", true );
                return false;
            }

            //Setting up to use the target workflow based on its Guid or Id
            var targetWorkflowReference = GetAttributeValue( action, "TargetWorkflowReference", true );
            Rock.Model.Workflow targetWorkflow = null;
            if ( targetWorkflowReference.AsGuidOrNull() != null )
            {
                var targetWorkflowReferenceGuid = targetWorkflowReference.AsGuid();
                targetWorkflow = new WorkflowService( rockContext ).Queryable()
                    .Where( w => w.Guid == targetWorkflowReferenceGuid )
                    .FirstOrDefault();
            }
            else if ( targetWorkflowReference.AsIntegerOrNull() != null )
            {
                var targetWorkflowReferenceInt = targetWorkflowReference.AsInteger();
                targetWorkflow = new WorkflowService( rockContext ).Queryable()
                   .Where( w => w.Id == targetWorkflowReferenceInt )
                   .FirstOrDefault();
            }
            else
            {
                action.AddLogEntry( "Invalid Target Workflow Property", true );
                return false;
            }


            /// <summary>
            /// Creating the attribute dictionary & copying the attributes from source to target workflow
            /// </summary>
            //Setting up a source key map
            Dictionary<string, string> sourceKeyMap = null;
            var workflowAttributeKeys = GetAttributeValue( action, "WorkflowAttributeKey" );
            if ( !string.IsNullOrWhiteSpace( workflowAttributeKeys ) )
            {
                sourceKeyMap = workflowAttributeKeys.AsDictionaryOrNull();
            }
            sourceKeyMap = sourceKeyMap ?? new Dictionary<string, string>();

            //Loading the attributes for source & target workflows
            sourceWorkflow.LoadAttributes( rockContext );
            targetWorkflow.LoadAttributes( rockContext );

            //Copying the attributes according to the source key map
            foreach ( var keyPair in sourceKeyMap )
            {
                // Does the source key exist as an attribute in the source workflow?
                if ( sourceWorkflow.Attributes.ContainsKey( keyPair.Key ) )
                {
                    //Does the target key exist as an attribute in the target workflow?
                    if ( targetWorkflow.Attributes.ContainsKey( keyPair.Value ) )
                    {
                        var value = sourceWorkflow.AttributeValues[keyPair.Key].Value;
                        targetWorkflow.SetAttributeValue( keyPair.Value, value );
                    }
                    else
                    {
                        errorMessages.Add( string.Format( "'{0}' is not an attribute key in the target workflow: '{1}'", keyPair.Value, targetWorkflow.Name ) );
                    }
                }
                else
                {
                    errorMessages.Add( string.Format( "'{0}' is not an attribute key in the source workflow: '{1}'", keyPair.Key, sourceWorkflow.Name ) );
                }
            }

            rockContext.SaveChanges();

            return true;

        }
    }
}