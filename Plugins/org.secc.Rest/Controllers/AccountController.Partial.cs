// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License shoud be included with this file.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Rock.Model;
using System.Web;
using System.Collections.Generic;
using System.Web.Routing;
using org.secc.OAuth.Data;
using org.secc.OAuth.Model;
using Rock;
using System;
using System.Linq;
using Rock.Security.ExternalAuthentication;
using Rock.Communication;
using Rock.Web.Cache;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using org.secc.PersonMatch;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Numerics;

namespace org.secc.Rest.Controllers
{
    /// <summary>
    /// Account REST API
    /// </summary>
    public partial class AccountController : ApiController
    {
        const int MINIMUM_AGE = 13;

        [Route( "api/account/create" )]
        [Authorize]
        public HttpResponseMessage CreateAccount( Account account )
        {
            OAuthContext oAuthContext = new OAuthContext();
            ClientService clientService = new ClientService( oAuthContext );
            var clientId = HttpContext.Current.User.Identity.Name;
            Client oAuthClient = clientService.GetByApiKey( clientId.AsGuid() );
            if ( oAuthClient.Active )
            {

                var rockContext = new Rock.Data.RockContext();
                PersonService personService = new PersonService( rockContext );
                UserLoginService userLoginService = new UserLoginService( rockContext );

                // Validate the Model
                if ( !string.IsNullOrEmpty( account.Username ) )
                {
                    // Make sure the username is unique
                    UserLogin user = userLoginService.GetByUserName( account.Username );
                    if ( user != null )
                    {
                        ModelState.AddModelError( "Account.Username", "Username already exists" );
                    }

                    // Make sure the password is valid
                    if ( !UserLoginService.IsPasswordValid( account.Password ) )
                    {
                        ModelState.AddModelError( "Account.Password", UserLoginService.FriendlyPasswordRules() );
                    }

                    // Make sure this person meets the minimum age requirement
                    var birthday = account.Birthdate ?? Rock.RockDateTime.Today;
                    if ( RockDateTime.Today.AddYears( MINIMUM_AGE * -1 ) < birthday )
                    {
                        ModelState.AddModelError( "Account.Birthdate", string.Format( "We are sorry, you must be at least {0} years old to create an account.", MINIMUM_AGE ) );
                    }
                }
                if ( !ModelState.IsValid )
                {
                    return Request.CreateErrorResponse( HttpStatusCode.BadRequest, ModelState );
                }

                // Try to match the person
                var matchPerson = personService.GetByMatch( account.FirstName, account.LastName, account.Birthdate, account.EmailAddress, account.MobileNumber, null, null );

                bool confirmed = false;
                Person person = new Person();
                if ( matchPerson != null && matchPerson.Count() == 1 )
                {
                    var mobilePhone = matchPerson.First().PhoneNumbers.Where( pn => pn.NumberTypeValueId == DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() ).Id ).FirstOrDefault();
                    // The emails MUST match for security
                    if ( matchPerson.First().Email == account.EmailAddress && ( mobilePhone == null || mobilePhone.Number.Right(10) == account.MobileNumber.Right( 10 ) ) )
                    {
                        person = matchPerson.First();

                        // If they don't have a current mobile phone, go ahead and set it
                        if ( mobilePhone == null )
                        {
                            string cleanNumber = PhoneNumber.CleanNumber( account.MobileNumber );
                            var phoneNumber = new PhoneNumber { NumberTypeValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() ).Id };
                            person.PhoneNumbers.Add( phoneNumber );
                            phoneNumber.CountryCode = cleanNumber.Length > 10 ? cleanNumber.Left( 10 - cleanNumber.Length ) : PhoneNumber.DefaultCountryCode();
                            phoneNumber.Number = cleanNumber.Right( 10 );
                            phoneNumber.IsMessagingEnabled = true;
                        }

                        // Make sure the gender matches
                        person.Gender = account.Gender;

                        confirmed = true;
                    }
                }

                // If we don't have a match, create a new web prospect
                if (!confirmed )
                {
                    DefinedValueCache dvcConnectionStatus = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_CONNECTION_STATUS_WEB_PROSPECT.AsGuid() );
                    DefinedValueCache dvcRecordStatus = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_PENDING.AsGuid() );

                    person.FirstName = account.FirstName;
                    person.LastName = account.LastName;
                    person.NickName = account.NickName;
                    person.Email = account.EmailAddress;
                    person.IsEmailActive = true;
                    person.EmailPreference = EmailPreference.EmailAllowed;
                    person.RecordTypeValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid() ).Id;
                    if ( dvcConnectionStatus != null )
                    {
                        person.ConnectionStatusValueId = dvcConnectionStatus.Id;
                    }

                    if ( dvcRecordStatus != null )
                    {
                        person.RecordStatusValueId = dvcRecordStatus.Id;
                    }

                    person.Gender = account.Gender;

                    var birthday = account.Birthdate;
                    if ( birthday.HasValue )
                    {
                        person.BirthMonth = birthday.Value.Month;
                        person.BirthDay = birthday.Value.Day;
                        if ( birthday.Value.Year != DateTime.MinValue.Year )
                        {
                            person.BirthYear = birthday.Value.Year;
                        }
                    }

                    if ( !string.IsNullOrWhiteSpace( account.MobileNumber ) )
                    {
                        string cleanNumber = PhoneNumber.CleanNumber( account.MobileNumber );
                        var phoneNumber = new PhoneNumber { NumberTypeValueId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() ).Id };
                        person.PhoneNumbers.Add( phoneNumber );
                        phoneNumber.CountryCode = cleanNumber.Length > 10 ? cleanNumber.Left( 10 - cleanNumber.Length ) : PhoneNumber.DefaultCountryCode();
                        phoneNumber.Number = cleanNumber.Right( 10 );
                        phoneNumber.IsMessagingEnabled = true;
                    }

                    PersonService.SaveNewPerson( person, rockContext );
                }
                UserLogin userLogin = null;

                if ( !string.IsNullOrWhiteSpace( account.Username ) && UserLoginService.IsPasswordValid( account.Password ) )
                {
                    // Create the user login (only require confirmation if we didn't match the person)
                    userLogin = UserLoginService.Create(
                        rockContext,
                        person,
                        AuthenticationServiceType.Internal,
                        EntityTypeCache.Get( Rock.SystemGuid.EntityType.AUTHENTICATION_DATABASE.AsGuid() ).Id,
                        account.Username,
                        account.Password,
                        confirmed );
                }
                else if ( !string.IsNullOrWhiteSpace( account.EmailAddress ) && !confirmed )
                {
                    userLogin = userLoginService.Queryable()
                        .Where( u => u.UserName == ( "SMS_" + person.Id.ToString() ) )
                        .FirstOrDefault();

                    // Create an unconfirmed SMS user login if does not exist
                    if ( userLogin == null )
                    {
                        var entityTypeId = EntityTypeCache.Get( "Rock.Security.ExternalAuthentication.SMSAuthentication" ).Id;

                        userLogin = new UserLogin()
                        {
                            UserName = "SMS_" + person.Id.ToString(),
                            EntityTypeId = entityTypeId,
                            IsConfirmed = false,
                            PersonId = person.Id
                        };
                        userLoginService.Add( userLogin );
                    }
                }
                // Send an email to confirm the account.
                if ( userLogin != null && userLogin.IsConfirmed != true )
                {

                    // For mobile we will make a custom/short confirmation code
                    var mobileConfirmationCode = new BigInteger( MD5.Create().ComputeHash( userLogin.Guid.ToByteArray() ) ).ToString().Right( 6 );
                    var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( null, person );
                    mergeFields.Add( "MobileConfirmationCode", mobileConfirmationCode );
                    mergeFields.Add( "ConfirmAccountUrl", GlobalAttributesCache.Get().GetValue( "PublicApplicationRoot" ).EnsureTrailingForwardslash() + "ConfirmAccount" );
                    mergeFields.Add( "Person", userLogin.Person );
                    mergeFields.Add( "User", userLogin );

                    var recipients = new List<RockEmailMessageRecipient>();
                    recipients.Add( new RockEmailMessageRecipient( userLogin.Person, mergeFields ) );

                    var message = new RockEmailMessage( Rock.SystemGuid.SystemCommunication.SECURITY_CONFIRM_ACCOUNT.AsGuid() );
                    message.SetRecipients( recipients );
                    message.AppRoot = GlobalAttributesCache.Get().GetValue( "PublicApplicationRoot" ).EnsureTrailingForwardslash();
                    message.CreateCommunicationRecord = false;
                    message.Send();
                }

                rockContext.SaveChanges();

                return ControllerContext.Request.CreateResponse( HttpStatusCode.OK, new StandardResponse() { 
                    Message = string.Format("Account has been created.{0}", confirmed?"": " An email has been sent to confirm the email address." ), 
                    Result = StandardResponse.ResultCode.Success } 
                );

            }

            return ControllerContext.Request.CreateResponse( HttpStatusCode.Forbidden );

        }

        /// <summary>
        /// API method to confirm an account given the confirmation code
        /// </summary>
        /// <param name="confirmationCode">A confirmation code for a user's account.</param>
        /// <returns></returns>
        [Route( "api/account/confirmaccount" )]
        [Authorize]
        public HttpResponseMessage ConfirmAccount( [FromBody] string confirmationCode )
        {
            OAuthContext oAuthContext = new OAuthContext();
            ClientService clientService = new ClientService( oAuthContext );
            var clientId = HttpContext.Current.User.Identity.Name;
            Client oAuthClient = clientService.GetByApiKey( clientId.AsGuid() );
            if ( oAuthClient.Active )
            {
                // Load the User Login that has the confirmation code and mark it as confirmed.
                var rockContext = new Rock.Data.RockContext();
                UserLoginService userLoginService = new UserLoginService( rockContext );
                UserLogin user = userLoginService.GetByConfirmationCode( confirmationCode );
                if (user == null)
                {
                    var unconfirmedAccounts = userLoginService.Queryable().Where( ul => ul.IsConfirmed == false ).ToList();
                    user = unconfirmedAccounts.FirstOrDefault( ul => new BigInteger( MD5.Create().ComputeHash( ul.Guid.ToByteArray() ) ).ToString().Right( 6 ) == confirmationCode );
                }
                if ( user != null )
                {
                    user.IsConfirmed = true;
                    rockContext.SaveChanges();
                    return ControllerContext.Request.CreateResponse( HttpStatusCode.OK, new StandardResponse() { Message = "Account has been confirmed", Result = StandardResponse.ResultCode.Success } );
                }

                return ControllerContext.Request.CreateResponse( HttpStatusCode.InternalServerError, new StandardResponse() { Message = "Error confirming account.", Result = StandardResponse.ResultCode.Error } );

            }

            return ControllerContext.Request.CreateResponse( HttpStatusCode.Forbidden );

        }


        /// <summary>
        /// Get a person's family information from Rock
        /// </summary>
        /// <returns>A list of family members.</returns>
        [Route( "api/account/family" )]
        [HttpGet]
        [Authorize]
        public HttpResponseMessage Family()
        {
            try
            {

                var currentUser = UserLoginService.GetCurrentUser();

                if ( currentUser == null )
                {
                    return ControllerContext.Request.CreateResponse( HttpStatusCode.Forbidden );
                }

                List<FamilyMemberProfile> familyMembers = new List<FamilyMemberProfile>();

                FamilyMemberProfile familyMember = new FamilyMemberProfile();
                foreach ( GroupMember member in currentUser.Person.GetFamilyMembers( true ) )
                {
                    familyMember = new FamilyMemberProfile();
                    familyMember.IsCurrentPerson = member.PersonId == currentUser.PersonId;
                    familyMember.FamilyRole = member.GroupRole.Name;
                    familyMember.FullName = member.Person.FullName;
                    familyMember.PersonId = member.Person.Id;
                    familyMember.Profile = new Profile( member.Person );
                    familyMembers.Add( familyMember );
                }

                return ControllerContext.Request.CreateResponse( HttpStatusCode.OK, familyMembers );

            }
            catch
            {
                return ControllerContext.Request.CreateResponse( HttpStatusCode.InternalServerError, "Internal Server Error" );
            }
        }

        /// <summary>
        /// API method to send a forgot password email.
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        [Route( "api/account/forgotpassword" )]
        [Authorize]
        public HttpResponseMessage ForgotPassword( [FromBody] string email )
        {
            OAuthContext oAuthContext = new OAuthContext();
            ClientService clientService = new ClientService( oAuthContext );
            var clientId = HttpContext.Current.User.Identity.Name;
            Client oAuthClient = clientService.GetByApiKey( clientId.AsGuid() );
            if ( oAuthClient.Active )
            {
                var response = new StandardResponse();
                var rockContext = new Rock.Data.RockContext();
                PersonService personService = new PersonService( rockContext );
                UserLoginService userLoginService = new UserLoginService( rockContext );
                bool hasAccountWithPasswordResetAbility = false;
                var results = new List<IDictionary<string, object>>();

                // Check to make sure we have accounts matching the email address given
                foreach ( Person person in personService.GetByEmail( email )
                    .Where( p => p.Users.Any() ) )
                {
                    var users = new List<UserLogin>();
                    foreach ( UserLogin user in userLoginService.GetByPersonId( person.Id ) )
                    {
                        if ( user.EntityType != null )
                        {
                            var component = Rock.Security.AuthenticationContainer.GetComponent( user.EntityType.Name );
                            if ( component != null && !component.RequiresRemoteAuthentication )
                            {
                                users.Add( user );
                                hasAccountWithPasswordResetAbility = true;
                            }
                        }
                    }

                    var resultsDictionary = new Dictionary<string, object>();
                    resultsDictionary.Add( "Person", person );
                    resultsDictionary.Add( "Users", users );
                    results.Add( resultsDictionary );
                }
                var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields(null, null );

                // If we found matching accounts that have the ability to be reset, go ahead and send the email
                if ( results.Count > 0 && hasAccountWithPasswordResetAbility )
                {
                    mergeFields.Add( "Results", results.ToArray() );

                    var emailMessage = new RockEmailMessage( Rock.SystemGuid.SystemCommunication.SECURITY_FORGOT_USERNAME.AsGuid() );
                    emailMessage.AddRecipient( RockEmailMessageRecipient.CreateAnonymous( email, mergeFields ) );
                    emailMessage.AppRoot = GlobalAttributesCache.Get().GetValue( "PublicApplicationRoot" ).EnsureTrailingForwardslash();
                    emailMessage.CreateCommunicationRecord = false;
                    emailMessage.Send();
                    response.Result = StandardResponse.ResultCode.Success;
                    response.Message = "Forgot password email has been sent successfully.";
                }
                else                 {
                    // the person either has no user accounts or none of them are allowed to have their passwords reset (Facebook/Google/SMS/etc)
                    response.Result = StandardResponse.ResultCode.Error;
                    response.Message = "No accounts associated with this email address are able to be reset via email.";
                }
                return ControllerContext.Request.CreateResponse( HttpStatusCode.OK, response );
            }

            return ControllerContext.Request.CreateResponse( HttpStatusCode.Forbidden );

        }


        /// <summary>
        /// Get a person's profile (person) information from Rock
        /// </summary>
        /// <returns>A Profile object</returns>
        [Route( "api/account/profile" )]
        [HttpGet]
        [Authorize]
        public HttpResponseMessage GetProfile()
        {
            try
            {
                var currentUser = UserLoginService.GetCurrentUser();

                if ( currentUser == null )
                {
                    return ControllerContext.Request.CreateResponse( HttpStatusCode.Forbidden );
                }

                return ControllerContext.Request.CreateResponse( HttpStatusCode.OK, new Profile( currentUser.Person ) );

            }
            catch
            {
                return ControllerContext.Request.CreateResponse( HttpStatusCode.InternalServerError, "Internal Server Error" );
            }
        }


        /// <summary>
        /// API method for kicking off the first leg of an SMS Login request
        /// </summary>
        /// <param name="phoneNumber">The phone number to use for authentication</param>
        /// <returns></returns>
        [Route( "api/account/smslogin" )]
        [Authorize]
        public HttpResponseMessage SMSLogin( [FromBody] string phoneNumber )
        {

            OAuthContext oAuthContext = new OAuthContext();
            ClientService clientService = new ClientService( oAuthContext );
            var clientId = HttpContext.Current.User.Identity.Name;
            Client oAuthClient = clientService.GetByApiKey( clientId.AsGuid() );
            if ( oAuthClient.Active )
            {
                Rock.Data.RockContext rockContext = new Rock.Data.RockContext();
                var smsAuth = ( SMSAuthentication ) Rock.Security.AuthenticationContainer.GetComponent( "Rock.Security.ExternalAuthentication.SMSAuthentication" );

                PhoneNumberService phoneNumberService = new PhoneNumberService( rockContext );
                var numberOwners = phoneNumberService.Queryable()
                    .Where( pn => pn.Number == phoneNumber )
                    .Select( pn => pn.Person )
                    .DistinctBy( p => p.Id )
                    .ToList();

                SMSLoginResponse loginResponse = new SMSLoginResponse();
                // If we don't have this phone number
                if ( numberOwners.Count == 0 )
                {
                    loginResponse.Result = SMSLoginResponse.ResultCode.NoMatch;
                    loginResponse.Message = "There was an issue with your request";
                }
                // If we match more than 1 person
                else if ( numberOwners.Count > 1 )
                {
                    loginResponse.Result = SMSLoginResponse.ResultCode.MultipleMatch;
                    loginResponse.Message = "There was an issue with your request";
                }
                // If we've matched a single person
                else if ( numberOwners.Count == 1 )
                {
                    var person = numberOwners.FirstOrDefault();
                    // Make sure the person is alive
                    if ( person.IsDeceased )
                    {
                        loginResponse.Result = SMSLoginResponse.ResultCode.NoMatch;
                        loginResponse.Message = "There was an issue with your request";
                    }

                    // Check the age of the person
                    var minimumAge = smsAuth.GetAttributeValue( "MinimumAge" ).AsInteger();
                    if ( minimumAge != 0 )
                    {
                        if ( person.Age == null )
                        {
                            loginResponse.Result = SMSLoginResponse.ResultCode.Error;
                            loginResponse.Message = string.Format( "We could not determine your age. You must be at least {0} years old to log in.", minimumAge );
                        }
                        if ( person.Age.Value < minimumAge )
                        {
                            loginResponse.Result = SMSLoginResponse.ResultCode.Error;
                            loginResponse.Message = string.Format( "You must be at least {0} years old to log in.", minimumAge );
                        }
                    }

                    // If we get all the way here, go ahead and attempt to login!
                    var response = smsAuth.SendSMSAuthentication( phoneNumber );
                    if ( response )
                    {
                        loginResponse.Result = SMSLoginResponse.ResultCode.Success;
                        loginResponse.Message = "We have sent you a code please enter it to login.";
                        loginResponse.Username = "SMS_" + person.Id.ToString();
                    }
                    else
                    {
                        loginResponse.Result = SMSLoginResponse.ResultCode.Error;
                        loginResponse.Message = "An unknown error occurred.";
                    }
                }

                return ControllerContext.Request.CreateResponse( HttpStatusCode.OK, loginResponse );
            }

            return ControllerContext.Request.CreateResponse( HttpStatusCode.Forbidden );
        }


        #region Response Objects

        /// <summary>
        /// A shared response object for any API method to use
        /// </summary>
        public class StandardResponse
        {
            [JsonConverter( typeof( StringEnumConverter ) )]
            public enum ResultCode
            {
                Error,
                Success
            }

            public ResultCode Result { get; set; }

            public string Message { get; set; }

        }

        /// <summary>
        /// A response object for SMS Login
        /// </summary>
        public class SMSLoginResponse
        {
            [JsonConverter( typeof( StringEnumConverter ) )]
            public enum ResultCode
            {
                Error,
                Success,
                NoMatch,
                MultipleMatch
            }

            public ResultCode Result { get; set; }

            public string Message { get; set; }

            public string Username { get; set; }

        }


        /// <summary>
        /// A profile object for describing a person in Rock
        /// </summary>
        public class Profile
        {
            public int? PersonId { get; set; }
            [Required]
            public string FirstName { get; set; }
            public string NickName { get; set; }
            [Required]
            public string LastName { get; set; }
            [Required]
            public DateTime? Birthdate { get; set; }
            [Required]
            [JsonConverter( typeof( StringEnumConverter ) )]
            public Gender Gender { get; set; }
            [Required]
            [EmailAddress]
            public string EmailAddress { get; set; }
            public List<int> PreviousPersonIDs { get; set; }
            [Required]
            [Phone]
            public string MobileNumber { get; set; }

            public Profile() { }

            public Profile( Person p )
            {

                PersonId = p.Id;
                FirstName = p.FirstName;
                NickName = p.NickName;
                LastName = p.LastName;
                Gender = p.Gender;
                Birthdate = p.BirthDate;
                EmailAddress = p.Email;
                PreviousPersonIDs = p.Aliases.AsQueryable().Where( pa => pa.Id != pa.Person.PrimaryAliasId ).Select( pa => pa.AliasPersonId.Value ).ToList();
            }
        }

        public class Account : Profile
        {
            public string Username { get; set; }

            public string Password { get; set; }
        }

        /// <summary>
        /// A FamilyMember object for describing a person's family member.
        /// </summary>
        public class FamilyMemberProfile
        {
            public int PersonId { get; set; }
            /// <summary>
            /// This flag indicates that this is the current authenticated person
            /// </summary>
            public bool IsCurrentPerson { get; set; }
            public string FamilyRole { get; set; }
            public string FullName { get; set; }
            public Profile Profile { get; set; }
        }

        #endregion

    }
}