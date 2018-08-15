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
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using System.Linq;
using Rock.Workflow;

namespace org.secc.Workflow.WorkflowAttributes
{
    [ActionCategory( "SECC > Workflow Attributes" )]
    [Description( "Delete an attribute matrix row." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Attribute Matrix - Delete Row" )]
    [WorkflowAttribute( "Attribute Matrix", "The attribute matrix to update.", fieldTypeClassNames: new string[] { "Rock.Field.Types.MatrixFieldType" } )]
    [WorkflowTextOrAttribute( "Item Guid", "Item Guid Attribute", "The guid of the item/row to update <span class='tip tip-lava'></span>.", true, key: "ItemGuid" )]
    class AttributeMatrixDeleteRow : ActionComponent
    {

        public override bool Execute( RockContext rockContext, WorkflowAction action, object entity, out List<string> errorMessages )
        {
            AttributeMatrixService attributeMatrixService = new AttributeMatrixService( rockContext );
            AttributeMatrixItemService attributeMatrixItemService = new AttributeMatrixItemService( rockContext );

            errorMessages = new List<string>();

            // Get all the attribute values
            var attributeMatrixGuid = action.GetWorklowAttributeValue( GetActionAttributeValue( action, "AttributeMatrix" ).AsGuid() ).AsGuidOrNull();
            var itemGuid = GetActionAttributeValue( action, "ItemGuid" ).ResolveMergeFields(GetMergeFields(action)).AsGuidOrNull();

            if ( attributeMatrixGuid.HasValue && itemGuid.HasValue )
            {
                // Load the matrix
                AttributeMatrix matrix = attributeMatrixService.Get( attributeMatrixGuid.Value );

                AttributeMatrixItem item = matrix.AttributeMatrixItems.Where( i => i.Guid == itemGuid.Value ).FirstOrDefault();

                if (item != null)
                {
                    matrix.AttributeMatrixItems.Remove( item );
                    attributeMatrixItemService.Delete( item );
                }
            }

            return true;
        }
    }
}
