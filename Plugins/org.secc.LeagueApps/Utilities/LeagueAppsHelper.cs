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

            // First try standard matching with the full last name as provided
            var matches = personService.GetByMatch(
                member.firstName?.Trim(),
                member.lastName?.Trim(),
                member.birthDate,
                email, null,
                member.address1,
                member.zipCode );

            // Track parsed suffix info (for use when creating a new person or retrying matches)
            string baseLastName = member.lastName?.Trim() ?? string.Empty;
            int? suffixValueId = null;

            // If no matches found, try matching with suffix parsed out of last name
            if ( !matches.Any() && !string.IsNullOrWhiteSpace( member.lastName ) )
            {
                var parsedName = ParseLastNameAndSuffix( member.lastName.Trim() );

                // Only retry if we actually found a suffix
                if ( parsedName.SuffixValueId.HasValue )
                {
                    baseLastName = parsedName.LastName;
                    suffixValueId = parsedName.SuffixValueId;

                    matches = personService.GetByMatch(
                        member.firstName?.Trim(),
                        baseLastName,
                        member.birthDate,
                        email, null,
                        member.address1,
                        member.zipCode );
                }
            }

            // If still no matches found, try matching with previous names using the original last name
            if ( !matches.Any() && !string.IsNullOrWhiteSpace( member.lastName ) && !string.IsNullOrWhiteSpace( email ) )
            {
                matches = FindMatchByPreviousName( personService, rockContext, member.firstName?.Trim(), member.lastName.Trim(), email );

                // Also try with the parsed base last name if we found a suffix
                if ( !matches.Any() && suffixValueId.HasValue )
                {
                    matches = FindMatchByPreviousName( personService, rockContext, member.firstName?.Trim(), baseLastName, email );
                }
            }

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

            // Validate and clear invalid email for matched persons
            if ( person != null && !string.IsNullOrWhiteSpace( person.Email ) )
            {
                if ( !person.Email.IsValidEmail() )
                {
                    person.Email = string.Empty;
                }
            }

            if ( person == null )
            {
                // Parse suffix now if we haven't already (for creating the new person correctly)
                if ( !suffixValueId.HasValue && !string.IsNullOrWhiteSpace( member.lastName ) )
                {
                    var parsedName = ParseLastNameAndSuffix( member.lastName.Trim() );
                    baseLastName = parsedName.LastName;
                    suffixValueId = parsedName.SuffixValueId;
                }

                // Create the person
                person = new Person();
                person.FirstName = member.firstName?.Trim() ?? string.Empty;
                person.LastName = baseLastName;

                // Set the suffix if we parsed one
                if ( suffixValueId.HasValue )
                {
                    person.SuffixValueId = suffixValueId;
                }

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

                if ( member.mobilePhone.IsNotNullOrWhiteSpace() && member.mobilePhone.StartsWith( "+1" ) )
                {
                    var cleanNumber = PhoneNumber.CleanNumber( member.mobilePhone ) ?? String.Empty;
                    var number = cleanNumber.Length >= 10 ? cleanNumber.Right( 10 ) : cleanNumber;
                    person.PhoneNumbers.Add( new PhoneNumber
                    {
                        Number = number,
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
                    var matchingFamily = matchingFamilies.Where( f => f.Members.All( m => m.Person.ConnectionStatusValueId == defaultConnectionValue.Id ) && f.Name == baseLastName + " Family" );
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

        /// <summary>
        /// Finds a person match by checking previous names.
        /// </summary>
        /// <param name="personService">The person service.</param>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="firstName">The first name to match.</param>
        /// <param name="lastName">The last name to check against previous names.</param>
        /// <param name="email">The email to match.</param>
        /// <returns>A queryable of matching persons.</returns>
        private static IQueryable<Person> FindMatchByPreviousName( PersonService personService, RockContext rockContext, string firstName, string lastName, string email )
        {
            // Build a query for people with matching first name and email,
            // and who have a previous last name that matches the provided last name.
            var personPreviousNameService = new PersonPreviousNameService( rockContext );

            var query = personService.Queryable()
                .Where( p => ( p.NickName == firstName || p.FirstName == firstName )
                            && p.Email == email )
                .Where( p => personPreviousNameService.Queryable()
                    .Any( ppn => ppn.PersonAlias.PersonId == p.Id
                                 && ppn.LastName == lastName ) );

            return query;
        }

        /// <summary>
        /// Finds a person match by checking previous names.
        /// This overload accepts pre-queried data for testability without database dependencies.
        /// </summary>
        /// <param name="potentialMatches">List of potential person matches with their previous names.</param>
        /// <param name="firstName">The first name to match.</param>
        /// <param name="lastName">The last name to check against previous names.</param>
        /// <param name="email">The email to match.</param>
        /// <returns>A list of matching PersonMatchInfo objects.</returns>
        public static List<PersonMatchInfo> FindMatchByPreviousName( List<PersonMatchInfo> potentialMatches, string firstName, string lastName, string email )
        {
            if ( potentialMatches == null || !potentialMatches.Any() )
            {
                return new List<PersonMatchInfo>();
            }

            if ( string.IsNullOrWhiteSpace( firstName ) || string.IsNullOrWhiteSpace( lastName ) || string.IsNullOrWhiteSpace( email ) )
            {
                return new List<PersonMatchInfo>();
            }

            var matches = potentialMatches
                .Where( p => ( string.Equals( p.NickName, firstName, StringComparison.OrdinalIgnoreCase ) ||
                               string.Equals( p.FirstName, firstName, StringComparison.OrdinalIgnoreCase ) ) &&
                         string.Equals( p.Email, email, StringComparison.OrdinalIgnoreCase ) &&
                         p.PreviousLastNames != null &&
                         p.PreviousLastNames.Any( pln => string.Equals( pln, lastName, StringComparison.OrdinalIgnoreCase ) ) )
                .Take( 1 )
                .ToList();

            return matches;
        }

        /// <summary>
        /// Parses a last name to separate the base last name from any suffix (e.g., "Smith Jr." -> "Smith" + Jr suffix).
        /// Uses Rock's DefinedType cache to get suffix values.
        /// </summary>
        /// <param name="fullLastName">The full last name which may include a suffix.</param>
        /// <returns>A ParsedLastName containing the base last name and optional suffix value ID.</returns>
        public static ParsedLastName ParseLastNameAndSuffix( string fullLastName )
        {
            // Get all suffix defined values from Rock's cache
            var suffixDefinedType = DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.PERSON_SUFFIX.AsGuid() );

            var suffixes = suffixDefinedType?.DefinedValues?
                .Select( dv => new SuffixInfo { Id = dv.Id, Value = dv.Value } )
                .ToList() ?? new List<SuffixInfo>();

            return ParseLastNameAndSuffix( fullLastName, suffixes );
        }

        /// <summary>
        /// Parses a last name to separate the base last name from any suffix (e.g., "Smith Jr." -> "Smith" + Jr suffix).
        /// This overload accepts a list of suffixes for testability without database dependencies.
        /// </summary>
        /// <param name="fullLastName">The full last name which may include a suffix.</param>
        /// <param name="suffixes">The list of valid suffix values to check against.</param>
        /// <returns>A ParsedLastName containing the base last name and optional suffix value ID.</returns>
        public static ParsedLastName ParseLastNameAndSuffix( string fullLastName, List<SuffixInfo> suffixes )
        {
            var result = new ParsedLastName { LastName = fullLastName, SuffixValueId = null };

            if ( string.IsNullOrWhiteSpace( fullLastName ) )
            {
                result.LastName = string.Empty;
                return result;
            }

            if ( suffixes == null || !suffixes.Any() )
            {
                return result;
            }

            // Order by length descending to check longer suffixes first (e.g., "III" before "II")
            // Filter out suffixes with null or empty values to prevent NullReferenceException
            var orderedSuffixes = suffixes
                .Where( s => !string.IsNullOrWhiteSpace( s.Value ) )
                .OrderByDescending( s => s.Value.Length )
                .ToList();

            foreach ( var suffix in orderedSuffixes )
            {
                var suffixValue = suffix.Value;

                // Check for suffix at the end with various separators
                // Handle cases like: "Smith Jr.", "Smith Jr", "Smith, Jr.", "Smith, Jr", "Smith III"
                var patterns = new[]
                {
                    $", {suffixValue}.",
                    $", {suffixValue}",
                    $" {suffixValue}.",
                    $" {suffixValue}"
                };

                foreach ( var pattern in patterns )
                {
                    if ( fullLastName.EndsWith( pattern, StringComparison.OrdinalIgnoreCase ) )
                    {
                        result.LastName = fullLastName.Substring( 0, fullLastName.Length - pattern.Length ).Trim();
                        result.SuffixValueId = suffix.Id;
                        return result;
                    }
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Represents a suffix with its ID and value for use in parsing.
    /// </summary>
    public class SuffixInfo
    {
        /// <summary>
        /// Gets or sets the defined value identifier for the suffix.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the suffix value (e.g., "Jr.", "Sr.", "III").
        /// </summary>
        public string Value { get; set; }
    }

    /// <summary>
    /// Represents person information for matching purposes.
    /// </summary>
    public class PersonMatchInfo
    {
        /// <summary>
        /// Gets or sets the person identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the person's first name.
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// Gets or sets the person's nick name.
        /// </summary>
        public string NickName { get; set; }

        /// <summary>
        /// Gets or sets the person's last name.
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// Gets or sets the person's email address.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the list of previous last names for this person.
        /// </summary>
        public List<string> PreviousLastNames { get; set; }
    }

    /// <summary>
    /// Result of parsing a last name that may contain a suffix.
    /// </summary>
    public class ParsedLastName
    {
        /// <summary>
        /// Gets or sets the base last name without the suffix.
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// Gets or sets the defined value identifier of the parsed suffix, or null if no suffix was found.
        /// </summary>
        public int? SuffixValueId { get; set; }
    }
}
