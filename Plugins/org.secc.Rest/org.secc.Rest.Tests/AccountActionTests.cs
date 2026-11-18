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
using Xunit;

namespace org.secc.Rest.Tests
{
    /// <summary>
    /// ROCK-8935 and ROCK-8820: the account actions, run on a controller whose OAuth client lookup,
    /// password rule and create budget are replaced. The replacement budget counts its calls and
    /// then refuses, so a create request that reaches it gets 429 and nothing after it runs. The
    /// controller has no database: an action that reaches a person or login lookup throws, which
    /// fails the test.
    /// </summary>
    public class AccountActionTests
    {
        private const string RulesMessage = "Passwords must be at least 8 characters.";
        private const string AgeMessage = "We are sorry, you must be at least 13 years old to create an account.";
        private const string NoAccountsMessage = "No accounts associated with this email address are able to be reset via email.";

        private static readonly DateTime Today = new DateTime( 2026, 10, 20 );

        private class TestAccountController : AccountController
        {
            public bool IsActiveClient { get; set; } = true;

            public int InputChecks { get; private set; }

            public int BudgetCalls { get; private set; }

            public TestAccountController()
            {
                Configuration = new HttpConfiguration();
                Request = new HttpRequestMessage();
            }

            protected override bool IsCallerActiveOAuthClient( string clientId )
            {
                return IsActiveClient;
            }

            protected override List<KeyValuePair<string, string>> GetPreThrottleInputErrors( Account account )
            {
                InputChecks++;
                return GetPreThrottleInputErrors( account, password => password.Length >= 8, RulesMessage, Today );
            }

            protected override bool TryRegisterAccountCreationAttempt( string clientId )
            {
                BudgetCalls++;
                return false;
            }
        }

        private static AccountController.Account NewAccount( string username, string password, int age )
        {
            return new AccountController.Account
            {
                FirstName = "Pat",
                LastName = "Sample",
                EmailAddress = "pat.sample@example.com",
                MobileNumber = "5025550100",
                Username = username,
                Password = password,
                Birthdate = Today.AddYears( -age )
            };
        }

        /// <summary>
        /// Runs the action and fails the test if it throws.
        /// </summary>
        private static HttpResponseMessage Run( Func<HttpResponseMessage> action )
        {
            HttpResponseMessage response = null;
            var exception = Record.Exception( () => response = action() );
            Assert.Null( exception );
            return response;
        }

        private static string SingleError( TestAccountController controller, string key )
        {
            Assert.Equal( new[] { key }, controller.ModelState.Keys.ToArray() );
            return controller.ModelState[key].Errors.Single().ErrorMessage;
        }

        [Theory]
        [InlineData( "short" )]
        [InlineData( "" )]
        [InlineData( null )]
        public void Create_PasswordFailsTheRule_Returns400WithoutUsingTheBudget( string password )
        {
            var controller = new TestAccountController();

            var response = Run( () => controller.CreateAccount( NewAccount( "pat.sample", password, 30 ) ) );

            Assert.Equal( HttpStatusCode.BadRequest, response.StatusCode );
            Assert.Equal( RulesMessage, SingleError( controller, "Account.Password" ) );
            Assert.Equal( 0, controller.BudgetCalls );
        }

        [Fact]
        public void Create_UnderMinimumAge_Returns400WithoutUsingTheBudget()
        {
            var controller = new TestAccountController();

            var response = Run( () => controller.CreateAccount( NewAccount( "pat.sample", "long-enough", 12 ) ) );

            Assert.Equal( HttpStatusCode.BadRequest, response.StatusCode );
            Assert.Equal( AgeMessage, SingleError( controller, "Account.Birthdate" ) );
            Assert.Equal( 0, controller.BudgetCalls );
        }

        [Fact]
        public void Create_NoBody_Returns400WithoutUsingTheBudget()
        {
            var controller = new TestAccountController();

            var response = Run( () => controller.CreateAccount( null ) );

            Assert.Equal( HttpStatusCode.BadRequest, response.StatusCode );
            Assert.Equal( "Account information is required.", SingleError( controller, "Account" ) );
            Assert.Equal( 0, controller.InputChecks );
            Assert.Equal( 0, controller.BudgetCalls );
        }

        [Fact]
        public void Create_ValidInput_IsCountedByTheBudget()
        {
            var controller = new TestAccountController();

            var response = Run( () => controller.CreateAccount( NewAccount( "pat.sample", "long-enough", 30 ) ) );

            Assert.Equal( ( HttpStatusCode ) 429, response.StatusCode );
            Assert.True( controller.ModelState.IsValid );
            Assert.Equal( 1, controller.InputChecks );
            Assert.Equal( 1, controller.BudgetCalls );
        }

        [Theory]
        [InlineData( null )]
        [InlineData( "" )]
        public void Create_NoUsername_IsCountedByTheBudget( string username )
        {
            var controller = new TestAccountController();

            var response = Run( () => controller.CreateAccount( NewAccount( username, null, 30 ) ) );

            Assert.Equal( ( HttpStatusCode ) 429, response.StatusCode );
            Assert.True( controller.ModelState.IsValid );
            Assert.Equal( 1, controller.BudgetCalls );
        }

        [Theory]
        [InlineData( null )]
        [InlineData( "" )]
        [InlineData( "   " )]
        [InlineData( "\t" )]
        public void ForgotPassword_BlankEmail_GetsTheNoAccountsResponseWithoutALookup( string email )
        {
            var controller = new TestAccountController();

            var response = Run( () => controller.ForgotPassword( email ) );

            Assert.Equal( HttpStatusCode.OK, response.StatusCode );
            Assert.True( response.TryGetContentValue( out AccountController.StandardResponse body ) );
            Assert.Equal( AccountController.StandardResponse.ResultCode.Error, body.Result );
            Assert.Equal( NoAccountsMessage, body.Message );
        }

        [Theory]
        [InlineData( "create" )]
        [InlineData( "confirmaccount" )]
        [InlineData( "forgotpassword" )]
        [InlineData( "smslogin" )]
        public void NotAnActiveClient_Returns403( string action )
        {
            var controller = new TestAccountController { IsActiveClient = false };

            var response = Run( () =>
            {
                switch ( action )
                {
                    case "create":
                        return controller.CreateAccount( NewAccount( "pat.sample", "long-enough", 30 ) );
                    case "confirmaccount":
                        return controller.ConfirmAccount( "123456" );
                    case "forgotpassword":
                        return controller.ForgotPassword( "pat.sample@example.com" );
                    default:
                        return controller.SMSLogin( "5025550100" );
                }
            } );

            Assert.Equal( HttpStatusCode.Forbidden, response.StatusCode );
            Assert.Equal( 0, controller.InputChecks );
            Assert.Equal( 0, controller.BudgetCalls );
        }
    }
}
