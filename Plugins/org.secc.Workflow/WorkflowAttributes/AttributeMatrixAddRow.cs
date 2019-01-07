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
using Rock.Web.Cache;
using Rock.Workflow;

namespace org.secc.Workflow.WorkflowAttributes
{
    [ActionCategory( "SECC > Workflow Attributes" )]
    [Description( "Add a new attribute matrix row." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Attribute Matrix - Add Row" )]
    [WorkflowAttribute( "Attribute Matrix", "The attribute matrix to update.", fieldTypeClassNames: new string[] { "Rock.Field.Types.MatrixFieldType" } )]
    [KeyValueListField( "Item Attributes", "Use this to map values to associated matrix attributes <span class='tip tip-lava'></span>.", false, keyPrompt: "Attribute Key", valuePrompt: "Value" )]

    class AddAttributeMatrixRow : ActionComponent
    {

        public override bool Execute( RockContext rockContext, WorkflowAction action, object entity, out List<string> errorMessages )
        {
            AttributeMatrixService attributeMatrixService = new AttributeMatrixService( rockContext );

            errorMessages = new List<string>();

            // Get all the attribute values
            var attributeGuid = GetActionAttributeValue( action, "AttributeMatrix" ).AsGuidOrNull();
            var attributeMatrixGuid = action.GetWorklowAttributeValue( attributeGuid.HasValue?attributeGuid.Value:Guid.Empty ).AsGuidOrNull();
            var itemAttributes = GetActionAttributeValue( action, "ItemAttributes" ).AsDictionaryOrNull();

            if ( attributeGuid.HasValue && itemAttributes != null)
            {
                // Load the matrix
                AttributeMatrix matrix = attributeMatrixService.Get( attributeMatrixGuid.HasValue? attributeMatrixGuid.Value:Guid.Empty );

                // If the matrix is null, create it first
                if (matrix == null)
                {
                    var attribute = AttributeCache.Get( GetActionAttributeValue( action, "AttributeMatrix" ).AsGuid() );
                    matrix = new AttributeMatrix();
                    matrix.AttributeMatrixItems = new List<AttributeMatrixItem>();
                    matrix.AttributeMatrixTemplateId = attribute.QualifierValues["attributematrixtemplate"]?.Value?.AsInteger()??0;
                    attributeMatrixService.Add( matrix );

                    // Persist it and make sure it gets saved
                    rockContext.SaveChanges();
                    if ( attribute.EntityTypeId == new Rock.Model.Workflow().TypeId )
                    {
                        action.Activity.Workflow.SetAttributeValue( attribute.Key, matrix.Guid.ToString() );
                    }
                    else if ( attribute.EntityTypeId == new WorkflowActivity().TypeId )
                    {
                        action.Activity.SetAttributeValue( attribute.Key, matrix.Guid.ToString() );
                    }
                }

                // Create the new item
                AttributeMatrixItem item = new AttributeMatrixItem();
                item.AttributeMatrix = matrix;
                item.LoadAttributes();

                foreach(var attribute in item.Attributes)
                {
                    if (itemAttributes.ContainsKey(attribute.Key))
                    {
                        item.SetAttributeValue( attribute.Key, itemAttributes[attribute.Key].ResolveMergeFields( GetMergeFields( action ) ) );
                    }
                }
                matrix.AttributeMatrixItems.Add( item );
                rockContext.SaveChanges();
                item.SaveAttributeValues( rockContext );
            }

            return true;
        }
    }
}
