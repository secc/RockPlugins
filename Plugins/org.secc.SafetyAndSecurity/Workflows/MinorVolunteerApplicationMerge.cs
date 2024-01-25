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
using iTextSharp.text.pdf;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Workflow;

namespace org.secc.SafetyAndSecurity
{
    [ActionCategory( "SECC > Safety and Security" )]
    [Description( "Merge the final PDF in the minor volunteer application (background check)." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Minor Volunteer Application Merge" )]
    [BinaryFileField( MinorVolunteerApplicationMerge.PDF_FORM_BINARY_FILE_TYPE, "Minor Volunteer Application PDF", "The Confidential Volunteer Application for Minors PDF form", true )]
    
    class MinorVolunteerApplicationMerge : ActionComponent
    {
        public const string BACKGROUND_CHECK_BINARY_FILE_TYPE = "5C701472-8A6B-4BBE-AEC6-EC833C859F2D";
        public const string PDF_FORM_BINARY_FILE_TYPE = "D587ECCB-F548-452A-A442-FE383CBED283";

        public override bool Execute( RockContext rockContext, WorkflowAction action, object entity, out List<string> errorMessages )
        {
            errorMessages = new List<string>();

            PersonAliasService personAliasService = new PersonAliasService( rockContext );
            Person person = personAliasService.Get( action.Activity.Workflow.GetAttributeValue( "Person" ).AsGuid() ).Person;

            LocationService locationService = new LocationService( rockContext );
            Location currentMailingAddress = locationService.Get( action.Activity.Workflow.GetAttributeValue( "CurrentMailingAddress" ).AsGuid() );
            Location reference1Address = locationService.Get( action.Activity.Workflow.GetAttributeValue( "Reference1Address" ).AsGuid() );

            Dictionary<string, string> fields = new Dictionary<string, string>()
                {

                    {"txtVolunteerName", string.Concat(
                        action.Activity.Workflow.GetAttributeValue("FirstName"), ' ',
                        action.Activity.Workflow.GetAttributeValue("MiddleName"), ' ',
                        action.Activity.Workflow.GetAttributeValue("LastName"))},
                    {"txtDateOfBirth", action.Activity.Workflow.GetAttributeValue("DateofBirth").AsDateTime().Value.ToShortDateString()},
                    {"txtCurrentAddress", string.Concat(
                        currentMailingAddress.Street1, ', ', 
                        currentMailingAddress.City, ', ', 
                        currentMailingAddress.State, ' ', 
                        currentMailingAddress.PostalCode)},
                                        
                    {"ministryOfInterest", action.Activity.Workflow.GetAttributeValue("MinistryOfInterest") },

                    {"txtParentSignature", "{{t:s;r:y;o:\"Parent\";}}" },
                    {"txtDate", "{{t:d;r:y;o:\"Parent\";l:\"Date\";dd:\""+DateTime.Now.ToShortDateString()+"\";}}" },
                    {"txtParentName", action.Activity.Workflow.GetAttributeValue("Parent")},
                    {"txtParentPhone", action.Activity.Workflow.GetAttributeValue("ParentCellPhone")},
                    {"txtParentEmail", action.Activity.Workflow.GetAttributeValue("ParentEmail")},                    

                    {"txtRef1Name", action.Activity.Workflow.GetAttributeValue("Reference1Name")},
                    {"txtRef1Address", string.Concat(
                        reference1Address.Street1, ', ', 
                        reference1Address.City, ', ', 
                        reference1Address.State, ' ', 
                        reference1Address.PostalCode)},
                    {"txtRef1Email", action.Activity.Workflow.GetAttributeValue("Reference1Email")}, 
                    {"txtRef1Phone", action.Activity.Workflow.GetAttributeValue("Reference1CellPhone")},
                    {"txtRef1Relationship", action.Activity.Workflow.GetAttributeValue("Reference1Relationship")},
                    {"radRef1MonthsKnown", action.Activity.Workflow.GetAttributeValue("Reference1MonthsKnown")},
                    {"txtRef1Staff", action.Activity.Workflow.GetAttributeValue("Reference1Staff").AsBoolean()?"Yes":"No"}             

                };

            BinaryFileService binaryFileService = new BinaryFileService( rockContext );
            BinaryFile PDF = null;
            
            PDF = binaryFileService.Get( GetActionAttributeValue( action, "MinorVolunteerApplicationPDF" ).AsGuid() );
            
            var pdfBytes = PDF.ContentStream.ReadBytesToEnd();

            using ( MemoryStream ms = new MemoryStream() )
            {
                PdfReader pdfReader = new PdfReader( pdfBytes );
                PdfStamper pdfStamper = new PdfStamper( pdfReader, ms );

                AcroFields pdfFormFields = pdfStamper.AcroFields;


                foreach ( var field in fields )
                    if ( pdfFormFields.Fields.ContainsKey( field.Key ) )
                        pdfFormFields.SetField( field.Key, field.Value );

                // flatten the form to remove editting options, set it to false
                // to leave the form open to subsequent manual edits
                pdfStamper.FormFlattening = true;

                // close the pdf
                pdfStamper.Close();
                //pdfReader.Close();
                pdfStamper.Dispose();
                pdfStamper = null;

                BinaryFile renderedPDF = new BinaryFile();
                renderedPDF.IsTemporary = false;
                renderedPDF.IsSystem = false;
                renderedPDF.Guid = Guid.NewGuid();
                renderedPDF.MimeType = PDF.MimeType;
                renderedPDF.FileName = "MinorVolunteerApplication_" + person.FirstName + person.LastName + ".pdf";
                renderedPDF.BinaryFileTypeId = new BinaryFileTypeService( rockContext ).Get( new Guid( BACKGROUND_CHECK_BINARY_FILE_TYPE ) ).Id;
                renderedPDF.DatabaseData = null;

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
