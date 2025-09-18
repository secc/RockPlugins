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
using Rock.Model;
using Rock.Workflow;

namespace org.secc.SafetyAndSecurity
{
    [ActionCategory( "SECC > Safety and Security" )]
    [Description( "Merge the final PDF in the external church reference process." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "External Church Reference Merge" )]
    [BinaryFileField( ExternalChurchReferenceMerge.PDF_FORM_BINARY_FILE_TYPE, "External Church Reference PDF", "The External Church Reference PDF form", true )]
    [BinaryFileTypeField("Binary File Type","The Guid for the reference document binary file type", true, "A72B7149-7161-4BB2-B6E4-BE2565AFE763", key:"binaryFileType" )]

    class ExternalChurchReferenceMerge : ActionComponent
    {
        
        public const string PDF_FORM_BINARY_FILE_TYPE = "D587ECCB-F548-452A-A442-FE383CBED283";

        public override bool Execute( RockContext rockContext, WorkflowAction action, object entity, out List<string> errorMessages )
        {
            errorMessages = new List<string>();
            
            PersonAliasService personAliasService = new PersonAliasService( rockContext );
            Person person = personAliasService.Get( action.Activity.Workflow.GetAttributeValue( "Person" ).AsGuid() ).Person;

            Dictionary<string, string> fields = new Dictionary<string, string>()
            {
                {"txtPersonName", action.Activity.Workflow.GetAttributeValue("PersonFullName")},
                {"txtMinistryOfInterest", action.Activity.Workflow.GetAttributeValue("MinistryOfInterest") },

                {"txtChurchName", action.Activity.Workflow.GetAttributeValue("ReferenceChurch")},
                {"txtNameAndTitle", string.Concat(
                        action.Activity.Workflow.GetAttributeValue("ReferenceName"), ", ",
                        action.Activity.Workflow.GetAttributeValue("ReferenceTitle"))},
                {"txtMinistryServed", action.Activity.Workflow.GetAttributeValue("PreviousMinistryServed")},
                {"referenceTimeServed", action.Activity.Workflow.GetAttributeValue("ReferenceTimeServed")},
                {"txtComments", action.Activity.Workflow.GetAttributeValue("Comments")},

                {"txtReferencePhone", action.Activity.Workflow.GetAttributeValue("ReferenceCellPhone")},
                {"txtReferenceEmail", action.Activity.Workflow.GetAttributeValue("ReferenceEmail")},

                {"txtSignature", "{{t:s;r:y;o:\"Referrer\";}}" },
                {"txtDate", "{{t:d;r:y;o:\"Referrer\";l:\"Date\";dd:\""+DateTime.Now.ToShortDateString()+"\";}}" }
            };

            BinaryFileService binaryFileService = new BinaryFileService( rockContext );

            BinaryFile PDF = binaryFileService.Get( GetActionAttributeValue( action, "ExternalChurchReferencePDF" ).AsGuid() );

            var pdfBytes = PDF.ContentStream.ReadBytesToEnd();

            using ( MemoryStream ms = new MemoryStream() )
            {
                PdfReader pdfReader = new PdfReader( new MemoryStream( pdfBytes ) );
                var pdfWriter = new PdfWriter( ms );

                var pdfDocument = new PdfDocument( pdfReader, pdfWriter );
                var form = PdfAcroForm.GetAcroForm( pdfDocument, true );

                var pdfFormFields = form.GetFormFields();


                foreach (var field in fields)
                    if (pdfFormFields.ContainsKey( field.Key ))
                    {
                        form.GetField( field.Key ).SetValue( field.Key, field.Value );
                    }

                form.FlattenFields();
                

                // close the pdf
                pdfDocument.Close();
                pdfReader.Close();
                pdfWriter.Close();
                pdfDocument = null;

                BinaryFile renderedPDF = new BinaryFile
                {
                    IsTemporary = false,
                    IsSystem = false,
                    Guid = Guid.NewGuid(),
                    MimeType = PDF.MimeType,
                    FileName = "ExternalChurchReference_" + person.FirstName + person.LastName + ".pdf",
                    BinaryFileTypeId = new BinaryFileTypeService( rockContext ).Get( new Guid( GetActionAttributeValue( action, "binaryFileType" ) ) ).Id,
                    DatabaseData = null
                };

                var bytes = ms.ToArray();
                renderedPDF.FileSize = bytes.Length;
                renderedPDF.ContentStream = new MemoryStream( bytes );

                binaryFileService.Add( renderedPDF );
                rockContext.SaveChanges();

                action.Activity.Workflow.SetAttributeValue( "PDF", renderedPDF.Guid );
            }

            return true;

        }
    }
}
