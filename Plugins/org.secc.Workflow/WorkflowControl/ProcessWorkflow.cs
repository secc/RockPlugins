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
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Workflow;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;

namespace org.secc.Workflow.WorkflowControl
{
    /// <summary>
    /// Processes another workflow with the provided attribute value.
    /// </summary>
    [ActionCategory("SECC > Workflow Control")]
    [Description("Processes another workflow with the provided attribute values.")]
    [Export(typeof(ActionComponent))]
    [ExportMetadata("ComponentName", "Process Workflow")]

    [WorkflowTextOrAttribute("Workflow", "Workflow Attribute", "The ID or Guid of the workflow that should be activated", true, key: "WorkflowReference")]
    public class ProcessWorkflow : ActionComponent
    {
        /// <summary>
        /// Executes the specified workflow.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="action">The action.</param>
        /// <param name="entity">The entity.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns></returns>
        public override bool Execute(RockContext rockContext, WorkflowAction action, Object entity, out List<string> errorMessages)
        {
            errorMessages = new List<string>();

            var workflowService = new WorkflowService(rockContext);

            var reference = GetAttributeValue(action, "WorkflowReference", true);
            Rock.Model.Workflow workflow = null;
            if (reference.AsGuidOrNull() != null)
            {
                var referenceGuid = reference.AsGuid();
                workflow = new WorkflowService(rockContext).Queryable()
                    .Where(w => w.Guid == referenceGuid)
                    .FirstOrDefault();
            }
            else if (reference.AsIntegerOrNull() != null)
            {
                var referenceInt = reference.AsInteger();
                workflow = new WorkflowService(rockContext).Queryable()
                   .Where(w => w.Id == referenceInt)
                   .FirstOrDefault();
            }
            else
            {
                action.AddLogEntry("Invalid Workflow Property", true);
                return false;
            }

            workflowService.Process(workflow, out errorMessages);

            return true;
        }
    }
}