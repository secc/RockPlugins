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
    [Description( "Merge the final PDF in the digital incident report." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Digital Incident Report Merge" )]
    [BinaryFileField( DigitalIncidentReportMerge.PDF_FORM_BINARY_FILE_TYPE, "Digital Incident Report PDF", "The confidential digital incident report PDF form", true )]
    [BinaryFileTypeField( "Binary File Type", "The Guid for the digital incident document binary file type", true, "9510D232-FD8A-4243-B3DC-CFA00BF55CE4", key: "binaryFileType" )]

    class DigitalIncidentReportMerge : ActionComponent
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
                {"address", action.Activity.Workflow.GetAttributeValue("address")},
                {"personPhone", action.Activity.Workflow.GetAttributeValue("personPhone")},
                {"datetimeIncident", action.Activity.Workflow.GetAttributeValue("datetimeIncident")},
                {"locationIncident", action.Activity.Workflow.GetAttributeValue("locationIncident")},
                {"witness", action.Activity.Workflow.GetAttributeValue("witness").Replace("^", " - ")},
                {"safety", action.Activity.Workflow.GetAttributeValue("safety").AsBoolean()?"X":""},
                {"cpsCalled", action.Activity.Workflow.GetAttributeValue("cpsCalled").AsBoolean()?"Yes":"No"},
                {"datetimeCPSNotifiedDT", action.Activity.Workflow.GetAttributeValue("datetimeCPSNotifiedDT")},
                {"cpsCaseNumber", action.Activity.Workflow.GetAttributeValue("cpsCaseNumber")},
                {"parentName", action.Activity.Workflow.GetAttributeValue("parentName")},
                {"datetimeNotified", action.Activity.Workflow.GetAttributeValue("datetimeNotified")},
                {"security", action.Activity.Workflow.GetAttributeValue("security").AsBoolean()?"X":""},
                {"policeFireCalled", String.Concat(action.Activity.Workflow.GetAttributeValue("policefireCalled").AsBoolean()?"Yes - ":"No", action.Activity.Workflow.GetAttributeValue("servicesCalled"))},
                {"timeCalled", action.Activity.Workflow.GetAttributeValue("timeCalled")},
                {"timeArrived", action.Activity.Workflow.GetAttributeValue("timeArrived")},
                {"policefireReport", action.Activity.Workflow.GetAttributeValue("policefireReport")},
                {"reportNumber", action.Activity.Workflow.GetAttributeValue("reportNumber")},
                {"disposition", action.Activity.Workflow.GetAttributeValue("disposition")},
                {"incidentDesc", action.Activity.Workflow.GetAttributeValue("incidentDesc")}
            };

            BinaryFileService binaryFileService = new BinaryFileService( rockContext );
            BinaryFile PDF = binaryFileService.Get( GetActionAttributeValue( action, "DigitalIncidentReportPDF" ).AsGuid() );

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
                        pdfFormFields.SetField( field.Key, field.Value );
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
                    FileName = "DigitalIncidentReport_" + action.Activity.Workflow.GetAttributeValue( "reportPerson" ) + ".pdf",
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

