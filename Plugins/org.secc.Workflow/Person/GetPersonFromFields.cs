// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
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
using System.Linq;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Workflow;
using org.secc.PersonMatch;

namespace org.secc.Workflow.Person.Action
{
    /// <summary>
    /// Sets an attribute's value to the selected person 
    /// </summary>
    [ActionCategory( "SECC > People" )]
    [Description( "Sets an attribute to a person using Southeast's custom person matching. If single match is not found a new person will be created." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Person Attribute From Fields" )]

    [WorkflowTextOrAttribute( "First Name", "Attribute Value", "The first name or an attribute that contains the first name of the person. <span class='tip tip-lava'></span>",
        false, "", "", 0, "FirstName", new string[] { "Rock.Field.Types.TextFieldType" } )]
    [WorkflowTextOrAttribute( "Last Name", "Attribute Value", "The last name or an attribute that contains the last name of the person. <span class='tip tip-lava'></span>",
        false, "", "", 1, "LastName", new string[] { "Rock.Field.Types.TextFieldType" } )]
    [WorkflowTextOrAttribute( "Date of Birth", "Attribute Value", "The date of birth or an attribute that contains the date of birth of the person. <span class='tip tip-lava'></span>",
        false, "", "", 2, "DOB", new string[] { "Rock.Field.Types.TextFieldType", "Rock.Field.Types.DateFieldType" } )]
    [WorkflowTextOrAttribute( "Email Address", "Attribute Value", "The email address or an attribute that contains the email address of the person. <span class='tip tip-lava'></span>", 
        false, "", "", 3, "Email", new string[] { "Rock.Field.Types.TextFieldType", "Rock.Field.Types.EmailFieldType" } )]
    [WorkflowTextOrAttribute("Phone Number", "Attribute Value", "The phone number or an attribute that contains the phone number of the person. <span class='tip tip-lava'></span>",
        false, "", "", 4, "Phone", new string[] { "Rock.Field.Types.TextFieldType", "Rock.Field.Types.PhoneNumberFieldType" })]
    [WorkflowAttribute("Address", "The address or an attribute that contains the address of the person.",
        false, "", "", 5, "Address", new string[] { "Rock.Field.Types.AddressFieldType" })]
    [WorkflowAttribute( "Person Attribute", "The person attribute to set the value to the person found or created.", 
        true, "", "", 6, "PersonAttribute", new string[] { "Rock.Field.Types.PersonFieldType" } )]
    [DefinedValueField( Rock.SystemGuid.DefinedType.PERSON_RECORD_STATUS, "Default Record Status", "The record status to use when creating a new person", false, false,
        Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_PENDING, "", 7 )]
    [DefinedValueField( Rock.SystemGuid.DefinedType.PERSON_CONNECTION_STATUS, "Default Connection Status", "The connection status to use when creating a new person", false, false, 
        Rock.SystemGuid.DefinedValue.PERSON_CONNECTION_STATUS_WEB_PROSPECT, "", 8)]
    [DefinedValueField( Rock.SystemGuid.DefinedType.PERSON_PHONE_TYPE, "Default Phone Number Type", "The phone number type to use when adding the phone number.", false, false,
        Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_HOME, "", 9 )]
    [WorkflowAttribute( "Default Campus", "The attribute value to use as the default campus when creating a new person.",
        true, "", "", 10, "DefaultCampus", new string[] { "Rock.Field.Types.CampusFieldType" } )]
    public class GetPersonFromFields : ActionComponent
    {
        /// <summary>
        /// Executes the specified workflow.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="action">The action.</param>
        /// <param name="entity">The entity.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns></returns>
        public override bool Execute( RockContext rockContext, WorkflowAction action, Object entity, out List<string> errorMessages )
        {
            errorMessages = new List<string>();

            var attribute = AttributeCache.Read( GetAttributeValue( action, "PersonAttribute" ).AsGuid(), rockContext );
            if ( attribute != null )
            {
                var mergeFields = GetMergeFields( action );
                string firstName = GetAttributeValue( action, "FirstName", true ).ResolveMergeFields( mergeFields );
                string lastName = GetAttributeValue( action, "LastName", true ).ResolveMergeFields( mergeFields );
                string email = GetAttributeValue( action, "Email", true ).ResolveMergeFields( mergeFields );
                string phone = GetAttributeValue( action, "Phone", true ).ResolveMergeFields( mergeFields );
                DateTime? dateofBirth = GetAttributeValue( action, "DOB", true ).AsDateTime();
                Guid? addressGuid = GetAttributeValue( action, "Address", true ).AsGuidOrNull();
                Location address = null;
                // Set the street and postal code if we have an address
                if ( addressGuid.HasValue )
                {
                    LocationService addressService = new LocationService( rockContext );
                    address = addressService.Get( addressGuid.Value );
                }


                if ( string.IsNullOrWhiteSpace( firstName ) ||
                     string.IsNullOrWhiteSpace( lastName ) ||
                    ( string.IsNullOrWhiteSpace( email ) &&
                        string.IsNullOrWhiteSpace( phone ) &&
                        !dateofBirth.HasValue &&
                        address == null && 
                        string.IsNullOrWhiteSpace( address.Street1 ) ) 
                    )
                {
                    errorMessages.Add( "First Name, Last Name, and one of Email, Phone, DoB, or Address Street are required. One or more of these values was not provided!" );
                }
                else
                {
                    Rock.Model.Person person = null;
                    PersonAlias personAlias = null;
                    var personService = new PersonService( rockContext );
                    var people = personService.GetByMatch(firstName, lastName, dateofBirth, email, phone, address?.Street1, address?.PostalCode).ToList();
                    if ( people.Count == 1 &&
                         // Make sure their email matches.  If it doesn't, we need to go ahead and create a new person to be matched later.
                         (string.IsNullOrWhiteSpace(email) || 
                         (people.First().Email != null &&
                         email.ToLower().Trim() == people.First().Email.ToLower().Trim() ) ) 
                       )
                    {
                        person = people.First();
                        personAlias = person.PrimaryAlias;
                    }
                    else
                    {
                        // Add New Person
                        person = new Rock.Model.Person();
                        person.FirstName = firstName;
                        person.LastName = lastName;
                        person.IsEmailActive = true;
                        person.Email = email;
                        if ( dateofBirth.HasValue )
                        {
                            person.BirthDay = dateofBirth.Value.Day;
                            person.BirthMonth = dateofBirth.Value.Month;
                            person.BirthYear = dateofBirth.Value.Year;
                        }
                        person.EmailPreference = EmailPreference.EmailAllowed;
                        person.RecordTypeValueId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid() ).Id;

                        var defaultConnectionStatus = DefinedValueCache.Read( GetAttributeValue( action, "DefaultConnectionStatus" ).AsGuid() );
                        if ( defaultConnectionStatus != null )
                        {
                            person.ConnectionStatusValueId = defaultConnectionStatus.Id;
                        }

                        var defaultRecordStatus = DefinedValueCache.Read( GetAttributeValue( action, "DefaultRecordStatus" ).AsGuid() );
                        if ( defaultRecordStatus != null )
                        {
                            person.RecordStatusValueId = defaultRecordStatus.Id;
                        }

                        var defaultCampus = CampusCache.Read( GetAttributeValue( action, "DefaultCampus", true ).AsGuid() );
                        var familyGroup = PersonService.SaveNewPerson( person, rockContext, ( defaultCampus != null ? defaultCampus.Id : (int?)null ), false );
                        if ( familyGroup != null && familyGroup.Members.Any() )
                        {
                            person = familyGroup.Members.Select( m => m.Person ).First();
                            personAlias = person.PrimaryAlias;

                            // If we have an address, go ahead and save it here.
                            if ( address != null )
                            {
                                GroupLocation location = new GroupLocation();
                                location.GroupLocationTypeValueId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME ).Id;
                                location.Location = address;
                                familyGroup.GroupLocations.Add( location );
                            }
                        }


                    }

                    // Save/update the phone number
                    if ( !string.IsNullOrWhiteSpace( phone ) )
                    {
                        List<string> changes = new List<string>();
                        var numberType = DefinedValueCache.Read( GetAttributeValue( action, "DefaultPhoneNumberType" ).AsGuid() );
                        if ( numberType != null )
                        {
                            var phoneModel = person.PhoneNumbers.FirstOrDefault( p => p.NumberTypeValueId == numberType.Id );
                            string oldPhoneNumber = phoneModel != null ? phoneModel.NumberFormattedWithCountryCode : string.Empty;
                            string newPhoneNumber = PhoneNumber.CleanNumber( phone );

                            if ( newPhoneNumber != string.Empty && newPhoneNumber != oldPhoneNumber )
                            {
                                if ( phone == null )
                                {
                                    phoneModel = new PhoneNumber();
                                    person.PhoneNumbers.Add( phoneModel );
                                    phoneModel.NumberTypeValueId = numberType.Id;
                                }
                                else
                                {
                                    oldPhoneNumber = phoneModel.NumberFormattedWithCountryCode;
                                }
                                phoneModel.CountryCode = PhoneNumber.CleanNumber( phone );
                                phoneModel.Number = newPhoneNumber;

                                History.EvaluateChange(
                                    changes,
                                    string.Format( "{0} Phone", numberType.Value ),
                                    oldPhoneNumber,
                                    phoneModel.NumberFormattedWithCountryCode );
                            }

                        }
                    }

                    if ( person != null && personAlias != null )
                    {
                        SetWorkflowAttributeValue( action, attribute.Guid, personAlias.Guid.ToString() );
                        action.AddLogEntry( string.Format( "Set '{0}' attribute to '{1}'.", attribute.Name, person.FullName ) );
                        return true;
                    }
                    else
                    {
                        errorMessages.Add( "Person or Primary Alias could not be determined!" );
                    }
                }
            }
            else
            {
                errorMessages.Add( "Person Attribute could not be found!" );
            }

            if ( errorMessages.Any() )
            {
                errorMessages.ForEach( m => action.AddLogEntry( m, true ) );
                return false;
            }

            return true;
        }

     }
}