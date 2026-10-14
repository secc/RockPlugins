// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License should be included with this file.
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
using System.Linq;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.PersonMatch
{
    // The following extension methods can be accessed by instances of any 
    // class that implements IMyInterface.
    public static class Extension
    {
        const string DIMINUTIVE_NAMES = "3E2D2BEE-01BE-4D1E-8634-01932718AEA3";
        const string GOES_BY_ATTRIBUTE = "C31FDA8A-8CAB-4A1C-B96D-275415B5BB1C";

        public static IEnumerable<Person> GetByMatch( this PersonService personService, String firstName, String lastName, DateTime? birthDate, String email = null, String phone = null, String street1 = null, String postalCode = null, bool createNameless = false )
        {
            using ( Rock.Data.RockContext context = new Rock.Data.RockContext() )
            {
                //FirstName LastName and (DOB or email or phone or street address) are required. If not return an empty list.
                if ( firstName.IsNullOrWhiteSpace() || lastName.IsNullOrWhiteSpace() ||
                    ( !birthDate.HasValue &&
                        email.IsNullOrWhiteSpace() &&
                        phone.IsNullOrWhiteSpace() &&
                        street1.IsNullOrWhiteSpace() ) )
                {
                    if ( createNameless && ( email.IsNotNullOrWhiteSpace() || phone.IsNotNullOrWhiteSpace() ) )
                    {
                        return new List<Person> { GetOrCreateNamelessPerson( email, phone, personService ) };
                    }
                    else
                    {
                        return new List<Person>();
                    }
                }

                LocationService locationService = new LocationService( context );
                List<List<string>> diminutiveGroups = GetDiminutiveGroups( context );

                firstName = ( firstName ?? string.Empty ).Trim();
                lastName = lastName ?? string.Empty;
                email = email == null ? string.Empty : email.ToLower() ?? string.Empty;
                phone = PhoneNumber.CleanNumber( phone ?? string.Empty );
                List<Person> matchingPersons = new List<Person>();

                // Do a quick check to see if we get a match right up front
                List<Person> persons = new List<Person>();
                if ( birthDate.HasValue || !string.IsNullOrEmpty( email ) )
                {
                    var fastQuery = personService.Queryable( false, false ).Where( p => ( p.FirstName.ToLower() == firstName.ToLower() || p.NickName.ToLower() == firstName.ToLower() ) && p.LastName == lastName );
                    if ( birthDate.HasValue )
                    {
                        fastQuery = fastQuery.Where( p => p.BirthDate == birthDate );
                    }
                    if ( !String.IsNullOrEmpty( email ) )
                    {
                        fastQuery = fastQuery.Where( p => p.Email.ToLower() == email );
                    }
                    persons = fastQuery.ToList();

                    // We have an exact match.  Just be done.
                    if ( persons.Count == 1 )
                    {
                        return persons;
                    }
                }

                // Go ahead and do this more leniant search if we get this far
                persons = personService.Queryable( false, false )
                    .Where( p =>
                         p.LastName == lastName &&
                         ( !birthDate.HasValue || p.BirthDate == null || ( birthDate.HasValue && p.BirthDate.Value == birthDate.Value ) ) )
                    .ToList();

                // Set a placeholder for the location so we only geocode it 1 time
                Location location = null;

                foreach ( Person person in persons )
                {
                    // Check to see if the phone exists anywhere in the family
                    Boolean phoneExists = !string.IsNullOrWhiteSpace( phone ) && person.GetFamilies().Where( f => f.Members.Where( m => m.Person.PhoneNumbers.Where( pn => pn.Number == phone ).Any() ).Any() ).Any();

                    // Check to see if the email exists anywhere in the family
                    Boolean emailExists = !string.IsNullOrWhiteSpace( email ) && person.GetFamilies().Where( f => f.Members.Where( m => m.Person.Email == email ).Any() ).Any();

                    Boolean addressMatches = false;
                    // Check the address if it was passed
                    try
                    {
                        if (!string.IsNullOrEmpty( street1 ) && !string.IsNullOrEmpty( postalCode ))
                        {
                            if (person.GetHomeLocation() != null)
                            {
                                if (person.GetHomeLocation().Street1 == street1)
                                {
                                    addressMatches = true;
                                }
                                // If it doesn't match, we need to geocode it and check it again
                                if (location == null && !string.IsNullOrEmpty( street1 ) && !string.IsNullOrEmpty( postalCode ))
                                {
                                    location = new Location();
                                    location.Street1 = street1;
                                    location.PostalCode = postalCode;
                                    locationService.Verify( location, true );

                                }
                                if (location != null && !addressMatches && person.GetHomeLocation().Street1 == location.Street1)
                                {
                                    addressMatches = true;
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                    }


                    // At least phone, email, or address have to match
                    if ( phoneExists || emailExists || addressMatches )
                    {
                        matchingPersons.Add( person );
                    }

                }

                List<Person> firstNameMatchingPersons = new List<Person>();

                // Now narrow down the list by looking for the first name (or diminutive name)
                foreach ( Person matchingPerson in matchingPersons )
                {
                    if ( FirstNameMatches( firstName, matchingPerson.FirstName, matchingPerson.NickName, diminutiveGroups ) )
                    {
                        firstNameMatchingPersons.Add( matchingPerson );
                    }
                }

                return firstNameMatchingPersons;
            }
        }

        /// <summary>
        /// Loads the Diminutive Names defined values as name groups. Each group holds the defined value
        /// plus its "Goes By" names. A defined value without a "Goes By" value is a group of one name.
        /// Returns an empty list if the defined type or the attribute is missing.
        /// </summary>
        private static List<List<string>> GetDiminutiveGroups( RockContext context )
        {
            var diminutiveGroups = new List<List<string>>();

            var diminutiveNames = DefinedTypeCache.Get( DIMINUTIVE_NAMES.AsGuid() );
            var goesByAttribute = AttributeCache.Get( GOES_BY_ATTRIBUTE.AsGuid() );
            if ( diminutiveNames == null || goesByAttribute == null )
            {
                return diminutiveGroups;
            }

            var goesByValues = new AttributeValueService( context )
                .GetByAttributeId( goesByAttribute.Id )
                .Where( av => av.EntityId.HasValue )
                .Select( av => new { EntityId = av.EntityId.Value, av.Value } )
                .ToList()
                .GroupBy( av => av.EntityId )
                .ToDictionary( g => g.Key, g => g.First().Value );

            foreach ( DefinedValueCache dv in diminutiveNames.DefinedValues.Where( v => v.IsActive ) )
            {
                var nameList = new List<string> { dv.Value };
                string goesBy;
                if ( goesByValues.TryGetValue( dv.Id, out goesBy ) && !string.IsNullOrEmpty( goesBy ) )
                {
                    nameList.AddRange( goesBy.Split( '|' ) );
                }

                diminutiveGroups.Add( nameList );
            }

            return diminutiveGroups;
        }

        /// <summary>
        /// Determines whether a person's first or nick name matches the requested first name. The names
        /// match when the input equals either name, or when a diminutive group contains both the input and
        /// either name. Comparisons are case-insensitive and ignore surrounding whitespace.
        /// </summary>
        internal static bool FirstNameMatches( string input, string firstName, string nickName, IEnumerable<IEnumerable<string>> diminutiveGroups )
        {
            input = input?.Trim();
            firstName = firstName?.Trim();
            nickName = nickName?.Trim();

            // No first name was requested, or the person has no first name or nick name to compare.
            if ( string.IsNullOrEmpty( input ) || ( string.IsNullOrEmpty( firstName ) && string.IsNullOrEmpty( nickName ) ) )
            {
                return true;
            }

            if ( NameEquals( input, firstName ) || NameEquals( input, nickName ) )
            {
                return true;
            }

            if ( diminutiveGroups == null )
            {
                return false;
            }

            foreach ( var group in diminutiveGroups )
            {
                if ( group == null )
                {
                    continue;
                }

                var names = group.Where( n => !string.IsNullOrWhiteSpace( n ) ).Select( n => n.Trim() ).ToList();
                if ( names.Any( n => NameEquals( n, input ) ) &&
                    names.Any( n => NameEquals( n, firstName ) || NameEquals( n, nickName ) ) )
                {
                    return true;
                }
            }

            return false;
        }

        private static bool NameEquals( string left, string right )
        {
            return !string.IsNullOrEmpty( left ) && string.Equals( left, right, StringComparison.OrdinalIgnoreCase );
        }

        private static Person GetOrCreateNamelessPerson( string email, string phone, PersonService personService )
        {

            var namelessPersonRecordValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_NAMELESS ).Id;
            int numberTypeMobileValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE ).Id;
            var number = PhoneNumber.CleanNumber( phone );

            var personQryOptions = new Rock.Model.PersonService.PersonQueryOptions
            {
                IncludeNameless = true
            };

            var qry = personService.Queryable( personQryOptions ).Where( p => p.RecordTypeValueId == namelessPersonRecordValueId );

            if ( email.IsNotNullOrWhiteSpace() )
            {
                qry = qry.Where( p => p.Email == email );
            }

            if ( phone.IsNotNullOrWhiteSpace() )
            {
                qry = qry.Where( p => p.PhoneNumbers.Select( pn => pn.Number == number ).Any() );
            }

            var people = qry.ToList();
            if ( people.Count() == 1 )
            {
                return people.First();
            }

            //Didn't get just one person... Time to make a new one!
            var person = new Person();
            person.RecordTypeValueId = namelessPersonRecordValueId;

            if ( email.IsNotNullOrWhiteSpace() )
            {
                person.Email = email;
            }

            if ( phone.IsNotNullOrWhiteSpace() )
            {
                var smsPhoneNumber = new PhoneNumber();
                smsPhoneNumber.NumberTypeValueId = numberTypeMobileValueId;
                smsPhoneNumber.Number = number;
                smsPhoneNumber.IsMessagingEnabled = true;
                person.PhoneNumbers.Add( smsPhoneNumber );
            }

            personService.Add( person );
            ( ( RockContext ) personService.Context ).SaveChanges();

            person = personService.Get( person.Id );
            return person;

        }
    }
}
