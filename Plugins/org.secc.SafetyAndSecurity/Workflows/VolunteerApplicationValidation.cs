using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Workflow;
using Rock.Attribute;
using System.Text.RegularExpressions;
using System.Text;
using Rock.Security;

namespace org.secc.SafetyAndSecurity
{
    [ActionCategory( "SECC > Safety and Security" )]
    [Description( "Validate various steps in the volunteer application (background check)." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Volunteer Application Validation" )]
    [CustomDropdownListField( "Action Step", "Action Step to Validate",  "Personal Information,First Reference,Second Reference,Third Reference,Fourth Reference,Special Circumstances,Statement of Faith", true)]
    [WorkflowAttribute("Error Messages", "Error Messages Attribute", true)]
    class VolunteerApplicationValidation : ActionComponent
    {
        public override bool Execute( RockContext rockContext, WorkflowAction action, object entity, out List<string> errorMessages )
        {
            errorMessages = new List<string>();


            var actionStep = GetActionAttributeValue( action, "ActionStep" );

            StringBuilder sbErrorMessages = new StringBuilder();

            switch (actionStep) {
                case "Statement of Faith":
                    // Should we fall through and validate everything in all the previous actions?
                    break;
                case "Special Circumstances":
                    break;
                case "Fourth Reference":
                    break;
                case "Third Reference":
                    break;
                case "Second Reference":
                    break;
                case "First Reference":
                    break;
                case "Personal Information":
                    // Check out lots of stuff with the current mailing address
                    Guid? locationGuid = action.Activity.Workflow.GetAttributeValue( "CurrentMailingAddress" ).AsGuidOrNull();
                    if ( locationGuid.HasValue) {
                        LocationService locationService = new LocationService( new RockContext() );
                        Location location = locationService.Get( locationGuid.Value );
                        if ( string.IsNullOrEmpty(location.Street1) )
                        {
                            sbErrorMessages.AppendLine( "<li>Current Mailing Address - Address Line 1 is Required.</li>" );
                        }
                        if ( string.IsNullOrEmpty( location.PostalCode ) )
                        {
                            sbErrorMessages.AppendLine( "<li>Current Mailing Address - Postal Code is Required.</li>" );
                        }
                    } else {
                        sbErrorMessages.AppendLine( "<li>Current Mailing Address is Required.</li>" );
                    }

                    // Make sure the middle name is over 1 character long
                    string middleName = action.Activity.Workflow.GetAttributeValue( "MiddleName" );
                    if (string.IsNullOrEmpty(middleName) || middleName.Length <= 1)
                    {
                        sbErrorMessages.AppendLine( "<li>A full Middle Name is Required." );
                    }

                    // Verify the SSN using regex
                    string ssn = Encryption.DecryptString( action.Activity.Workflow.GetAttributeValue( "SSN1" ) ) + "-" +
                        Encryption.DecryptString( action.Activity.Workflow.GetAttributeValue( "SSN2" ) ) + "-" +
                        Encryption.DecryptString( action.Activity.Workflow.GetAttributeValue( "SSN3" ) );
                    
                    if (!Regex.Match(ssn, @"^(?!(123[ -]?45[ -]?6789)|((\d)\3\3[ -]?\3\3[ -]?\3\3\3\3))(\d{3}[- ]?\d{2}[- ]?\d{4})$").Success) {
                        sbErrorMessages.AppendLine("<li>Social Security Number is Required and must be in a valid format (XXX-XX-XXXX).</li>");
                    }

                    // Verify the Phone Numbers
                    string homePhone = action.Activity.Workflow.GetAttributeValue( "HomePhone" );
                    if (!string.IsNullOrEmpty(homePhone) && !Regex.Match( homePhone, @"^[01]?[- .]?(\([2-9]\d{2}\)|[2-9]\d{2})[- .]?\d{3}[- .]?\d{4}$" ).Success )
                    {
                        sbErrorMessages.AppendLine( "<li>Home Phone must be in a valid format (XXX) XXX-XXXX.</li>" );
                    }
                    string cellPhone = action.Activity.Workflow.GetAttributeValue( "CellPhone" );
                    if ( !string.IsNullOrEmpty( cellPhone ) && !Regex.Match( cellPhone, @"^[01]?[- .]?(\([2-9]\d{2}\)|[2-9]\d{2})[- .]?\d{3}[- .]?\d{4}$" ).Success )
                    {
                        sbErrorMessages.AppendLine( "<li>Cell Phone must be in a valid format (XXX) XXX-XXXX.</li>" );
                    }
                    if ( string.IsNullOrEmpty( homePhone ) && string.IsNullOrEmpty( cellPhone ) )
                    {
                        sbErrorMessages.AppendLine( "<li>Home or Cell Phone is Required.</li>" );
                    }
                    string workPhone = action.Activity.Workflow.GetAttributeValue( "WorkPhone" );
                    if ( !string.IsNullOrEmpty( workPhone ) && !Regex.Match( workPhone, @"^[01]?[- .]?(\([2-9]\d{2}\)|[2-9]\d{2})[- .]?\d{3}[- .]?\d{4}$" ).Success )
                    {
                        sbErrorMessages.AppendLine( "<li>Work Phone must be in a valid format (XXX) XXX-XXXX.</li>" );
                    }
                    break;
            }
            SetWorkflowAttributeValue( action, GetActionAttributeValue( action, "ErrorMessages" ).AsGuid(), sbErrorMessages.ToString() );

            return true;
        }
    }
}
