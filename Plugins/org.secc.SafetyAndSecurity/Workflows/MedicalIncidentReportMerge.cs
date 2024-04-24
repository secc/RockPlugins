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
    [Description( "Merge the final PDF in the medical incident report." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Medical Incident Report Merge" )]
    [BinaryFileField( MedicalIncidentReportMerge.PDF_FORM_BINARY_FILE_TYPE, "Medical Incident Report PDF", "The confidential medical incident report PDF form", true )]
    [BinaryFileTypeField( "Binary File Type", "The Guid for the medical incident document binary file type", true, "E68932AD-9A95-4C3B-87BA-1E6B998BCB46", key: "binaryFileType" )]

    class MedicalIncidentReportMerge : ActionComponent
    {
        public const string PDF_FORM_BINARY_FILE_TYPE = "D587ECCB-F548-452A-A442-FE383CBED283";

        public override bool Execute( RockContext rockContext, WorkflowAction action, object entity, out List<string> errorMessages )
        {
            errorMessages = new List<string>();

            Dictionary<string, string> fields = new Dictionary<string, string>()
            {
                {"campusLoc", action.Activity.Workflow.GetAttributeValue("campusLoc")},
                {"reportPerson", action.Activity.Workflow.GetAttributeValue("reportPerson")},
                {"reportPersonPhone", action.Activity.Workflow.GetAttributeValue("reportPersonPhone")},
                {"incidentPerson", action.Activity.Workflow.GetAttributeValue("incidentPerson")},
                {"age", action.Activity.Workflow.GetAttributeValue("age")},
                {"sex", action.Activity.Workflow.GetAttributeValue("sex")},
                {"parentName", action.Activity.Workflow.GetAttributeValue("parentName")},
                {"datetimeNotified", action.Activity.Workflow.GetAttributeValue("datetimeNotified")},
                {"address", action.Activity.Workflow.GetAttributeValue("address")},
                {"parentPhone", action.Activity.Workflow.GetAttributeValue("parentPhone")},
                {"datetimeIncident", action.Activity.Workflow.GetAttributeValue("datetimeIncident")},
                {"locationIncident", action.Activity.Workflow.GetAttributeValue("locationIncident")},
                {"witness", action.Activity.Workflow.GetAttributeValue("witness").Replace("^", " - ")},
                {"emsCalled", action.Activity.Workflow.GetAttributeValue("emsCalled").AsBoolean()?"Yes":"No"},
                {"timeCalled", action.Activity.Workflow.GetAttributeValue("timeCalled")},
                {"timeArrived", action.Activity.Workflow.GetAttributeValue("timeArrived")},
                {"emsHospital", action.Activity.Workflow.GetAttributeValue("emsHospital").AsBoolean()?"Yes":"No"},
                {"hospital", action.Activity.Workflow.GetAttributeValue("hospital")},
                {"emsRefusalWitness", action.Activity.Workflow.GetAttributeValue("emsRefusalWitness").Replace("^", " - ")},
                {"treatment", action.Activity.Workflow.GetAttributeValue("treatment")},
                {"pulseRate", action.Activity.Workflow.GetAttributeValue("pulseRate")},
                {"O2Oxygen", action.Activity.Workflow.GetAttributeValue("O2Oxygen")},
                {"respirations", action.Activity.Workflow.GetAttributeValue("respirations")},
                {"bloodPressure", action.Activity.Workflow.GetAttributeValue("bloodPressure")},
                {"bloodSugar", action.Activity.Workflow.GetAttributeValue("bloodSugar")},
                {"knownMedical", action.Activity.Workflow.GetAttributeValue("knownMedical")},
                {"envFactors", action.Activity.Workflow.GetAttributeValue("envFactors")},
                {"incidentDesc", action.Activity.Workflow.GetAttributeValue("incidentDesc")},

                {"personSignature", "{{t:s;r:y;o:\"Person\";}}" },
                {"personSigDatetime", "{{t:d;r:y;o:\"Person\";l:\"Date\";dd:\""+DateTime.Now.ToShortDateString()+"\";}}" }
            };

            BinaryFileService binaryFileService = new BinaryFileService( rockContext );
            BinaryFile PDF = binaryFileService.Get( GetActionAttributeValue( action, "MedicalIncidentReportPDF" ).AsGuid() );

            var pdfBytes = PDF.ContentStream.ReadBytesToEnd();

            using ( MemoryStream ms = new MemoryStream() )
            {
                PdfReader pdfReader = new PdfReader( pdfBytes );
                PdfStamper pdfStamper = new PdfStamper( pdfReader, ms );

                AcroFields pdfFormFields = pdfStamper.AcroFields;


                foreach ( var field in fields )
                {
                    if ( pdfFormFields.Fields.ContainsKey( field.Key ) )
                    {
                        if ( field.Key == "personSignature" || field.Key == "personSigDatetime" )
                        {
                            if ( action.Activity.Workflow.GetAttributeValue( "emsHospital" ).AsBoolean() )
                            {
                                pdfFormFields.SetField( field.Key, "" );
                            }
                            else
                            {
                                pdfFormFields.SetField( field.Key, field.Value );
                            }
                        }
                        else
                        {
                            pdfFormFields.SetField( field.Key, field.Value );
                        }
                    }   
                }
                    

                // flatten the form to remove editting options, set it to false
                // to leave the form open to subsequent manual edits
                pdfStamper.FormFlattening = true;

                // close the pdf
                pdfStamper.Close();
                //pdfReader.Close();
                pdfStamper.Dispose();
                pdfStamper = null;

                BinaryFile renderedPDF = new BinaryFile
                {
                    IsTemporary = false,
                    IsSystem = false,
                    Guid = Guid.NewGuid(),
                    MimeType = PDF.MimeType,
                    FileName = "MedicalIncidentReport_" + action.Activity.Workflow.GetAttributeValue( "reportPerson" ) + ".pdf",
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
