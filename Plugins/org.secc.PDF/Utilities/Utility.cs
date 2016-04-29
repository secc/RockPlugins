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
