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
    /// login lookup, password-change check, application root, email message creation, email send and exception
    /// log are replaced. The message's recipients are recorded instead of sent. Every test uses email addresses
    /// that no other test uses, so the action's per-address limits apply only where a test spends them, and
    /// each request comes from a connecting address that no other request uses unless the test sets one.
    /// </summary>
    public class ForgotPasswordRecipientTests
    {
        private const string NeutralMessage = "If an account with that email address can be reset by email, we've sent instructions to it.";
        private const string ApplicationRoot = "https://www.example.test/";

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
            /// The Ids of the logins whose authentication provider doesn't let the person change their password.
            /// </summary>
            public HashSet<int> NoChangePassword { get; } = new HashSet<int>();

            /// <summary>
            /// The messages of the exceptions the controller logged, in order.
            /// </summary>
            public List<string> Logged { get; } = new List<string>();

            /// <summary>
            /// The connecting address of every request, or null for a new one per request.
            /// </summary>
            public string PeerAddress { get; set; }

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
                return new FakeRequest( null, PeerAddress ?? FakeRequest.NewAddress() );
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

            protected override bool SupportsChangePassword( UserLogin user )
            {
                return !NoChangePassword.Contains( user.Id );
            }

            protected override string GetPublicApplicationRoot()
            {
                return ApplicationRoot;
            }

            protected override void LogException( System.Exception ex )
            {
                Logged.Add( ex.Message );
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

        /// <summary>
        /// Every request with a non-blank email address that passes the request limits gets this response,
        /// whether or not an email was sent.
        /// </summary>
        private static void AssertNeutral( AccountController.StandardResponse body )
        {
            Assert.Equal( AccountController.StandardResponse.ResultCode.Success, body.Result );
            Assert.Equal( NeutralMessage, body.Message );
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

            AssertNeutral( Forgot( stored.ToLowerInvariant() ) );

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

            AssertNeutral( Forgot( "jo.strasse." + n + "@encyclopaedia.test" ) );

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

            AssertNeutral( Forgot( stored.ToUpperInvariant() ) );

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

            AssertNeutral( Forgot( "pat.sample." + n + "@example.test" ) );

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

            AssertNeutral( Forgot( stored ) );

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

            AssertNeutral( Forgot( "pat.sample." + n + "@example.test" ) );

            Assert.Empty( _controller.Sends );
        }

        [Fact]
        public void NobodyMatched_SendsNothing()
        {
            AssertNeutral( Forgot( "nobody." + NewNumber() + "@example.test" ) );

            Assert.Empty( _controller.Sends );
        }

        [Fact]
        public void StoredAddressesDifferingOnlyInCase_EachGetsItsOwnEmail()
        {
            var n = NewNumber();
            var upper = _controller.AddPerson( 1, "Pat.Sample." + n + "@example.test" );
            var lower = _controller.AddPerson( 2, "pat.sample." + n + "@example.test" );

            AssertNeutral( Forgot( "pat.sample." + n + "@example.test" ) );

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

            AssertNeutral( Forgot( string.Format( typed, n ) ) );

            var recipient = Assert.Single( SingleSend() );
            Assert.Equal( person.Email, recipient.To );
            Assert.Equal( new[] { person }, Listed( recipient ) );
        }

        /// <summary>
        /// The four spellings of one address that compare equal in the database (ss and ß, ae and æ) and have
        /// four different typed-address budgets.
        /// </summary>
        private static string[] CollationEqualSpellings( int n )
        {
            return new[]
            {
                "pat.strasse." + n + "@encyclopaedia.test",
                "pat.straße." + n + "@encyclopaedia.test",
                "pat.strasse." + n + "@encyclopædia.test",
                "pat.straße." + n + "@encyclopædia.test"
            };
        }

        /// <summary>
        /// How many emails were sent to this address over all the sends.
        /// </summary>
        private int SentTo( string address )
        {
            return _controller.Sends.Sum( recipients => recipients.Count( r => r.To == address ) );
        }

        /// <summary>
        /// The SupportsChangePassword list of each result in a recipient's email, in order.
        /// </summary>
        private static List<List<string>> PasswordChangeLogins( RockEmailMessageRecipient recipient )
        {
            var results = Assert.IsType<IDictionary<string, object>[]>( recipient.MergeFields["Results"] );
            return results.Select( r => Assert.IsType<List<string>>( r["SupportsChangePassword"] ) ).ToList();
        }

        [Fact]
        public void CollationEqualSpellings_ShareTheStoredAddressBudget()
        {
            var n = NewNumber();
            var spellings = CollationEqualSpellings( n );
            var person = _controller.AddPerson( 1, spellings[0] );

            for ( var i = 0; i < 8; i++ )
            {
                AssertNeutral( Forgot( spellings[i % 4] ) );
            }

            Assert.Equal( 3, SentTo( person.Email ) );
            Assert.Equal( 3, _controller.Sends.Count );
        }

        [Fact]
        public void SpentStoredAddress_IsLoggedWithoutTheAddress()
        {
            var n = NewNumber();
            var spellings = CollationEqualSpellings( n );
            _controller.AddPerson( 1, spellings[0] );

            for ( var i = 0; i < 4; i++ )
            {
                AssertNeutral( Forgot( spellings[i] ) );
            }

            var line = Assert.Single( _controller.Logged );
            Assert.StartsWith( "Forgot password stored email address budget exceeded. Request from Client: ", line );
            Assert.DoesNotContain( "@", line );
        }

        [Fact]
        public void SpentStoredAddress_RefusalsFromOneClientAndAddress_AreLoggedOnce()
        {
            var spellings = CollationEqualSpellings( NewNumber() );
            _controller.AddPerson( 1, spellings[0] );
            _controller.PeerAddress = FakeRequest.NewAddress();

            // Three emails, then three refused by the stored address's budget; no spelling is typed more than twice.
            for ( var i = 0; i < 6; i++ )
            {
                AssertNeutral( Forgot( spellings[i % 4] ) );
            }

            Assert.Equal( 3, _controller.Sends.Count );
            var line = Assert.Single( _controller.Logged );
            Assert.StartsWith( "Forgot password stored email address budget exceeded. Request from Client: ", line );
            Assert.Contains( "PeerIP: " + _controller.PeerAddress + " ", line );
        }

        [Fact]
        public void TypedAsStored_ThreeRequests_AllSend()
        {
            var stored = "pat.sample." + NewNumber() + "@example.test";
            _controller.AddPerson( 1, stored );

            for ( var i = 0; i < 3; i++ )
            {
                AssertNeutral( Forgot( stored ) );
            }

            Assert.Equal( 3, SentTo( stored ) );
        }

        [Fact]
        public void PeopleSharingOneStoredAddress_ThreeRequests_AllSend()
        {
            var stored = "Sample.Family." + NewNumber() + "@example.test";
            var first = _controller.AddPerson( 1, stored );
            var second = _controller.AddPerson( 2, stored );

            for ( var i = 0; i < 3; i++ )
            {
                AssertNeutral( Forgot( stored ) );
            }

            Assert.Equal( 3, _controller.Sends.Count );
            Assert.All( _controller.Sends, recipients => Assert.Equal( new[] { first, second }, Listed( Assert.Single( recipients ) ) ) );
        }

        [Fact]
        public void StoredAddressesDifferingOnlyInCase_ShareOneBudget()
        {
            var n = NewNumber();
            var upper = _controller.AddPerson( 1, "Pat.Sample." + n + "@example.test" );
            var lower = _controller.AddPerson( 2, "pat.sample." + n + "@example.test" );

            for ( var i = 0; i < 3; i++ )
            {
                AssertNeutral( Forgot( lower.Email ) );
            }

            Assert.Equal( 3, SentTo( upper.Email ) + SentTo( lower.Email ) );
            Assert.Equal( new[] { 2, 1 }, _controller.Sends.Select( recipients => recipients.Count ) );
        }

        [Fact]
        public void SpentStoredAddress_DoesNotBlockAnotherStoredAddress()
        {
            var n = NewNumber();
            var spent = _controller.AddPerson( 1, "jo.strasse." + n + "@example.test" );
            for ( var i = 0; i < 3; i++ )
            {
                AssertNeutral( Forgot( spent.Email ) );
            }

            var other = _controller.AddPerson( 2, "jo.straße." + n + "@example.test" );
            AssertNeutral( Forgot( other.Email ) );

            Assert.Equal( 3, SentTo( spent.Email ) );
            Assert.Equal( 4, _controller.Sends.Count );
            var recipient = Assert.Single( _controller.Sends.Last() );
            Assert.Equal( other.Email, recipient.To );
            Assert.Equal( new[] { other }, Listed( recipient ) );
        }

        [Fact]
        public void PersonWithoutAResettableLogin_DoesNotSpendTheStoredAddressBudget()
        {
            var n = NewNumber();
            var spellings = CollationEqualSpellings( n );
            var person = _controller.AddPerson( 1, spellings[0], resettable: false );
            for ( var i = 0; i < 3; i++ )
            {
                AssertNeutral( Forgot( spellings[i] ) );
            }

            _controller.Logins[person.Id] = new List<UserLogin> { new UserLogin { Id = 10, UserName = "patsample1", PersonId = 1 } };
            AssertNeutral( Forgot( spellings[3] ) );

            Assert.Equal( person.Email, Assert.Single( SingleSend() ).To );
        }

        [Theory]
        [InlineData( "match" )]
        [InlineData( "nobody" )]
        [InlineData( "notResettable" )]
        [InlineData( "spentStoredAddress" )]
        public void EveryOutcomeAfterTheLookup_GetsTheSameResponse( string outcome )
        {
            var spellings = CollationEqualSpellings( NewNumber() );
            switch ( outcome )
            {
                case "match":
                    _controller.AddPerson( 1, spellings[0] );
                    break;
                case "notResettable":
                    _controller.AddPerson( 1, spellings[0], resettable: false );
                    break;
                case "spentStoredAddress":
                    _controller.AddPerson( 1, spellings[0] );
                    for ( var i = 1; i < 4; i++ )
                    {
                        Forgot( spellings[i] );
                    }

                    _controller.Sends.Clear();
                    break;
            }

            var response = _controller.ForgotPassword( spellings[0] );

            Assert.Equal( HttpStatusCode.OK, response.StatusCode );
            Assert.True( response.TryGetContentValue( out AccountController.StandardResponse body ) );
            Assert.Equal( AccountController.StandardResponse.ResultCode.Success, body.Result );
            Assert.Equal( NeutralMessage, body.Message );
            Assert.Equal( outcome == "match" ? 1 : 0, _controller.Sends.Count );
        }

        [Fact]
        public void EachEmail_HasTheConfirmAccountUrl_AndItsOwnPeoplesPasswordChangeLogins()
        {
            var n = NewNumber();
            var first = _controller.AddPerson( 1, "pat.sample." + n + "@example.test" );
            _controller.Logins[1].Add( new UserLogin { Id = 11, UserName = "patsample1-other", PersonId = 1 } );
            _controller.NoChangePassword.Add( 11 );
            _controller.AddPerson( 2, first.Email, resettable: false );
            _controller.AddPerson( 3, "Pat.Sample." + n + "@example.test" );

            AssertNeutral( Forgot( first.Email ) );

            var recipients = SingleSend();
            Assert.Equal( 2, recipients.Count );
            Assert.All( recipients, r => Assert.Equal( "https://www.example.test/ConfirmAccount", r.MergeFields["ConfirmAccountUrl"] ) );
            Assert.Equal( new[] { new List<string> { "patsample1" }, new List<string>() }, PasswordChangeLogins( recipients[0] ) );
            Assert.Equal( new[] { new List<string> { "patsample3" } }, PasswordChangeLogins( recipients[1] ) );
        }

        /// <summary>
        /// An authentication provider whose only behavior is whether it lets the person change their password.
        /// </summary>
        private abstract class TestAuthenticationComponent : Rock.Security.AuthenticationComponent
        {
            public override Rock.Model.AuthenticationServiceType ServiceType => Rock.Model.AuthenticationServiceType.Internal;

            public override bool RequiresRemoteAuthentication => false;

            public override bool Authenticate( UserLogin user, string password ) => throw new System.NotSupportedException();

            public override bool Authenticate( System.Web.HttpRequest request, out string userName, out string returnUrl ) => throw new System.NotSupportedException();

            public override string EncodePassword( UserLogin user, string password ) => throw new System.NotSupportedException();

            public override System.Uri GenerateLoginUrl( System.Web.HttpRequest request ) => throw new System.NotSupportedException();

            public override bool IsReturningFromAuthentication( System.Web.HttpRequest request ) => throw new System.NotSupportedException();

            public override string ImageUrl() => throw new System.NotSupportedException();

            public override bool ChangePassword( UserLogin user, string oldPassword, string newPassword, out string warningMessage ) => throw new System.NotSupportedException();

            public override void SetPassword( UserLogin user, string password ) => throw new System.NotSupportedException();
        }

        private class ChangePasswordProvider : TestAuthenticationComponent
        {
            public override bool SupportsChangePassword => true;
        }

        private class NoChangePasswordProvider : TestAuthenticationComponent
        {
            public override bool SupportsChangePassword => false;
        }

        /// <summary>
        /// A provider instance created without running its constructors, because a component's constructor
        /// loads its attributes from the database.
        /// </summary>
        private static T Provider<T>() where T : TestAuthenticationComponent
        {
            return ( T ) System.Runtime.Serialization.FormatterServices.GetUninitializedObject( typeof( T ) );
        }

        [Fact]
        public void ProviderSupportsChangePassword_IsTheProvidersSetting()
        {
            Assert.True( AccountController.ProviderSupportsChangePassword( Provider<ChangePasswordProvider>() ) );
            Assert.False( AccountController.ProviderSupportsChangePassword( Provider<NoChangePasswordProvider>() ) );
        }

        [Fact]
        public void ProviderSupportsChangePassword_IsFalseWithoutAProvider()
        {
            Assert.False( AccountController.ProviderSupportsChangePassword( null ) );
        }

        [Theory]
        [InlineData( "https://www.example.test" )]
        [InlineData( "https://www.example.test/" )]
        public void ConfirmAccountUrl_HasOneSlashAfterTheApplicationRoot( string publicApplicationRoot )
        {
            var applicationRoot = AccountController.ApplicationRootFrom( publicApplicationRoot );

            Assert.Equal( "https://www.example.test/", applicationRoot );
            Assert.Equal( "https://www.example.test/ConfirmAccount", AccountController.ConfirmAccountUrlFrom( applicationRoot ) );
        }
    }
}
