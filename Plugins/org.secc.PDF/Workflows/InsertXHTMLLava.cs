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
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Workflow;
using Rock.Attribute;

namespace org.secc.PDF
{
    [ActionCategory( "PDF" )]
    [Description( "Inserts XTML and Lava into workflow to be merged and generated." )]
    [Export( typeof( Rock.Workflow.ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Insert XHTML and Lava" )]
    [CodeEditorField( "XHTML", "XHTML and Lava to be merged with merge fields." )]
    class InsertXTMLLava : ActionComponent
    {
        public override bool Execute( RockContext rockContext, WorkflowAction action, object entity, out List<string> errorMessages )
        {
            errorMessages = new List<string>();

            Utility.EnsureAttributes( action, rockContext );

            action.Activity.Workflow.SetAttributeValue( "XHTML", GetActionAttributeValue( action, "XHTML" ) );

            return true;
        }
    }
}
