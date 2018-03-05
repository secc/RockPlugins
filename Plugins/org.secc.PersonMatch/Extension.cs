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
using Rock.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rock;
using Rock.Web.Cache;

namespace org.secc.PersonMatch
{
    // The following extension methods can be accessed by instances of any 
    // class that implements IMyInterface.
    public static class Extension
    {
        const string DIMINUTIVE_NAMES = "3E2D2BEE-01BE-4D1E-8634-01932718AEA3";
        const string GOES_BY_ATTRIBUTE = "C31FDA8A-8CAB-4A1C-B96D-275415B5BB1C";
        public static IEnumerable<Person> GetByMatch(this PersonService personService, String firstName, String lastName, DateTime? birthDate, String email = null, String phone = null, String street1 = null, String postalCode = null)      {
            using ( Rock.Data.RockContext context = new Rock.Data.RockContext() )
            {
                
                LocationService locationService = new LocationService( context );
                AttributeValueService attributeValueService = new AttributeValueService( context );
                List<AttributeValue> attributeValues = attributeValueService.GetByAttributeId( AttributeCache.Read( GOES_BY_ATTRIBUTE.AsGuid() ).Id ).ToList();
                var diminutiveName = DefinedTypeCache.Read( DIMINUTIVE_NAMES.AsGuid() );

                firstName = firstName ?? string.Empty;
                lastName = lastName ?? string.Empty;
                email = email ?? string.Empty;
                phone = phone ?? string.Empty;
                List<Person> matchingPersons = new List<Person>();


                List<Person> persons = personService.Queryable(false, false)
                    .Where(p =>
                       p.LastName == lastName &&
                       (p.BirthDate == null || p.BirthDate.Value == p.BirthDate))
                    .ToList();

                // Check the address if it was passed
                Location location = new Location();
                if ( persons.Count() > 0 && !string.IsNullOrEmpty( street1 ) && !string.IsNullOrEmpty( postalCode ) )
                {
                    location.Street1 = street1;
                    location.PostalCode = postalCode;
                    locationService.Verify( location, true );
                }

                foreach (Person person in persons)
                {
                    // Check to see if the phone exists anywhere in the family
                    Boolean phoneExists = person.GetFamilies().Where(f => f.Members.Where(m => m.Person.PhoneNumbers.Where(pn => pn.Number == phone).Any()).Any()).Any();

                    // Check to see if the email exists anywhere in the family
                    Boolean emailExists = person.GetFamilies().Where(f => f.Members.Where(m => m.Person.Email == email).Any()).Any();

                    Boolean addressMatches = false;
                    // Check the address if it was passed
                    if (!string.IsNullOrEmpty(street1) && !string.IsNullOrEmpty(postalCode))
                    { 
                        if (person.GetHomeLocation() != null) { 
                            if (person.GetHomeLocation().Street1 == street1)
                            {
                                addressMatches = true;
                            }
                            // If it doesn't match, we need to geocode it and check it again
                            if (!addressMatches) {

                                if (person.GetHomeLocation().Street1 == location.Street1)
                                {
                                    addressMatches = true;
                                }
                            }
                        }
                    }

                    // At least phone, email, or address have to match
                    if (phoneExists || emailExists || addressMatches)
                    {
                        matchingPersons.Add(person);
                    }

                }

                List<Person> firstNameMatchingPersons = new List<Person>();

                // Now narrow down the list by looking for the first name (or diminutive name)
                foreach (Person matchingPerson in matchingPersons )
                {
                    if ( firstName != null && ( ( matchingPerson.FirstName != null && matchingPerson.FirstName.ToLower() != firstName.ToLower() ) || ( matchingPerson.NickName != null && matchingPerson.NickName.ToLower() != firstName.ToLower() ) ) )
                    {
                        foreach ( DefinedValueCache dv in diminutiveName.DefinedValues )
                        {
                            AttributeValue av = attributeValues.Where( av2 => av2.EntityId == dv.Id ).FirstOrDefault();
                            List<string> nameList = new List<string>();
                            nameList = av.Value.Split( '|' ).ToList();
                            nameList.Add( dv.Value );
                            if ( nameList.Contains( firstName.ToLower() ) && 
                                ( nameList.Contains( matchingPerson.FirstName.ToLower() )  ||  nameList.Contains( matchingPerson.NickName.ToLower() ) ) )
                            {
                                firstNameMatchingPersons.Add( matchingPerson );
                                break;
                            }
                        }
                    } else
                    {
                        firstNameMatchingPersons.Add( matchingPerson );
                    }
                }

                return firstNameMatchingPersons;
            }
        }

    }
}
