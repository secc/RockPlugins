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
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Http;
using System.Web.Routing;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using org.secc.OAuth.Data;
using org.secc.OAuth.Model;
using org.secc.PersonMatch;
using Rock;
using Rock.Communication;
using Rock.Model;
using Rock.Security.ExternalAuthentication;
using Rock.Web.Cache;

namespace org.secc.Rest.Controllers
{
    /// <summary>
    /// Account REST API
    /// </summary>
    public partial class AccountController : ApiController
    {
        const int MINIMUM_AGE = 13;

        #region Account Confirmation Code (ROCK-8762)

        /// <summary>
        /// Guid of the UserLogin entity attribute holding the hash of the outstanding mobile
        /// confirmation code. Created by org.secc.Migrations 034_UserLogin_ConfirmationCodeAttributes.
        /// </summary>
        private const string CONFIRMATION_CODE_HASH_ATTRIBUTE_GUID = "7B3F9A42-5C81-4E6D-9F2A-1D8C4B60E3A7";

        /// <summary>
        /// Guid of the UserLogin entity attribute holding the time the outstanding code was issued.
        /// </summary>
        private const string CONFIRMATION_CODE_ISSUED_ATTRIBUTE_GUID = "C4E81D57-2A93-4F16-B8D5-6E70F9A24C1B";

        /// <summary>
        /// Attribute keys backing the outstanding mobile confirmation code. These must stay in
        /// sync with org.secc.Migrations 034_UserLogin_ConfirmationCodeAttributes.
        /// </summary>
        private const string ATTRIBUTE_KEY_CONFIRMATION_CODE_HASH = "SeccConfirmationCodeHash";
        private const string ATTRIBUTE_KEY_CONFIRMATION_CODE_ISSUED = "SeccConfirmationCodeIssued";

        /// <summary>
        /// Number of digits in the emailed confirmation code. Kept short because users type it
        /// into the mobile app by hand. The code lifetime and the attempt budgets below are sized
        /// together with it; see ROCK-8762 before changing any of them.
        /// </summary>
        private const int CONFIRMATION_CODE_DIGITS = 6;

        /// <summary>
        /// How long an issued confirmation code stays valid. Sized together with
        /// CONFIRMATION_CODE_DIGITS and the attempt budgets (ROCK-8762).
        /// </summary>
        private const int CONFIRMATION_CODE_LIFETIME_MINUTES = 60;

        /// <summary>
        /// Confirmation attempts allowed per window for a single forwarded client IP. Tight,
        /// because a legitimate user needs one or two tries. X-Forwarded-For comes from the
        /// request, so the socket-level budget below applies as well.
        /// </summary>
        private const int CONFIRMATION_MAX_ATTEMPTS_PER_FORWARDED_IP = 10;

        /// <summary>
        /// Confirmation attempts allowed per window for a single socket-level peer (REMOTE_ADDR),
        /// which the caller cannot forge. Sized generously because behind a reverse proxy every
        /// user shares one REMOTE_ADDR: this is a ceiling on one network's traffic, not a
        /// per-user limit. Raise it if legitimate confirmations ever hit it.
        /// </summary>
        private const int CONFIRMATION_MAX_ATTEMPTS_PER_PEER = 120;

        /// <summary>
        /// Attempts per window across the whole OAuth client that trigger an alert. This is a log
        /// threshold, not a block: every user of the mobile app shares one OAuth client, so this
        /// count covers all of them together.
        /// </summary>
        private const int CONFIRMATION_ALERT_THRESHOLD_PER_CLIENT = 100;

        private const int CONFIRMATION_ATTEMPT_WINDOW_MINUTES = 15;

        /// <summary>
        /// Logins with a still-live confirmation code allowed per person (ROCK-8762).
        ///
        /// Counted over the code lifetime rather than over all unconfirmed logins, so it self-clears
        /// and so people who already have abandoned unconfirmed logins from the old flow are not
        /// permanently locked out of signing up.
        /// </summary>
        private const int MAX_LIVE_CONFIRMATION_CODES_PER_PERSON = 3;

        /// <summary>
        /// Expired confirmation code cleanup: at most one run per interval on each server, each
        /// looking at the oldest batch of issued-code rows.
        /// </summary>
        private const int CONFIRMATION_CODE_CLEANUP_INTERVAL_MINUTES = 10;
        private const int CONFIRMATION_CODE_CLEANUP_BATCH_SIZE = 500;
        private static readonly CleanupSlot _confirmationCodeCleanupSlot = new CleanupSlot();

        /// <summary>
        /// Account creations allowed per window for one forwarded client IP. api/account/create
        /// issues confirmation codes, so it has its own budget alongside the confirmation budget.
        /// </summary>
        private const int CREATE_MAX_ATTEMPTS_PER_FORWARDED_IP = 5;

        /// <summary>
        /// Account creations allowed per window for one socket peer. Sized generously for the same
        /// reason as the confirmation peer budget: behind a reverse proxy every user of the app
        /// shares one REMOTE_ADDR, so this is a ceiling on one network's signups, not a per-user
        /// limit.
        /// </summary>
        private const int CREATE_MAX_ATTEMPTS_PER_PEER = 200;

        #endregion

        /// <summary>
        /// The caller check for the OAuth-client-only actions: the caller's identity name is not
        /// blank, and parsed as a Guid it is the ApiKey of an OAuth client that is Active.
        /// </summary>
        internal static bool IsActiveOAuthClient( string clientId, Func<Guid, Client> getClientByApiKey )
        {
            if ( clientId.IsNullOrWhiteSpace() )
            {
                return false;
            }

            var oAuthClient = getClientByApiKey( clientId.AsGuid() );
            return oAuthClient != null && oAuthClient.Active;
        }

        /// <summary>
        /// <see cref="IsActiveOAuthClient"/> against the OAuth clients stored in Rock.
        /// </summary>
        protected virtual bool IsCallerActiveOAuthClient( string clientId )
        {
            return IsActiveOAuthClient( clientId, new ClientService( new OAuthContext() ).GetByApiKey );
        }

        /// <summary>
        /// <see cref="GetPreThrottleInputErrors(Account, Func{string, bool}, string, DateTime)"/> with
        /// Rock's password rule and today's date.
        /// </summary>
        protected virtual List<KeyValuePair<string, string>> GetPreThrottleInputErrors( Account account )
        {
            return GetPreThrottleInputErrors( account, UserLoginService.IsPasswordValid, UserLoginService.FriendlyPasswordRules(), RockDateTime.Today );
        }

        /// <summary>
        /// Counts an account creation attempt against the caller's create budgets. Returns false when
        /// a budget is already spent, and logs that refusal at most once per window.
        /// </summary>
        protected virtual bool TryRegisterAccountCreationAttempt( string clientId )
        {
            var createPeerKey = "create|client:" + clientId + "|peer:" + GetPeerIpAddress();
            var createForwardedKey = "create|client:" + clientId + "|fwd:" + GetForwardedIpAddress();

            if ( !ConfirmationAttempts.TryRegisterAttempt( createPeerKey, CREATE_MAX_ATTEMPTS_PER_PEER )
              || !ConfirmationAttempts.TryRegisterAttempt( createForwardedKey, CREATE_MAX_ATTEMPTS_PER_FORWARDED_IP ) )
            {
                if ( ConfirmationAttempts.ShouldLogRefusal( createPeerKey ) )
                {
                    ExceptionLogService.LogException( new Exception( string.Format(
                        "Account creation budget exceeded. Client: {0} PeerIP: {1} ForwardedIP: {2}",
                        clientId, GetPeerIpAddress(), GetForwardedIpAddress() ) ) );
                }

                return false;
            }

            return true;
        }

        [Route( "api/account/create" )]
        [Authorize]
        public HttpResponseMessage CreateAccount( Account account )
        {
            var clientId = HttpContext.Current?.User?.Identity?.Name;

            if ( IsCallerActiveOAuthClient( clientId ) )
            {
                if ( account == null )
                {
                    ModelState.AddModelError( "Account", "Account information is required." );
                    return Request.CreateErrorResponse( HttpStatusCode.BadRequest, ModelState );
                }

                // The password and age checks need no lookup, so they run before the throttle and a
                // rejected attempt does not use up the caller's budget. The username check stays after it.
                foreach ( var error in GetPreThrottleInputErrors( account ) )
                {
                    ModelState.AddModelError( error.Key, error.Value );
                }

                if ( !ModelState.IsValid )
                {
                    return Request.CreateErrorResponse( HttpStatusCode.BadRequest, ModelState );
                }

                // Budget account creation, which issues confirmation codes. Same two-tier keying as
                // confirmation: tight on the forwarded client IP, generous on the socket peer, which
                // every user behind the reverse proxy shares. Checked before the missing-migration
                // check below so its logging is also budgeted.
                if ( !TryRegisterAccountCreationAttempt( clientId ) )
                {
                    return ControllerContext.Request.CreateResponse( ( HttpStatusCode ) 429, new StandardResponse()
                    {
                        Message = "Too many account creation attempts. Please try again later.",
                        Result = StandardResponse.ResultCode.Error
                    } );
                }

                // Checked before anything is written. SetAttributeValue silently no-ops on an
                // unknown key, so without the migration we would create logins and email codes
                // that were never stored and can never be confirmed. Failing here leaves no
                // half-created account and no burned username behind.
                if ( AttributeCache.Get( CONFIRMATION_CODE_HASH_ATTRIBUTE_GUID.AsGuid() ) == null
                  || AttributeCache.Get( CONFIRMATION_CODE_ISSUED_ATTRIBUTE_GUID.AsGuid() ) == null )
                {
                    if ( ConfirmationAttempts.ShouldLogRefusal( "create|missing-migration" ) )
                    {
                        ExceptionLogService.LogException( new Exception(
                            "Account confirmation code attributes are missing. Run org.secc.Migrations migration 034 (ROCK-8762)." ) );
                    }

                    return ControllerContext.Request.CreateResponse( HttpStatusCode.InternalServerError, new StandardResponse()
                    {
                        Message = "Account creation is temporarily unavailable.",
                        Result = StandardResponse.ResultCode.Error
                    } );
                }

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
                }
                if ( !ModelState.IsValid )
                {
                    return Request.CreateErrorResponse( HttpStatusCode.BadRequest, ModelState );
                }

                // Try to match the person
                var matchPerson = personService.GetByMatch( account.FirstName, account.LastName, account.Birthdate, account.EmailAddress, account.MobileNumber, null, null );

                // Never auto-confirm a login bound to an existing person from an OAuth
                // client-credentials request. Any login created below is unconfirmed and
                // must complete the emailed/OOB confirmation step. A matched existing
                // person with no username is left untouched (no login, no email).
                bool matched = false;
                Person person = new Person();
                if ( matchPerson != null && matchPerson.Count() == 1 )
                {
                    var mobilePhone = matchPerson.First().PhoneNumbers.Where( pn => pn.NumberTypeValueId == DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() ).Id ).FirstOrDefault();
                    // The emails MUST match for security
                    if ( string.Equals( matchPerson.First().Email, account.EmailAddress, StringComparison.OrdinalIgnoreCase ) && ( mobilePhone == null || mobilePhone.Number.Right( 10 ) == account.MobileNumber.Right( 10 ) ) )
                    {
                        // Adopt the existing person as-is. Never write requester-supplied fields
                        // (phone, gender, etc.) onto an existing record — those writes happen only
                        // when populating a genuinely new person below.
                        person = matchPerson.First();
                        matched = true;
                    }
                }

                // If we don't have a match, create a new web prospect
                if ( !matched )
                {
                    DefinedValueCache dvcConnectionStatus = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_CONNECTION_STATUS_PROSPECT.AsGuid() );
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
                        phoneNumber.CountryCode = cleanNumber.Length > 10 ? cleanNumber.Left( cleanNumber.Length - 10 ) : PhoneNumber.DefaultCountryCode();
                        phoneNumber.Number = cleanNumber.Right( 10 );
                        phoneNumber.IsMessagingEnabled = true;
                    }

                    PersonService.SaveNewPerson( person, rockContext );
                }
                UserLogin userLogin = null;

                if ( !string.IsNullOrWhiteSpace( account.Username ) && UserLoginService.IsPasswordValid( account.Password ) )
                {
                    // Bound how many confirmation codes can be live for one person at once (ROCK-8762).
                    //
                    // Only the window matters, not the all-time count: people with abandoned
                    // unconfirmed logins from the old flow must not be locked out of signing up,
                    // and nothing in Rock ever cleans those rows up.
                    //
                    // Scoped to this branch, which is the path that stacks codes onto an *existing*
                    // person. The SMS branch below only runs for a person who was not matched, i.e.
                    // one just created by this request, so a per-person cap could never bind there;
                    // there a new person is created per code, which the create budget above covers.
                    var liveCodeCutoff = Rock.RockDateTime.Now.AddMinutes( -CONFIRMATION_CODE_LIFETIME_MINUTES );

                    if ( person.Id > 0
                      && userLoginService.Queryable().Count( ul => ul.PersonId == person.Id
                            && ul.IsConfirmed != true
                            && ul.CreatedDateTime.HasValue
                            && ul.CreatedDateTime > liveCodeCutoff ) >= MAX_LIVE_CONFIRMATION_CODES_PER_PERSON )
                    {
                        return ControllerContext.Request.CreateResponse( ( HttpStatusCode ) 429, new StandardResponse()
                        {
                            Message = "A confirmation email was already sent for this person recently. Please use the most recent code, or try again later.",
                            Result = StandardResponse.ResultCode.Error
                        } );
                    }

                    // Create the user login. Always unconfirmed: every account must confirm below.
                    userLogin = UserLoginService.Create(
                        rockContext,
                        person,
                        AuthenticationServiceType.Internal,
                        EntityTypeCache.Get( Rock.SystemGuid.EntityType.AUTHENTICATION_DATABASE.AsGuid() ).Id,
                        account.Username,
                        account.Password,
                        false );
                }
                else if ( !string.IsNullOrWhiteSpace( account.EmailAddress ) && !matched )
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
                    // The login has to exist in the database before attribute values can be
                    // attached to it, so flush it now rather than at the end of the method.
                    rockContext.SaveChanges();

                    // For mobile we issue a custom/short confirmation code. It is randomly
                    // generated, expiring and single-use, and only its hash is persisted
                    // (ROCK-8762).
                    var mobileConfirmationCode = IssueConfirmationCode( userLogin, rockContext );
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

                    // RockMessage.Send() logs and returns false on failure (it does NOT throw), so
                    // execution would otherwise fall through to SaveChanges and return a 200 claiming a
                    // confirmation email was sent. Fail closed on both paths with a retryable 503.
                    //
                    // Username path: UserLoginService.Create above already committed the login (it calls
                    // SaveChanges internally), so it has to be deleted explicitly — leaving it behind
                    // orphans the account AND traps the username against the up-front uniqueness check,
                    // blocking any retry. The Person and its "User Login" history entry survive the
                    // delete; a retry matches that person and binds the new login to it.
                    //
                    // SMS path: the SMS_<personId> login and its code rows were saved above and are
                    // kept. SMSAuthentication.SendSMSAuthentication finds that login by name at the
                    // next SMS login attempt. The unused code expires, and the expired-code cleanup
                    // removes its rows.
                    if ( !message.Send() )
                    {
                        if ( !string.IsNullOrWhiteSpace( account.Username ) )
                        {
                            userLoginService.Delete( userLogin );
                            rockContext.SaveChanges();
                        }

                        return ControllerContext.Request.CreateResponse( HttpStatusCode.ServiceUnavailable, new StandardResponse()
                        {
                            Message = "We were unable to send your confirmation email. Please try again.",
                            Result = StandardResponse.ResultCode.Error
                        } );
                    }
                }

                rockContext.SaveChanges();

                return ControllerContext.Request.CreateResponse( HttpStatusCode.OK, new StandardResponse()
                {
                    Message = ( matched && userLogin == null )
                        ? "An account already exists for this email address. Please sign in or reset your password."
                        : string.Format( "Account has been created.{0}", userLogin != null ? " An email has been sent to confirm the email address." : "" ),
                    Result = StandardResponse.ResultCode.Success
                }
                );

            }

            return ControllerContext.Request.CreateResponse( HttpStatusCode.Forbidden );

        }

        /// <summary>
        /// The account checks that need no lookup: password rules and minimum age. They run whenever
        /// the username is not null or empty, including a whitespace-only username, which CreateAccount
        /// treats as no username.
        /// </summary>
        internal static List<KeyValuePair<string, string>> GetPreThrottleInputErrors( Account account, Func<string, bool> isPasswordValid, string passwordRulesMessage, DateTime today )
        {
            var errors = new List<KeyValuePair<string, string>>();

            if ( string.IsNullOrEmpty( account.Username ) )
            {
                return errors;
            }

            // Make sure the password is valid. A username that is not whitespace gets a password
            // login in CreateAccount, so it needs a password. For a whitespace-only username a
            // missing password is checked as an empty one.
            if ( ( account.Password == null && !string.IsNullOrWhiteSpace( account.Username ) )
              || !isPasswordValid( account.Password ?? string.Empty ) )
            {
                errors.Add( new KeyValuePair<string, string>( "Account.Password", passwordRulesMessage ) );
            }

            // Make sure this person meets the minimum age requirement
            var birthday = account.Birthdate ?? today;
            if ( today.AddYears( MINIMUM_AGE * -1 ) < birthday )
            {
                errors.Add( new KeyValuePair<string, string>( "Account.Birthdate", string.Format( "We are sorry, you must be at least {0} years old to create an account.", MINIMUM_AGE ) ) );
            }

            return errors;
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
            var clientId = HttpContext.Current?.User?.Identity?.Name;

            if ( !IsCallerActiveOAuthClient( clientId ) )
            {
                return ControllerContext.Request.CreateResponse( HttpStatusCode.Forbidden );
            }

            // Throttle before doing any lookup work. Budgets are keyed on the caller (the OAuth
            // client plus the forwarded IP or the socket peer), not on an account: a confirmation
            // request carries only the code and never names an account.
            //
            // The attempt is registered up front, so the check and the increment are a single
            // atomic operation. Checking first and incrementing later would let concurrent requests
            // all observe an empty budget and sail through together. Nothing is refunded on
            // success -- see the note at the end of this method.
            var forwardedIp = GetForwardedIpAddress();
            var peerIp = GetPeerIpAddress();
            string clientKey = "client:" + clientId;
            string forwardedKey = clientKey + "|fwd:" + forwardedIp;
            string peerKey = clientKey + "|peer:" + peerIp;

            bool withinBudget =
                ConfirmationAttempts.TryRegisterAttempt( peerKey, CONFIRMATION_MAX_ATTEMPTS_PER_PEER )
                && ConfirmationAttempts.TryRegisterAttempt( forwardedKey, CONFIRMATION_MAX_ATTEMPTS_PER_FORWARDED_IP );

            if ( !withinBudget )
            {
                // Logged at most once per key per window, so further refused requests for the same
                // key in that window add no ExceptionLog rows.
                if ( ConfirmationAttempts.ShouldLogRefusal( peerKey ) )
                {
                    ExceptionLogService.LogException( new Exception( string.Format(
                        "Account confirmation attempt budget exceeded. Client: {0} PeerIP: {1} ForwardedIP: {2}",
                        clientId, peerIp, forwardedIp ) ) );
                }

                return ControllerContext.Request.CreateResponse( ( HttpStatusCode ) 429, new StandardResponse()
                {
                    Message = "Too many confirmation attempts. Please try again later.",
                    Result = StandardResponse.ResultCode.Error
                } );
            }

            // Client-wide volume is logged, never blocked, because every user of the mobile app
            // shares one OAuth client. Counted only for requests that were inside budget, so
            // requests refused with 429 do not add to it.
            if ( ConfirmationAttempts.RegisterAndCheckAlertThreshold( clientKey, CONFIRMATION_ALERT_THRESHOLD_PER_CLIENT ) )
            {
                ExceptionLogService.LogException( new Exception( string.Format(
                    "Unusual volume of account confirmation attempts for OAuth client {0} ({1}+ in {2} minutes). Possible confirmation-code guessing.",
                    clientId, CONFIRMATION_ALERT_THRESHOLD_PER_CLIENT, CONFIRMATION_ATTEMPT_WINDOW_MINUTES ) ) );
            }

            if ( confirmationCode.IsNullOrWhiteSpace() )
            {
                return ConfirmationFailedResponse();
            }

            // Load the User Login that has the confirmation code and mark it as confirmed.
            var rockContext = new Rock.Data.RockContext();
            UserLoginService userLoginService = new UserLoginService( rockContext );

            // Rock's native confirmation code: a long encrypted token delivered as a URL.
            UserLogin user = userLoginService.GetByConfirmationCode( confirmationCode );

            if ( user == null )
            {
                // Short mobile code. Single scoped lookup against the stored hash, which resolves
                // to at most one account (ROCK-8762).
                user = FindLoginByConfirmationCode( confirmationCode, rockContext );

                if ( user != null )
                {
                    var issuedDateTime = GetConfirmationCodeIssuedDateTime( user, rockContext );

                    if ( IsConfirmationCodeExpired( issuedDateTime, Rock.RockDateTime.Now ) )
                    {
                        // Expired. Delete the code so it cannot be used again, and return the same
                        // failure response as an unknown code.
                        ClearConfirmationCode( user, rockContext );

                        return ConfirmationFailedResponse();
                    }
                }
            }

            if ( user == null )
            {
                return ConfirmationFailedResponse();
            }

            user.IsConfirmed = true;
            rockContext.SaveChanges();

            // Single use: a consumed code must not confirm anything again.
            ClearConfirmationCode( user, rockContext );

            // No budget refund on success: every attempt counts, including successful ones, since
            // any caller can create an account and confirm its code. Ten attempts per window is
            // ample for someone mistyping their own code.

            return ControllerContext.Request.CreateResponse( HttpStatusCode.OK, new StandardResponse() { Message = "Account has been confirmed", Result = StandardResponse.ResultCode.Success } );
        }

        #region Account Confirmation Code helpers (ROCK-8762)

        /// <summary>
        /// The single response used for every failed confirmation, so the caller cannot tell an
        /// unknown code from an expired one. Status code and message are unchanged from the
        /// previous implementation to keep the mobile client's error handling working.
        /// </summary>
        private HttpResponseMessage ConfirmationFailedResponse()
        {
            return ControllerContext.Request.CreateResponse( HttpStatusCode.InternalServerError, new StandardResponse() { Message = "Error confirming account.", Result = StandardResponse.ResultCode.Error } );
        }

        /// <summary>
        /// Generates a cryptographically random confirmation code of <see cref="CONFIRMATION_CODE_DIGITS"/> digits.
        /// </summary>
        private static string GenerateConfirmationCode()
        {
            // ulong rather than uint so raising CONFIRMATION_CODE_DIGITS past 9 cannot silently
            // overflow the range.
            ulong range = 1;
            for ( int i = 0; i < CONFIRMATION_CODE_DIGITS; i++ )
            {
                range *= 10;
            }

            // Reject the top partial bucket so every code is equally likely. A plain
            // "random % range" would make the lowest codes slightly more probable.
            ulong limit = ulong.MaxValue - ( ulong.MaxValue % range );
            var buffer = new byte[8];

            using ( var rng = new RNGCryptoServiceProvider() )
            {
                ulong value;
                do
                {
                    rng.GetBytes( buffer );
                    value = BitConverter.ToUInt64( buffer, 0 );
                }
                while ( value >= limit );

                return ( value % range ).ToString( CultureInfo.InvariantCulture ).PadLeft( CONFIRMATION_CODE_DIGITS, '0' );
            }
        }

        /// <summary>
        /// Hashes a confirmation code for storage and lookup.
        /// </summary>
        /// <remarks>
        /// An unsalted digest, because the code has to be found by value, which a per-row salt
        /// would prevent. Only the digest is stored, never the code itself.
        /// </remarks>
        private static string HashConfirmationCode( string code )
        {
            using ( var sha = SHA256.Create() )
            {
                return Convert.ToBase64String( sha.ComputeHash( Encoding.UTF8.GetBytes( code.Trim() ) ) );
            }
        }

        /// <summary>
        /// Issues a fresh confirmation code for the login, persisting only its hash plus the time
        /// it was issued. The login must already be saved, since attribute values need its Id.
        /// </summary>
        /// <returns>The plaintext code, to be emailed to the person. It is not stored anywhere.</returns>
        private static string IssueConfirmationCode( UserLogin userLogin, Rock.Data.RockContext rockContext )
        {
            // Fail loudly if migration 034 has not run. SetAttributeValue silently does nothing
            // for an unknown key, so without this guard we would email a code that was never
            // stored and can never be confirmed, with no error anywhere.
            if ( AttributeCache.Get( CONFIRMATION_CODE_HASH_ATTRIBUTE_GUID.AsGuid() ) == null
              || AttributeCache.Get( CONFIRMATION_CODE_ISSUED_ATTRIBUTE_GUID.AsGuid() ) == null )
            {
                throw new Exception( "Account confirmation code attributes are missing. Run org.secc.Migrations migration 034 (ROCK-8762) before creating accounts." );
            }

            DeleteExpiredConfirmationCodes();

            string code = null;
            string hash = null;
            bool unique = false;

            // Each new code must differ from every outstanding code, so a code matches at most one
            // login. Collisions are unlikely; if no free code is found in 10 tries, fail rather than
            // issue an ambiguous one.
            for ( int attempt = 0; attempt < 10 && !unique; attempt++ )
            {
                code = GenerateConfirmationCode();
                hash = HashConfirmationCode( code );
                unique = !FindLoginIdByConfirmationCodeHash( hash, rockContext ).HasValue;
            }

            if ( !unique )
            {
                throw new Exception( "Unable to allocate a unique account confirmation code. The outstanding-code population may be abnormally large." );
            }

            userLogin.LoadAttributes( rockContext );
            userLogin.SetAttributeValue( ATTRIBUTE_KEY_CONFIRMATION_CODE_HASH, hash );

            // Stored as ticks: a round-trip formatted string carries the offset in effect when it
            // was written, so a DST change or a differently-configured server would shift the
            // expiry window by an hour.
            userLogin.SetAttributeValue( ATTRIBUTE_KEY_CONFIRMATION_CODE_ISSUED, Rock.RockDateTime.Now.Ticks.ToString( CultureInfo.InvariantCulture ) );
            userLogin.SaveAttributeValues( rockContext );

            return code;
        }

        /// <summary>
        /// Resolves a plaintext confirmation code to at most one UserLogin by matching the stored
        /// hash. Note what this deliberately does not do: enumerate unconfirmed logins.
        /// </summary>
        private static UserLogin FindLoginByConfirmationCode( string code, Rock.Data.RockContext rockContext )
        {
            var userLoginId = FindLoginIdByConfirmationCodeHash( HashConfirmationCode( code ), rockContext );

            if ( !userLoginId.HasValue )
            {
                return null;
            }

            return new UserLoginService( rockContext ).Get( userLoginId.Value );
        }

        /// <summary>
        /// Single equality lookup on the confirmation-code-hash attribute value.
        /// </summary>
        private static int? FindLoginIdByConfirmationCodeHash( string hash, Rock.Data.RockContext rockContext )
        {
            var attribute = AttributeCache.Get( CONFIRMATION_CODE_HASH_ATTRIBUTE_GUID.AsGuid() );

            if ( attribute == null || hash.IsNullOrWhiteSpace() )
            {
                // Fail closed: with no attribute there is nothing to match against, so no code
                // can confirm anything.
                return null;
            }

            var attributeValueService = new AttributeValueService( rockContext );

            var candidateIds = attributeValueService.Queryable()
                .Where( av => av.AttributeId == attribute.Id && av.Value == hash && av.EntityId.HasValue )
                .Select( av => av.EntityId.Value );

            // Restricted to logins that are still unconfirmed, so a hash left behind on an
            // already-confirmed login can never be matched again.
            return new UserLoginService( rockContext ).Queryable()
                .Where( ul => ul.IsConfirmed != true && candidateIds.Contains( ul.Id ) )
                .Select( ul => ( int? ) ul.Id )
                .FirstOrDefault();
        }

        /// <summary>
        /// Reads the time the outstanding confirmation code was issued, or null if absent/unparseable.
        /// </summary>
        private static DateTime? GetConfirmationCodeIssuedDateTime( UserLogin userLogin, Rock.Data.RockContext rockContext )
        {
            userLogin.LoadAttributes( rockContext );

            return ParseConfirmationCodeIssued( userLogin.GetAttributeValue( ATTRIBUTE_KEY_CONFIRMATION_CODE_ISSUED ) );
        }

        /// <summary>
        /// Parses a stored issued value (ticks), or returns null if it is blank or unparseable.
        /// </summary>
        internal static DateTime? ParseConfirmationCodeIssued( string issued )
        {
            long ticks;
            if ( !issued.IsNullOrWhiteSpace()
              && long.TryParse( issued, NumberStyles.Integer, CultureInfo.InvariantCulture, out ticks )
              && ticks > 0
              && ticks <= DateTime.MaxValue.Ticks )
            {
                return new DateTime( ticks );
            }

            return null;
        }

        /// <summary>
        /// The expiry rule for confirmation codes, used by ConfirmAccount and the cleanup. A missing
        /// issued time counts as expired; a code is still live exactly at the end of its lifetime.
        /// </summary>
        internal static bool IsConfirmationCodeExpired( DateTime? issued, DateTime now )
        {
            return !issued.HasValue || issued.Value < now.AddMinutes( -CONFIRMATION_CODE_LIFETIME_MINUTES );
        }

        /// <summary>
        /// Returns the login ids whose issued value is expired, without duplicates, in input order.
        /// </summary>
        internal static List<int> SelectExpiredLoginIds( IEnumerable<KeyValuePair<int, string>> issuedRows, DateTime now )
        {
            return issuedRows
                .Where( row => IsConfirmationCodeExpired( ParseConfirmationCodeIssued( row.Value ), now ) )
                .Select( row => row.Key )
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Deletes the rows of expired codes among the oldest issued-code rows, if this server's
        /// cleanup slot is free. A failure is logged and does not stop the new code being issued.
        /// </summary>
        private static void DeleteExpiredConfirmationCodes()
        {
            var now = Rock.RockDateTime.Now;

            if ( !_confirmationCodeCleanupSlot.TryClaim( now ) )
            {
                return;
            }

            try
            {
                var issuedAttributeId = AttributeCache.Get( CONFIRMATION_CODE_ISSUED_ATTRIBUTE_GUID.AsGuid() )?.Id;
                var rockContext = new Rock.Data.RockContext();

                var issuedRows = new AttributeValueService( rockContext ).Queryable()
                    .Where( av => av.AttributeId == issuedAttributeId && av.EntityId.HasValue )
                    .OrderBy( av => av.Id )
                    .Take( CONFIRMATION_CODE_CLEANUP_BATCH_SIZE )
                    .Select( av => new { EntityId = av.EntityId.Value, av.Value } )
                    .ToList();

                var expiredLoginIds = SelectExpiredLoginIds( issuedRows.Select( av => new KeyValuePair<int, string>( av.EntityId, av.Value ) ), now );
                DeleteConfirmationCodeRows( rockContext, expiredLoginIds );
            }
            catch ( Exception ex )
            {
                ExceptionLogService.LogException( ex );
            }
        }

        /// <summary>
        /// Clears any outstanding confirmation code so it cannot be reused.
        /// </summary>
        /// <remarks>
        /// The rows are deleted rather than blanked. Leaving empty-valued rows behind would grow
        /// the set that <see cref="FindLoginIdByConfirmationCodeHash"/> has to search forever
        /// (AttributeValue.Value is nvarchar(max) and cannot be indexed) and would keep dead
        /// hashes around to collide with freshly issued codes.
        /// </remarks>
        private static void ClearConfirmationCode( UserLogin userLogin, Rock.Data.RockContext rockContext )
        {
            DeleteConfirmationCodeRows( rockContext, new[] { userLogin.Id } );
        }

        /// <summary>
        /// Deletes the code hash and issued rows of the given logins in one statement. Rows that
        /// are already gone (e.g. removed by a concurrent cleanup) are not an error.
        /// </summary>
        private static void DeleteConfirmationCodeRows( Rock.Data.RockContext rockContext, ICollection<int> userLoginIds )
        {
            var attributeIds = new List<int>();

            foreach ( var attributeGuid in new[] { CONFIRMATION_CODE_HASH_ATTRIBUTE_GUID, CONFIRMATION_CODE_ISSUED_ATTRIBUTE_GUID } )
            {
                var attribute = AttributeCache.Get( attributeGuid.AsGuid() );

                if ( attribute != null )
                {
                    attributeIds.Add( attribute.Id );
                }
            }

            if ( !attributeIds.Any() || !userLoginIds.Any() )
            {
                return;
            }

            rockContext.BulkDelete( new AttributeValueService( rockContext ).Queryable()
                .Where( ConfirmationCodeRowsOf( attributeIds, EntityTypeCache.Get<UserLogin>().Id, userLoginIds ) ) );
        }

        /// <summary>
        /// The rows <see cref="DeleteConfirmationCodeRows"/> deletes: values of the given attributes
        /// on the given logins, limited to attributes of the UserLogin entity type.
        /// </summary>
        internal static Expression<Func<AttributeValue, bool>> ConfirmationCodeRowsOf( ICollection<int> attributeIds, int userLoginEntityTypeId, ICollection<int> userLoginIds )
        {
            return av => attributeIds.Contains( av.AttributeId )
                && av.Attribute.EntityTypeId == userLoginEntityTypeId
                && av.EntityId.HasValue
                && userLoginIds.Contains( av.EntityId.Value );
        }

        /// <summary>
        /// The client IP from X-Forwarded-For, falling back to the socket peer.
        /// </summary>
        /// <remarks>
        /// Reads the RIGHTMOST entry: proxies append, so it is the value our own edge recorded; the
        /// leftmost entries are client-supplied. Assumes a single trusted proxy hop; with a second
        /// reverse proxy in front, select the Nth-from-right entry instead.
        ///
        /// This is still only used to subdivide a budget, never to grant access, and the socket
        /// peer is budgeted separately as the value the caller genuinely cannot control.
        /// </remarks>
        private static string GetForwardedIpAddress()
        {
            var request = HttpContext.Current?.Request;

            if ( request == null )
            {
                return "unknown";
            }

            var forwardedFor = request.ServerVariables["HTTP_X_FORWARDED_FOR"];

            if ( !forwardedFor.IsNullOrWhiteSpace() )
            {
                var hops = forwardedFor.Split( ',' );
                var candidate = hops[hops.Length - 1].Trim();

                // Strip a trailing :port. Only for IPv4-looking values, so bracketed IPv6
                // addresses keep their colons.
                if ( candidate.Count( c => c == ':' ) == 1 )
                {
                    candidate = candidate.Split( ':' )[0];
                }

                if ( !candidate.IsNullOrWhiteSpace() )
                {
                    return candidate;
                }
            }

            return GetPeerIpAddress();
        }

        /// <summary>
        /// The socket-level peer address, which the caller cannot forge. Behind a reverse proxy
        /// this is the proxy, so it is shared by every user on that path.
        /// </summary>
        private static string GetPeerIpAddress()
        {
            var request = HttpContext.Current?.Request;

            if ( request == null )
            {
                return "unknown";
            }

            var remoteAddress = request.ServerVariables["REMOTE_ADDR"];

            return remoteAddress.IsNullOrWhiteSpace() ? "unknown" : remoteAddress;
        }

        /// <summary>
        /// In-process rolling-window record of confirmation attempts, following the pattern already
        /// used by SMSAuthentication's SMSRecords.
        /// </summary>
        /// <remarks>
        /// Attempts are registered before the code is checked, so the check and the increment are
        /// one atomic operation under the lock. Attempts are not refunded.
        ///
        /// State is per server and is reset when the app pool recycles, so each server behind a
        /// load balancer applies the budgets separately. The code hashes are stored in the
        /// database, not here. To share the budgets across servers, back this with RockCache.
        /// </remarks>
        private static class ConfirmationAttempts
        {
            /// <summary>
            /// Ceiling on tracked keys. Each distinct X-Forwarded-For value gets its own key, so the
            /// table has a hard bound. When it is reached the oldest windows are evicted; eviction
            /// only discards counts, so it never refuses a request. The peer key is registered
            /// first, so once a peer's budget is spent no further forwarded keys are added for it
            /// in that window.
            /// </summary>
            private const int MAX_TRACKED_KEYS = 20000;

            private class Window
            {
                public DateTime StartedAt;
                public int Count;
            }

            private static readonly object _lock = new object();
            private static readonly Dictionary<string, Window> _windows = new Dictionary<string, Window>();
            private static readonly Dictionary<string, DateTime> _lastLogged = new Dictionary<string, DateTime>();
            private static DateTime _lastPrunedAt = DateTime.MinValue;

            /// <summary>
            /// Registers an attempt against a key if the budget allows, atomically. Counting and
            /// checking in one locked operation is what stops concurrent requests from all
            /// observing an empty budget and passing together.
            /// </summary>
            /// <returns>False if the budget for this key is already spent.</returns>
            public static bool TryRegisterAttempt( string key, int maxAttempts )
            {
                lock ( _lock )
                {
                    var window = GetOrStartWindow( key );

                    if ( window.Count >= maxAttempts )
                    {
                        return false;
                    }

                    window.Count++;
                    return true;
                }
            }

            /// <summary>
            /// Counts an attempt and reports whether the key has reached the alert threshold. Never
            /// refuses anything; true at most once per key per window.
            /// </summary>
            public static bool RegisterAndCheckAlertThreshold( string key, int threshold )
            {
                lock ( _lock )
                {
                    var window = GetOrStartWindow( key );
                    window.Count++;

                    return window.Count >= threshold && ShouldLogInternal( "alert:" + key );
                }
            }

            /// <summary>
            /// Limits how often refusals are written to the Exception Log for a given key: true at
            /// most once per key per window.
            /// </summary>
            public static bool ShouldLogRefusal( string key )
            {
                lock ( _lock )
                {
                    return ShouldLogInternal( "refusal:" + key );
                }
            }

            private static Window GetOrStartWindow( string key )
            {
                Prune();

                var now = Rock.RockDateTime.Now;
                var cutoff = now.AddMinutes( -CONFIRMATION_ATTEMPT_WINDOW_MINUTES );

                Window window;
                if ( _windows.TryGetValue( key, out window ) )
                {
                    if ( window.StartedAt >= cutoff )
                    {
                        return window;
                    }

                    window.StartedAt = now;
                    window.Count = 0;
                    return window;
                }

                if ( _windows.Count >= MAX_TRACKED_KEYS )
                {
                    EvictOldest();
                }

                window = new Window { StartedAt = now, Count = 0 };
                _windows[key] = window;
                return window;
            }

            private static bool ShouldLogInternal( string key )
            {
                var now = Rock.RockDateTime.Now;
                DateTime lastLogged;

                if ( _lastLogged.TryGetValue( key, out lastLogged )
                  && lastLogged > now.AddMinutes( -CONFIRMATION_ATTEMPT_WINDOW_MINUTES ) )
                {
                    return false;
                }

                _lastLogged[key] = now;
                return true;
            }

            /// <summary>
            /// Sweeps expired windows, at most once a minute so the cost is not paid per request.
            /// </summary>
            private static void Prune()
            {
                var now = Rock.RockDateTime.Now;

                if ( _lastPrunedAt > now.AddMinutes( -1 ) )
                {
                    return;
                }

                _lastPrunedAt = now;
                var cutoff = now.AddMinutes( -CONFIRMATION_ATTEMPT_WINDOW_MINUTES );

                foreach ( var staleKey in _windows.Where( kv => kv.Value.StartedAt < cutoff ).Select( kv => kv.Key ).ToList() )
                {
                    _windows.Remove( staleKey );
                }

                foreach ( var staleKey in _lastLogged.Where( kv => kv.Value < cutoff ).Select( kv => kv.Key ).ToList() )
                {
                    _lastLogged.Remove( staleKey );
                }
            }

            private static void EvictOldest()
            {
                foreach ( var staleKey in _windows.OrderBy( kv => kv.Value.StartedAt )
                    .Take( Math.Max( 1, MAX_TRACKED_KEYS / 10 ) )
                    .Select( kv => kv.Key )
                    .ToList() )
                {
                    _windows.Remove( staleKey );
                }
            }
        }

        /// <summary>
        /// Gives the cleanup to one caller per interval on this server. Claimed before the cleanup
        /// runs, so a failed run still waits for the next interval.
        /// </summary>
        internal class CleanupSlot
        {
            private long _lastClaimTicks;

            internal bool TryClaim( DateTime now )
            {
                var lastClaimTicks = Interlocked.Read( ref _lastClaimTicks );

                return now.Ticks - lastClaimTicks >= TimeSpan.FromMinutes( CONFIRMATION_CODE_CLEANUP_INTERVAL_MINUTES ).Ticks
                    && Interlocked.CompareExchange( ref _lastClaimTicks, now.Ticks, lastClaimTicks ) == lastClaimTicks;
            }
        }

        #endregion


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
                foreach ( GroupMember member in currentUser.Person.PrimaryFamily.Members )
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
            var clientId = HttpContext.Current?.User?.Identity?.Name;

            if ( IsCallerActiveOAuthClient( clientId ) )
            {
                var response = new StandardResponse();

                // A blank email gets the no-accounts response without a person lookup.
                if ( string.IsNullOrWhiteSpace( email ) )
                {
                    response.Result = StandardResponse.ResultCode.Error;
                    response.Message = "No accounts associated with this email address are able to be reset via email.";
                    return ControllerContext.Request.CreateResponse( HttpStatusCode.OK, response );
                }

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
                var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( null, null );

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
                else
                {
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
            var clientId = HttpContext.Current?.User?.Identity?.Name;

            if ( IsCallerActiveOAuthClient( clientId ) )
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
                        return ControllerContext.Request.CreateResponse( HttpStatusCode.OK, loginResponse );
                    }

                    // Check the age of the person
                    var minimumAge = smsAuth.GetAttributeValue( "MinimumAge" ).AsInteger();
                    if ( minimumAge != 0 )
                    {
                        if ( person.Age == null )
                        {
                            loginResponse.Result = SMSLoginResponse.ResultCode.Error;
                            loginResponse.Message = string.Format( "We could not determine your age. You must be at least {0} years old to log in.", minimumAge );
                            return ControllerContext.Request.CreateResponse( HttpStatusCode.OK, loginResponse );
                        }
                        if ( person.Age.Value < minimumAge )
                        {
                            loginResponse.Result = SMSLoginResponse.ResultCode.Error;
                            loginResponse.Message = string.Format( "You must be at least {0} years old to log in.", minimumAge );
                            return ControllerContext.Request.CreateResponse( HttpStatusCode.OK, loginResponse );
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
                else
                {
                    loginResponse.Result = SMSLoginResponse.ResultCode.Error;
                    loginResponse.Message = "An unknown error occurred.";
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