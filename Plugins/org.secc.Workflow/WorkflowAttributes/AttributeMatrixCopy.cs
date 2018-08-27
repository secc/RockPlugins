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
    [Description( "Copy/Clone an attribute matrix with all of it's items." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Attribute Matrix - Copy" )]
    [WorkflowAttribute( "Source Attribute Matrix", "The source attribute matrix to copy from.", fieldTypeClassNames: new string[] { "Rock.Field.Types.MatrixFieldType" } )]
    [WorkflowAttribute( "Target Attribute Matrix", "The target attribute matrix to copy to.", fieldTypeClassNames: new string[] { "Rock.Field.Types.MatrixFieldType" } )]
    class AttributeMatrixCopy : ActionComponent
    {

        public override bool Execute( RockContext rockContext, WorkflowAction action, object entity, out List<string> errorMessages )
        {
            AttributeMatrixService attributeMatrixService = new AttributeMatrixService( rockContext );

            errorMessages = new List<string>();

            // Get all the attribute values
            var sourceMatrixAttributeGuid = action.GetWorklowAttributeValue( GetActionAttributeValue( action, "SourceAttributeMatrix" ).AsGuid() ).AsGuidOrNull();
            var targetMatrixAttributeGuid = action.GetWorklowAttributeValue( GetActionAttributeValue( action, "TargetAttributeMatrix" ).AsGuid() ).AsGuidOrNull();

            if ( sourceMatrixAttributeGuid.HasValue )
            {
                // Load the source matrix
                AttributeMatrix sourceMatrix = attributeMatrixService.Get( sourceMatrixAttributeGuid.Value );
                AttributeMatrix targetMatrix = null;

                if ( targetMatrixAttributeGuid.HasValue )
                {
                    // Just delete all the existing items and add new items from the source attribute
                    targetMatrix = attributeMatrixService.Get( targetMatrixAttributeGuid.Value );
                    targetMatrix.AttributeMatrixItems.Clear();
                }
                else
                {
                    targetMatrix = new AttributeMatrix();
                    targetMatrix.AttributeMatrixItems = new List<AttributeMatrixItem>();
                    targetMatrix.AttributeMatrixTemplateId = sourceMatrix.AttributeMatrixTemplateId;
                    attributeMatrixService.Add( targetMatrix );
                }

                // Now copy all the items from the source to the target
                foreach ( var sourceItem in sourceMatrix.AttributeMatrixItems )
                {
                    var targetItem = new AttributeMatrixItem();
                    sourceItem.LoadAttributes();
                    targetItem.AttributeMatrix = targetMatrix;
                    targetItem.LoadAttributes();
                    foreach ( var attribute in sourceItem.AttributeValues )
                    {
                        targetItem.SetAttributeValue( attribute.Key, attribute.Value.Value );
                    }
                    targetMatrix.AttributeMatrixItems.Add( targetItem );
                    rockContext.SaveChanges();
                    targetItem.SaveAttributeValues( rockContext );
                }


                // Now store the target attribute
                var targetAttribute = AttributeCache.Read( GetActionAttributeValue( action, "TargetAttributeMatrix" ).AsGuid(), rockContext );
                if ( targetAttribute.EntityTypeId == new Rock.Model.Workflow().TypeId )
                {
                    action.Activity.Workflow.SetAttributeValue( targetAttribute.Key, targetMatrix.Guid.ToString() );
                }
                else if ( targetAttribute.EntityTypeId == new WorkflowActivity().TypeId )
                {
                    action.Activity.SetAttributeValue( targetAttribute.Key, targetMatrix.Guid.ToString() );
                }
            }

            return true;
        }
    }
}
