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
    [Description( "Update an attribute matrix row." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Attribute Matrix - Update Row" )]
    [WorkflowAttribute( "Attribute Matrix", "The attribute matrix to update.", fieldTypeClassNames: new string[] { "Rock.Field.Types.MatrixFieldType" } )]
    [WorkflowTextOrAttribute( "Item Guid", "Item Guid Attribute", "The guid of the item/row to update <span class='tip tip-lava'></span>.", true, key:"ItemGuid" )]
    [KeyValueListField( "Item Attributes", "Use this to map values to associated matrix attributes <span class='tip tip-lava'></span>.", false, keyPrompt: "Attribute Key", valuePrompt: "Value" )]


    class AttributeMatrixUpdateRow : ActionComponent
    {

        public override bool Execute( RockContext rockContext, WorkflowAction action, object entity, out List<string> errorMessages )
        {
            AttributeMatrixService attributeMatrixService = new AttributeMatrixService( rockContext );

            errorMessages = new List<string>();

            // Get all the attribute values
            var attributeMatrixGuid = action.GetWorklowAttributeValue( GetActionAttributeValue( action, "AttributeMatrix" ).AsGuid() ).AsGuidOrNull();
            var itemGuid = GetActionAttributeValue( action, "ItemGuid" ).ResolveMergeFields(GetMergeFields(action)).AsGuidOrNull();
            var itemAttributes = GetActionAttributeValue( action, "ItemAttributes" ).AsDictionaryOrNull();

            if ( attributeMatrixGuid.HasValue && itemGuid.HasValue )
            {
                // Load the matrix
                AttributeMatrix matrix = attributeMatrixService.Get( attributeMatrixGuid.Value );

                AttributeMatrixItem item = matrix.AttributeMatrixItems.Where( i => i.Guid == itemGuid.Value ).FirstOrDefault();
                if (item != null)
                {
                    item.LoadAttributes();

                    foreach ( var attribute in item.Attributes )
                    {
                        if ( itemAttributes.ContainsKey( attribute.Key ) )
                        {
                            item.SetAttributeValue( attribute.Key, itemAttributes[attribute.Key].ResolveMergeFields( GetMergeFields( action ) ) );
                        }
                    }
                    item.SaveAttributeValues( rockContext );
                }

            }

            return true;
        }
    }
}
