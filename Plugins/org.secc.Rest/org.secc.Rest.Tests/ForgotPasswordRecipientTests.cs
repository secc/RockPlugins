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
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Web;
using System.Web.Http;
using org.secc.Authentication.Tests;
using org.secc.Rest.Controllers;
using Rock.Communication;
using Rock.Data;
using Rock.Model;
using Xunit;

namespace org.secc.Rest.Tests
{
    /// <summary>
    /// Who api/account/forgotpassword sends its email to and which people each email lists, run through the
    /// action on a controller whose OAuth client check, current request, database context, person lookup,
    /// login lookup, email message creation and email send are replaced. The message's recipients are recorded
    /// instead of sent. Every test uses email
    /// addresses that no other test uses, so the action's per-address limit never applies, and a request
    /// from a connecting address that no other request uses.
    /// </summary>
    public class ForgotPasswordRecipientTests
    {
        private const string SentMessage = "Forgot password email has been sent successfully.";
        private const string NoAccountsMessage = "No accounts associated with this email address are able to be reset via email.";

        private static int _lastAddress;

        private readonly RecipientTestController _controller = new RecipientTestController();

        private class RecipientTestController : AccountController
        {
            public RecipientTestController()
            {
                Configuration = new HttpConfiguration();
                Request = new HttpRequestMessage();
            }

            /// <summary>
            /// The people the person lookup returns, in this order.
            /// </summary>
            public List<Person> People { get; } = new List<Person>();

            /// <summary>
            /// The resettable logins of each person, by person Id. A person not listed has none.
            /// </summary>
            public Dictionary<int, List<UserLogin>> Logins { get; } = new Dictionary<int, List<UserLogin>>();

            /// <summary>
            /// The email address the person lookup was asked for.
            /// </summary>
            public string LookedUpEmail { get; private set; }

            /// <summary>
            /// The recipients of each message sent, in order.
            /// </summary>
            public List<List<RockEmailMessageRecipient>> Sends { get; } = new List<List<RockEmailMessageRecipient>>();

            protected override bool IsCallerActiveOAuthClient( string clientId )
            {
                return true;
            }

            protected override HttpRequestBase GetCurrentRequest()
            {
                return new FakeRequest( null, FakeRequest.NewAddress() );
            }

            protected override RockContext CreateRockContext()
            {
                // Never connects: every step that would use it is replaced below.
                return new RockContext( "name=UnusedByTheseTests" );
            }

            protected override IEnumerable<Person> GetPeopleByEmail( PersonService personService, string email )
            {
                LookedUpEmail = email;
                return People;
            }

            protected override List<UserLogin> GetResettableLogins( UserLoginService userLoginService, Person person )
            {
                return Logins.TryGetValue( person.Id, out var logins ) ? logins : new List<UserLogin>();
            }

            protected override RockEmailMessage CreateForgotPasswordEmailMessage()
            {
                // No system communication: the parameterless message needs no database.
                return new RockEmailMessage();
            }

            protected override void SendEmailMessage( RockEmailMessage emailMessage )
            {
                Sends.Add( emailMessage.GetRecipients().Cast<RockEmailMessageRecipient>().ToList() );
            }

            /// <summary>
            /// Adds a person with this stored email address and, unless resettable is false, one resettable login.
            /// </summary>
            public Person AddPerson( int id, string storedEmail, bool resettable = true )
            {
                var person = new Person { Id = id, Email = storedEmail, NickName = "Pat" + id, LastName = "Sample" };
                People.Add( person );
                if ( resettable )
                {
                    Logins[id] = new List<UserLogin> { new UserLogin { Id = id * 10, UserName = "patsample" + id, PersonId = id } };
                }

                return person;
            }
        }

        /// <summary>
        /// A number that makes an email address no other test uses.
        /// </summary>
        private static int NewNumber()
        {
            return Interlocked.Increment( ref _lastAddress );
        }

        private AccountController.StandardResponse Forgot( string email )
        {
            var response = _controller.ForgotPassword( email );
            Assert.Equal( HttpStatusCode.OK, response.StatusCode );
            Assert.True( response.TryGetContentValue( out AccountController.StandardResponse body ) );
            Assert.Equal( email, _controller.LookedUpEmail );
            return body;
        }

        private static void AssertSent( AccountController.StandardResponse body )
        {
            Assert.Equal( AccountController.StandardResponse.ResultCode.Success, body.Result );
            Assert.Equal( SentMessage, body.Message );
        }

        /// <summary>
        /// The people a recipient's email lists, in order.
        /// </summary>
        private static List<Person> Listed( RockEmailMessageRecipient recipient )
        {
            var results = Assert.IsType<IDictionary<string, object>[]>( recipient.MergeFields["Results"] );
            return results.Select( r => Assert.IsType<Person>( r["Person"] ) ).ToList();
        }

        /// <summary>
        /// The single send's recipients.
        /// </summary>
        private List<RockEmailMessageRecipient> SingleSend()
        {
            return Assert.Single( _controller.Sends );
        }

        [Fact]
        public void TypedInAnotherCase_EmailGoesToTheStoredAddress()
        {
            var n = NewNumber();
            var stored = "Pat.Sample." + n + "@Example.test";
            var person = _controller.AddPerson( 1, stored );

            AssertSent( Forgot( stored.ToLowerInvariant() ) );

            var recipient = Assert.Single( SingleSend() );
            Assert.Equal( stored, recipient.To );
            Assert.Equal( new[] { person }, Listed( recipient ) );
            var result = Assert.Single( Assert.IsType<IDictionary<string, object>[]>( recipient.MergeFields["Results"] ) );
            Assert.Equal( new[] { "patsample1" }, Assert.IsType<List<UserLogin>>( result["Users"] ).Select( u => u.UserName ) );
        }

        [Fact]
        public void TwoPeopleWithDifferentStoredAddresses_EachGetsAnEmailListingOnlyThemselves()
        {
            var n = NewNumber();
            var plain = _controller.AddPerson( 1, "jo.strasse." + n + "@encyclopaedia.test" );
            var sharpS = _controller.AddPerson( 2, "jo.straße." + n + "@encyclopædia.test" );

            AssertSent( Forgot( "jo.strasse." + n + "@encyclopaedia.test" ) );

            var recipients = SingleSend();
            Assert.Equal( 2, recipients.Count );
            Assert.Equal( plain.Email, recipients[0].To );
            Assert.Equal( new[] { plain }, Listed( recipients[0] ) );
            Assert.Equal( sharpS.Email, recipients[1].To );
            Assert.Equal( new[] { sharpS }, Listed( recipients[1] ) );
            Assert.NotSame( recipients[0].MergeFields, recipients[1].MergeFields );
        }

        [Fact]
        public void PeopleSharingOneStoredAddress_GetOneEmailListingThemInIdOrder()
        {
            var n = NewNumber();
            var stored = "Sample.Family." + n + "@example.test";
            var later = _controller.AddPerson( 7, stored );
            var earlier = _controller.AddPerson( 3, stored );

            AssertSent( Forgot( stored.ToUpperInvariant() ) );

            var recipient = Assert.Single( SingleSend() );
            Assert.Equal( stored, recipient.To );
            Assert.Equal( new[] { earlier, later }, Listed( recipient ) );
        }

        [Fact]
        public void OnlyTheStoredAddressWithAResettableLogin_GetsAnEmail()
        {
            var n = NewNumber();
            var resettable = _controller.AddPerson( 1, "Pat.Sample." + n + "@example.test" );
            _controller.AddPerson( 2, "pat.sample." + n + "@EXAMPLE.test", resettable: false );

            AssertSent( Forgot( "pat.sample." + n + "@example.test" ) );

            var recipient = Assert.Single( SingleSend() );
            Assert.Equal( resettable.Email, recipient.To );
            Assert.Equal( new[] { resettable }, Listed( recipient ) );
        }

        [Fact]
        public void PeopleSharingOneStoredAddress_AreAllListed_WhenOnlyOneHasAResettableLogin()
        {
            var n = NewNumber();
            var stored = "Sample.Household." + n + "@example.test";
            var resettable = _controller.AddPerson( 1, stored );
            var notResettable = _controller.AddPerson( 2, stored, resettable: false );

            AssertSent( Forgot( stored ) );

            var recipient = Assert.Single( SingleSend() );
            Assert.Equal( stored, recipient.To );
            Assert.Equal( new[] { resettable, notResettable }, Listed( recipient ) );
            var results = Assert.IsType<IDictionary<string, object>[]>( recipient.MergeFields["Results"] );
            Assert.Single( Assert.IsType<List<UserLogin>>( results[0]["Users"] ) );
            Assert.Empty( Assert.IsType<List<UserLogin>>( results[1]["Users"] ) );
        }

        [Fact]
        public void NoResettableLogin_SendsNothing()
        {
            var n = NewNumber();
            _controller.AddPerson( 1, "pat.sample." + n + "@example.test", resettable: false );

            var body = Forgot( "pat.sample." + n + "@example.test" );

            Assert.Empty( _controller.Sends );
            Assert.Equal( AccountController.StandardResponse.ResultCode.Error, body.Result );
            Assert.Equal( NoAccountsMessage, body.Message );
        }

        [Fact]
        public void NobodyMatched_SendsNothing()
        {
            var body = Forgot( "nobody." + NewNumber() + "@example.test" );

            Assert.Empty( _controller.Sends );
            Assert.Equal( AccountController.StandardResponse.ResultCode.Error, body.Result );
            Assert.Equal( NoAccountsMessage, body.Message );
        }

        [Fact]
        public void StoredAddressesDifferingOnlyInCase_EachGetsItsOwnEmail()
        {
            var n = NewNumber();
            var upper = _controller.AddPerson( 1, "Pat.Sample." + n + "@example.test" );
            var lower = _controller.AddPerson( 2, "pat.sample." + n + "@example.test" );

            AssertSent( Forgot( "pat.sample." + n + "@example.test" ) );

            var recipients = SingleSend();
            Assert.Equal( 2, recipients.Count );
            Assert.Equal( upper.Email, recipients[0].To );
            Assert.Equal( new[] { upper }, Listed( recipients[0] ) );
            Assert.Equal( lower.Email, recipients[1].To );
            Assert.Equal( new[] { lower }, Listed( recipients[1] ) );
        }

        [Theory]
        [InlineData( "pat.sample.{0}@example.test  " )]
        [InlineData( "ｐａｔ.sample.{0}@example.test" )]
        public void TypedWithTrailingSpacesOrFullWidthLetters_EmailGoesToTheStoredAddress( string typed )
        {
            var n = NewNumber();
            var person = _controller.AddPerson( 1, "pat.sample." + n + "@example.test" );

            AssertSent( Forgot( string.Format( typed, n ) ) );

            var recipient = Assert.Single( SingleSend() );
            Assert.Equal( person.Email, recipient.To );
            Assert.Equal( new[] { person }, Listed( recipient ) );
        }
    }
}
