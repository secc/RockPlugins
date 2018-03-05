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
    [Description( "Handle storing the SSN." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Volunteer Application SSN" )]
    class VolunteerApplicationSSN : ActionComponent
    {
        public override bool Execute( RockContext rockContext, WorkflowAction action, object entity, out List<string> errorMessages )
        {
            errorMessages = new List<string>();


            var actionStep = GetActionAttributeValue( action, "ActionStep" );
            
            if ( Encryption.DecryptString( action.Activity.Workflow.GetAttributeValue( "SSN1" ) ) == "***")
            {
                // ***'s mean to just ignore it.
                return activateActivity( rockContext, action, "SuccessActivity" );
            }

            // Verify the SSN using regex
            string ssn = Encryption.DecryptString( action.Activity.Workflow.GetAttributeValue( "SSN1" ) ) + "-" +
                Encryption.DecryptString( action.Activity.Workflow.GetAttributeValue( "SSN2" ) ) + "-" +
                Encryption.DecryptString( action.Activity.Workflow.GetAttributeValue( "SSN3" ) );
                    
            if (Regex.Match(ssn, @"^(?!(123[ -]?45[ -]?6789)|((\d)\3\3[ -]?\3\3[ -]?\3\3\3\3))(\d{3}[- ]?\d{2}[- ]?\d{4})$").Success)
            {
                PersonAliasService personAliasService = new PersonAliasService( rockContext );
                // Store the SSN and then set it to ***'s
                PersonAlias personAlias = personAliasService.Get( action.Activity.Workflow.GetAttributeValue( "Person" ).AsGuid() );
                personAlias.Person.LoadAttributes();
                personAlias.Person.SetAttributeValue( "SSN", Encryption.EncryptString( ssn ) );
                personAlias.Person.SaveAttributeValues();
                action.Activity.Workflow.SetAttributeValue( "SSN1", Encryption.EncryptString( "***" ) );
                action.Activity.Workflow.SetAttributeValue( "SSN2", Encryption.EncryptString( "**" ) );
                rockContext.SaveChanges();
            }

            SetWorkflowAttributeValue( action, GetActionAttributeValue( action, "ErrorMessages" ).AsGuid(), "" );

            // If we get here, it was successful
            return activateActivity( rockContext, action, "SuccessActivity");

        }

        private bool activateActivity( RockContext rockContext, WorkflowAction action, string activityAttributeName)
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

        private void validateAddress(Guid? addressGuid, String addressName, StringBuilder sbErrorMessages )
        {

            // Check out lots of stuff with the fourth reference address
            if ( addressGuid.HasValue && addressGuid != new Guid())
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
