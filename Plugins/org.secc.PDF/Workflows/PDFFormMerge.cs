using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Workflow;
using System.IO;
using iTextSharp.text.pdf;

namespace org.secc.PDF
{
    [ActionCategory( "PDF" )]
    [Description( "Merges" )]
    [Export( typeof( Rock.Workflow.ActionComponent ) )]
    [ExportMetadata( "ComponentName", "PDF Form Merge" )]

    //Settings
    [WorkflowTextOrAttribute( "Registration Registrant Id", "RegistrationRegistrantId" )]
    [BooleanField( "Flatten", "Should the action flatten the PDF locking the form fields" )]

    class PDFFormMerge : ActionComponent
    {
        /// <summary>
        /// Executes the action.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="action">The action.</param>
        /// <param name="entity">The entity.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns></returns>
        public override bool Execute( RockContext rockContext, WorkflowAction action, object entity, out List<string> errorMessages )
        {
            errorMessages = new List<string>();

            PDFWorkflowObject pdfWorkflowObject = new PDFWorkflowObject();

            //A PDF merge can enter in two ways, kicked off with trigger or called from a block
            //If it is called from a block we will get our information from a PDFWorkflowObject
            //Otherwise we will need to get our information from the workflow attributes
            if ( entity is PDFWorkflowObject )
            {
                 pdfWorkflowObject = Utility.GetPDFFormMergeFromEntity( entity, out errorMessages );
            }
            else
            {
                pdfWorkflowObject = new PDFWorkflowObject( action, rockContext );
            }

            //Merge PDF
            using ( MemoryStream ms = new MemoryStream() )
            {
                var pdfBytes = pdfWorkflowObject.PDF.ContentStream.ReadBytesToEnd();
                var pdfReader = new PdfReader( pdfBytes );

                var stamper = new PdfStamper( pdfReader, ms );

                var form = stamper.AcroFields;

                form.GenerateAppearances = true;

                var fieldKeys = form.Fields.Keys;

                //Field keys are the names of form fields in a pdf form
                foreach ( string fieldKey in fieldKeys )
                {
                    //If this is a key value pairing
                    if ( pdfWorkflowObject.MergeObjects.ContainsKey( fieldKey ) )
                    {
                        if ( pdfWorkflowObject.MergeObjects[fieldKey] is string )
                        {
                            form.SetField( fieldKey, pdfWorkflowObject.MergeObjects[fieldKey] as string );
                        }
                    }
                    //otherwise test for lava and use the form value as the lava input
                    else
                    {
                        string fieldValue = form.GetField( fieldKey );
                        if ( !string.IsNullOrWhiteSpace( fieldValue ) && fieldValue.HasMergeFields() )
                            form.SetField( fieldKey, fieldValue.ResolveMergeFields( pdfWorkflowObject.MergeObjects ) );
                    }
                }

                //Should we flatten the form
                stamper.FormFlattening = GetActionAttributeValue( action, "Flatten" ).AsBoolean();

                stamper.Close();
                pdfReader.Close();

                //Generate New Object
                
                BinaryFile renderedPDF = new BinaryFile();
                renderedPDF.CopyPropertiesFrom( pdfWorkflowObject.PDF );
                renderedPDF.Guid = Guid.NewGuid();
                renderedPDF.BinaryFileTypeId = new BinaryFileTypeService( rockContext ).Get( new Guid( Rock.SystemGuid.BinaryFiletype.DEFAULT ) ).Id;

                BinaryFileData pdfData = new BinaryFileData();
                pdfData.Content = ms.ToArray();

                renderedPDF.DatabaseData = pdfData;

                pdfWorkflowObject.PDF = renderedPDF;

                pdfReader.Close();
            }

            if (entity is PDFWorkflowObject )
            {
                entity = pdfWorkflowObject;
            }
            else
            {
                BinaryFileService binaryFileService = new BinaryFileService( rockContext );
                binaryFileService.Add( pdfWorkflowObject.PDF );
                rockContext.SaveChanges();
                action.Activity.Workflow.SetAttributeValue( "PDFGuid", pdfWorkflowObject.PDF.Guid );
            }

            return true;
        }

        public static byte[] ReadFully( Stream input )
        {
            byte[] buffer = new byte[16 * 1024];
            using ( MemoryStream ms = new MemoryStream() )
            {
                int read;
                while ( ( read = input.Read( buffer, 0, buffer.Length ) ) > 0 )
                {
                    ms.Write( buffer, 0, read );
                }
                return ms.ToArray();
            }
        }
    }
}
