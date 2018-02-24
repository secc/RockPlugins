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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.PDF
{
    class Utility
    {
        public static PDFWorkflowObject GetPDFFormMergeFromEntity( object entity, out List<string> errorMessages )
        {
            errorMessages = new List<string>();

            if ( entity is PDFWorkflowObject )
            {
                return ( PDFWorkflowObject ) entity;
            }

            errorMessages.Add( "Could not get PDFFormMergeEntity object" );
            return null;
        }

        public static void EnsureAttributes( WorkflowAction action, RockContext rockContext )
        {

            if ( action.Activity.Workflow.Attributes == null || action.Activity.Workflow.AttributeValues.Count == 0 )
            {
                action.Activity.Workflow.LoadAttributes();
            }

            var workflowAttributes = action.Activity.Workflow.Attributes;

            if ( !workflowAttributes.ContainsKey( "PersonId" ) )
            {
                CreateAttribute( "PersonId", action, rockContext );
            }

            if ( !workflowAttributes.ContainsKey( "RegistrationRegistrantId" ) )
            {
                CreateAttribute( "RegistrationRegistrantId", action, rockContext );
            }

            if ( !workflowAttributes.ContainsKey( "GroupMemberId" ) )
            {
                CreateAttribute( "GroupMemberId", action, rockContext );
            }

            if ( !workflowAttributes.ContainsKey( "PDFGuid" ) )
            {
                CreateAttribute( "PDFGuid", action, rockContext );
            }

            if ( !workflowAttributes.ContainsKey( "XHTML" ) )
            {
                CreateAttribute( "XHTML", action, rockContext );
            }
        }

        private static void CreateAttribute( string name, WorkflowAction action, RockContext rockContext )
        {
            Rock.Model.Attribute newAttribute = new Rock.Model.Attribute();
            newAttribute.Key = name;
            newAttribute.Name = name;
            newAttribute.FieldTypeId = FieldTypeCache.Read( new Guid( Rock.SystemGuid.FieldType.TEXT ) ).Id;
            newAttribute.Order = 0;
            newAttribute.AttributeQualifiers.Add( new AttributeQualifier() { Key = "ispassword", Value = "False" } );
            newAttribute.EntityTypeId = EntityTypeCache.Read( action.Activity.Workflow.GetType() ).Id;
            newAttribute.EntityTypeQualifierColumn = "WorkflowTypeId";
            newAttribute.EntityTypeQualifierValue = action.Activity.Workflow.WorkflowType.Id.ToString();
            AttributeService attributeService = new AttributeService( rockContext );
            attributeService.Add( newAttribute );
            rockContext.SaveChanges();
            AttributeCache.FlushEntityAttributes();

            action.Activity.Workflow.LoadAttributes();
        }
    }
}
