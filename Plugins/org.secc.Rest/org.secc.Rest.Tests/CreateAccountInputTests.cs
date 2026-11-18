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
using System.Text.RegularExpressions;
using org.secc.Rest.Controllers;
using Xunit;

namespace org.secc.Rest.Tests
{
    /// <summary>
    /// ROCK-8935: the account checks that run before the api/account/create throttle.
    /// </summary>
    public class CreateAccountInputTests
    {
        private const string RulesMessage = "Passwords must be at least 6 characters.";
        private const string AgeMessage = "We are sorry, you must be at least 13 years old to create an account.";

        private static readonly DateTime Today = new DateTime( 2026, 10, 16 );

        private int _passwordChecks;

        private bool AcceptAll( string password )
        {
            _passwordChecks++;
            return true;
        }

        private bool RejectAll( string password )
        {
            _passwordChecks++;
            return false;
        }

        private readonly List<string> _checkedPasswords = new List<string>();

        /// <summary>
        /// UserLoginService.IsPasswordValid with a blank PasswordRegularExpression: accepts everything.
        /// </summary>
        private bool BlankPattern( string password )
        {
            _checkedPasswords.Add( password );
            return true;
        }

        /// <summary>
        /// UserLoginService.IsPasswordValid with Rock's default pattern. Regex.IsMatch throws on null.
        /// </summary>
        private bool DefaultPattern( string password )
        {
            _checkedPasswords.Add( password );
            return new Regex( @"\w{6,255}" ).IsMatch( password );
        }

        private static AccountController.Account NewAccount( string username, string password, DateTime? birthdate )
        {
            return new AccountController.Account { Username = username, Password = password, Birthdate = birthdate };
        }

        private List<KeyValuePair<string, string>> Check( AccountController.Account account, Func<string, bool> isPasswordValid )
        {
            return AccountController.GetPreThrottleInputErrors( account, isPasswordValid, RulesMessage, Today );
        }

        [Fact]
        public void NullPassword_WithUsername_IsRejectedWithoutCallingTheRule()
        {
            var errors = Check( NewAccount( "someone", null, Today.AddYears( -30 ) ), AcceptAll );

            var error = Assert.Single( errors );
            Assert.Equal( "Account.Password", error.Key );
            Assert.Equal( RulesMessage, error.Value );
            Assert.Equal( 0, _passwordChecks );
        }

        [Fact]
        public void PasswordTheRuleRejects_IsRejected()
        {
            var errors = Check( NewAccount( "someone", "short", Today.AddYears( -30 ) ), RejectAll );

            var error = Assert.Single( errors );
            Assert.Equal( "Account.Password", error.Key );
            Assert.Equal( RulesMessage, error.Value );
            Assert.Equal( 1, _passwordChecks );
        }

        [Fact]
        public void UnderThirteen_IsRejected()
        {
            var errors = Check( NewAccount( "someone", "good-password", Today.AddYears( -13 ).AddDays( 1 ) ), AcceptAll );

            var error = Assert.Single( errors );
            Assert.Equal( "Account.Birthdate", error.Key );
            Assert.Equal( AgeMessage, error.Value );
        }

        [Fact]
        public void ExactlyThirteen_IsAccepted()
        {
            Assert.Empty( Check( NewAccount( "someone", "good-password", Today.AddYears( -13 ) ), AcceptAll ) );
        }

        [Fact]
        public void MissingBirthdate_CountsAsToday()
        {
            var error = Assert.Single( Check( NewAccount( "someone", "good-password", null ), AcceptAll ) );
            Assert.Equal( "Account.Birthdate", error.Key );
        }

        [Fact]
        public void BadPasswordAndAge_GiveBothErrors()
        {
            var errors = Check( NewAccount( "someone", "short", Today.AddYears( -10 ) ), RejectAll );

            Assert.Equal( new[] { "Account.Password", "Account.Birthdate" }, errors.Select( e => e.Key ).ToArray() );
            Assert.Equal( new[] { RulesMessage, AgeMessage }, errors.Select( e => e.Value ).ToArray() );
        }

        [Theory]
        [InlineData( null )]
        [InlineData( "" )]
        public void NoUsername_SkipsTheChecks( string username )
        {
            // SMS-style signups send no username and no password.
            Assert.Empty( Check( NewAccount( username, null, Today.AddYears( -10 ) ), RejectAll ) );
            Assert.Equal( 0, _passwordChecks );
        }

        [Fact]
        public void WhitespaceUsername_NullPassword_BlankPattern_HasNoErrors()
        {
            // A whitespace-only username gets no password login, so with a blank pattern the
            // request goes on to the signup path without a username.
            Assert.Empty( Check( NewAccount( " ", null, Today.AddYears( -30 ) ), BlankPattern ) );
            Assert.Equal( new[] { "" }, _checkedPasswords );
        }

        [Fact]
        public void WhitespaceUsername_NullPassword_DefaultPattern_IsRejectedWithoutAnException()
        {
            List<KeyValuePair<string, string>> errors = null;
            var exception = Record.Exception( () => errors = Check( NewAccount( " ", null, Today.AddYears( -30 ) ), DefaultPattern ) );

            Assert.Null( exception );
            var error = Assert.Single( errors );
            Assert.Equal( "Account.Password", error.Key );
            Assert.Equal( RulesMessage, error.Value );
        }

        [Fact]
        public void WhitespaceUsername_StillRunsThePasswordAndAgeChecks()
        {
            var errors = Check( NewAccount( " ", "short", Today.AddYears( -10 ) ), DefaultPattern );

            Assert.Equal( new[] { "Account.Password", "Account.Birthdate" }, errors.Select( e => e.Key ).ToArray() );
        }

        [Fact]
        public void ValidInput_HasNoErrors()
        {
            Assert.Empty( Check( NewAccount( "someone", "good-password", Today.AddYears( -30 ) ), AcceptAll ) );
            Assert.Equal( 1, _passwordChecks );
        }
    }
}
