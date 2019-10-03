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
    [Description( "Sets an attribute to a person using Southeast's custom person matching. If single match is not found a new person will be created if configured." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Person Attribute From Fields" )]

    /* Person Information */
    [WorkflowTextOrAttribute( "First Name", "Attribute Value", "The first name or an attribute that contains the first name of the person. <span class='tip tip-lava'></span>",
        false, "", "1: Person Information", 0, "FirstName", new string[] { "Rock.Field.Types.TextFieldType" } )]
    [WorkflowTextOrAttribute( "Last Name", "Attribute Value", "The last name or an attribute that contains the last name of the person. <span class='tip tip-lava'></span>",
        false, "", "1: Person Information", 1, "LastName", new string[] { "Rock.Field.Types.TextFieldType" } )]
    [WorkflowTextOrAttribute( "Date of Birth", "Attribute Value", "The date of birth or an attribute that contains the date of birth of the person. <span class='tip tip-lava'></span>",
        false, "", "1: Person Information", 2, "DOB", new string[] { "Rock.Field.Types.TextFieldType", "Rock.Field.Types.DateFieldType" } )]
    [WorkflowAttribute( "Default Campus", "The attribute value to use as the default campus when creating a new person.",
        true, "", "1: Person Information", 3, "DefaultCampus", new string[] { "Rock.Field.Types.CampusFieldType" } )]

    /* Contact Information */
    [WorkflowAttribute( "Address", "The address or an attribute that contains the address of the person.",
        false, "", "2: Contact Information", 4, "Address", new string[] { "Rock.Field.Types.AddressFieldType" } )]
    [WorkflowTextOrAttribute( "Email Address", "Attribute Value", "The email address or an attribute that contains the email address of the person. <span class='tip tip-lava'></span>",
        false, "", "2: Contact Information", 5, "Email", new string[] { "Rock.Field.Types.TextFieldType", "Rock.Field.Types.EmailFieldType" } )]
    [WorkflowTextOrAttribute( "Phone Number", "Attribute Value", "The phone number or an attribute that contains the phone number of the person. <span class='tip tip-lava'></span>",
        false, "", "2: Contact Information", 6, "Phone", new string[] { "Rock.Field.Types.TextFieldType", "Rock.Field.Types.PhoneNumberFieldType" } )]
    [DefinedValueField( Rock.SystemGuid.DefinedType.PERSON_PHONE_TYPE, "Phone Number Type", "The phone number type to use when adding the phone number.", false, false,
        Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_HOME, "2: Contact Information", 7 )]
    [WorkflowTextOrAttribute( "Unlisted", "Attribute Value", "The value or attribute value to indicate if number should be unlisted. Only valid values are 'True' or 'False' any other value will be ignored. <span class='tip tip-lava'></span>",
        false, "", "2: Contact Information", 8, "Unlisted" )]
    [WorkflowTextOrAttribute( "Messaging Enabled", "Attribute Value", "The value or attribute value to indicate if messaging (SMS) should be enabled for phone. Only valid values are 'True' or 'False' any other value will be ignored. <span class='tip tip-lava'></span>",
        false, "", "2: Contact Information", 9, "MessagingEnabled" )]

    /* Other Settings */
    [WorkflowAttribute( "Person Attribute", "The person attribute to set the value to the person found or created.",
        true, "", "3: Other Settings", 10, "PersonAttribute", new string[] { "Rock.Field.Types.PersonFieldType" } )]
    [DefinedValueField( Rock.SystemGuid.DefinedType.PERSON_RECORD_STATUS, "Default Record Status", "The record status to use when creating a new person", false, false,
        Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_PENDING, "3: Other Settings", 11 )]
    [DefinedValueField( Rock.SystemGuid.DefinedType.PERSON_CONNECTION_STATUS, "Default Connection Status", "The connection status to use when creating a new person", false, false,
        Rock.SystemGuid.DefinedValue.PERSON_CONNECTION_STATUS_WEB_PROSPECT, "3: Other Settings", 12 )]
    [WorkflowAttribute( "Family Group/Member", "A family group or family member to use if this creates a new person.",
        false, "", "3: Other Settings", 13, "FamilyAttribute", new string[] { "Rock.Field.Types.PersonFieldType", "Rock.Field.Types.GroupFieldType" } )]
    [BooleanField ( "Match Only", "If Yes, this will NOT create new person records and the person will only be set if a single match is found.", false,
        "3: Other Settings", 14)]

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
                Guid? familyOrPersonGuid = GetAttributeValue( action, "FamilyAttribute", true ).AsGuidOrNull();
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
                        ( address == null || address != null && string.IsNullOrWhiteSpace( address.Street1 ) ) )
                    )
                {
                    errorMessages.Add( "First Name, Last Name, and one of Email, Phone, DoB, or Address Street are required. One or more of these values was not provided!" );
                }
                else
                {
                    Rock.Model.Person person = null;
                    PersonAlias personAlias = null;
                    var personService = new PersonService( rockContext );
                    var people = personService.GetByMatch( firstName, lastName, dateofBirth, email, phone, address?.Street1, address?.PostalCode ).ToList();
                    if ( people.Count == 1 &&
                         // Make sure their email matches.  If it doesn't, we need to go ahead and create a new person to be matched later.
                         ( string.IsNullOrWhiteSpace( email ) ||
                         ( people.First().Email != null &&
                         email.ToLower().Trim() == people.First().Email.ToLower().Trim() ) )
                       )
                    {
                        person = people.First();
                        personAlias = person.PrimaryAlias;
                    }
                    else if ( !GetAttributeValue( action, "MatchOnly" ).AsBoolean() )
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

                        // Get the default family if applicable
                        Group family = null;
                        if ( familyOrPersonGuid.HasValue )
                        {
                            PersonAliasService personAliasService = new PersonAliasService( rockContext );
                            family = personAliasService.Get( familyOrPersonGuid.Value )?.Person?.GetFamily();
                            if (family == null)
                            {
                                GroupService groupService = new GroupService( rockContext );
                                family = groupService.Get( familyOrPersonGuid.Value );
                            }
                        }
                        var familyGroup = SaveNewPerson( person, family, ( defaultCampus != null ? defaultCampus.Id : ( int? ) null ), rockContext );
                        if ( familyGroup != null && familyGroup.Members.Any() )
                        {
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

                            // gets value indicating if phone number is unlisted
                            string unlistedValue = GetAttributeValue( action, "Unlisted" );
                            Guid? unlistedValueGuid = unlistedValue.AsGuidOrNull();
                            if ( unlistedValueGuid.HasValue )
                            {
                                unlistedValue = action.GetWorklowAttributeValue( unlistedValueGuid.Value );
                            }
                            else
                            {
                                unlistedValue = unlistedValue.ResolveMergeFields( GetMergeFields( action ) );
                            }
                            bool unlisted = unlistedValue.AsBoolean();

                            // gets value indicating if messaging should be enabled for phone number
                            string smsEnabledValue = GetAttributeValue( action, "MessagingEnabled" );
                            Guid? smsEnabledValueGuid = smsEnabledValue.AsGuidOrNull();
                            if ( smsEnabledValueGuid.HasValue )
                            {
                                smsEnabledValue = action.GetWorklowAttributeValue( smsEnabledValueGuid.Value );
                            }
                            else
                            {
                                smsEnabledValue = smsEnabledValue.ResolveMergeFields( GetMergeFields( action ) );
                            }
                            bool smsEnabled = smsEnabledValue.AsBoolean();


                            var phoneModel = person.PhoneNumbers.FirstOrDefault( p => p.NumberTypeValueId == numberType.Id );
                            string oldPhoneNumber = phoneModel != null ? phoneModel.NumberFormattedWithCountryCode : string.Empty;
                            string newPhoneNumber = PhoneNumber.CleanNumber( phone );

                            if ( newPhoneNumber != string.Empty && newPhoneNumber != oldPhoneNumber )
                            {
                                if ( phoneModel == null )
                                {
                                    phoneModel = new PhoneNumber();
                                    person.PhoneNumbers.Add( phoneModel );
                                    phoneModel.NumberTypeValueId = numberType.Id;
                                }
                                else
                                {
                                    oldPhoneNumber = phoneModel.NumberFormattedWithCountryCode;
                                }
                                phoneModel.Number = newPhoneNumber;
                                phoneModel.IsUnlisted = unlisted;
                                phoneModel.IsMessagingEnabled = smsEnabled;

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
                    else if ( !GetAttributeValue( action, "MatchOnly" ).AsBoolean() )
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

        private Group SaveNewPerson(Rock.Model.Person person, Group existingFamily, int? defaultCampus, RockContext rockContext)
        {
            if (existingFamily == null)
            {
                return PersonService.SaveNewPerson( person, rockContext, ( defaultCampus != null ? defaultCampus : ( int? ) null ), false );

            } else
            {
                person.FirstName = person.FirstName.FixCase();
                person.NickName = person.NickName.FixCase();
                person.MiddleName = person.MiddleName.FixCase();
                person.LastName = person.LastName.FixCase();

                // Create/Save Known Relationship Group
                var knownRelationshipGroupType = GroupTypeCache.Read( Rock.SystemGuid.GroupType.GROUPTYPE_KNOWN_RELATIONSHIPS );
                if ( knownRelationshipGroupType != null )
                {
                    var ownerRole = knownRelationshipGroupType.Roles
                        .FirstOrDefault( r =>
                            r.Guid.Equals( Rock.SystemGuid.GroupRole.GROUPROLE_KNOWN_RELATIONSHIPS_OWNER.AsGuid() ) );
                    if ( ownerRole != null )
                    {
                        var groupMember = new GroupMember();
                        groupMember.Person = person;
                        groupMember.GroupRoleId = ownerRole.Id;

                        var group = new Group();
                        group.Name = knownRelationshipGroupType.Name;
                        group.GroupTypeId = knownRelationshipGroupType.Id;
                        group.Members.Add( groupMember );

                        var groupService = new GroupService( rockContext );
                        groupService.Add( group );
                    }
                }

                // Create/Save Implied Relationship Group
                var impliedRelationshipGroupType = GroupTypeCache.Read( Rock.SystemGuid.GroupType.GROUPTYPE_IMPLIED_RELATIONSHIPS );
                if ( impliedRelationshipGroupType != null )
                {
                    var ownerRole = impliedRelationshipGroupType.Roles
                        .FirstOrDefault( r =>
                            r.Guid.Equals( Rock.SystemGuid.GroupRole.GROUPROLE_IMPLIED_RELATIONSHIPS_OWNER.AsGuid() ) );
                    if ( ownerRole != null )
                    {
                        var groupMember = new GroupMember();
                        groupMember.Person = person;
                        groupMember.GroupRoleId = ownerRole.Id;

                        var group = new Group();
                        group.Name = impliedRelationshipGroupType.Name;
                        group.GroupTypeId = impliedRelationshipGroupType.Id;
                        group.Members.Add( groupMember );

                        var groupService = new GroupService( rockContext );
                        groupService.Add( group );
                    }
                }
                var familyGroupType = GroupTypeCache.Read( Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY );

                var adultRole = familyGroupType?.Roles
                   .FirstOrDefault( r =>
                       r.Guid.Equals( Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_ADULT.AsGuid() ) );

                var childRole = familyGroupType?.Roles
                    .FirstOrDefault( r =>
                        r.Guid.Equals( Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_CHILD.AsGuid() ) );

                var age = person.Age;

                var familyRole = age.HasValue && age < 18 ? childRole : adultRole;

                // Add to the existing family
                PersonService.AddPersonToFamily( person, true, existingFamily.Id, familyRole.Id, rockContext );
                return existingFamily;
            }

            
        }

     }
}
 