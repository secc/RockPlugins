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
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Principal;
using System.Threading;
using System.Web;
using System.Web.Http;
using org.secc.Authentication.Tests;
using org.secc.Rest.Controllers;
using Rock;
using Rock.Data;
using Rock.Web.Cache;
using Xunit;

// The account limit tests share AccountController's in-memory budgets, set HttpContext.Current and move
// RockDateTime's clock, so no test in this assembly runs alongside another.
[assembly: CollectionBehavior( DisableTestParallelization = true )]

namespace org.secc.Rest.Tests
{
    /// <summary>
    /// The per-caller limits on api/account/create, api/account/confirmaccount and api/account/forgotpassword,
    /// run through the actions on a controller whose OAuth client check and current request are replaced, and
    /// which records the refusal lines it logs instead of writing them. The budgets and the once-per-window
    /// log record are the controller's own and are kept between tests, so every test uses caller addresses and
    /// email addresses that no other test uses. The tests that build their own requests also use connecting
    /// addresses that no other test uses; the tests that run on a real current request all connect from
    /// 127.0.0.1 and together stay far below that address's limits. A request that passes the limits gets to
    /// the next step, which the controller can't complete without a database: create answers "Account creation
    /// is temporarily unavailable." (no confirmation code attributes), confirm answers "Error confirming
    /// account." (blank code), and forgotpassword asks for the database context, which the controller replaces
    /// with an exception.
    /// </summary>
    public class AccountLimitTests
    {
        private const string CreateRefusal = "Too many account creation attempts. Please try again later.";
        private const string ConfirmRefusal = "Too many confirmation attempts. Please try again later.";
        private const string ForgotRefusal = "Too many password reset requests. Please try again later.";
        private const string NoAccountsMessage = "No accounts associated with this email address are able to be reset via email.";

        /// <summary>
        /// Forgot password requests allowed per window for one caller address.
        /// </summary>
        private const int ForgotPerCaller = 25;

        /// <summary>
        /// An OAuth client ApiKey, as the tests' callers name themselves (the client check is replaced).
        /// </summary>
        private const string ApiKey = "ABF391DA-6C2E-4F55-9D0E-8B7A2C41F0D3";

        private static int _lastForwarded;
        private static int _lastEmail;

        private readonly LimitTestController _controller = new LimitTestController();

        private class LimitTestController : AccountController
        {
            public LimitTestController()
            {
                Configuration = new HttpConfiguration();
                Request = new HttpRequestMessage();
            }

            /// <summary>
            /// The request the address helpers read; when null, they read HttpContext.Current.
            /// </summary>
            public HttpRequestBase CurrentRequest { get; set; }

            public bool IsActiveClient { get; set; } = true;

            /// <summary>
            /// How many requests asked for the database context.
            /// </summary>
            public int Lookups { get; private set; }

            protected override bool IsCallerActiveOAuthClient( string clientId )
            {
                return IsActiveClient;
            }

            protected override HttpRequestBase GetCurrentRequest()
            {
                return CurrentRequest ?? base.GetCurrentRequest();
            }

            protected override List<KeyValuePair<string, string>> GetPreThrottleInputErrors( Account account )
            {
                return new List<KeyValuePair<string, string>>();
            }

            protected override AttributeCache GetAttributeCache( Guid guid )
            {
                return null;
            }

            protected override RockContext CreateRockContext()
            {
                Lookups++;
                throw new LookupReachedException();
            }

            /// <summary>
            /// The messages of the exceptions the controller logged, in order. They are recorded here instead of
            /// being written to the Exception Log.
            /// </summary>
            public List<string> Logged { get; } = new List<string>();

            protected override void LogException( Exception ex )
            {
                Logged.Add( ex.Message );
            }
        }

        private class LookupReachedException : Exception
        {
        }

        /// <summary>
        /// Moves RockDateTime.Now by whole minutes (through the organization time zone) until disposed.
        /// </summary>
        private sealed class TestClock : IDisposable
        {
            private readonly TimeZoneInfo _timeZone = RockDateTime.OrgTimeZoneInfo;

            public void MoveBy( int minutes )
            {
                var offset = _timeZone.GetUtcOffset( DateTime.UtcNow ) + TimeSpan.FromMinutes( minutes );
                RockDateTime.Initialize( TimeZoneInfo.CreateCustomTimeZone( "Test " + minutes, offset, "Test", "Test" ) );
            }

            public void Dispose()
            {
                RockDateTime.Initialize( _timeZone );
            }
        }

        /// <summary>
        /// A caller address (in 10.0.0.0/8) that no other request in the test run uses.
        /// </summary>
        private static string NewForwarded()
        {
            var n = Interlocked.Increment( ref _lastForwarded );
            return "10." + ( ( n >> 16 ) & 255 ) + "." + ( ( n >> 8 ) & 255 ) + "." + ( n & 255 );
        }

        /// <summary>
        /// An email address that no other request in the test run uses.
        /// </summary>
        private static string NewEmail()
        {
            return "pat.sample." + Interlocked.Increment( ref _lastEmail ) + "@example.com";
        }

        /// <summary>
        /// A request with this X-Forwarded-For header, presented as IIS presents it (the header is also the
        /// HTTP_X_FORWARDED_FOR server variable), from a connecting address no other request uses unless one
        /// is given.
        /// </summary>
        private static FakeRequest IisRequest( string forwardedFor, string remoteAddress = null )
        {
            var request = new FakeRequest( forwardedFor, remoteAddress ?? FakeRequest.NewAddress() );
            request.ServerVariables["HTTP_X_FORWARDED_FOR"] = forwardedFor;
            return request;
        }

        /// <summary>
        /// Runs the call with HttpContext.Current signed in under this name, the name the actions read as the
        /// OAuth client's. Without it the name is null.
        /// </summary>
        private static bool AsClient( string name, Func<bool> call )
        {
            var previous = HttpContext.Current;
            HttpContext.Current = new HttpContext( new HttpRequest( string.Empty, "http://localhost/", string.Empty ), new HttpResponse( TextWriter.Null ) )
            {
                User = new GenericPrincipal( new GenericIdentity( name ), new string[0] )
            };

            try
            {
                return call();
            }
            finally
            {
                HttpContext.Current = previous;
            }
        }

        /// <summary>
        /// Runs the call n times and returns how many of them passed the limits.
        /// </summary>
        private static int Passed( int n, Func<bool> call )
        {
            var passed = 0;
            for ( var i = 0; i < n; i++ )
            {
                if ( call() )
                {
                    passed++;
                }
            }

            return passed;
        }

        /// <summary>
        /// True for the response of a request that passed the limits, false for the refusal. Any other
        /// response fails the test.
        /// </summary>
        private static bool PassedLimits( HttpResponseMessage response, HttpStatusCode passedStatus, string passedMessage, string refusal )
        {
            Assert.True( response.TryGetContentValue( out AccountController.StandardResponse body ) );
            Assert.Equal( AccountController.StandardResponse.ResultCode.Error, body.Result );

            if ( response.StatusCode == passedStatus )
            {
                Assert.Equal( passedMessage, body.Message );
                return true;
            }

            Assert.Equal( ( HttpStatusCode ) 429, response.StatusCode );
            Assert.Equal( refusal, body.Message );
            return false;
        }

        /// <summary>
        /// Runs api/account/create for the request (null: HttpContext.Current) and returns whether it passed
        /// the limits.
        /// </summary>
        private bool Create( HttpRequestBase request )
        {
            _controller.CurrentRequest = request;
            var account = new AccountController.Account
            {
                FirstName = "Pat",
                LastName = "Sample",
                EmailAddress = "pat.sample@example.com",
                Username = "pat.sample",
                Password = "long-enough",
                Birthdate = new DateTime( 1990, 1, 1 )
            };

            return PassedLimits( _controller.CreateAccount( account ), HttpStatusCode.InternalServerError,
                "Account creation is temporarily unavailable.", CreateRefusal );
        }

        /// <summary>
        /// Runs api/account/confirmaccount with a blank code for the request (null: HttpContext.Current) and
        /// returns whether it passed the limits.
        /// </summary>
        private bool Confirm( HttpRequestBase request )
        {
            _controller.CurrentRequest = request;
            return PassedLimits( _controller.ConfirmAccount( null ), HttpStatusCode.InternalServerError,
                "Error confirming account.", ConfirmRefusal );
        }

        /// <summary>
        /// Runs api/account/forgotpassword for the request (null: HttpContext.Current) and returns whether it
        /// passed the limits (it asked for the database context). A request that didn't must have got the
        /// refusal without asking for it.
        /// </summary>
        private bool Forgot( HttpRequestBase request, string email )
        {
            return Forgot( request, email, out _ );
        }

        private bool Forgot( HttpRequestBase request, string email, out string refusal )
        {
            _controller.CurrentRequest = request;
            var lookups = _controller.Lookups;
            HttpResponseMessage response;
            refusal = null;

            try
            {
                response = _controller.ForgotPassword( email );
            }
            catch ( LookupReachedException )
            {
                Assert.Equal( lookups + 1, _controller.Lookups );
                return true;
            }

            Assert.Equal( lookups, _controller.Lookups );
            Assert.False( PassedLimits( response, HttpStatusCode.OK, null, ForgotRefusal ) );
            refusal = ( int ) response.StatusCode + " " + response.Content.ReadAsStringAsync().Result;
            return false;
        }

        [Fact]
        public void ConfirmAccount_BracketedIpv6Entries_ShareOneBudget_WhateverTheirPort()
        {
            Assert.Equal( 10, Passed( 10, () => Confirm( IisRequest( "[2001:db8::5]:1111" ) ) ) );

            Assert.False( Confirm( IisRequest( "[2001:db8::5]:2222" ) ) );
        }

        [Fact]
        public void CreateAccount_BracketedIpv6Entries_ShareOneBudget_WhateverTheirPort()
        {
            Assert.Equal( 5, Passed( 5, () => Create( IisRequest( "[2001:db8::6]:1" ) ) ) );

            Assert.False( Create( IisRequest( "[2001:db8::6]:2" ) ) );
        }

        [Fact]
        public void ConfirmAccount_UnderscoreHeader_IsNotTheCallerAddress()
        {
            // IIS presents a header named X_Forwarded_For as the HTTP_X_FORWARDED_FOR server variable too.
            Assert.Equal( 11, Passed( 11, () =>
            {
                var request = new FakeRequest( null, FakeRequest.NewAddress() );
                request.Headers["X_Forwarded_For"] = "203.0.113.66";
                request.ServerVariables["HTTP_X_FORWARDED_FOR"] = "203.0.113.66";
                return Confirm( request );
            } ) );
        }

        [Fact]
        public void ConfirmAccount_EarlierEntries_ShareTheLastEntrysBudget()
        {
            Assert.Equal( 10, Passed( 10, () => Confirm( IisRequest( NewForwarded() + ", 203.0.113.44" ) ) ) );

            Assert.False( Confirm( IisRequest( NewForwarded() + ", 203.0.113.44" ) ) );
        }

        [Fact]
        public void ConfirmAccount_CurrentRequest_IsCountedByItsLastEntry_WhateverItsPort()
        {
            var passed = 0;
            for ( var i = 0; i < 10; i++ )
            {
                RealRequest.Run( "[2001:db8::7]:1111", () => passed += Confirm( null ) ? 1 : 0 );
            }

            var last = true;
            RealRequest.Run( "[2001:db8::7]:2222", () => last = Confirm( null ) );

            Assert.Equal( 10, passed );
            Assert.False( last );
        }

        [Fact]
        public void ConfirmAccount_RequestsRefusedForTheirAddress_DoNotCountTowardThePeer()
        {
            var peer = FakeRequest.NewAddress();
            var a = NewForwarded();

            Assert.Equal( 10, Passed( 10, () => Confirm( IisRequest( a, peer ) ) ) );
            Assert.Equal( 0, Passed( 20, () => Confirm( IisRequest( a, peer ) ) ) );
            Assert.Equal( 110, Passed( 110, () => Confirm( IisRequest( NewForwarded(), peer ) ) ) );

            Assert.False( Confirm( IisRequest( NewForwarded(), peer ) ) );
        }

        [Fact]
        public void CreateAccount_RequestsRefusedForTheirAddress_DoNotCountTowardThePeer()
        {
            var peer = FakeRequest.NewAddress();
            var a = NewForwarded();

            Assert.Equal( 5, Passed( 5, () => Create( IisRequest( a, peer ) ) ) );
            Assert.Equal( 0, Passed( 10, () => Create( IisRequest( a, peer ) ) ) );
            Assert.Equal( 195, Passed( 195, () => Create( IisRequest( NewForwarded(), peer ) ) ) );

            Assert.False( Create( IisRequest( NewForwarded(), peer ) ) );
        }

        [Fact]
        public void CreateAccount_NamesOfOneClient_ShareItsBudget()
        {
            var a = NewForwarded();

            Assert.Equal( 5, Passed( 5, () => AsClient( ApiKey, () => Create( IisRequest( a ) ) ) ) );

            Assert.False( AsClient( ApiKey.ToLowerInvariant() + " ", () => Create( IisRequest( a ) ) ) );
        }

        [Fact]
        public void ConfirmAccount_NamesOfOneClient_ShareItsBudget()
        {
            var b = NewForwarded();

            Assert.Equal( 10, Passed( 10, () => AsClient( ApiKey, () => Confirm( IisRequest( b ) ) ) ) );

            Assert.False( AsClient( "{" + ApiKey.ToLowerInvariant() + "}", () => Confirm( IisRequest( b ) ) ) );
        }

        [Fact]
        public void ForgotPassword_CallerAddress_HasTwentyFiveRequestsPerWindow()
        {
            var a = NewForwarded();

            Assert.Equal( ForgotPerCaller, Passed( ForgotPerCaller, () => Forgot( IisRequest( a ), NewEmail() ) ) );

            Assert.False( Forgot( IisRequest( a ), NewEmail() ) );
            Assert.Equal( ForgotPerCaller, _controller.Lookups );
        }

        [Fact]
        public void ForgotPassword_AnotherCallerAddress_BehindTheSamePeer_IsAllowed()
        {
            var peer = FakeRequest.NewAddress();
            var a = NewForwarded();
            Assert.Equal( ForgotPerCaller, Passed( ForgotPerCaller, () => Forgot( IisRequest( a, peer ), NewEmail() ) ) );
            Assert.False( Forgot( IisRequest( a, peer ), NewEmail() ) );

            Assert.True( Forgot( IisRequest( NewForwarded(), peer ), NewEmail() ) );
        }

        [Fact]
        public void ForgotPassword_EmailAddress_HasThreeRequestsPerWindow_FromAnyCaller()
        {
            var email = NewEmail();

            Assert.Equal( 3, Passed( 3, () => Forgot( IisRequest( NewForwarded() ), email ) ) );

            Assert.False( Forgot( IisRequest( NewForwarded() ), email ) );
            Assert.True( Forgot( IisRequest( NewForwarded() ), NewEmail() ) );
        }

        [Fact]
        public void ForgotPassword_VariantsOfAnEmailAddress_ShareItsBudget()
        {
            var variants = new[]
            {
                "Pat.Sample.{0}@Example.com",
                "PAT.SAMPLE.{0}@EXAMPLE.COM",
                "  pat.sample.{0}@example.com\t",
                "pat.sample. {0}@example.com",
                "pat.sample.{0}@example.com\u007F",
                "pat.sample.{0}@\uFF45\uFF58\uFF41\uFF4D\uFF50\uFF4C\uFF45.com",
                "pat.sample.{0}\uFF20example.com",
                "pat.sam\u00ADple.{0}@example.com",
                "pat.sample.{0}\u200B@example.com",
                "pat.sample.{0}@example.com\u00A0",
                "pat.sample.{0}\u00E9@example.com",
                "\uD800pat.sample.{0}@example.com",
                "pat.sample.{0}@example.com\uDC00"
            };

            for ( var i = 0; i < variants.Length; i++ )
            {
                var n = Interlocked.Increment( ref _lastEmail );
                var variant = string.Format( variants[i], n );

                Assert.True( Passed( 3, () => Forgot( IisRequest( NewForwarded() ), variant ) ) == 3, "variant " + i );
                Assert.False( Forgot( IisRequest( NewForwarded() ), "pat.sample." + n + "@example.com" ), "variant " + i );
            }
        }

        [Theory]
        [InlineData( "pat!sample.{0}@example.com" )]
        [InlineData( "pat~sample.{0}@example.com" )]
        public void ForgotPassword_EmailAddressesThatDifferInPrintableCharacters_HaveTheirOwnBudgets( string other )
        {
            var n = Interlocked.Increment( ref _lastEmail );
            var email = "patsample." + n + "@example.com";
            Assert.Equal( 3, Passed( 3, () => Forgot( IisRequest( NewForwarded() ), email ) ) );
            Assert.False( Forgot( IisRequest( NewForwarded() ), email ) );

            Assert.True( Forgot( IisRequest( NewForwarded() ), string.Format( other, n ) ) );
        }

        [Fact]
        public void ForgotPassword_LongEmailAddresses_AreComparedOnTheirFirst254Characters()
        {
            var n = Interlocked.Increment( ref _lastEmail ).ToString();
            var first253 = n + new string( 'x', 253 - n.Length );

            Assert.True( Forgot( IisRequest( NewForwarded() ), first253 + "a1@example.com" ) );
            Assert.True( Forgot( IisRequest( NewForwarded() ), first253 + "a2@example.com" ) );
            Assert.True( Forgot( IisRequest( NewForwarded() ), "   " + first253 + "a3@example.com" ) );

            Assert.False( Forgot( IisRequest( NewForwarded() ), first253 + "a4@example.com" ) );
            Assert.True( Forgot( IisRequest( NewForwarded() ), first253 + "b1@example.com" ) );
        }

        [Fact]
        public void ForgotPassword_SurrogatePairCutAt254Characters_IsCountedWithoutAnError()
        {
            var n = Interlocked.Increment( ref _lastEmail ).ToString();
            var first253 = n + new string( 'y', 253 - n.Length );

            Assert.True( Forgot( IisRequest( NewForwarded() ), first253 + "\uD83D\uDE00@example.com" ) );
            Assert.True( Forgot( IisRequest( NewForwarded() ), first253 + "\uD83D\uDE01@example.com" ) );
            Assert.True( Forgot( IisRequest( NewForwarded() ), first253 + "\uD83D\uDE00@example.com" ) );

            Assert.False( Forgot( IisRequest( NewForwarded() ), first253 + "\uD83D\uDE00@example.com" ) );
        }

        [Fact]
        public void ForgotPassword_LongEmailAddresses_AreCutAt254CharactersBeforeTheyAreNormalized()
        {
            var n = Interlocked.Increment( ref _lastEmail ).ToString();
            var first253 = n + new string( 'z', 253 - n.Length );

            // The 254th character is an "e" followed by a combining acute accent, which the cut leaves out.
            for ( var i = 1; i <= 3; i++ )
            {
                Assert.True( Forgot( IisRequest( NewForwarded() ), first253 + "e\u0301" + i + "@example.com" ), "request " + i );
            }

            Assert.False( Forgot( IisRequest( NewForwarded() ), first253 + "e4@example.com" ) );
        }

        [Fact]
        public void ForgotPassword_Peer_HasTwoHundredRequestsPerWindow()
        {
            var peer = FakeRequest.NewAddress();

            Assert.Equal( 200, Passed( 200, () => Forgot( IisRequest( NewForwarded(), peer ), NewEmail() ) ) );

            Assert.False( Forgot( IisRequest( NewForwarded(), peer ), NewEmail() ) );
        }

        [Fact]
        public void ForgotPassword_SpentPeer_LeavesOtherPeers_AndItsCallersAddressBudgets()
        {
            var peer = FakeRequest.NewAddress();
            Assert.Equal( 200, Passed( 200, () => Forgot( IisRequest( NewForwarded(), peer ), NewEmail() ) ) );
            var a = NewForwarded();
            Assert.Equal( 0, Passed( 3, () => Forgot( IisRequest( a, peer ), NewEmail() ) ) );

            Assert.Equal( ForgotPerCaller, Passed( ForgotPerCaller, () => Forgot( IisRequest( a ), NewEmail() ) ) );
            Assert.False( Forgot( IisRequest( a ), NewEmail() ) );
        }

        [Fact]
        public void ForgotPassword_SpentPeer_AddsNoCallerAddressKeys()
        {
            // The email address's window is started ten minutes back, so it is the oldest one tracked: it would
            // be among the first dropped if the 20,000-key table filled up.
            var email = NewEmail();
            using ( var clock = new TestClock() )
            {
                clock.MoveBy( -10 );
                Assert.Equal( 3, Passed( 3, () => Forgot( IisRequest( NewForwarded() ), email ) ) );
            }

            Assert.False( Forgot( IisRequest( NewForwarded() ), email ) );

            var peer = FakeRequest.NewAddress();
            Assert.Equal( 200, Passed( 200, () => Forgot( IisRequest( NewForwarded(), peer ), NewEmail() ) ) );
            Assert.Equal( 0, Passed( 20000, () => Forgot( IisRequest( NewForwarded(), peer ), NewEmail() ) ) );

            Assert.False( Forgot( IisRequest( NewForwarded() ), email ) );
        }

        [Fact]
        public void ForgotPassword_BlankEmail_IsNotCounted()
        {
            var a = NewForwarded();
            foreach ( var blank in new[] { null, "", "   ", "\t" } )
            {
                for ( var i = 0; i < 3; i++ )
                {
                    _controller.CurrentRequest = IisRequest( a );
                    var response = _controller.ForgotPassword( blank );

                    Assert.Equal( HttpStatusCode.OK, response.StatusCode );
                    Assert.True( response.TryGetContentValue( out AccountController.StandardResponse body ) );
                    Assert.Equal( NoAccountsMessage, body.Message );
                }
            }

            Assert.Equal( ForgotPerCaller, Passed( ForgotPerCaller, () => Forgot( IisRequest( a ), NewEmail() ) ) );
        }

        [Fact]
        public void ForgotPassword_NotAnActiveClient_IsNotCounted()
        {
            var a = NewForwarded();
            var email = NewEmail();
            _controller.IsActiveClient = false;
            for ( var i = 0; i < 6; i++ )
            {
                _controller.CurrentRequest = IisRequest( a );
                Assert.Equal( HttpStatusCode.Forbidden, _controller.ForgotPassword( email ).StatusCode );
            }

            _controller.IsActiveClient = true;

            Assert.Equal( 3, Passed( 3, () => Forgot( IisRequest( a ), email ) ) );
            Assert.Equal( ForgotPerCaller - 3, Passed( ForgotPerCaller - 3, () => Forgot( IisRequest( a ), NewEmail() ) ) );
        }

        [Fact]
        public void ForgotPassword_EveryLimit_GivesTheSameRefusal()
        {
            var a = NewForwarded();
            Passed( ForgotPerCaller, () => Forgot( IisRequest( a ), NewEmail() ) );
            Assert.False( Forgot( IisRequest( a ), NewEmail(), out var callerRefusal ) );

            var email = NewEmail();
            Passed( 3, () => Forgot( IisRequest( NewForwarded() ), email ) );
            Assert.False( Forgot( IisRequest( NewForwarded() ), email, out var emailRefusal ) );

            var peer = FakeRequest.NewAddress();
            Passed( 200, () => Forgot( IisRequest( NewForwarded(), peer ), NewEmail() ) );
            Assert.False( Forgot( IisRequest( NewForwarded(), peer ), NewEmail(), out var peerRefusal ) );

            Assert.StartsWith( "429 ", callerRefusal );
            Assert.Equal( callerRefusal, emailRefusal );
            Assert.Equal( callerRefusal, peerRefusal );
        }

        [Fact]
        public void ForgotPassword_Refusals_AreLoggedOncePerConnectingAddressInAWindow()
        {
            var peer = FakeRequest.NewAddress();
            var a = NewForwarded();
            Assert.Equal( ForgotPerCaller, Passed( ForgotPerCaller, () => Forgot( IisRequest( a, peer ), NewEmail() ) ) );
            Assert.False( Forgot( IisRequest( a, peer ), NewEmail() ) );
            Assert.Equal( new[] { ForgotLogLine( peer, a ) }, _controller.Logged );

            var b = NewForwarded();
            Assert.Equal( ForgotPerCaller, Passed( ForgotPerCaller, () => Forgot( IisRequest( b, peer ), NewEmail() ) ) );
            Assert.Equal( 0, Passed( 2, () => Forgot( IisRequest( b, peer ), NewEmail() ) ) );
            Assert.Equal( new[] { ForgotLogLine( peer, a ) }, _controller.Logged );

            var otherPeer = FakeRequest.NewAddress();
            var c = NewForwarded();
            Assert.Equal( ForgotPerCaller, Passed( ForgotPerCaller, () => Forgot( IisRequest( c, otherPeer ), NewEmail() ) ) );
            Assert.False( Forgot( IisRequest( c, otherPeer ), NewEmail() ) );
            Assert.Equal( new[] { ForgotLogLine( peer, a ), ForgotLogLine( otherPeer, c ) }, _controller.Logged );
        }

        [Fact]
        public void EveryAction_Refusals_AreLoggedOncePerConnectingAddressInAWindow()
        {
            var peer = FakeRequest.NewAddress();
            var a = NewForwarded();
            Assert.Equal( 5, Passed( 5, () => Create( IisRequest( a, peer ) ) ) );
            Assert.False( Create( IisRequest( a, peer ) ) );
            Assert.Equal( 10, Passed( 10, () => Confirm( IisRequest( a, peer ) ) ) );
            Assert.False( Confirm( IisRequest( a, peer ) ) );
            Assert.Equal( ForgotPerCaller, Passed( ForgotPerCaller, () => Forgot( IisRequest( a, peer ), NewEmail() ) ) );
            Assert.False( Forgot( IisRequest( a, peer ), NewEmail() ) );

            var lines = new[]
            {
                "Account creation budget exceeded. Client:  PeerIP: " + peer + " ForwardedIP: " + a,
                "Account confirmation attempt budget exceeded. Client:  PeerIP: " + peer + " ForwardedIP: " + a,
                ForgotLogLine( peer, a )
            };
            Assert.Equal( lines, _controller.Logged );

            var b = NewForwarded();
            Assert.Equal( 5, Passed( 5, () => Create( IisRequest( b, peer ) ) ) );
            Assert.Equal( 0, Passed( 2, () => Create( IisRequest( b, peer ) ) ) );
            Assert.Equal( 10, Passed( 10, () => Confirm( IisRequest( b, peer ) ) ) );
            Assert.Equal( 0, Passed( 2, () => Confirm( IisRequest( b, peer ) ) ) );
            Assert.Equal( lines, _controller.Logged );
        }

        /// <summary>
        /// The Exception Log line for a forgot password request from a caller with no client name, refused by the
        /// caller-address or connecting-address limit.
        /// </summary>
        private static string ForgotLogLine( string peer, string forwarded )
        {
            return "Forgot password request budget exceeded. Client:  PeerIP: " + peer + " ForwardedIP: " + forwarded;
        }

        /// <summary>
        /// The Exception Log line for a forgot password request from a caller with no client name, refused by the
        /// email address limit.
        /// </summary>
        private static string ForgotEmailLogLine( string peer, string forwarded )
        {
            return "Forgot password email address budget exceeded. Request from Client:  PeerIP: " + peer + " ForwardedIP: " + forwarded;
        }

        [Fact]
        public void ForgotPassword_EmailLimitRefusals_HaveTheirOwnLine_LoggedOncePerConnectingAddressInAWindow()
        {
            // At one connecting address the email limit refuses first and then the caller-address limit; at another,
            // the other way round. Each kind of refusal is logged once there, and the other kind doesn't stop it.
            // Another client refused for the same email address there gets its own line, and a second spent email
            // address refused there adds none.
            var peer = FakeRequest.NewAddress();
            var email = NewEmail();
            Assert.Equal( 3, Passed( 3, () => Forgot( IisRequest( NewForwarded(), peer ), email ) ) );
            var b = NewForwarded();
            Assert.False( Forgot( IisRequest( b, peer ), email ) );
            Assert.False( Forgot( IisRequest( NewForwarded(), peer ), email ) );
            var client = Guid.NewGuid().ToString();
            var e = NewForwarded();
            Assert.False( AsClient( client, () => Forgot( IisRequest( e, peer ), email ) ) );
            var secondEmail = NewEmail();
            Assert.Equal( 3, Passed( 3, () => Forgot( IisRequest( NewForwarded(), peer ), secondEmail ) ) );
            Assert.False( Forgot( IisRequest( NewForwarded(), peer ), secondEmail ) );
            var a = NewForwarded();
            Assert.Equal( ForgotPerCaller, Passed( ForgotPerCaller + 2, () => Forgot( IisRequest( a, peer ), NewEmail() ) ) );

            var otherPeer = FakeRequest.NewAddress();
            var c = NewForwarded();
            Assert.Equal( ForgotPerCaller, Passed( ForgotPerCaller + 2, () => Forgot( IisRequest( c, otherPeer ), NewEmail() ) ) );
            var otherEmail = NewEmail();
            Assert.Equal( 3, Passed( 3, () => Forgot( IisRequest( NewForwarded(), otherPeer ), otherEmail ) ) );
            var d = NewForwarded();
            Assert.False( Forgot( IisRequest( d, otherPeer ), otherEmail ) );
            Assert.False( Forgot( IisRequest( NewForwarded(), otherPeer ), otherEmail ) );

            var lines = new[]
            {
                ForgotEmailLogLine( peer, b ),
                "Forgot password email address budget exceeded. Request from Client: " + client + " PeerIP: " + peer + " ForwardedIP: " + e,
                ForgotLogLine( peer, a ),
                ForgotLogLine( otherPeer, c ),
                ForgotEmailLogLine( otherPeer, d )
            };
            Assert.Equal( lines, _controller.Logged );
        }

        [Fact]
        public void EveryAction_ClientsAtOneConnectingAddress_HaveTheirOwnRefusalLines()
        {
            var peer = FakeRequest.NewAddress();
            var lines = new List<string>();
            foreach ( var client in new[] { Guid.NewGuid().ToString(), Guid.NewGuid().ToString() } )
            {
                var a = NewForwarded();
                Assert.Equal( 5, Passed( 6, () => AsClient( client, () => Create( IisRequest( a, peer ) ) ) ) );
                Assert.Equal( 10, Passed( 11, () => AsClient( client, () => Confirm( IisRequest( a, peer ) ) ) ) );
                Assert.Equal( ForgotPerCaller, Passed( ForgotPerCaller + 1, () => AsClient( client, () => Forgot( IisRequest( a, peer ), NewEmail() ) ) ) );

                var addresses = " Client: " + client + " PeerIP: " + peer + " ForwardedIP: " + a;
                lines.Add( "Account creation budget exceeded." + addresses );
                lines.Add( "Account confirmation attempt budget exceeded." + addresses );
                lines.Add( "Forgot password request budget exceeded." + addresses );
            }

            Assert.Equal( lines, _controller.Logged );
        }

        [Fact]
        public void EveryAction_ConnectingAddressLimits_AreKeptPerClient()
        {
            var peer = FakeRequest.NewAddress();
            var spent = Guid.NewGuid().ToString();
            Assert.Equal( 200, Passed( 201, () => AsClient( spent, () => Create( IisRequest( NewForwarded(), peer ) ) ) ) );
            Assert.Equal( 120, Passed( 121, () => AsClient( spent, () => Confirm( IisRequest( NewForwarded(), peer ) ) ) ) );
            Assert.Equal( 200, Passed( 201, () => AsClient( spent, () => Forgot( IisRequest( NewForwarded(), peer ), NewEmail() ) ) ) );

            var other = Guid.NewGuid().ToString();
            Assert.True( AsClient( other, () => Create( IisRequest( NewForwarded(), peer ) ) ) );
            Assert.True( AsClient( other, () => Confirm( IisRequest( NewForwarded(), peer ) ) ) );
            Assert.True( AsClient( other, () => Forgot( IisRequest( NewForwarded(), peer ), NewEmail() ) ) );
        }

        [Fact]
        public void ForgotPassword_NamesOfOneClient_ShareItsBudget()
        {
            var a = NewForwarded();
            var lower = ApiKey.ToLowerInvariant();

            Assert.Equal( ForgotPerCaller - 2, Passed( ForgotPerCaller - 2, () => AsClient( ApiKey, () => Forgot( IisRequest( a ), NewEmail() ) ) ) );
            Assert.True( AsClient( lower + " ", () => Forgot( IisRequest( a ), NewEmail() ) ) );
            Assert.True( AsClient( "{" + lower + "}", () => Forgot( IisRequest( a ), NewEmail() ) ) );

            Assert.False( AsClient( ApiKey.Replace( "-", "" ), () => Forgot( IisRequest( a ), NewEmail() ) ) );
        }

        [Fact]
        public void CreateAccount_AnotherClient_HasItsOwnBudget()
        {
            var a = NewForwarded();
            Assert.Equal( 5, Passed( 5, () => AsClient( ApiKey, () => Create( IisRequest( a ) ) ) ) );

            Assert.True( AsClient( Guid.NewGuid().ToString(), () => Create( IisRequest( a ) ) ) );
            Assert.False( AsClient( ApiKey, () => Create( IisRequest( a ) ) ) );
        }

        [Fact]
        public void ConfirmAccount_AnotherClient_HasItsOwnBudget()
        {
            var a = NewForwarded();
            Assert.Equal( 10, Passed( 10, () => AsClient( ApiKey, () => Confirm( IisRequest( a ) ) ) ) );

            Assert.True( AsClient( Guid.NewGuid().ToString(), () => Confirm( IisRequest( a ) ) ) );
            Assert.False( AsClient( ApiKey, () => Confirm( IisRequest( a ) ) ) );
        }

        [Fact]
        public void ForgotPassword_AnotherClient_HasItsOwnBudget()
        {
            var a = NewForwarded();
            Assert.Equal( ForgotPerCaller, Passed( ForgotPerCaller, () => AsClient( ApiKey, () => Forgot( IisRequest( a ), NewEmail() ) ) ) );

            Assert.True( AsClient( Guid.NewGuid().ToString(), () => Forgot( IisRequest( a ), NewEmail() ) ) );
            Assert.False( AsClient( ApiKey, () => Forgot( IisRequest( a ), NewEmail() ) ) );
        }

        [Fact]
        public void ForgotPassword_EmailBudget_IsSeparateFromTheCallerAddressBudget()
        {
            // The email addresses read like a's caller-address budget key, whole and without its start: these
            // callers send no client name, which counts as the empty Guid.
            var a = NewForwarded();
            foreach ( var email in new[] { "forgot|client:" + Guid.Empty + "|fwd:" + a, Guid.Empty + "|fwd:" + a } )
            {
                Assert.Equal( 3, Passed( 3, () => Forgot( IisRequest( NewForwarded() ), email ) ) );
            }

            Assert.Equal( ForgotPerCaller, Passed( ForgotPerCaller, () => Forgot( IisRequest( a ), NewEmail() ) ) );
            Assert.False( Forgot( IisRequest( a ), NewEmail() ) );
        }

        [Fact]
        public void ForgotPassword_SameLastEntryBehindTwoPeers_SharesItsBudget()
        {
            var a = NewForwarded();
            var peer1 = FakeRequest.NewAddress();
            var peer2 = FakeRequest.NewAddress();
            Assert.Equal( 3, Passed( 3, () => Forgot( IisRequest( a, peer1 ), NewEmail() ) ) );
            Assert.Equal( ForgotPerCaller - 3, Passed( ForgotPerCaller - 3, () => Forgot( IisRequest( a, peer2 ), NewEmail() ) ) );

            Assert.False( Forgot( IisRequest( a, peer1 ), NewEmail() ) );
            Assert.False( Forgot( IisRequest( a, peer2 ), NewEmail() ) );
        }

        [Fact]
        public void ForgotPassword_EarlierEntries_ShareTheLastEntrysBudget()
        {
            var a = NewForwarded();

            Assert.Equal( ForgotPerCaller, Passed( ForgotPerCaller, () => Forgot( IisRequest( NewForwarded() + ", " + a ), NewEmail() ) ) );

            Assert.False( Forgot( IisRequest( NewForwarded() + ", " + a ), NewEmail() ) );
        }

        [Fact]
        public void ForgotPassword_EmailBudget_IsSharedByEveryClient()
        {
            var email = NewEmail();
            Assert.True( AsClient( ApiKey, () => Forgot( IisRequest( NewForwarded() ), email ) ) );
            Assert.True( AsClient( Guid.NewGuid().ToString(), () => Forgot( IisRequest( NewForwarded() ), email ) ) );
            Assert.True( Forgot( IisRequest( NewForwarded() ), email ) );

            Assert.False( AsClient( Guid.NewGuid().ToString(), () => Forgot( IisRequest( NewForwarded() ), email ) ) );
        }

        [Fact]
        public void ForgotPassword_Budgets_AreSeparateFromCreateAndConfirm()
        {
            var a = NewForwarded();
            Assert.Equal( 5, Passed( 5, () => Create( IisRequest( a ) ) ) );
            Assert.Equal( 10, Passed( 10, () => Confirm( IisRequest( a ) ) ) );

            Assert.Equal( ForgotPerCaller, Passed( ForgotPerCaller, () => Forgot( IisRequest( a ), NewEmail() ) ) );
            Assert.False( Forgot( IisRequest( a ), NewEmail() ) );
            Assert.False( Create( IisRequest( a ) ) );
            Assert.False( Confirm( IisRequest( a ) ) );
        }

        [Fact]
        public void ForgotPassword_RequestsRefusedForTheirAddress_DoNotCountTowardThePeer()
        {
            var peer = FakeRequest.NewAddress();
            var a = NewForwarded();

            Assert.Equal( ForgotPerCaller, Passed( ForgotPerCaller, () => Forgot( IisRequest( a, peer ), NewEmail() ) ) );
            Assert.Equal( 0, Passed( 10, () => Forgot( IisRequest( a, peer ), NewEmail() ) ) );
            Assert.Equal( 200 - ForgotPerCaller, Passed( 200 - ForgotPerCaller, () => Forgot( IisRequest( NewForwarded(), peer ), NewEmail() ) ) );

            Assert.False( Forgot( IisRequest( NewForwarded(), peer ), NewEmail() ) );
        }

        [Fact]
        public void ForgotPassword_RequestRefusedForItsCallerAddress_DoesNotCountTowardTheEmail()
        {
            var a = NewForwarded();
            var email = NewEmail();
            Assert.Equal( ForgotPerCaller, Passed( ForgotPerCaller, () => Forgot( IisRequest( a ), NewEmail() ) ) );
            Assert.Equal( 0, Passed( 3, () => Forgot( IisRequest( a ), email ) ) );

            Assert.Equal( 3, Passed( 3, () => Forgot( IisRequest( NewForwarded() ), email ) ) );
        }

        [Fact]
        public void ForgotPassword_RequestRefusedForItsEmail_StillCountsTowardTheCaller()
        {
            var email = NewEmail();
            Assert.Equal( 3, Passed( 3, () => Forgot( IisRequest( NewForwarded() ), email ) ) );
            var a = NewForwarded();

            Assert.False( Forgot( IisRequest( a ), email ) );

            Assert.Equal( ForgotPerCaller - 1, Passed( ForgotPerCaller, () => Forgot( IisRequest( a ), NewEmail() ) ) );
        }

        [Fact]
        public void CallerBudgets_ConcurrentRequests_AreEachCountedOnce()
        {
            // The counting step itself (a private method of the controller's budget table), from 8 threads at once on
            // one peer key and one caller-address key, every call within budget: each call must be counted.
            var countCallerAttempt = ( Func<string, int, string, int, bool> ) Delegate.CreateDelegate(
                typeof( Func<string, int, string, int, bool> ),
                typeof( AccountController ).GetNestedType( "ConfirmationAttempts", BindingFlags.NonPublic ).GetMethod( "TryRegisterCallerAttempt" ) );
            var peerKey = "test|peer:" + FakeRequest.NewAddress();
            var forwardedKey = "test|fwd:" + NewForwarded();
            Assert.True( countCallerAttempt( peerKey, 1, forwardedKey, 1 ) );

            var errors = new List<Exception>();
            var threads = Enumerable.Range( 0, 8 ).Select( i => new Thread( () =>
            {
                try
                {
                    for ( var n = 0; n < 20000; n++ )
                    {
                        countCallerAttempt( peerKey, int.MaxValue, forwardedKey, int.MaxValue );
                    }
                }
                catch ( Exception ex )
                {
                    lock ( errors )
                    {
                        errors.Add( ex );
                    }
                }
            } ) ).ToList();
            threads.ForEach( t => t.Start() );
            threads.ForEach( t => t.Join() );

            Assert.Empty( errors );
            Assert.False( countCallerAttempt( peerKey, 160001, forwardedKey, int.MaxValue ) );
            Assert.True( countCallerAttempt( peerKey, 160002, forwardedKey, int.MaxValue ) );
            Assert.False( countCallerAttempt( forwardedKey, 160002, peerKey, int.MaxValue ) );
            Assert.True( countCallerAttempt( forwardedKey, 160003, peerKey, int.MaxValue ) );
        }

        [Fact]
        public void ForgotPassword_Budgets_LastFifteenMinutes()
        {
            var a = NewForwarded();
            var email = NewEmail();
            using ( var clock = new TestClock() )
            {
                // A request an hour ahead runs the table's once-a-minute sweep of expired windows then, so the sweep
                // doesn't run again in this test and the windows below can only restart on their own.
                clock.MoveBy( 60 );
                Assert.True( Forgot( IisRequest( NewForwarded() ), NewEmail() ) );
                clock.MoveBy( 0 );

                Assert.Equal( ForgotPerCaller, Passed( ForgotPerCaller, () => Forgot( IisRequest( a ), NewEmail() ) ) );
                Assert.Equal( 3, Passed( 3, () => Forgot( IisRequest( NewForwarded() ), email ) ) );
                Thread.Sleep( 50 );

                clock.MoveBy( 14 );
                Assert.False( Forgot( IisRequest( a ), NewEmail() ) );
                Assert.False( Forgot( IisRequest( NewForwarded() ), email ) );

                clock.MoveBy( 15 );
                Assert.True( Forgot( IisRequest( a ), NewEmail() ) );
                Assert.True( Forgot( IisRequest( NewForwarded() ), email ) );
            }
        }

        [Fact]
        public void ForgotPassword_CurrentRequest_IsCountedByItsLastEntry()
        {
            var passed = 0;
            for ( var i = 0; i < ForgotPerCaller; i++ )
            {
                RealRequest.Run( "198.18.0." + i + ", 203.0.113.91:" + ( 1000 + i ), () => passed += Forgot( null, NewEmail() ) ? 1 : 0 );
            }

            var next = true;
            RealRequest.Run( "198.18.0.9, 203.0.113.91:2000", () => next = Forgot( null, NewEmail() ) );

            Assert.Equal( ForgotPerCaller, passed );
            Assert.False( next );
        }

        [Fact]
        public void ForgotPassword_CurrentRequest_WithAnotherLastEntry_IsAllowed()
        {
            var passed = 0;
            for ( var i = 0; i <= ForgotPerCaller; i++ )
            {
                RealRequest.Run( "203.0.113.92", () => passed += Forgot( null, NewEmail() ) ? 1 : 0 );
            }

            var other = false;
            RealRequest.Run( "203.0.113.92, 203.0.113.93", () => other = Forgot( null, NewEmail() ) );

            Assert.Equal( ForgotPerCaller, passed );
            Assert.True( other );
        }

        [Fact]
        public void ForgotPassword_CurrentRequest_WithoutForwardedFor_IsCountedByItsConnectingAddress()
        {
            var passed = 0;
            for ( var i = 0; i <= ForgotPerCaller; i++ )
            {
                RealRequest.Run( null, () => passed += Forgot( null, NewEmail() ) ? 1 : 0 );
            }

            Assert.Equal( ForgotPerCaller, passed );
        }
    }
}
