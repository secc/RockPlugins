using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Workflow;

namespace org.secc.Workflow.WorkflowAttributes
{
    [ActionCategory( "SECC > Workflow Attributes" )]
    [Description( "Saves a Base64 encoded string as a Binary File." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Binary File from Base64 String" )]

    [WorkflowTextOrAttribute( "FileName", "File Name Attribute", "The name of the file that is being saved as a Binary File.", true,
        Order = 0, Key = FILE_NAME_KEY, FieldTypeClassNames = new string[] { "Rock.Field.Types.TextFieldType" } )]
    [WorkflowTextOrAttribute( "Mime Type", "Mime Type", "The Mime Type of the Binary File", true, Order = 1,
        Key = MIME_TYPE_KEY, FieldTypeClassNames = new string[] { "Rock.Field.Types.TextFieldType" } )]
    [WorkflowAttribute( "Binary File Type", "The Binary File Type of the File that is being saved.", true, "c1142570-8cd6-4a20-83b1-acb47c1cd377",
        Order = 2, Key = BINARY_FILE_TYPE_KEY, FieldTypeClassNames = new string[] { "Rock.Field.Types.BinaryFileTypeFieldType" } )]
    [WorkflowAttribute( "Base 64 String", "The file's Base64 string", true, Order = 3, Key = BASE64_KEY, FieldTypeClassNames = new string[] { "Rock.Field.Types.TextFieldType" } )]
    [WorkflowAttribute( "Binary File", "The Binary file that is output from the base 64 string.", true, Order = 4, Key = BINARY_FILE_KEY, FieldTypeClassNames = new string[] { "Rock.Field.Types.BinaryFileFieldType" } )]
    public class BinaryFileFromBase64String : ActionComponent
    {

        private const string FILE_NAME_KEY = "FileName";
        private const string MIME_TYPE_KEY = "MimeType";
        private const string BINARY_FILE_TYPE_KEY = "BinaryFileType";
        private const string BASE64_KEY = "Base64String";
        private const string BINARY_FILE_KEY = "BinaryFile";


        public override bool Execute( RockContext rockContext, WorkflowAction action, object entity, out List<string> errorMessages )
        {
            errorMessages = new List<string>();
            var binaryFileAttribute = AttributeCache.Get( GetAttributeValue( action, BINARY_FILE_KEY ).AsGuid(), rockContext );

            if (binaryFileAttribute == null)
            {
                errorMessages.Add( "Binary File Attribute could not be found." );
                errorMessages.ForEach( m => action.AddLogEntry( m, true ) );
                return false;
            }

            var fileName = GetAttributeValue( action, FILE_NAME_KEY, true );
            var mimeType = GetAttributeValue( action, MIME_TYPE_KEY, true );
            var binaryFileType = BinaryFileTypeCache.Get( GetAttributeValue( action, BINARY_FILE_TYPE_KEY, true ).AsGuid() );
            var fileString = GetAttributeValue( action, BASE64_KEY, true );


            if(binaryFileType == null)
            {
                errorMessages.Add( "Binary File Type could not be found." );
            }
            if(string.IsNullOrEmpty(mimeType))
            {
                errorMessages.Add( "Mime Type is required." );
            }
            if(string.IsNullOrEmpty(fileString))
            {
                errorMessages.Add( "Base64 string is required." );
            }

            byte[] fileByteArray = null;
            try
            {
                fileByteArray = Convert.FromBase64String( fileString );
            }
            catch(Exception ex)
            {
                errorMessages.Add( $"Could not convert file string to byte array., Reason: {ex.Message}" );
            }

            if(errorMessages.Any())
            {
                errorMessages.ForEach( m => action.AddLogEntry( m, true ) );
                return false;
            }

            var binaryFileService = new BinaryFileService( rockContext );
            var binaryFile = new BinaryFile();

            binaryFile.MimeType = mimeType;
            binaryFile.FileName = fileName;
            binaryFile.IsTemporary = false;
            binaryFile.Guid = Guid.NewGuid();
            binaryFile.BinaryFileTypeId = binaryFileType.Id;
            binaryFile.ContentStream = new MemoryStream( fileByteArray );

            binaryFileService.Add( binaryFile );
            rockContext.SaveChanges();

            SetWorkflowAttributeValue( action, binaryFileAttribute.Guid, binaryFile.Guid.ToString() );


            return true;

        }
    }
}
