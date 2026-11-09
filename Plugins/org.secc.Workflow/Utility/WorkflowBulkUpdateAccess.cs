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
using System.Collections.Generic;
using Rock.Security;

namespace org.secc.Workflow.Utility
{
    /// <summary>
    /// Decides which workflows the Workflow Bulk Update block may change for a person.
    /// </summary>
    public static class WorkflowBulkUpdateAccess
    {
        /// <summary>
        /// Returns the workflows the person is authorized to edit, in their original order.
        /// </summary>
        /// <param name="workflows">The workflows in the entity set.</param>
        /// <param name="person">The current person.</param>
        /// <param name="skippedCount">The number of workflows left out because the person can't edit them.</param>
        public static List<Rock.Model.Workflow> GetEditableWorkflows( IEnumerable<Rock.Model.Workflow> workflows, Rock.Model.Person person, out int skippedCount )
        {
            var editableWorkflows = new List<Rock.Model.Workflow>();
            skippedCount = 0;

            if ( workflows == null )
            {
                return editableWorkflows;
            }

            foreach ( var workflow in workflows )
            {
                if ( workflow.IsAuthorized( Authorization.EDIT, person ) )
                {
                    editableWorkflows.Add( workflow );
                }
                else
                {
                    skippedCount++;
                }
            }

            return editableWorkflows;
        }
    }
}
