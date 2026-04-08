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

            PDFWorkflowObject pdfWorkflowObject;
            if ( entity is PDFWorkflowObject )
            {
                pdfWorkflowObject = Utility.GetPDFFormMergeFromEntity( entity, out errorMessages );
            }
            else
            {
                pdfWorkflowObject = new PDFWorkflowObject( action, rockContext );
            }

            if ( pdfWorkflowObject == null )
            {
                errorMessages.Add( "PDF form merge input could not be created." );
                return false;
            }

            if ( pdfWorkflowObject.MergeObjects == null )
            {
                pdfWorkflowObject.MergeObjects = new Dictionary<string, object>();
            }

            var pdfGuidValue = GetAttributeValue( action, "PDFTemplate" );
            var pdfGuid = pdfGuidValue.AsGuid();
            if ( pdfGuid == Guid.Empty )
            {
                errorMessages.Add( "PDF Template is not configured on the workflow action." );
                return false;
            }

            var pdf = new BinaryFileService( rockContext ).Get( pdfGuid );
            if ( pdf == null )
            {
                errorMessages.Add( "PDF Template binary file was not found. Guid: " + pdfGuid );
                return false;
            }

            if ( pdf.ContentStream == null )
            {
                errorMessages.Add( "PDF Template content stream is empty or unavailable. Guid: " + pdfGuid );
                return false;
            }

            var pdfBytes = pdf.ContentStream.ReadBytesToEnd();
            if ( pdfBytes == null || pdfBytes.Length == 0 )
            {
                errorMessages.Add( "PDF Template has no content. Guid: " + pdfGuid );
                return false;
            }

            BinaryFile renderedPDF = new BinaryFile();

            using ( MemoryStream ms = new MemoryStream() )
            using ( var pdfReader = new PdfReader( new MemoryStream( pdfBytes ) ) )
            using ( var pdfWriter = new PdfWriter( ms ) )
            using ( var pdfDocument = new PdfDocument( pdfReader, pdfWriter ) )
            {
                var form = PdfAcroForm.GetAcroForm( pdfDocument, true );
                if ( form == null )
                {
                    errorMessages.Add( "PDF form could not be loaded for template Guid: " + pdfGuid );
                    return false;
                }

                form.SetGenerateAppearance( true );

                var formFields = form.GetFormFields();
                if ( formFields != null )
                {
                    foreach ( string fieldKey in formFields.Keys )
                    {
                        var field = form.GetField( fieldKey );
                        if ( field == null )
                        {
                            continue;
                        }

                        object mergeValue;
                        if ( pdfWorkflowObject.MergeObjects.TryGetValue( fieldKey, out mergeValue ) )
                        {
                            if ( mergeValue is string )
                            {
                                field.SetValue( fieldKey, mergeValue as string );
                            }
                        }
                        else
                        {
                            PdfObject fieldValuePdfObj = field.GetValue();
                            string fieldValue = fieldValuePdfObj != null ? fieldValuePdfObj.ToString() : string.Empty;

                            if ( !string.IsNullOrWhiteSpace( fieldValue ) && LavaHelper.IsLavaTemplate( fieldValue ) )
                            {
                                field.SetValue( fieldKey, fieldValue.ResolveMergeFields( pdfWorkflowObject.MergeObjects ) );
                            }
                        }
                    }
                }

                if ( GetActionAttributeValue( action, "Flatten" ).AsBoolean() )
                {
                    form.FlattenFields();
                }

                pdfDocument.Close();

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

                var binaryFileType = new BinaryFileTypeService( rockContext ).Get( binaryFileTypeGuid.AsGuid() );
                if ( binaryFileType == null )
                {
                    errorMessages.Add( "Resolved BinaryFileType was not found for Guid: " + binaryFileTypeGuid );
                    return false;
                }

                renderedPDF.MimeType = pdf.MimeType;
                renderedPDF.FileName = pdf.FileName;
                renderedPDF.IsTemporary = false;
                renderedPDF.Guid = Guid.NewGuid();
                renderedPDF.BinaryFileTypeId = binaryFileType.Id;
                renderedPDF.ContentStream = new MemoryStream( ms.ToArray() );

                if ( entity is PDFWorkflowObject )
                {
                    pdfWorkflowObject.PDF = renderedPDF;
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
