using Rock.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.secc.PersonMatch
{
    // The following extension methods can be accessed by instances of any 
    // class that implements IMyInterface.
    public static class Extension
    {
        public static IEnumerable<Person> GetByMatch(this PersonService personService, String firstName, String lastName, DateTime? birthDate, String email = null, String phone = null, String street1 = null, String postalCode = null)      {
            LocationService locationService = new LocationService(new Rock.Data.RockContext());

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
                            Location location = new Location();
                            location.Street1 = street1;
                            location.PostalCode = postalCode;
                            locationService.Verify(location, true);

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

            return matchingPersons;
        }

    }
}
