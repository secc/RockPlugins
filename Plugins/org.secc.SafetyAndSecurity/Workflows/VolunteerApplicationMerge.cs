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
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Workflow;
using Rock.Attribute;
using iTextSharp.text.pdf;
using System.IO;
using Rock.Web.Cache;

namespace org.secc.SafetyAndSecurity
{
    [ActionCategory( "SECC > Safety and Security" )]
    [Description( "Merge the final PDF in the volunteer application (background check)." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Volunteer Application Merge" )]
    [BinaryFileField( VolunteerApplicationMerge.PDF_FORM_BINARY_FILE_TYPE, "Adult Volunteer Application PDF", "The Confidential Volunteer Application for Adult PDF form", true )]
    [BinaryFileField( VolunteerApplicationMerge.PDF_FORM_BINARY_FILE_TYPE, "Minor Volunteer Application PDF", "The Confidential Volunteer Application for Minors PDF form", true )]
    [WorkflowAttribute( "Is Minor Application", "Mark as yes if the application is for minors", fieldTypeClassNames: new string[] { "Rock.Field.Types.BooleanFieldType" } )]
    class VolunteerApplicationMerge : ActionComponent
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

            Dictionary<string, string> fields = new Dictionary<string, string>()
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
				    //{"txtSSN", action.Activity.Workflow.GetAttributeValue("")},
				    {"radGender", action.Activity.Workflow.GetAttributeValue("")},
                    {"radMale", action.Activity.Workflow.GetAttributeValue("Gender") == "Male"?"Yes":"No" },
                    {"radFemale", action.Activity.Workflow.GetAttributeValue("Gender") == "Female"?"Yes":"No"},
                    {"txtSCCAttendanceDuration", action.Activity.Workflow.GetAttributeValue("HowLongAttended")},
                    {"txtSCCMember", person.ConnectionStatusValue.Guid==Rock.SystemGuid.DefinedValue.PERSON_CONNECTION_STATUS_MEMBER.AsGuid()?"Yes":"No"},
                    {"txtSCCMemberPDFNo", person.ConnectionStatusValue.Guid==Rock.SystemGuid.DefinedValue.PERSON_CONNECTION_STATUS_MEMBER.AsGuid()?"No":"Yes"},
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
                    {"radOutOfState", action.Activity.Workflow.GetAttributeValue("OutsideKentuckyIndiana")},
                    {"radOutOfStatePDFYes", action.Activity.Workflow.GetAttributeValue("OutsideKentuckyIndiana").AsBoolean()?"Yes":"No"},
                    {"radOutOfStatePDFNo", action.Activity.Workflow.GetAttributeValue("OutsideKentuckyIndiana").AsBoolean()?"No":"Yes"},
                    {"txtOutOfState_Dates", action.Activity.Workflow.GetAttributeValue("WhenOutsideKentuckyIndiana")},
                    {"txtOutOfState_State", action.Activity.Workflow.GetAttributeValue("StatesOutsideKentuckyIndiana")},
                    {"txtEmployer", action.Activity.Workflow.GetAttributeValue("CurrentEmployer")},
                    {"txtPosition", action.Activity.Workflow.GetAttributeValue("PositionHeld")},
                    {"txtWorkPhone", action.Activity.Workflow.GetAttributeValue("WorkPhone")},
                    {"txtHomePhone", action.Activity.Workflow.GetAttributeValue("HomePhone")},
                    {"txtCellPhone", action.Activity.Workflow.GetAttributeValue("CellPhone")},

                    //{"txtWorkEmail", action.Activity.Workflow.GetAttributeValue("")},

                    {"txtEmail", person.Email},

                    {"txtRef1Name", action.Activity.Workflow.GetAttributeValue("Reference1Name")},
                    //{"txtRef1Relationship", action.Activity.Workflow.GetAttributeValue("")},
				    {"radRef1YearsKnow", action.Activity.Workflow.GetAttributeValue("Reference1Relationship")
                                        + "/" + action.Activity.Workflow.GetAttributeValue("Reference1YearsKnown")},
                    {"txtRef1Address", reference1Address.Street1},
                    {"txtRef1City", reference1Address.City},
                    {"txtRef1State", reference1Address.State},
                    {"txtRef1Zip", reference1Address.PostalCode},
				    //{"txtRef1PersPhone", action.Activity.Workflow.GetAttributeValue("")},
				    //{"txtRef1PersPhoneTime", action.Activity.Workflow.GetAttributeValue("")},
				    {"txtRef1WorkPhone", action.Activity.Workflow.GetAttributeValue("Reference1WorkPhone")},
				    //{"txtRef1WorkPhoneTime", action.Activity.Workflow.GetAttributeValue("")},
				    {"txtRef1HomePhone", action.Activity.Workflow.GetAttributeValue("Reference1HomePhone")},
                    {"txtRef1CellPhone", action.Activity.Workflow.GetAttributeValue("Reference1CellPhone")},
                    {"txtRef1Email", action.Activity.Workflow.GetAttributeValue("Reference1Email")},

                    {"txtRef2Name", action.Activity.Workflow.GetAttributeValue("Reference2Name")},
                    //{"txtRef2Relationship", action.Activity.Workflow.GetAttributeValue("")},
				    {"radRef2YearsKnow", action.Activity.Workflow.GetAttributeValue("Reference2Relationship")
                                        + "/" + action.Activity.Workflow.GetAttributeValue("Reference2YearsKnown")},
                    {"txtRef2Address", reference2Address.Street1},
                    {"txtRef2City", reference2Address.City},
                    {"txtRef2State", reference2Address.State},
                    {"txtRef2Zip", reference2Address.PostalCode},
				    //{"txtRef2PersPhone", action.Activity.Workflow.GetAttributeValue("")},
				    //{"txtRef2PersPhoneTime", action.Activity.Workflow.GetAttributeValue("")},
				    {"txtRef2WorkPhone", action.Activity.Workflow.GetAttributeValue("Reference2WorkPhone")},
				    //{"txtRef2WorkPhoneTime", action.Activity.Workflow.GetAttributeValue("")},
				    {"txtRef2HomePhone", action.Activity.Workflow.GetAttributeValue("Reference2HomePhone")},
                    {"txtRef2CellPhone", action.Activity.Workflow.GetAttributeValue("Reference2CellPhone")},
                    {"txtRef2Email", action.Activity.Workflow.GetAttributeValue("Reference2Email")},

                    {"txtRef3Name", action.Activity.Workflow.GetAttributeValue("Reference3Name")},
                    //{"txtRef3Relationship", action.Activity.Workflow.GetAttributeValue("")},
				    {"radRef3YearsKnow", action.Activity.Workflow.GetAttributeValue("Reference3Relationship")
                                        + "/" + action.Activity.Workflow.GetAttributeValue("Reference3YearsKnown")},
                    {"txtRef3Address", reference3Address.Street1},
                    {"txtRef3City", reference3Address.City},
                    {"txtRef3State", reference3Address.State},
                    {"txtRef3Zip", reference3Address.PostalCode},
				    //{"txtRef3PersPhone", action.Activity.Workflow.GetAttributeValue("")},
				    //{"txtRef3PersPhoneTime", action.Activity.Workflow.GetAttributeValue("")},
				    {"txtRef3WorkPhone", action.Activity.Workflow.GetAttributeValue("Reference3WorkPhone")},
				    //{"txtRef3WorkPhoneTime", action.Activity.Workflow.GetAttributeValue("")},
				    {"txtRef3HomePhone", action.Activity.Workflow.GetAttributeValue("Reference3HomePhone")},
                    {"txtRef3CellPhone", action.Activity.Workflow.GetAttributeValue("Reference3CellPhone")},
                    {"txtRef3Email", action.Activity.Workflow.GetAttributeValue("Reference3Email")},

                    {"txtRef4Name", action.Activity.Workflow.GetAttributeValue("Reference4Name")},
                    //{"txtRef4Relationship", action.Activity.Workflow.GetAttributeValue("")},
				    {"radRef4YearsKnow", action.Activity.Workflow.GetAttributeValue("Reference4Relationship")
                                        + "/" + action.Activity.Workflow.GetAttributeValue("Reference4YearsKnown")},
                    {"txtRef4Address", reference4Address.Street1},
                    {"txtRef4City", reference4Address.City},
                    {"txtRef4State", reference4Address.State},
                    {"txtRef4Zip", reference4Address.PostalCode},
				    //{"txtRef4PersPhone", action.Activity.Workflow.GetAttributeValue("")},
				    //{"txtRef4PersPhoneTime", action.Activity.Workflow.GetAttributeValue("")},
				    {"txtRef4WorkPhone", action.Activity.Workflow.GetAttributeValue("Reference4WorkPhone")},
				    //{"txtRef4WorkPhoneTime", action.Activity.Workflow.GetAttributeValue("")},
				    {"txtRef4HomePhone", action.Activity.Workflow.GetAttributeValue("Reference4HomePhone")},
                    {"txtRef4CellPhone", action.Activity.Workflow.GetAttributeValue("Reference4CellPhone")},
                    {"txtRef4Email", action.Activity.Workflow.GetAttributeValue("Reference4Email") },
                
                    //{"txtPhysLimitations", action.Activity.Workflow.GetAttributeValue("")},
                    {"txtPhysLimitations",  action.Activity.Workflow.GetAttributeValue("PhysicalLimitationsExplanation") },
                    {"txtPhysLimitationsPDF", action.Activity.Workflow.GetAttributeValue("PhysicalLimitationsExplanation")},
                    {"radPhysLimitationsPDFYes", action.Activity.Workflow.GetAttributeValue("PhysicalLimitations").AsBoolean()?"Yes":"No" },
                    {"radPhysLimitationsPDFNo", action.Activity.Workflow.GetAttributeValue("PhysicalLimitations").AsBoolean()?"No":"Yes" },
                    {"radCrimePersons", action.Activity.Workflow.GetAttributeValue("Crime").AsBoolean()?"Yes":"No" },
                    {"radCrimePersonsPDFNo", action.Activity.Workflow.GetAttributeValue("Crime").AsBoolean()?"No":"Yes" },
                    //{"radCrimeProperty", action.Activity.Workflow.GetAttributeValue("")},
				    {"radThreatToMinors", action.Activity.Workflow.GetAttributeValue("Threat").AsBoolean()?"Yes":"No" },
                    {"radThreatToMinorsPDFNo", action.Activity.Workflow.GetAttributeValue("Threat").AsBoolean()?"No":"Yes" },
                    {"radCrimeCounseled", action.Activity.Workflow.GetAttributeValue("CrimeCounsel").AsBoolean()?"Yes":"No" },
                    {"radCrimeCounseledPDFYes", action.Activity.Workflow.GetAttributeValue("CrimeCounsel").AsBoolean()?"Yes":"No" },
                    {"radCrimeCounseledPDFNo", action.Activity.Workflow.GetAttributeValue("CrimeCounsel").AsBoolean()?"No":"Yes" },
                    {"radNeedsStaffContact", action.Activity.Workflow.GetAttributeValue("Contact").AsBoolean()?"Yes":"No"},
                    {"radNeedsStaffContactPDFNo", action.Activity.Workflow.GetAttributeValue("Contact").AsBoolean()?"No":"Yes" },
                    {"personDetailPage", GlobalAttributesCache.Value("InternalApplicationRoot") + "/Person/" + person.Id },

                    {"txtAppSigned", "{{t:s;r:y;o:\"Applicant\";}}" },
                    {"txtAppDated", "{{t:d;r:y;o:\"Applicant\";l:\"Date\";dd:\""+DateTime.Now.ToShortDateString()+"\";}}" },
                    {"txtAppPrintedName", person.FullNameFormal },

                    {"txtSOFSigned", "{{t:s;r:n;o:\"Applicant\";}}" },
                    {"txtSOFDated", "{{t:d;r:n;o:\"Applicant\";l:\"Date\";dd:\""+DateTime.Now.ToShortDateString()+"\";}}" },
                    {"txtSOFPrintedName", person.FullNameFormal },

                    {"txtParentSignature", "{{t:s;r:y;o:\"Parent\";}}" },
                    {"txtDate1", "{{t:d;r:y;o:\"Parent\";l:\"Date\";dd:\""+DateTime.Now.ToShortDateString()+"\";}}" },

                    {"radReadSOFYes", action.Activity.Workflow.GetAttributeValue("ReadStatementOfFaith").AsBoolean()?"Yes":"No" },
                    {"radReadSOFNo", action.Activity.Workflow.GetAttributeValue("ReadStatementOfFaith").AsBoolean()?"No":"Yes" },
                    {"radAgreeSOFYes", action.Activity.Workflow.GetAttributeValue("AgreeStatementOfFaith").AsBoolean()?"Yes":"No" },
                    {"radAgreeSOFNo", action.Activity.Workflow.GetAttributeValue("AgreeStatementOfFaith").AsBoolean()?"No":"Yes" },
                    {"txtSOFCommentsAmendments", String.IsNullOrEmpty(action.Activity.Workflow.GetAttributeValue("CommentsStatementOfFaith"))?" ":action.Activity.Workflow.GetAttributeValue("CommentsStatementOfFaith") },
                };

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
                renderedPDF.CopyPropertiesFrom( PDF );
                renderedPDF.Guid = Guid.NewGuid();
                renderedPDF.BinaryFileTypeId = new BinaryFileTypeService( rockContext ).Get( new Guid( BACKGROUND_CHECK_BINARY_FILE_TYPE ) ).Id;

                BinaryFileData pdfData = new BinaryFileData();
                pdfData.Content = ms.ToArray();

                renderedPDF.DatabaseData = pdfData;

                binaryFileService.Add( renderedPDF );
                rockContext.SaveChanges();

                action.Activity.Workflow.SetAttributeValue( "PDF", renderedPDF.Guid );
            }


            return true;

        }
    }
}
