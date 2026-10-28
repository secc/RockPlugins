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
using System.Net;
using System.Net.Http;
using System.Web.Http;
using org.secc.Rest.Controllers;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Xunit;

namespace org.secc.Rest.Tests
{
    /// <summary>
    /// ROCK-9116: api/account/create for a request that matches no existing person, run up to the
    /// step that saves the new person. The controller's lookups are replaced: the caller is an active
    /// OAuth client within its create budget, the confirmation code attributes exist unless a test
    /// removes one, no person matches, and each defined value Guid gets its own Id. The replacement
    /// save step records the person and then stops the action, so nothing after it runs.
    /// </summary>
    public class CreateAccountNewPersonTests
    {
        private const string ConfirmationCodeHashAttributeGuid = "7B3F9A42-5C81-4E6D-9F2A-1D8C4B60E3A7";
        private const string ConfirmationCodeIssuedAttributeGuid = "C4E81D57-2A93-4F16-B8D5-6E70F9A24C1B";

        private static readonly Guid MobileTypeGuid = new Guid( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE );

        private class SaveStepReachedException : Exception
        {
        }

        private class TestDefinedValueCache : DefinedValueCache
        {
            public TestDefinedValueCache( int id )
            {
                Id = id;
            }
        }

        private class TestAccountController : AccountController
        {
            private readonly Dictionary<Guid, int> _definedValueIds = new Dictionary<Guid, int>();

            public Guid? MissingAttributeGuid { get; set; }

            public Person SavedPerson { get; private set; }

            public TestAccountController()
            {
                Configuration = new HttpConfiguration();
                Request = new HttpRequestMessage();
            }

            public int DefinedValueId( Guid guid )
            {
                if ( !_definedValueIds.TryGetValue( guid, out var id ) )
                {
                    id = 1000 + _definedValueIds.Count;
                    _definedValueIds.Add( guid, id );
                }

                return id;
            }

            protected override bool IsCallerActiveOAuthClient( string clientId )
            {
                return true;
            }

            protected override List<KeyValuePair<string, string>> GetPreThrottleInputErrors( Account account )
            {
                return new List<KeyValuePair<string, string>>();
            }

            protected override bool TryRegisterAccountCreationAttempt( string clientId )
            {
                return true;
            }

            protected override AttributeCache GetAttributeCache( Guid guid )
            {
                return guid == MissingAttributeGuid ? null : new AttributeCache();
            }

            protected override DefinedValueCache GetDefinedValueCache( Guid guid )
            {
                return new TestDefinedValueCache( DefinedValueId( guid ) );
            }

            protected override string GetDefaultCountryCode()
            {
                return "1";
            }

            protected override RockContext CreateRockContext()
            {
                // Never connects: every step that would use it is replaced above.
                return new RockContext( "name=UnusedByTheseTests" );
            }

            protected override IEnumerable<Person> GetMatchingPeople( PersonService personService, Account account )
            {
                return Enumerable.Empty<Person>();
            }

            protected override void SaveNewPerson( Person person, RockContext rockContext )
            {
                SavedPerson = person;
                throw new SaveStepReachedException();
            }
        }

        private static AccountController.Account NewAccount( string mobileNumber, bool? smsOptIn )
        {
            return new AccountController.Account
            {
                FirstName = "Pat",
                LastName = "Sample",
                EmailAddress = "pat.sample@example.com",
                MobileNumber = mobileNumber,
                SmsOptIn = smsOptIn,
                Birthdate = new DateTime( 1990, 5, 1 )
            };
        }

        private static Person SavedPerson( TestAccountController controller, AccountController.Account account )
        {
            Assert.Throws<SaveStepReachedException>( () => controller.CreateAccount( account ) );
            Assert.NotNull( controller.SavedPerson );
            return controller.SavedPerson;
        }

        [Theory]
        [InlineData( true, true )]
        [InlineData( false, false )]
        [InlineData( null, false )]
        public void NewPerson_GetsOneMobilePhone_MessagingFollowsSmsOptIn( bool? smsOptIn, bool expectedMessaging )
        {
            var controller = new TestAccountController();

            var person = SavedPerson( controller, NewAccount( "(502) 555-0100", smsOptIn ) );

            var phone = Assert.Single( person.PhoneNumbers );
            Assert.Equal( controller.DefinedValueId( MobileTypeGuid ), phone.NumberTypeValueId );
            Assert.Equal( "5025550100", phone.Number );
            Assert.Equal( expectedMessaging, phone.IsMessagingEnabled );
        }

        [Theory]
        [InlineData( null )]
        [InlineData( "" )]
        [InlineData( "   " )]
        public void NewPerson_BlankMobileNumber_GetsNoPhone( string mobileNumber )
        {
            var controller = new TestAccountController();

            var person = SavedPerson( controller, NewAccount( mobileNumber, true ) );

            Assert.Empty( person.PhoneNumbers );
        }

        [Theory]
        [InlineData( ConfirmationCodeHashAttributeGuid )]
        [InlineData( ConfirmationCodeIssuedAttributeGuid )]
        public void MissingConfirmationCodeAttribute_Returns500WithoutSavingAPerson( string attributeGuid )
        {
            var controller = new TestAccountController { MissingAttributeGuid = new Guid( attributeGuid ) };

            HttpResponseMessage response = null;
            var exception = Record.Exception( () => response = controller.CreateAccount( NewAccount( "5025550100", true ) ) );

            Assert.Null( exception );
            Assert.Equal( HttpStatusCode.InternalServerError, response.StatusCode );
            Assert.True( response.TryGetContentValue( out AccountController.StandardResponse body ) );
            Assert.Equal( "Account creation is temporarily unavailable.", body.Message );
            Assert.Null( controller.SavedPerson );
        }
    }
}
