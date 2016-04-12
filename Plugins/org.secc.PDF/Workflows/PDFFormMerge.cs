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

            PDFWorkflowObject pdfWorkflowObject = GetPDFFormMergeFromEntity( entity, out errorMessages );

            if ( pdfWorkflowObject == null )
            {
                return false;
            }

            var pdf = pdfWorkflowObject.PDFInput;

            using ( MemoryStream ms = new MemoryStream() )
            {
                var pdfBytes = pdf.ContentStream.ReadBytesToEnd();
                var pdfReader = new PdfReader( pdfBytes );

                var stamper = new PdfStamper( pdfReader, ms );

                var form = stamper.AcroFields;

                form.GenerateAppearances = true;

                var fieldKeys = form.Fields.Keys;

                foreach ( string fieldKey in fieldKeys )
                {
                    if ( pdfWorkflowObject.MergeObjects.ContainsKey( fieldKey ) )
                    {
                        if ( pdfWorkflowObject.MergeObjects[fieldKey] is string )
                        {
                            form.SetField( fieldKey, pdfWorkflowObject.MergeObjects[fieldKey] as string );
                        }
                        else
                        {
                            errorMessages.Add( "Merge object is not string. Cannot insert non strings into form fields." );
                            return false;
                        }
                    }
                }

                //Should we flatten the form
                stamper.FormFlattening = GetActionAttributeValue( action, "Flatten" ).AsBoolean();

                stamper.Close();
                pdfReader.Close();

                BinaryFile renderedPDF = new BinaryFile();
                renderedPDF.CopyPropertiesFrom( pdf );
                renderedPDF.Guid = Guid.NewGuid();
                renderedPDF.BinaryFileTypeId = new BinaryFileTypeService( rockContext ).Get( new Guid( Rock.SystemGuid.BinaryFiletype.DEFAULT ) ).Id;

                BinaryFileData pdfData = new BinaryFileData();
                pdfData.Content = ms.ToArray();

                renderedPDF.DatabaseData = pdfData;

                pdfWorkflowObject.RenderedPDF = renderedPDF;

                pdfReader.Close();
            }

            return true;
        }

        private PDFWorkflowObject GetPDFFormMergeFromEntity( object entity, out List<string> errorMessages )
        {
            errorMessages = new List<string>();

            if ( entity is PDFWorkflowObject )
            {
                return ( PDFWorkflowObject ) entity;
            }

            errorMessages.Add( "Could not get PDFFormMergeEntity object" );
            return null;
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
