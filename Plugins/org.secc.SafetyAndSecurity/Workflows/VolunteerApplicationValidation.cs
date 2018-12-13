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
    [CustomDropdownListField( "Action Step", "Action Step to Validate", "Personal Information,First Reference,Second Reference,Third Reference,Fourth Reference,Special Circumstances,Statement of Faith", true )]
    [WorkflowAttribute( "Error Messages", "Error Messages Attribute", true )]
    [WorkflowActivityType( "Success Activity", "The activity type to activate upon success.  Leave blank to end here.", false, "", "", 0 )]
    [WorkflowActivityType( "Fail Activity", "The activity type to activate upon failure.", true, "", "", 0 )]
    [WorkflowAttribute( "Is Minor Application", "Mark as yes if the application is for minors", fieldTypeClassNames: new string[] { "Rock.Field.Types.BooleanFieldType" } )]
    class VolunteerApplicationValidation : ActionComponent
    {
        public override bool Execute( RockContext rockContext, WorkflowAction action, object entity, out List<string> errorMessages )
        {
            errorMessages = new List<string>();


            var actionStep = GetActionAttributeValue( action, "ActionStep" );

            StringBuilder sbErrorMessages = new StringBuilder();

            switch ( actionStep )
            {
                case "Statement of Faith":
                    if ( !action.Activity.Workflow.GetAttributeValue( "ReadStatementOfFaith" ).AsBoolean() )
                    {
                        sbErrorMessages.AppendLine( "<li>Please acknowledge that you have read the statement of faith.</li>" );
                    }
                    if ( !action.Activity.Workflow.GetAttributeValue( "AgreeStatementOfFaith" ).AsBoolean()
                        && string.IsNullOrEmpty( action.Activity.Workflow.GetAttributeValue( "CommentsStatementOfFaith" ) ) )
                    {
                        sbErrorMessages.AppendLine( "<li>Please explain your disagreement with the statement of faith.</li>" );
                    }
                    break;
                case "Special Circumstances":

                    if ( action.Activity.Workflow.GetAttributeValue( "Crime" ).AsBoolean()
                        && string.IsNullOrEmpty( action.Activity.Workflow.GetAttributeValue( "CrimeCounsel" ) ) )
                    {
                        sbErrorMessages.AppendLine( "<li>Please select whether you have ben counselled about the issue.</li>" );
                    }
                    if ( action.Activity.Workflow.GetAttributeValue( "PhysicalLimitations" ).AsBoolean()
                        && string.IsNullOrEmpty( action.Activity.Workflow.GetAttributeValue( "PhysicalLimitationsExplanation" ) ) )
                    {
                        sbErrorMessages.AppendLine( "<li>Please explain the limitations.</li>" );
                    }
                    break;
                case "Fourth Reference":
                    if ( !GetAttributeValue( action, "IsMinorApplication", true ).AsBoolean() )
                    {
                        Guid? reference4Guid = action.Activity.Workflow.GetAttributeValue( "Reference4Address" ).AsGuidOrNull();
                        validateAddress( reference4Guid.Value, "Fourth Reference Address", sbErrorMessages );
                        addressesUnique( rockContext, action, "Reference4Address", sbErrorMessages );

                        // Verify phone numbers
                        validatePhones( action, "Reference4HomePhone", "Reference4CellPhone", sbErrorMessages );

                        // Verify Relative/Staff
                        if ( action.Activity.Workflow.GetAttributeValue( "Reference4Relative" ).AsBoolean() )
                        {
                            sbErrorMessages.AppendLine( "<li>Your reference must not be a relative.</li>" );
                        }
                        if ( action.Activity.Workflow.GetAttributeValue( "Reference4Staff" ).AsBoolean() )
                        {
                            sbErrorMessages.AppendLine( "<li>Your reference must not be a staff member of Southeast Christian Church.</li>" );
                        }
                    }
                    break;
                case "Third Reference":
                    Guid? reference3Guid = action.Activity.Workflow.GetAttributeValue( "Reference3Address" ).AsGuidOrNull();
                    validateAddress( reference3Guid.Value, "Third Reference Address", sbErrorMessages );
                    addressesUnique( rockContext, action, "Reference3Address", sbErrorMessages );

                    // Verify phone numbers
                    validatePhones( action, "Reference3HomePhone", "Reference3CellPhone", sbErrorMessages );

                    // Verify Relative/Staff
                    if ( action.Activity.Workflow.GetAttributeValue( "Reference3Relative" ).AsBoolean() )
                    {
                        sbErrorMessages.AppendLine( "<li>Your reference must not be a relative.</li>" );
                    }
                    if ( action.Activity.Workflow.GetAttributeValue( "Reference3Staff" ).AsBoolean() )
                    {
                        sbErrorMessages.AppendLine( "<li>Your reference must not be a staff member of Southeast Christian Church.</li>" );
                    }
                    break;
                case "Second Reference":
                    Guid? reference2Guid = action.Activity.Workflow.GetAttributeValue( "Reference2Address" ).AsGuidOrNull();
                    validateAddress( reference2Guid.Value, "Second Reference Address", sbErrorMessages );
                    addressesUnique( rockContext, action, "Reference2Address", sbErrorMessages );

                    // Verify phone numbers
                    validatePhones( action, "Reference2HomePhone", "Reference2CellPhone", sbErrorMessages );

                    // Verify Relative/Staff
                    if ( action.Activity.Workflow.GetAttributeValue( "Reference2Relative" ).AsBoolean() )
                    {
                        sbErrorMessages.AppendLine( "<li>Your reference must not be a relative.</li>" );
                    }
                    if ( action.Activity.Workflow.GetAttributeValue( "Reference2Staff" ).AsBoolean() )
                    {
                        sbErrorMessages.AppendLine( "<li>Your reference must not be a staff member of Southeast Christian Church.</li>" );
                    }
                    break;
                case "First Reference":
                    Guid? reference1Guid = action.Activity.Workflow.GetAttributeValue( "Reference1Address" ).AsGuidOrNull();
                    validateAddress( reference1Guid.Value, "First Reference Address", sbErrorMessages );
                    addressesUnique( rockContext, action, "Reference1Address", sbErrorMessages );

                    // Verify phone numbers
                    validatePhones( action, "Reference1HomePhone", "Reference1CellPhone", sbErrorMessages );

                    // Verify Relative/Staff
                    if ( action.Activity.Workflow.GetAttributeValue( "Reference1Relative" ).AsBoolean() )
                    {
                        sbErrorMessages.AppendLine( "<li>Your reference must not be a relative.</li>" );
                    }
                    if ( action.Activity.Workflow.GetAttributeValue( "Reference1Staff" ).AsBoolean() )
                    {
                        sbErrorMessages.AppendLine( "<li>Your reference must not be a staff member of Southeast Christian Church.</li>" );
                    }
                    break;
                case "Personal Information":
                    // Check out lots of stuff with the current mailing address
                    Guid? locationGuid = action.Activity.Workflow.GetAttributeValue( "CurrentMailingAddress" ).AsGuidOrNull();
                    if ( locationGuid.HasValue )
                        validateAddress( locationGuid.Value, "Current Mailing Address", sbErrorMessages );

                    // Make sure the middle name is over 1 character long
                    string middleName = action.Activity.Workflow.GetAttributeValue( "MiddleName" );
                    if ( string.IsNullOrEmpty( middleName ) || middleName.Length <= 1 )
                    {
                        sbErrorMessages.AppendLine( "<li>A full Middle Name is Required." );
                    }

                    if ( !GetAttributeValue( action, "IsMinorApplication", true ).AsBoolean() )
                    {
                        // Verify the SSN using regex
                        string ssn = Encryption.DecryptString( action.Activity.Workflow.GetAttributeValue( "SSN1" ) ) + "-" +
                            Encryption.DecryptString( action.Activity.Workflow.GetAttributeValue( "SSN2" ) ) + "-" +
                            Encryption.DecryptString( action.Activity.Workflow.GetAttributeValue( "SSN3" ) );

                        if ( Encryption.DecryptString( action.Activity.Workflow.GetAttributeValue( "SSN1" ) ) != "***"
                            && !Regex.Match( ssn, @"^(?!(123[ -]?45[ -]?6789)|((\d)\3\3[ -]?\3\3[ -]?\3\3\3\3))(\d{3}[- ]?\d{2}[- ]?\d{4})$" ).Success )
                        {
                            sbErrorMessages.AppendLine( "<li>Social Security Number is Required and must be in a valid format (XXX-XX-XXXX).</li>" );
                        }

                        // Verify phone numbers
                        validatePhones( action, "HomePhone", "CellPhone", sbErrorMessages );
                        string workPhone = action.Activity.Workflow.GetAttributeValue( "WorkPhone" );
                        if ( !string.IsNullOrEmpty( workPhone ) && !Regex.Match( workPhone, @"^[01]?[- .]?(\([2-9]\d{2}\)|[2-9]\d{2})[- .]?\d{3}[- .]?\d{4}$" ).Success )
                        {
                            sbErrorMessages.AppendLine( "<li>Work Phone must be in a valid format (XXX) XXX-XXXX.</li>" );
                        }
                    }

                    // Make sure we have data for required fields which depend on other fields
                    if ( action.Activity.Workflow.GetAttributeValue( "OutsideKentuckyIndiana" ).AsBoolean() )
                    {
                        if ( string.IsNullOrEmpty( action.Activity.Workflow.GetAttributeValue( "WhenOutsideKentuckyIndiana" ) ) )
                        {
                            sbErrorMessages.AppendLine( "<li>Please indicate when you lived out of state.</li>" );
                        }
                        if ( string.IsNullOrEmpty( action.Activity.Workflow.GetAttributeValue( "StatesOutsideKentuckyIndiana" ) ) )
                        {
                            sbErrorMessages.AppendLine( "<li>Please enter any states you lived in outside of Kentucky or Indiana.</li>" );
                        }
                    }
                    break;
            }
            if ( sbErrorMessages.Length > 0 )
            {
                SetWorkflowAttributeValue( action, GetActionAttributeValue( action, "ErrorMessages" ).AsGuid(), sbErrorMessages.ToString() );

                // If we get here, it failed validation
                return activateActivity( rockContext, action, "FailActivity" );
            }

            SetWorkflowAttributeValue( action, GetActionAttributeValue( action, "ErrorMessages" ).AsGuid(), "" );

            // If we get here, it was successful
            return activateActivity( rockContext, action, "SuccessActivity" );

        }

        private bool activateActivity( RockContext rockContext, WorkflowAction action, string activityAttributeName )
        {

            Guid guid = GetAttributeValue( action, activityAttributeName ).AsGuid();
            if ( guid.IsEmpty() )
            {
                // No activity.  Just be done.
                return true;
            }

            var workflow = action.Activity.Workflow;

            var activityType = new WorkflowActivityTypeService( rockContext ).Queryable()
                .Where( a => a.Guid.Equals( guid ) ).FirstOrDefault();

            if ( activityType == null )
            {
                action.AddLogEntry( "Invalid Activity Property", true );
                return false;
            }

            WorkflowActivity.Activate( activityType, workflow );
            action.AddLogEntry( string.Format( "Activated new '{0}' activity", activityType.ToString() ) );

            return true;
        }

        private void validatePhones( WorkflowAction action, string homeAttributeName, string cellAttributeName, StringBuilder sbErrorMessages )
        {
            // Verify the Phone Numbers
            string homePhone = action.Activity.Workflow.GetAttributeValue( homeAttributeName );
            if ( !string.IsNullOrEmpty( homePhone ) && !Regex.Match( homePhone, @"^[01]?[- .]?(\([2-9]\d{2}\)|[2-9]\d{2})[- .]?\d{3}[- .]?\d{4}$" ).Success )
            {
                sbErrorMessages.AppendLine( "<li>Home Phone must be in a valid format (XXX) XXX-XXXX.</li>" );
            }
            string cellPhone = action.Activity.Workflow.GetAttributeValue( cellAttributeName );
            if ( !string.IsNullOrEmpty( cellPhone ) && !Regex.Match( cellPhone, @"^[01]?[- .]?(\([2-9]\d{2}\)|[2-9]\d{2})[- .]?\d{3}[- .]?\d{4}$" ).Success )
            {
                sbErrorMessages.AppendLine( "<li>Cell Phone must be in a valid format (XXX) XXX-XXXX.</li>" );
            }
            if ( string.IsNullOrEmpty( homePhone ) && string.IsNullOrEmpty( cellPhone ) )
            {
                sbErrorMessages.AppendLine( "<li>Home or Cell Phone is Required.</li>" );
            }
        }

        private void validateAddress( Guid? addressGuid, String addressName, StringBuilder sbErrorMessages )
        {

            // Check out lots of stuff with the fourth reference address
            if ( addressGuid.HasValue && addressGuid != new Guid() )
            {
                LocationService locationService = new LocationService( new RockContext() );
                Location location = locationService.Get( addressGuid.Value );
                if ( string.IsNullOrEmpty( location.Street1 ) )
                {
                    sbErrorMessages.AppendLine( "<li>" + addressName + " - Address Line 1 is Required.</li>" );
                }
                if ( string.IsNullOrEmpty( location.PostalCode ) )
                {
                    sbErrorMessages.AppendLine( "<li>" + addressName + " - Postal Code is Required.</li>" );
                }
            }
            else
            {
                sbErrorMessages.AppendLine( "<li>" + addressName + " is Required.</li>" );
            }
        }

        private void addressesUnique( RockContext rockContext, WorkflowAction action, string activityAttributeName, StringBuilder sbErrorMessages )
        {

            string[] addressAttributes = { "Reference1Address", "Reference2Address", "Reference3Address", "Reference4Address" };

            Guid? addressGuid = action.Activity.Workflow.GetAttributeValue( activityAttributeName ).AsGuidOrNull();
            LocationService locationService = new LocationService( new RockContext() );
            Location address = locationService.Get( addressGuid.Value );
            if ( address != null )
            {
                foreach ( string addressAttribute in addressAttributes )
                {
                    // Don't check to see if an address matches itself.  It does.  We don't care.
                    if ( addressAttribute == activityAttributeName )
                    {
                        continue;
                    }

                    Guid? addressMatchGuid = action.Activity.Workflow.GetAttributeValue( addressAttribute ).AsGuidOrNull();
                    if ( addressMatchGuid.HasValue && addressMatchGuid != new Guid() )
                    {
                        Location addressMatch = locationService.Get( addressMatchGuid.Value );

                        if ( address.Street1.ToLower() == addressMatch.Street1.ToLower()
                            && address.PostalCode.ToLower() == addressMatch.PostalCode.ToLower() )
                        {
                            sbErrorMessages.AppendLine( "<li>Address is NOT unique.  Please make sure all references have different addresses.</li>" );
                        }

                    }
                }
            }
        }
    }
}
