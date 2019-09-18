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
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Workflow;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace org.secc.SafetyAndSecurity
{
    [ActionCategory( "SECC > Safety and Security" )]
    [Description( "Validate references in Get References." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Reference Validation" )]
    [WorkflowAttribute( "Error Messages", "Error Messages Attribute", true )]
    [WorkflowAttribute( "Is Minor Application", "Mark as yes if the application is for minors", fieldTypeClassNames: new string[] { "Rock.Field.Types.BooleanFieldType" } )]
    [WorkflowAttribute( "Reference Count", "The current reference count", fieldTypeClassNames: new string[] { "Rock.Field.Types.IntegerFieldType" } )]
    [WorkflowAttribute( "Max Reference Limit", "The maximum reference limit", fieldTypeClassNames: new string[] { "Rock.Field.Types.IntegerFieldType" } )]


    class ReferenceValidation : ActionComponent
    {
        internal class ContactInfo
        {
            public string name { set; get; }
            public string address { set; get; }
            public string phoneNumber { set; get; }
            public string email { set; get; }

        }
        List<ContactInfo> Reference = new List<ContactInfo>();

        public override bool Execute( RockContext rockContext, WorkflowAction action, object entity, out List<string> errorMessages )
        {
            errorMessages = new List<string>();
            StringBuilder sbErrorMessages = new StringBuilder();

            if ( !action.IsNotNull() )
            {
                action.AddLogEntry( "ReferenceValidation: Input (action) is null." );
                return false;
            }

            int refCount = action.Activity.Workflow.GetAttributeValue( "ReferenceCount" ).AsInteger();
            int maxRef = action.Activity.Workflow.GetAttributeValue( "ReferenceLimit" ).AsInteger();
            string firstName = action.Activity.GetAttributeValue( "Firstname" );
            string lastName = action.Activity.GetAttributeValue( "Lastname" );
            string phoneNumber = action.Activity.GetAttributeValue( "Phone" );
            string phoneType = action.Activity.GetAttributeValue( "PhoneType" );
            string email = action.Activity.GetAttributeValue( "Email" );
            Guid? addressGuid = action.Activity.GetAttributeValue( "Address" ).AsGuidOrNull();

            if ( refCount < maxRef )
            {
                validateName( firstName, lastName, sbErrorMessages );
                validatePhone( phoneNumber, phoneType, sbErrorMessages );
                validateEmail( email, sbErrorMessages );
                validateRelationships( action, refCount, maxRef, sbErrorMessages );

                LocationService locationService = new LocationService( new RockContext() );
                Location location = locationService.Get( addressGuid.Value );
                if ( addressGuid.HasValue && addressGuid != new Guid() )
                {
                    validateAddress( location, sbErrorMessages );
                }
                else
                {
                    sbErrorMessages.AppendLine( "<li> A valid address is required.</li>" );
                }

                if ( sbErrorMessages.Length == 0 )
                {
                    ContactInfo userInfo = new ContactInfo();
                    userInfo.name = formatName( firstName, lastName );
                    userInfo.address = formatAddress( location );
                    userInfo.phoneNumber = formatPhone( phoneNumber );
                    userInfo.email = email.Trim();

                    //if this is the 1st successful entry,
                    //store in the list for future comparison.
                    if ( refCount == 0 )
                    {
                        Reference.Clear();
                        Reference.Add( userInfo );
                    }
                    else if ( refCount > 0 && refCount < maxRef )
                    {
                        for ( var i = 0; i < Reference.Count; i++ )
                        {
                            string fieldCorrectionStr = "";
                            if ( checkForDuplicates( Reference[i], userInfo, out fieldCorrectionStr ) )
                            {
                                if ( !String.IsNullOrEmpty( fieldCorrectionStr ) )
                                {
                                    string[] words = fieldCorrectionStr.Trim().Split( ' ' );
                                    foreach ( var word in words )
                                    {
                                        sbErrorMessages.AppendFormat( "<li>This reference's {0} matches that of reference {1}, Please correct.</li>", word.Trim(), i + 1 );
                                    }
                                }
                                else
                                    action.AddLogEntry( "ReferenceValidation: CheckForDuplicates returns true, but correction string is empty." );
                            }
                        }
                        if ( sbErrorMessages.Length == 0 )
                        {
                            Reference.Add( userInfo );
                        }
                    }
                }
            }
            Guid ErMsg = GetActionAttributeValue( action, "ErrorMessages" ).AsGuid();

            if ( sbErrorMessages.Length > 0 )
            {
                // If we get here, validation has failed
                SetWorkflowAttributeValue( action, ErMsg, sbErrorMessages.ToString() );
                action.AddLogEntry( "ReferenceValidation: Validation failed" );
                return reactivateCurrentActions( rockContext, action );
            }
            // If we get here, validation was successful
            SetWorkflowAttributeValue( action, ErMsg, string.Empty );
            action.AddLogEntry( "ReferenceValidation: Validation successful" );
            return true;
        }
        private bool reactivateCurrentActions( RockContext rockContext, WorkflowAction action )
        {
            foreach ( var a in action.Activity.Actions )
            {
                a.CompletedDateTime = null;
            }
            action.AddLogEntry( "Reactivated all actions in activity" );
            return true;
        }
        private void validateName( string fname, string lname, StringBuilder sbErrorMessages )
        {
            if ( String.IsNullOrEmpty( fname ) || fname.Any( char.IsDigit ) )
            {
                sbErrorMessages.AppendLine( "<li>A valid first name is required.</li>" );
            }
            if ( String.IsNullOrEmpty( lname ) || lname.Any( char.IsDigit ) )
            {
                sbErrorMessages.AppendLine( "<li>A valid last name is required.</li>" );
            }
        }
        private void validatePhone( string phone, string phone_type, StringBuilder sbErrorMessages )
        {
            if ( string.IsNullOrEmpty( phone_type ) )
            {
                sbErrorMessages.AppendLine( "<li>Your reference's phone type is required.</li>" );

            }
            if ( string.IsNullOrEmpty( phone ) )
            {
                sbErrorMessages.AppendLine( "<li>Your reference's phone number is required.</li>" );

            }
            else if ( !Regex.Match( phone, @"^[01]?[- .]?(\([2-9]\d{2}\)|[2-9]\d{2})[- .]?\d{3}[- .]?\d{4}$" ).Success )
            {
                sbErrorMessages.AppendLine( "<li>Phone must be in a valid format (XXX) XXX-XXXX.</li>" );
            }
        }

        private void validateAddress( Location location, StringBuilder sbErrorMessages )
        {
            if ( string.IsNullOrEmpty( location.Street1 ) || !location.Street1.Any( char.IsDigit ) )
            {
                sbErrorMessages.AppendLine( "<li> A valid street address is required.</li>" );
            }
            if ( string.IsNullOrEmpty( location.City ) || location.City.Any( char.IsDigit ) )
            {
                sbErrorMessages.AppendLine( "<li> A valid city is required.</li>" );
            }
            if ( string.IsNullOrEmpty( location.State ) || location.State.Any( char.IsDigit ) )
            {
                sbErrorMessages.AppendLine( "<li> A valid state is required.</li>" );
            }
            if ( string.IsNullOrEmpty( location.PostalCode ) )
            {
                sbErrorMessages.AppendLine( "<li> A valid postal code is required.</li>" );
            }
            else if ( !Regex.IsMatch( location.PostalCode, "^[0-9]{5}(?:-[0-9]{4})?$" ) )
            {
                sbErrorMessages.AppendLine( "<li> The postal code must be a valid format: XXXXX OR XXXXX-XXXX.</li>" );
            }
        }
        private void validateEmail( string email, StringBuilder sbErrorMessages )
        {
            if ( String.IsNullOrEmpty( email ) )
            {
                sbErrorMessages.AppendLine( "<li>An email is required.</li>" );
            }
        }
        private void validateRelationships( WorkflowAction action, int ref_count, int max_ref, StringBuilder sbErrorMessages )
        {
            bool minor = action.Activity.Workflow.GetAttributeValue( "IsMinor" ).AsBoolean();
            bool employee = action.Activity.GetAttributeValue( "SCCEmployee" ).AsBoolean();

            if ( action.Activity.GetAttributeValue( "Relative" ).AsBoolean() )
            {
                sbErrorMessages.AppendLine( "<li>Your reference must not be a relative.</li>" );
            }
            if ( String.IsNullOrEmpty( action.Activity.GetAttributeValue( "Relationship" ) ) )
            {
                sbErrorMessages.AppendLine( "<li>A relationship is required.</li>" );
            }
            if ( employee )
            {
                //if this is the not the last reference OR
                //if this is the last reference and it is a minor
                if ( ( ref_count < max_ref - 1 ) || (ref_count == max_ref - 1 && !minor ) )
                {
                    sbErrorMessages.AppendLine( "<li>Your reference must not be a staff member of Southeast Christian Church.</li>" );
                }
            }
        }
        private string formatName( string fname, string lname )
        {
            string name = String.Concat( fname.ToLower().Trim(), lname.ToLower().Trim() );
            name = name.Replace( " ", "" );
            return ( name );
        }

        private string formatPhone( string number )
        {
            number = number.Replace( "(", "" ).Trim();
            number = number.Replace( ")", "" );
            number = number.Replace( "-", "" );
            number = number.Replace( " ", "" );
            return ( number );
        }
        private string formatStreet( string street )
        {
            street = street.Trim();
            street = street.Substring( 0, street.LastIndexOf( " " ) + 1 );
            street = street.Replace( " ", "" );
            return ( street);
        }

        private string formatAddress( Location location )
        {
            string address;
            address = String.Concat( formatStreet( location.Street1.ToLower() ), 
                                     formatStreet( location.Street2.ToLower() ),
                                     location.City.ToLower(), location.State.ToLower(), 
                                     location.PostalCode.SafeSubstring( 0, 5 ) );
            address = address.Replace( " ", "" );
            address = address.Replace( "-", "" ).Trim();
            return ( address );
        }

        private bool checkForDuplicates( ContactInfo listInfo, ContactInfo userInfo, out string correctionStr )
        {
            bool duplicates = false;
            correctionStr = "";
            if ( listInfo.name == userInfo.name )
            {
                duplicates = true;
                correctionStr = string.Concat( correctionStr, "name " );
            }
            if ( listInfo.address == userInfo.address )
            {
                duplicates = true;
                correctionStr = string.Concat( correctionStr, "address " );
            }
            if ( listInfo.phoneNumber == userInfo.phoneNumber )
            {
                duplicates = true;
                correctionStr = string.Concat( correctionStr, "phone " );
            }
            if ( listInfo.email == userInfo.email )
            {
                duplicates = true;
                correctionStr = string.Concat( correctionStr, "email " );
            }
            return ( duplicates );
        }
    }
}