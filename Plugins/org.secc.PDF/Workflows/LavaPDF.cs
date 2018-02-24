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
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Workflow;
using iTextSharp.text.pdf;
using System.IO;
using System.ComponentModel.Composition;
using iTextSharp.text;
using iTextSharp.text.html.simpleparser;
using iTextSharp.tool.xml;

namespace org.secc.PDF
{
    [ActionCategory( "PDF" )]
    [Description( "Creates pdf from lava" )]
    [Export( typeof( Rock.Workflow.ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Lava PDF" )]
    class LavaPDF : ActionComponent
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

            using ( MemoryStream msPDF = createPDF( pdfWorkflowObject.RenderedXHTML ) )
            {
                BinaryFile pdfBinary = new BinaryFile();
                pdfBinary.Guid = Guid.NewGuid();
                pdfBinary.FileName = "GeneratedPDF.pdf";
                pdfBinary.MimeType = "application/pdf";
                pdfBinary.BinaryFileTypeId = new BinaryFileTypeService( rockContext ).Get( new Guid( Rock.SystemGuid.BinaryFiletype.DEFAULT ) ).Id;

                BinaryFileData pdfData = new BinaryFileData();
                pdfData.Content = msPDF.ToArray();
                
                pdfBinary.DatabaseData = pdfData;

                pdfWorkflowObject.PDF = pdfBinary;
            }

            if ( entity is PDFWorkflowObject )
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
        private MemoryStream createPDF( string xhtml )
        {
            MemoryStream msOutput = new MemoryStream();
            TextReader xhtmlReader = new StringReader( xhtml );

            using ( Document document = new Document( PageSize.LETTER ) )
            {
                PdfWriter writer = PdfWriter.GetInstance( document, msOutput );
                document.Open();

                XMLWorkerHelper.GetInstance().ParseXHtml( writer, document, xhtmlReader );

                document.Close();
            }

            return msOutput;
        }

    }
}
