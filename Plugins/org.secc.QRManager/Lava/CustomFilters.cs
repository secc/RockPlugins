using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.QRManager.Lava
{
    public static class CustomFilters
    {
        public static string QRUrl( object input )
        {
            return GetURL( input );
        }

        public static string QRImage( object input, int width = 300 )
        {
            var url = GetURL( input );
            return $"<img src='{url}' width='{width}'>";
        }

        private static string GetURL( object input )
        {
            RockContext rockContext = new RockContext();
            Person person;

            if ( input is Person )
            {
                person = input as Person;
            }
            else if ( input is PersonAlias )
            {
                person = ( ( PersonAlias ) input ).Person;
            }
            else if ( input is GroupMember )
            {
                person = ( ( GroupMember ) input ).Person;
            }
            else if ( input is RegistrationRegistrant )
            {
                person = ( ( RegistrationRegistrant ) input ).Person;
            }
            else if ( input is int )
            {
                PersonService personService = new PersonService( rockContext );
                person = personService.Get( ( int ) input );
            }
            else
            {
                return "";
            }
            var key = GetSearchKey( person, rockContext );
            
            var url = GlobalAttributesCache.Get().GetValue( "PublicApplicationRoot", rockContext ).EnsureTrailingForwardslash();
            return $"{url}api/qr/{key}";
        }

        private static object GetSearchKey( Person person, RockContext rockContext )
        {
            var alternateIdSearchTypeValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_SEARCH_KEYS_ALTERNATE_ID.AsGuid() ).Id;
            var searchKey = person.GetPersonSearchKeys( rockContext ).Where( k => k.SearchTypeValueId == alternateIdSearchTypeValueId ).FirstOrDefault();
            if (searchKey !=null )
            {
                return searchKey.SearchValue;
            }

            //Create Search Key if there is none.
            var personSearchKeyService = new PersonSearchKeyService( rockContext );
            var personService = new PersonService( rockContext );
            var personAlias = personService.Get( person.Id ).Aliases.First();
            PersonSearchKey personSearchKey = new PersonSearchKey()
            {
                PersonAlias = personAlias,
                SearchTypeValueId = alternateIdSearchTypeValueId,
                SearchValue = PersonSearchKeyService.GenerateRandomAlternateId( true, rockContext )
            };
            personSearchKeyService.Add( personSearchKey );
            rockContext.SaveChanges();
            return personSearchKey.SearchValue;
        }
    }
}
