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
using System.Web;
using Xunit;

namespace org.secc.Authentication.Tests
{
    /// <summary>
    /// Authenticate's limit on code entries per caller address, with its SQL run against a temporary database.
    /// Each test uses caller addresses no other test uses, because the counts are kept between tests.
    /// </summary>
    public class SMSAuthenticationCodeEntryTests : IClassFixture<UserLoginTestDatabase>
    {
        private const string Code = "123456";
        private const string WrongCode = "654321";
        private const string LimitReached = "SMS login code entry limit reached. IP: ";

        private readonly UserLoginTestDatabase _database;

        public SMSAuthenticationCodeEntryTests( UserLoginTestDatabase database )
        {
            _database = database;
        }

        private static string Hash( string code )
        {
            return BCrypt.Net.BCrypt.HashPassword( code, BCrypt.Net.BCrypt.GenerateSalt( 4 ) );
        }

        private TestSMSAuthentication From( string forwardedFor )
        {
            var authentication = TestSMSAuthentication.Create( _database );
            authentication.Request = new FakeRequest( forwardedFor, "10.9.9.9" );
            return authentication;
        }

        private void EnterCorrectCodes( TestSMSAuthentication authentication, int id, int count )
        {
            for ( var entry = 1; entry <= count; entry++ )
            {
                Assert.True( authentication.Authenticate( _database.Load( id ), Code ) );
            }
        }

        [Fact]
        public void ThirtyEntriesAreAccepted_ThenEntriesAreRefused_WithoutUsingTheLoginsAttempts()
        {
            var id = _database.AddLogin( Hash( Code ), 0 );
            var authentication = From( "203.0.113.21:5000" );

            // Correct entries count too.
            EnterCorrectCodes( authentication, id, 30 );
            Assert.Empty( authentication.Logged );

            Assert.False( authentication.Authenticate( _database.Load( id ), Code ) );
            Assert.False( authentication.Authenticate( _database.Load( id ), Code ) );
            Assert.Equal( 0, _database.GetFailedPasswordAttemptCount( id ) );

            // The first refusal in the window is logged, once.
            Assert.Equal( new[] { LimitReached + "203.0.113.21" }, authentication.Logged );
        }

        [Fact]
        public void AnotherAddress_IsStillAccepted()
        {
            var id = _database.AddLogin( Hash( Code ), 0 );
            var authentication = From( "203.0.113.22" );
            EnterCorrectCodes( authentication, id, 30 );
            Assert.False( authentication.Authenticate( _database.Load( id ), Code ) );

            Assert.True( From( "203.0.113.122" ).Authenticate( _database.Load( id ), Code ) );
        }

        [Fact]
        public void RefusedEntry_IsNotCountedAgainstAnyLogin()
        {
            var hash = Hash( Code );
            var used = _database.AddLogin( hash, 0 );
            var other = _database.AddLogin( hash, 0 );
            var authentication = From( "203.0.113.24" );
            EnterCorrectCodes( authentication, used, 30 );

            Assert.False( authentication.Authenticate( _database.Load( other ), WrongCode ) );
            Assert.False( authentication.Authenticate( _database.Load( other ), Code ) );
            Assert.Equal( 0, _database.GetFailedPasswordAttemptCount( other ) );
        }

        [Theory]
        [InlineData( "198.18.0.", "203.0.113.23:443", "203.0.113.23" )]
        [InlineData( "198.51.100.", "192.0.2.25:4431", "192.0.2.25" )]
        public void EntriesWithTheSameLastForwardedEntry_ShareOneBudget( string earlierEntries, string lastEntry, string address )
        {
            var id = _database.AddLogin( Hash( Code ), 0 );

            for ( var entry = 1; entry <= 30; entry++ )
            {
                Assert.True( From( earlierEntries + entry + ", " + lastEntry ).Authenticate( _database.Load( id ), Code ) );
            }

            var refused = From( earlierEntries + "200, " + lastEntry );
            Assert.False( refused.Authenticate( _database.Load( id ), Code ) );
            Assert.Equal( new[] { LimitReached + address }, refused.Logged );
        }

        [Fact]
        public void BracketedIpv6Entries_ShareOneBudget_WhateverTheirPort()
        {
            var id = _database.AddLogin( Hash( Code ), 0 );

            for ( var entry = 1; entry <= 30; entry++ )
            {
                Assert.True( From( "[2001:db8:a::1]:" + ( 1000 + entry ) ).Authenticate( _database.Load( id ), Code ) );
            }

            var refused = From( "[2001:db8:a::1]:2222" );
            Assert.False( refused.Authenticate( _database.Load( id ), Code ) );
            Assert.Equal( new[] { LimitReached + "2001:db8:a::1" }, refused.Logged );
        }

        [Fact]
        public void EntriesForAnExpiredOrBlankCode_AreNotCounted()
        {
            var id = _database.AddLogin( Hash( Code ), 0 );
            var blank = _database.AddLogin( " ", 0 );
            var authentication = From( "203.0.113.28" );

            for ( var entry = 1; entry <= 20; entry++ )
            {
                var expired = _database.Load( id );
                expired.LastPasswordChangedDateTime = Rock.RockDateTime.Now.AddMinutes( -31 );
                Assert.False( authentication.Authenticate( expired, Code ) );

                var undated = _database.Load( id );
                undated.LastPasswordChangedDateTime = null;
                Assert.False( authentication.Authenticate( undated, Code ) );

                Assert.False( authentication.Authenticate( _database.Load( blank ), Code ) );
            }

            EnterCorrectCodes( authentication, id, 30 );
            Assert.False( authentication.Authenticate( _database.Load( id ), Code ) );
        }

        [Fact]
        public void WithoutARequest_EntriesAreCountedAsUnknown()
        {
            Assert.Null( HttpContext.Current );
            var id = _database.AddLogin( Hash( Code ), 0 );
            var authentication = TestSMSAuthentication.Create( _database );
            authentication.UseCurrentRequest = true;

            EnterCorrectCodes( authentication, id, 30 );
            Assert.False( authentication.Authenticate( _database.Load( id ), Code ) );
            Assert.Equal( new[] { LimitReached + "unknown" }, authentication.Logged );
        }

        [Fact]
        public void CurrentRequest_IsCountedByItsConnectingAddress()
        {
            var id = _database.AddLogin( Hash( Code ), 0 );
            var authentication = TestSMSAuthentication.Create( _database );
            authentication.UseCurrentRequest = true;

            RealRequest.Run( null, () =>
            {
                EnterCorrectCodes( authentication, id, 30 );
                Assert.False( authentication.Authenticate( _database.Load( id ), Code ) );
            } );

            Assert.Equal( new[] { LimitReached + "127.0.0.1" }, authentication.Logged );
        }
    }
}
