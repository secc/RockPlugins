using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Workflow;


namespace org.secc.Workflow.Media
{
    [ExportMetadata( "ComponentName", "Binary File Delete" )]
    [ActionCategory( "SECC > Media" )]
    [Description( "Removes the Binary File." )]
    [Export( typeof( ActionComponent ) )]
    [WorkflowAttribute( "File to Delete",
        Description = "The file that you would like to delete.",
        IsRequired = true,
        FieldTypeClassNames = new string[] { "Rock.Field.Types.FileFieldType" },
        Key = "FileToDelete",
        Order = 0 )]
    public class BinaryFileRemove : ActionComponent
    {
        public override bool Execute( RockContext rockContext, WorkflowAction action, object entity, out List<string> errorMessages )
        {
            errorMessages = new List<string>();
            var fileAttributeGuid = GetAttributeValue( action, "FileToDelete" ).AsGuid();
            AttributeCache fileAttribute = AttributeCache.Get( fileAttributeGuid, rockContext );
            if (fileAttribute == null)
            {
                errorMessages.Add( "File not set." );
                return false;
            }

            var fileguid = action.GetWorkflowAttributeValue( fileAttribute.Guid ).AsGuid();

            var binaryFileService = new BinaryFileService( rockContext );
            var file = binaryFileService.Get( fileguid );

            if (file != null)
            {
                binaryFileService.Delete( file );
                rockContext.SaveChanges();
            }

            return true;

        }
    }
}
