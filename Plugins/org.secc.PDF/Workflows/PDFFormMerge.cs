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

    public class PDFFormMerge : ActionComponent
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

            try
            {
                var pdfWorkflowObject = ResolvePdfWorkflowObject( entity, action, rockContext, out errorMessages );
                if ( pdfWorkflowObject == null )
                {
                    return false;
                }

                var pdfTemplateGuid = GetAttributeValue( action, "PDFTemplate" ).AsGuid();
                var flatten = GetActionAttributeValue( action, "Flatten" ).AsBoolean();
                var outputAttributeGuid = GetAttributeValue( action, "PDFOutput" ).AsGuid();

                BinaryFile renderedPDF = MergePdfTemplate( pdfTemplateGuid, pdfWorkflowObject, flatten, rockContext );

                if ( entity is PDFWorkflowObject )
                {
                    entity = pdfWorkflowObject;
                }
                else
                {
                    SaveAndAssignPdf( renderedPDF, outputAttributeGuid, action, rockContext );
                }

                return true;
            }
            catch ( Exception ex )
            {
                errorMessages.Add( ex.Message );
                return false;
            }
        }

        /// <summary>
        /// Extracted for testability: resolves the <see cref="PDFWorkflowObject"/> from the
        /// entity (block-triggered path) or from the workflow action attributes (trigger path).
        /// </summary>
        protected virtual PDFWorkflowObject ResolvePdfWorkflowObject( object entity, WorkflowAction action, RockContext rockContext, out List<string> errorMessages )
        {
            if ( entity is PDFWorkflowObject )
            {
                return Utility.GetPDFFormMergeFromEntity( entity, out errorMessages );
            }

            errorMessages = new List<string>();
            return new PDFWorkflowObject( action, rockContext );
        }

        /// <summary>
        /// Extracted for testability: loads the PDF template bytes from Rock and delegates
        /// the actual iText merge to <see cref="ApplyMergeFields"/>.
        /// </summary>
        protected virtual BinaryFile MergePdfTemplate( Guid templateGuid, PDFWorkflowObject pdfWorkflowObject, bool flatten, RockContext rockContext )
        {
            var pdf = new BinaryFileService( rockContext ).Get( templateGuid );
            var pdfBytes = pdf.ContentStream.ReadBytesToEnd();
            var mergedBytes = ApplyMergeFields( pdfBytes, pdfWorkflowObject, flatten );

            return new BinaryFile
            {
                MimeType = pdf.MimeType,
                FileName = pdf.FileName,
                IsTemporary = false,
                Guid = Guid.NewGuid(),
                ContentStream = new MemoryStream( mergedBytes )
            };
        }

        /// <summary>
        /// Extracted for testability: performs the iText AcroForm field merge against raw PDF bytes.
        /// Has no Rock infrastructure dependencies and can be called directly in component tests.
        /// </summary>
        protected virtual byte[] ApplyMergeFields( byte[] pdfBytes, PDFWorkflowObject pdfWorkflowObject, bool flatten )
        {
            using ( var ms = new MemoryStream() )
            {
                var pdfReader = new PdfReader( new MemoryStream( pdfBytes ) );
                var pdfWriter = new PdfWriter( ms );
                var pdfDocument = new PdfDocument( pdfReader, pdfWriter );
                var form = PdfAcroForm.GetAcroForm( pdfDocument, true );

                form.SetGenerateAppearance( true );

                foreach ( string fieldKey in form.GetAllFormFields().Keys )
                {
                    if ( pdfWorkflowObject.MergeObjects.ContainsKey( fieldKey ) )
                    {
                        if ( pdfWorkflowObject.MergeObjects[fieldKey] is string )
                        {
                            form.GetField( fieldKey ).SetValue( pdfWorkflowObject.MergeObjects[fieldKey] as string );
                        }
                    }
                    else
                    {
                        iText.Kernel.Pdf.PdfObject fieldValuePdfObj = form.GetField( fieldKey ).GetValue();
                        string fieldValue = fieldValuePdfObj?.ToString();
                        if ( !string.IsNullOrWhiteSpace( fieldValue ) && LavaHelper.IsLavaTemplate( fieldValue ) )
                        {
                            form.GetField( fieldKey ).SetValue( fieldValue.ResolveMergeFields( pdfWorkflowObject.MergeObjects ) );
                        }
                    }
                }

                if ( flatten )
                {
                    form.FlattenFields();
                }

                pdfDocument.Close();
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Extracted for testability: assigns the correct binary file type, persists the merged PDF,
        /// and sets the workflow attribute value.
        /// </summary>
        protected virtual void SaveAndAssignPdf( BinaryFile renderedPDF, Guid outputAttributeGuid, WorkflowAction action, RockContext rockContext )
        {
            AttributeCache attribute = null;
            var binaryFileTypeGuid = Rock.SystemGuid.BinaryFiletype.DEFAULT;

            if ( !outputAttributeGuid.IsEmpty() )
            {
                attribute = AttributeCache.Get( outputAttributeGuid, rockContext );
                if ( attribute != null
                    && attribute.QualifierValues.ContainsKey( "binaryFileType" )
                    && ( attribute.QualifierValues["binaryFileType"]?.Value ).IsNotNullOrWhiteSpace() )
                {
                    binaryFileTypeGuid = attribute.QualifierValues["binaryFileType"].Value;
                }
            }

            renderedPDF.BinaryFileTypeId = new BinaryFileTypeService( rockContext ).Get( binaryFileTypeGuid.AsGuid() ).Id;

            var binaryFileService = new BinaryFileService( rockContext );
            binaryFileService.Add( renderedPDF );
            rockContext.SaveChanges();

            if ( attribute != null )
            {
                SetWorkflowAttributeValue( action, outputAttributeGuid, renderedPDF.Guid.ToString() );
            }
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
