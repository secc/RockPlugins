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
using System.IO;
using iText.Forms;
using iText.Kernel.Pdf;
using iText.Layout;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Lava;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Workflow;

namespace org.secc.PDF
{
    [ActionCategory( "PDF" )]
    [Description( "Merges" )]
    [Export( typeof( Rock.Workflow.ActionComponent ) )]
    [ExportMetadata( "ComponentName", "PDF Form Merge" )]

    //Settings
    [BinaryFileField( "D587ECCB-F548-452A-A442-FE383CBED283", "PDF Template", "PDF to merge information into" )]
    [WorkflowAttribute( "PDF Output", "Workflow attribute to output pdf into." )]
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
            BinaryFile renderedPDF = new BinaryFile();
            //Merge PDF
            using ( MemoryStream ms = new MemoryStream() )
            {
                var pdfGuid = GetAttributeValue( action, "PDFTemplate" );

                var pdf = new BinaryFileService( rockContext ).Get( pdfGuid.AsGuid() );

                var pdfBytes = pdf.ContentStream.ReadBytesToEnd();
                var pdfReader = new PdfReader( new MemoryStream( pdfBytes ) );
                var pdfWriter = new PdfWriter( ms );

                var pdfDocument = new PdfDocument( pdfReader, pdfWriter );
                var form = PdfAcroForm.GetAcroForm( pdfDocument, true );

                form.SetGenerateAppearance( true );

                var fieldKeys = form.GetFormFields().Keys;

                //Field keys are the names of form fields in a pdf form
                foreach ( string fieldKey in fieldKeys )
                {
                    //If this is a key value pairing
                    if ( pdfWorkflowObject.MergeObjects.ContainsKey( fieldKey ) )
                    {
                        if ( pdfWorkflowObject.MergeObjects[fieldKey] is string )
                        {
                            form.GetField( fieldKey ).SetValue( fieldKey, pdfWorkflowObject.MergeObjects[fieldKey] as string );
                        }
                    }
                    //otherwise test for lava and use the form value as the lava input
                    else
                    {
                        PdfObject fieldValuePdfObj = form.GetField( fieldKey ).GetValue();
                        string fieldValue = fieldValuePdfObj.ToString();
                        if ( !string.IsNullOrWhiteSpace( fieldValue ) && LavaHelper.IsLavaTemplate( fieldValue ) )
                            form.GetField( fieldKey ).SetValue( fieldKey, fieldValue.ResolveMergeFields( pdfWorkflowObject.MergeObjects ) );
                    }
                }

                //Should we flatten the form
                if ( GetActionAttributeValue( action, "Flatten" ).AsBoolean() )
                {
                    form.FlattenFields();
                }

                pdfDocument.Close();
                pdfReader.Close();
                pdfWriter.Close();

                //Generate New Object

                Guid guid = GetAttributeValue( action, "PDFOutput" ).AsGuid();
                AttributeCache attribute = null;
                var binaryFileTypeGuid = Rock.SystemGuid.BinaryFiletype.DEFAULT;
                if ( !guid.IsEmpty() )
                {
                    attribute = AttributeCache.Get( guid, rockContext );
                    if ( attribute != null
                        && attribute.QualifierValues.ContainsKey( "binaryFileType" )
                        && ( attribute.QualifierValues["binaryFileType"]?.Value ).IsNotNullOrWhiteSpace() )
                    {
                        binaryFileTypeGuid = attribute.QualifierValues["binaryFileType"].Value;
                    }

                }

                renderedPDF.MimeType = pdf.MimeType;
                renderedPDF.FileName = pdf.FileName;
                renderedPDF.IsTemporary = false;
                renderedPDF.Guid = Guid.NewGuid();
                renderedPDF.BinaryFileTypeId = new BinaryFileTypeService( rockContext ).Get( binaryFileTypeGuid.AsGuid() ).Id;
                renderedPDF.ContentStream = new MemoryStream( ms.ToArray() );
                pdfReader.Close();


                if ( entity is PDFWorkflowObject )
                {
                    entity = pdfWorkflowObject;
                }
                else
                {
                    BinaryFileService binaryFileService = new BinaryFileService( rockContext );
                    binaryFileService.Add( renderedPDF );
                    rockContext.SaveChanges();

                    if ( attribute != null )
                    {
                        SetWorkflowAttributeValue( action, guid, renderedPDF.Guid.ToString() );
                    }
                }
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
