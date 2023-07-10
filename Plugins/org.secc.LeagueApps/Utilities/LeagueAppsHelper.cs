﻿// <copyright>
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
using System.Data.Entity;
using System.Linq;
using org.secc.LeagueApps.Contracts;
using org.secc.PersonMatch;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.LeagueApps.Utilities
{
    public static class LeagueAppsHelper
    {
        public static Group GetFamilyByApiId( int leagueAppsGroupId )
        {
            if ( leagueAppsGroupId < 1 )
            {
                return null;
            }

            RockContext rockContext = new RockContext();
            GroupService groupService = new GroupService( rockContext );

            var attribute = AttributeCache.Get( Constants.ATTRIBUTE_GROUP_FAMILY_ID.AsGuid() );

            AttributeValueService attributeValueService = new AttributeValueService( rockContext );
            var attributeValue = attributeValueService.Queryable().AsNoTracking()
                .Where( av => av.AttributeId == attribute.Id && av.Value == leagueAppsGroupId.ToString() )
                .FirstOrDefault();

            if ( attributeValue == null )
            {
                return null;
            }

            return groupService.GetNoTracking( attributeValue.EntityId ?? 0 );
        }


        public static Person GetPersonByApiId( int leagueAppsPersonId )
        {
            RockContext rockContext = new RockContext();
            AttributeValueService attributeValueService = new AttributeValueService( rockContext );
            PersonService personService = new PersonService( rockContext );

            var attribute = AttributeCache.Get( Constants.ATTRIBUTE_PERSON_USER_ID.AsGuid() );

            var userId = leagueAppsPersonId.ToString() + '|';

            var attributeValue = attributeValueService.Queryable().AsNoTracking()
                .Where( av => av.AttributeId == attribute.Id && av.Value.Contains( userId ) )
                .FirstOrDefault();

            if ( attributeValue == null )
            {
                return null;
            }

            return personService.GetNoTracking( attributeValue.EntityId ?? 0 );
        }

        public static Person CreatePersonFromMember( Member member, DefinedValueCache defaultConnectionValue )
        {
            Person person = null;

            RockContext rockContext = new RockContext();
            PersonService personService = new PersonService( rockContext );
            LocationService locationService = new LocationService( rockContext );

            var email = string.IsNullOrEmpty( member.email ) ? String.Empty : member.email.Trim();

            if ( email != String.Empty )
            {
                if ( !email.IsValidEmail() )
                    email = String.Empty;
            }

            var matches = personService.GetByMatch(
                member.firstName.Trim(),
                member.lastName.Trim(),
                member.birthDate,
                email, null,
                 member.address1,
                member.zipCode );

            Location location = new Location()
            {
                Street1 = member.address1,
                Street2 = member.address2,
                City = member.city,
                State = member.state,
                PostalCode = member.zipCode,
                Country = member.country ?? "US"
            };

            if ( matches.Count() > 1 )
            {
                // Find the oldest member record in the list
                person = matches.Where( p => p.ConnectionStatusValue.Value == "Member" ).OrderBy( p => p.Id ).FirstOrDefault();

                if ( person == null )
                {
                    // Find the oldest attendee record in the list
                    person = matches.Where( p => p.ConnectionStatusValue.Value == "Attendee" ).OrderBy( p => p.Id ).FirstOrDefault();
                    if ( person == null )
                    {
                        person = matches.OrderBy( p => p.Id ).First();
                    }
                }
            }
            else if ( matches.Count() == 1 )
            {
                person = matches.First();
            }
            else
            {
                // Create the person
                person = new Person();
                person.FirstName = member.firstName.Trim();
                person.LastName = member.lastName.Trim();

                if ( member.birthDate.HasValue )
                {
                    person.SetBirthDate( member.birthDate.Value );
                }

                if ( email.IsNotNullOrWhiteSpace() )
                {
                    person.Email = email;
                }

                person.RecordTypeValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid() ).Id;
                person.ConnectionStatusValueId = defaultConnectionValue.Id;
                person.RecordStatusValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_ACTIVE.AsGuid() ).Id;
                person.IsSystem = false;
                person.IsDeceased = false;
                var gender = member.gender;

                if ( !String.IsNullOrEmpty( gender ) )
                {
                    gender.Trim();

                    if ( gender == "Male" || gender == "male" )
                        person.Gender = Gender.Male;
                    else if ( gender == "Female" || gender == "female" )
                        person.Gender = Gender.Female;
                    else
                        person.Gender = Gender.Unknown;
                }
                else
                {
                    person.Gender = Gender.Unknown;
                }

                if(member.mobilePhone.IsNotNullOrWhiteSpace() && member.mobilePhone.StartsWith("+1"))
                {
                    var cleanNumber = PhoneNumber.CleanNumber( member.mobilePhone );
                    person.PhoneNumbers.Add( new PhoneNumber
                    {
                        Number = cleanNumber.Right( 10 ),
                        CountryCode = PhoneNumber.DefaultCountryCode(),
                        NumberTypeValueId = DefinedValueCache.GetId( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() ),
                        IsUnlisted = false
                    } );
                }

                locationService.Verify( location, true );
                bool existingFamily = false;

                var familyGroupType = GroupTypeCache.Get( Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY );
                var adultRole = familyGroupType.Roles
                         .FirstOrDefault( r =>
                             r.Guid.Equals( Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_ADULT.AsGuid() ) );

                var childRole = familyGroupType.Roles
                    .FirstOrDefault( r =>
                        r.Guid.Equals( Rock.SystemGuid.GroupRole.GROUPROLE_FAMILY_MEMBER_CHILD.AsGuid() ) );

                //Look for the family by groupId
                var familyFromAttribute = LeagueAppsHelper.GetFamilyByApiId( member.groupId );
                if ( familyFromAttribute != null )
                {
                    var familyRole = person.Age.HasValue && person.Age < 18 ? childRole : adultRole;
                    PersonService.AddPersonToFamily( person, true, familyFromAttribute.Id, familyRole.Id, rockContext );
                    existingFamily = true;
                }


                if ( !existingFamily && !string.IsNullOrWhiteSpace( member.address1 ) )
                {
                    // See if we can find an existing family using the location where everyone is a web prospect
                    var matchingLocations = locationService.Queryable().Where( l => l.Street1 == location.Street1 && l.PostalCode == location.PostalCode );
                    var matchingFamilies = matchingLocations.Where( l => l.GroupLocations.Any( gl => gl.Group.GroupTypeId == familyGroupType.Id ) ).SelectMany( l => l.GroupLocations ).Select( gl => gl.Group );
                    var matchingFamily = matchingFamilies.Where( f => f.Members.All( m => m.Person.ConnectionStatusValueId == defaultConnectionValue.Id ) && f.Name == member.lastName + " Family" );
                    if ( matchingFamily.Count() == 1 )
                    {
                        var familyRole = person.Age.HasValue && person.Age < 18 ? childRole : adultRole;
                        PersonService.AddPersonToFamily( person, true, matchingFamily.FirstOrDefault().Id, familyRole.Id, rockContext );
                        existingFamily = true;
                    }
                }

                if ( !existingFamily )
                {
                    Group family = PersonService.SaveNewPerson( person, rockContext );
                    GroupLocation groupLocation = new GroupLocation();
                    groupLocation.GroupLocationTypeValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME ).Id;
                    groupLocation.Location = location;
                    groupLocation.IsMappedLocation = true;
                    family.CampusId = CampusCache.All().FirstOrDefault().Id;
                    family.GroupLocations.Add( groupLocation );
                    if ( member.groupId > 0 )
                    {
                        family.LoadAttributes();
                        family.SetAttributeValue( Constants.LeagueAppsFamilyId, member.groupId );
                        family.SaveAttributeValues();
                    }
                }

                rockContext.SaveChanges();
            }


            var groupLocationType = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME );

            // Check to see if the address/location should be updated.
            if ( member.dateJoined > person.GetFamilies().SelectMany( f => f.GroupLocations ).Where( gl => gl.GroupLocationTypeValueId == groupLocationType.Id ).Max( gl => gl.CreatedDateTime ?? gl.Location.CreatedDateTime ) )
            {
                if ( !location.StandardizedDateTime.HasValue )
                {
                    locationService.Verify( location, true );
                }
                var allLocations = person.GetFamilies().SelectMany( f => f.GroupLocations );
                if ( location.Street1 != null && location.StandardizedDateTime != null && !allLocations.Any( hl => hl.Location.Street1 == location.Street1 ) && !location.Street1.Contains( "PO Box" ) && !location.Street1.Contains( "PMB" ) )
                {
                    locationService.Add( location );
                    rockContext.SaveChanges();


                    // Get all existing addresses of the specified type
                    var groupLocations = person.GetFamily().GroupLocations.Where( l => l.GroupLocationTypeValueId == groupLocationType.Id ).ToList();

                    // Create a new address of the specified type, saving all existing addresses of that type as Previous Addresses
                    // Use the Is Mailing and Is Mapped values from any of the existing addresses of that type have those values set to true
                    GroupService.AddNewGroupAddress( rockContext, person.GetFamily(),
                        groupLocationType.Guid.ToString(), location.Id, true,
                        "LeagueApps Import Data Job",
                        groupLocations.Any( x => x.IsMailingLocation ),
                        groupLocations.Any( x => x.IsMappedLocation ) );
                }
            }

            // Update the person's LeagueApps User ID attribute
            person.LoadAttributes();
            var attributevaluelist = person.GetAttributeValue( Constants.LeagueAppsUserId ).SplitDelimitedValues().ToList();
            if ( !attributevaluelist.Contains( member.userId.ToString() ) )
            {
                attributevaluelist.Add( member.userId.ToString() );
                person.SetAttributeValue( Constants.LeagueAppsUserId, string.Join( "|", attributevaluelist ) + "|" );
                person.SaveAttributeValues();
            }

            return person;
        }
    }
}
