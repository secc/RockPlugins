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
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Workflow;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;

namespace org.secc.Workflow.WorkflowControl
{
    [ActionCategory("SECC > Workflow Control")]
    [Description("Clears the Authorization Cache.")]
    [Export(typeof(ActionComponent))]
    [ExportMetadata("ComponentName", "Clear Auth Cache")]
    public class ClearAuthCache : ActionComponent
    {
        public override bool Execute(RockContext rockContext, WorkflowAction action, object entity, out List<string> errorMessages)
        {
            errorMessages = new List<string>();

            try
            {
                Authorization.Clear();
            }
            catch (Exception ex)
            {
                errorMessages.Add(ex.Message);
            }

            return errorMessages.Count == 0;
        }
    }
}
