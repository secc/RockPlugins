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
using Rock.Web.Cache;
using Rock.Workflow;

namespace org.secc.SafetyAndSecurity
{
    [ActionCategory( "SECC > Safety and Security" )]
    [Description( "Merge the final PDF in the volunteer application (background check)." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Volunteer Application Merge" )]
    [BinaryFileField( VolunteerApplicationMerge.PDF_FORM_BINARY_FILE_TYPE, "Adult Volunteer Application PDF", "The Confidential Volunteer Application for Adult PDF form", true )]
    [BinaryFileField( VolunteerApplicationMerge.PDF_FORM_BINARY_FILE_TYPE, "Minor Volunteer Application PDF", "The Confidential Volunteer Application for Minors PDF form", true )]
    [WorkflowAttribute( "Is Minor Application", "Mark as yes if the application is for minors", fieldTypeClassNames: new string[] { "Rock.Field.Types.BooleanFieldType" } )]
    public class VolunteerApplicationMerge : ActionComponent
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
            Location previousMailingAddress = locationService.Get( action.Activity.Workflow.GetAttributeValue( "PreviousMailingAddress" ).AsGuid() );
            if ( previousMailingAddress == null )
            {
                previousMailingAddress = new Location();
            }
            Location reference1Address = locationService.Get( action.Activity.Workflow.GetAttributeValue( "Reference1Address" ).AsGuid() );
            Location reference2Address = locationService.Get( action.Activity.Workflow.GetAttributeValue( "Reference2Address" ).AsGuid() );
            Location reference3Address = locationService.Get( action.Activity.Workflow.GetAttributeValue( "Reference3Address" ).AsGuid() );
            Location reference4Address = locationService.Get( action.Activity.Workflow.GetAttributeValue( "Reference4Address" ).AsGuid() );

            Dictionary<string, string> fields = BuildFieldDictionary(
                action,
                person,
                currentMailingAddress,
                previousMailingAddress,
                reference1Address,
                reference2Address,
                reference3Address,
                reference4Address
            );

            BinaryFileService binaryFileService = new BinaryFileService( rockContext );
            BinaryFile PDF = null;
            var isMinorApplicant = GetAttributeValue( action, "IsMinorApplication", true ).AsBoolean();
            if ( isMinorApplicant )
            {
                PDF = binaryFileService.Get( GetActionAttributeValue( action, "MinorVolunteerApplicationPDF" ).AsGuid() );
            }
            else
            {
                PDF = binaryFileService.Get( GetActionAttributeValue( action, "AdultVolunteerApplicationPDF" ).AsGuid() );
            }

            var pdfBytes = PDF.ContentStream.ReadBytesToEnd();

            var renderedPdfBytes = GeneratePdfWithFields( pdfBytes, fields, flattenFields: true );

            BinaryFile renderedPDF = new BinaryFile();
            renderedPDF.IsTemporary = false;
            renderedPDF.IsSystem = false;
            renderedPDF.Guid = Guid.NewGuid();
            renderedPDF.MimeType = PDF.MimeType;
            renderedPDF.FileName = "VolunteerApplication_" + person.FirstName + person.LastName + ".pdf";
            renderedPDF.BinaryFileTypeId = new BinaryFileTypeService( rockContext ).Get( new Guid( BACKGROUND_CHECK_BINARY_FILE_TYPE ) ).Id;
            renderedPDF.DatabaseData = null;
            renderedPDF.FileSize = renderedPdfBytes.Length;
            renderedPDF.ContentStream = new MemoryStream( renderedPdfBytes );

            binaryFileService.Add( renderedPDF );
            rockContext.SaveChanges();

            action.Activity.Workflow.SetAttributeValue( "PDF", renderedPDF.Guid );

            return true;
        }

        /// <summary>
        /// Builds the field dictionary for the PDF form from workflow attributes.
        /// </summary>
        /// <param name="action">The workflow action.</param>
        /// <param name="person">The person.</param>
        /// <param name="currentMailingAddress">The current mailing address.</param>
        /// <param name="previousMailingAddress">The previous mailing address.</param>
        /// <param name="reference1Address">The reference1 address.</param>
        /// <param name="reference2Address">The reference2 address.</param>
        /// <param name="reference3Address">The reference3 address.</param>
        /// <param name="reference4Address">The reference4 address.</param>
        /// <returns>Dictionary of PDF field names and values</returns>
        private Dictionary<string, string> BuildFieldDictionary(
            WorkflowAction action,
            Person person,
            Location currentMailingAddress,
            Location previousMailingAddress,
            Location reference1Address,
            Location reference2Address,
            Location reference3Address,
            Location reference4Address )
        {
            // Get boolean values for radio buttons
            bool isMale = action.Activity.Workflow.GetAttributeValue( "Gender" ) == "Male";
            bool isFemale = action.Activity.Workflow.GetAttributeValue( "Gender" ) == "Female";
            bool hasPhysicalLimitations = action.Activity.Workflow.GetAttributeValue("PhysicalLimitations").AsBoolean();
            bool hasCrime = action.Activity.Workflow.GetAttributeValue("Crime").AsBoolean();
            bool hasThreat = action.Activity.Workflow.GetAttributeValue("Threat").AsBoolean();
            bool hasCrimeCounsel = action.Activity.Workflow.GetAttributeValue("CrimeCounsel").AsBoolean();
            bool needsStaffContact = action.Activity.Workflow.GetAttributeValue("Contact").AsBoolean();
            bool hasReadSOF = action.Activity.Workflow.GetAttributeValue("ReadStatementOfFaith").AsBoolean();
            bool hasAgreeSOF = action.Activity.Workflow.GetAttributeValue("AgreeStatementOfFaith").AsBoolean();
            bool isOutOfState = action.Activity.Workflow.GetAttributeValue("OutsideKentuckyIndiana").AsBoolean();

            return new Dictionary<string, string>()
            {
                {"ministryOfInterest", action.Activity.Workflow.GetAttributeValue("MinistryOfInterest") },
                {"intPersonID", person.Id.ToString()},

                {"txtLastName", action.Activity.Workflow.GetAttributeValue("LastName")},
                {"txtFirstName",  action.Activity.Workflow.GetAttributeValue("FirstName")},
                {"txtMiddleName", action.Activity.Workflow.GetAttributeValue("MiddleName")},
                {"txtMaidenOtherName", action.Activity.Workflow.GetAttributeValue("MaidenOtherNames")},
                {"txtParent", action.Activity.Workflow.GetAttributeValue("Parent")},
                {"txtParentEmail", action.Activity.Workflow.GetAttributeValue("ParentEmail")},
                {"txtParentHomePhone", action.Activity.Workflow.GetAttributeValue("ParentHomePhone")},
                {"txtParentCellPhone", action.Activity.Workflow.GetAttributeValue("ParentCellPhone")},

                {"txtDateOfBirth", action.Activity.Workflow.GetAttributeValue("DateofBirth").AsDateTime().Value.ToShortDateString()},
                {"radMale", isMale ? "Yes" : "Off"},
                {"radFemale", isFemale ? "Yes" : "Off"},
                {"txtSCCAttendanceDuration", action.Activity.Workflow.GetAttributeValue("HowLongAttended")},
                {"txtSCCMember", person.ConnectionStatusValue.Guid==Rock.SystemGuid.DefinedValue.PERSON_CONNECTION_STATUS_MEMBER.AsGuid()?"Yes":"Off"},
                {"txtSCCMemberPDFNo", person.ConnectionStatusValue.Guid==Rock.SystemGuid.DefinedValue.PERSON_CONNECTION_STATUS_MEMBER.AsGuid()?"Off":"Yes"},
                {"txtSCCInvolvement", action.Activity.Workflow.GetAttributeValue("CurrentParticipation")},
                {"txtChurch", action.Activity.Workflow.GetAttributeValue("ChurchAtttended")},

                {"txtStreet", currentMailingAddress.Street1},
                {"txtCity", currentMailingAddress.City},
                {"txtState", currentMailingAddress.State},
                {"txtZip", currentMailingAddress.PostalCode},
                {"SaveUpToThisPoint", ""},
                {"txtPrevStreet", previousMailingAddress.Street1},
                {"txtPrevCity", previousMailingAddress.City},
                {"txtPrevState", previousMailingAddress.State},
                {"txtPrevZip", previousMailingAddress.PostalCode},

                {"radOutOfStatePDFYes", isOutOfState ? "Yes" : "Off"},
                {"radOutOfStatePDFNo", !isOutOfState ? "Yes" : "Off"},
                
                {"txtOutOfState_Dates", action.Activity.Workflow.GetAttributeValue("WhenOutsideKentuckyIndiana")},
                {"txtOutOfState_State", action.Activity.Workflow.GetAttributeValue("StatesOutsideKentuckyIndiana")},
                {"txtEmployer", action.Activity.Workflow.GetAttributeValue("CurrentEmployer")},
                {"txtPosition", action.Activity.Workflow.GetAttributeValue("PositionHeld")},
                {"txtWorkPhone", action.Activity.Workflow.GetAttributeValue("WorkPhone")},
                {"txtHomePhone", action.Activity.Workflow.GetAttributeValue("HomePhone")},
                {"txtCellPhone", action.Activity.Workflow.GetAttributeValue("CellPhone")},

                {"txtEmail", person.Email},

                {"txtRef1Name", action.Activity.Workflow.GetAttributeValue("Reference1Name")},
                {"radRef1YearsKnow", action.Activity.Workflow.GetAttributeValue("Reference1Relationship")
                                    + "/" + action.Activity.Workflow.GetAttributeValue("Reference1YearsKnown")},
                {"txtRef1Address", reference1Address.Street1},
                {"txtRef1City", reference1Address.City},
                {"txtRef1State", reference1Address.State},
                {"txtRef1Zip", reference1Address.PostalCode},
                {"txtRef1WorkPhone", action.Activity.Workflow.GetAttributeValue("Reference1WorkPhone")},
                {"txtRef1HomePhone", action.Activity.Workflow.GetAttributeValue("Reference1HomePhone")},
                {"txtRef1CellPhone", action.Activity.Workflow.GetAttributeValue("Reference1CellPhone")},
                {"txtRef1Email", action.Activity.Workflow.GetAttributeValue("Reference1Email")},

                {"txtRef2Name", action.Activity.Workflow.GetAttributeValue("Reference2Name")},
                {"radRef2YearsKnow", action.Activity.Workflow.GetAttributeValue("Reference2Relationship")
                                    + "/" + action.Activity.Workflow.GetAttributeValue("Reference2YearsKnown")},
                {"txtRef2Address", reference2Address.Street1},
                {"txtRef2City", reference2Address.City},
                {"txtRef2State", reference2Address.State},
                {"txtRef2Zip", reference2Address.PostalCode},
                {"txtRef2WorkPhone", action.Activity.Workflow.GetAttributeValue("Reference2WorkPhone")},
                {"txtRef2HomePhone", action.Activity.Workflow.GetAttributeValue("Reference2HomePhone")},
                {"txtRef2CellPhone", action.Activity.Workflow.GetAttributeValue("Reference2CellPhone")},
                {"txtRef2Email", action.Activity.Workflow.GetAttributeValue("Reference2Email")},

                {"txtRef3Name", action.Activity.Workflow.GetAttributeValue("Reference3Name")},
                {"radRef3YearsKnow", action.Activity.Workflow.GetAttributeValue("Reference3Relationship")
                                    + "/" + action.Activity.Workflow.GetAttributeValue("Reference3YearsKnown")},
                {"txtRef3Address", reference3Address.Street1},
                {"txtRef3City", reference3Address.City},
                {"txtRef3State", reference3Address.State},
                {"txtRef3Zip", reference3Address.PostalCode},
                {"txtRef3WorkPhone", action.Activity.Workflow.GetAttributeValue("Reference3WorkPhone")},
                {"txtRef3HomePhone", action.Activity.Workflow.GetAttributeValue("Reference3HomePhone")},
                {"txtRef3CellPhone", action.Activity.Workflow.GetAttributeValue("Reference3CellPhone")},
                {"txtRef3Email", action.Activity.Workflow.GetAttributeValue("Reference3Email")},

                {"txtRef4Name", action.Activity.Workflow.GetAttributeValue("Reference4Name")},
                {"radRef4YearsKnow", action.Activity.Workflow.GetAttributeValue("Reference4Relationship")
                                    + "/" + action.Activity.Workflow.GetAttributeValue("Reference4YearsKnown")},
                {"txtRef4Address", reference4Address?.Street1},
                {"txtRef4City", reference4Address?.City},
                {"txtRef4State", reference4Address?.State},
                {"txtRef4Zip", reference4Address?.PostalCode},
                {"txtRef4WorkPhone", action.Activity.Workflow.GetAttributeValue("Reference4WorkPhone")},
                {"txtRef4HomePhone", action.Activity.Workflow.GetAttributeValue("Reference4HomePhone")},
                {"txtRef4CellPhone", action.Activity.Workflow.GetAttributeValue("Reference4CellPhone")},
                {"txtRef4Email", action.Activity.Workflow.GetAttributeValue("Reference4Email") },

                {"txtPhysLimitations",  action.Activity.Workflow.GetAttributeValue("PhysicalLimitationsExplanation") },
                {"txtPhysLimitationsPDF", action.Activity.Workflow.GetAttributeValue("PhysicalLimitationsExplanation")},
                
                // Physical Limitations - set both Yes and No radio buttons
                {"radPhysLimitationsPDFYes", hasPhysicalLimitations ? "Yes" : "Off" },
                {"radPhysLimitationsPDFNo", !hasPhysicalLimitations ? "Yes" : "Off" },
                
                // Crime - set both Yes and No radio buttons
                {"radCrimePersons", hasCrime ? "Yes" : "Off" },
                {"radCrimePersonsPDFNo", !hasCrime ? "Yes" : "Off" },
                
                // Threat to Minors - set both Yes and No radio buttons
                {"radThreatToMinors", hasThreat ? "Yes" : "Off" },
                {"radThreatToMinorsPDFNo", !hasThreat ? "Yes" : "Off" },
                
                // Crime Counseled - set both Yes and No radio buttons
                {"radCrimeCounseledPDFYes", hasCrimeCounsel ? "Yes" : "Off" },
                {"radCrimeCounseledPDFNo", !hasCrimeCounsel ? "Yes" : "Off" },
                
                // Needs Staff Contact - set both Yes and No radio buttons
                {"radNeedsStaffContact", needsStaffContact ? "Yes" : "Off"},
                {"radNeedsStaffContactPDFNo", !needsStaffContact ? "Yes" : "Off"},
                
                {"personDetailPage", GlobalAttributesCache.Value("InternalApplicationRoot") + "/Person/" + person.Id },

                {"txtAppSigned", "{{t:s;r:y;o:\"Applicant\";}}" },
                {"txtAppDated", "{{t:d;r:y;o:\"Applicant\";l:\"Date\";dd:\""+DateTime.Now.ToShortDateString()+"\";}}" },
                {"txtAppPrintedName", person.FullNameFormal },

                {"txtSOFSigned", "{{t:s;r:y;o:\"Applicant\";}}" },
                {"txtSOFDated", "{{t:d;r:y;o:\"Applicant\";l:\"Date\";dd:\""+DateTime.Now.ToShortDateString()+"\";}}" },
                {"txtSOFPrintedName", person.FullNameFormal },

                {"txtParentSignature", "{{t:s;r:y;o:\"Parent\";}}" },
                {"txtDate1", "{{t:d;r:y;o:\"Parent\";l:\"Date\";dd:\""+DateTime.Now.ToShortDateString()+"\";}}" },

                // Read Statement of Faith - set both Yes and No radio buttons
                {"radReadSOFYes", hasReadSOF ? "Yes" : "Off" },
                {"radReadSOFNo", !hasReadSOF ? "Yes" : "Off" },
                
                // Agree Statement of Faith - set both Yes and No radio buttons
                {"radAgreeSOFYes", hasAgreeSOF ? "Yes" : "Off" },
                {"radAgreeSOFNo", !hasAgreeSOF ? "Yes" : "Off" },
                
                {"txtSOFCommentsAmendments", String.IsNullOrEmpty(action.Activity.Workflow.GetAttributeValue("CommentsStatementOfFaith"))?" ":action.Activity.Workflow.GetAttributeValue("CommentsStatementOfFaith") },
            };
        }

        /// <summary>
        /// Generates a PDF by merging field values into a template PDF form.
        /// </summary>
        /// <param name="templatePdfBytes">The template PDF bytes.</param>
        /// <param name="fields">The field dictionary with field names and values.</param>
        /// <param name="flattenFields">if set to <c>true</c> flatten the form fields to make them non-editable.</param>
        /// <returns>The generated PDF as a byte array</returns>
        public byte[] GeneratePdfWithFields( byte[] templatePdfBytes, Dictionary<string, string> fields, bool flattenFields = true )
        {
            using ( MemoryStream ms = new MemoryStream() )
            {
                PdfReader pdfReader = new PdfReader( new MemoryStream( templatePdfBytes ) );
                var pdfWriter = new PdfWriter( ms );

                var pdfDocument = new PdfDocument( pdfReader, pdfWriter );
                var form = PdfAcroForm.GetAcroForm( pdfDocument, true );

                if ( form != null )
                {
                    var pdfFormFields = form.GetFormFields();

                    foreach ( var field in fields )
                    {
                        if ( pdfFormFields.ContainsKey( field.Key ) && !string.IsNullOrEmpty( field.Value ) )
                        {
                            try
                            {
                                var pdfField = form.GetField( field.Key );
                                if ( pdfField != null )
                                {
                                    pdfField.SetValue( field.Value ?? string.Empty );
                                }
                            }
                            catch ( Exception )
                            {
                                // Ignore errors setting individual fields to prevent entire operation from failing
                                // This can happen with font issues or field configuration problems
                            }
                        }
                    }

                    // flatten the form to remove editing options, set it to false
                    // to leave the form open to subsequent manual edits
                    if ( flattenFields )
                    {
                        form.FlattenFields();
                    }
                }

                // close the pdf
                pdfDocument.Close();
                pdfReader.Close();
                pdfWriter.Close();

                return ms.ToArray();
            }
        }
    }
}
