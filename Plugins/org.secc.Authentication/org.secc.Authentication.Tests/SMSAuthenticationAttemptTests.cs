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
using System.Linq;
using System.Threading;
using Xunit;

namespace org.secc.Authentication.Tests
{
    /// <summary>
    /// Authenticate's per-code attempt limit and single use, with its SQL run against a temporary database.
    /// </summary>
    public class SMSAuthenticationAttemptTests : IClassFixture<UserLoginTestDatabase>
    {
        private const string Code = "123456";
        private const string WrongCode = "654321";

        private readonly UserLoginTestDatabase _database;
        private readonly TestSMSAuthentication _authentication;

        public SMSAuthenticationAttemptTests( UserLoginTestDatabase database )
        {
            _database = database;
            _authentication = TestSMSAuthentication.Create( database );
        }

        private static string Hash( string code )
        {
            // Low cost factor to keep the tests fast; Authenticate reads the cost from the hash.
            return BCrypt.Net.BCrypt.HashPassword( code, BCrypt.Net.BCrypt.GenerateSalt( 4 ) );
        }

        [Theory]
        [InlineData( 0 )]
        [InlineData( null )]
        public void WrongCodes_AreCountedUpToFive_ThenEveryEntryIsRefused( int? initialAttemptCount )
        {
            var id = _database.AddLogin( Hash( Code ), initialAttemptCount );

            for ( var attempt = 1; attempt <= 5; attempt++ )
            {
                Assert.False( _authentication.Authenticate( _database.Load( id ), WrongCode ) );
                Assert.Equal( attempt, _database.GetFailedPasswordAttemptCount( id ) );
            }

            Assert.False( _authentication.Authenticate( _database.Load( id ), WrongCode ) );
            Assert.False( _authentication.Authenticate( _database.Load( id ), Code ) );
            Assert.Equal( 5, _database.GetFailedPasswordAttemptCount( id ) );
        }

        [Fact]
        public void CorrectCode_AfterFourAttempts_Succeeds()
        {
            var id = _database.AddLogin( Hash( Code ), 4 );

            Assert.True( _authentication.Authenticate( _database.Load( id ), Code ) );
            Assert.Null( _database.GetPassword( id ) );
        }

        [Fact]
        public void CorrectCode_SignsInOnce()
        {
            var id = _database.AddLogin( Hash( Code ), 0 );

            Assert.True( _authentication.Authenticate( _database.Load( id ), Code ) );
            Assert.Null( _database.GetPassword( id ) );
            Assert.False( _authentication.Authenticate( _database.Load( id ), Code ) );
        }

        [Fact]
        public void CorrectCode_IsRefused_ForALoginLoadedBeforeItWasUsed()
        {
            var id = _database.AddLogin( Hash( Code ), 0 );
            var first = _database.Load( id );
            var second = _database.Load( id );

            Assert.True( _authentication.Authenticate( first, Code ) );
            var countAfterUse = _database.GetFailedPasswordAttemptCount( id );

            Assert.False( _authentication.Authenticate( second, Code ) );
            Assert.Equal( countAfterUse, _database.GetFailedPasswordAttemptCount( id ) );
        }

        [Fact]
        public void ConcurrentCorrectCodes_SignInExactlyOnce()
        {
            const int threadCount = 20;
            var id = _database.AddLogin( Hash( Code ), 0 );

            // Every entry uses the login as it was loaded before any of them was checked.
            var loaded = _database.Load( id );
            var accepted = 0;
            var start = new Barrier( threadCount );

            var threads = Enumerable.Range( 0, threadCount ).Select( t => new Thread( () =>
            {
                start.SignalAndWait();
                if ( _authentication.Authenticate( loaded, Code ) )
                {
                    Interlocked.Increment( ref accepted );
                }
            } ) ).ToList();

            threads.ForEach( t => t.Start() );
            threads.ForEach( t => t.Join() );

            Assert.Equal( 1, accepted );
            Assert.Null( _database.GetPassword( id ) );
        }

        [Fact]
        public void CorrectCode_IsRefused_WhenUsingUpTheCodeFails()
        {
            var id = _database.AddLogin( Hash( Code ), 0 );
            var authentication = TestSMSAuthentication.Create( _database );
            authentication.BeforeStatement = sql =>
            {
                if ( sql.Contains( "[Password] = NULL" ) )
                {
                    throw new InvalidOperationException();
                }
            };

            Assert.False( authentication.Authenticate( _database.Load( id ), Code ) );
            Assert.IsType<InvalidOperationException>( Assert.Single( authentication.LoggedExceptions ) );
        }

        [Fact]
        public void WrongAndCorrectCodes_LogNoException()
        {
            var id = _database.AddLogin( Hash( Code ), 0 );
            var authentication = TestSMSAuthentication.Create( _database );

            Assert.False( authentication.Authenticate( _database.Load( id ), WrongCode ) );
            Assert.True( authentication.Authenticate( _database.Load( id ), Code ) );
            Assert.Empty( authentication.LoggedExceptions );
        }

        [Fact]
        public void CorrectCode_IsRefused_AndLeavesACodeIssuedMeanwhile()
        {
            const string newCode = "111111";
            var newHash = Hash( newCode );
            var id = _database.AddLogin( Hash( Code ), 0 );
            var authentication = TestSMSAuthentication.Create( _database );
            authentication.BeforeStatement = sql =>
            {
                // A new code is issued, and has used 3 attempts, between checking this entry and using it up.
                if ( sql.Contains( "[Password] = NULL" ) )
                {
                    _database.SetLogin( id, newHash, 3 );
                }
            };

            Assert.False( authentication.Authenticate( _database.Load( id ), Code ) );
            Assert.Equal( newHash, _database.GetPassword( id ) );
            Assert.Equal( 3, _database.GetFailedPasswordAttemptCount( id ) );

            Assert.True( _authentication.Authenticate( _database.Load( id ), newCode ) );
        }

        [Fact]
        public void WrongCode_DoesNotUseUpTheCode()
        {
            var id = _database.AddLogin( Hash( Code ), 0 );

            Assert.False( _authentication.Authenticate( _database.Load( id ), WrongCode ) );
            Assert.NotNull( _database.GetPassword( id ) );
            Assert.True( _authentication.Authenticate( _database.Load( id ), Code ) );
        }

        [Fact]
        public void NewCode_SignsIn_AfterTheLastOneWasUsed()
        {
            const string newCode = "111111";
            var id = _database.AddLogin( Hash( Code ), 0 );

            Assert.True( _authentication.Authenticate( _database.Load( id ), Code ) );
            _database.ReplaceCode( id, Hash( newCode ) );

            Assert.True( _authentication.Authenticate( _database.Load( id ), newCode ) );
        }

        [Fact]
        public void ReplacedCode_IsRefused_WithoutCountingAnAttempt()
        {
            var id = _database.AddLogin( Hash( Code ), 0 );
            var loadedBeforeReplacement = _database.Load( id );
            _database.ReplaceCode( id, Hash( "111111" ) );

            Assert.False( _authentication.Authenticate( loadedBeforeReplacement, Code ) );
            Assert.Equal( 0, _database.GetFailedPasswordAttemptCount( id ) );
        }

        [Fact]
        public void BlankStoredCode_IsRefused_WithoutCountingAnAttempt()
        {
            var id = _database.AddLogin( " ", 0 );

            Assert.False( _authentication.Authenticate( _database.Load( id ), Code ) );
            Assert.Equal( 0, _database.GetFailedPasswordAttemptCount( id ) );
        }

        [Fact]
        public void WrongCode_CountsOnlyTheLoginBeingChecked()
        {
            var hash = Hash( Code );
            var id = _database.AddLogin( hash, 0 );
            var otherId = _database.AddLogin( hash, 0 );

            Assert.False( _authentication.Authenticate( _database.Load( id ), WrongCode ) );
            Assert.Equal( 1, _database.GetFailedPasswordAttemptCount( id ) );
            Assert.Equal( 0, _database.GetFailedPasswordAttemptCount( otherId ) );
        }

        [Fact]
        public void ConcurrentWrongCodes_UseExactlyFiveAttempts()
        {
            const int threadCount = 20;
            var id = _database.AddLogin( Hash( Code ), 0 );

            // Every entry uses the login as it was loaded before any of them was counted.
            var loaded = _database.Load( id );
            var accepted = 0;
            var start = new Barrier( threadCount );

            var threads = Enumerable.Range( 0, threadCount ).Select( t => new Thread( () =>
            {
                start.SignalAndWait();
                if ( _authentication.Authenticate( loaded, WrongCode ) )
                {
                    Interlocked.Increment( ref accepted );
                }
            } ) ).ToList();

            threads.ForEach( t => t.Start() );
            threads.ForEach( t => t.Join() );

            Assert.Equal( 0, accepted );
            Assert.Equal( 5, _database.GetFailedPasswordAttemptCount( id ) );
            Assert.False( _authentication.Authenticate( loaded, Code ) );
        }
    }
}
