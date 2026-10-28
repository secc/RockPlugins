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
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web;
using Rock.Attribute;
using Rock.Communication;
using Rock.Data;
using Rock.Model;
using Rock.Web;
using Rock.Web.Cache;

namespace Rock.Security.ExternalAuthentication
{
    /// <summary>
    /// Authenticates a username/password using a phone number and pin
    /// </summary>
    [Description( "SMS Authentication Provider" )]
    [Export( typeof( AuthenticationComponent ) )]
    [ExportMetadata( "ComponentName", "SMS Authentication" )]
    [LinkedPage( "SMS Login Page", "Page that contains the SMS login block.", defaultValue: "C137E7F2-DDB6-404F-AFD3-4D741E0DA43A" )]
    [IntegerField( "BCrypt Cost Factor", "The higher this number, the more secure BCrypt can be. However it also will be slower.", false, 11 )]
    [SystemPhoneNumberField( "From", "The number to originate message from (configured under Admin Tools > Communications > SMS From Values).", true, false, "", "", 3 )]
    [TextField( "Message", "Message that will be sent along with the login code.", true, "Use {{ password }} to log in to {{ 'Global' | Attribute:'OrganizationName' }}.", order: 4 )]
    [IntegerField( "Minimum Age", "Minimum age which someone is allowed to log in.", true, 13 )]
    [IntegerField( "Max Code Requests", "Maximum number of login codes that can be requested for a single person/number within the rolling time window before further requests are refused.", true, 3, order: 6 )]
    [IntegerField( "Code Request Window Minutes", "Length of the rolling window (in minutes) over which 'Max Code Requests' is counted.", true, 15, order: 7 )]
    public class SMSAuthentication : AuthenticationComponent
    {
        #region Override Methods
        /// <summary>
        /// Gets the type of the service.
        /// </summary>
        /// <value>
        /// The type of the service.
        /// </value>
        public override AuthenticationServiceType ServiceType
        {
            get { return AuthenticationServiceType.External; }
        }

        /// <summary>
        /// Determines if user is directed to another site (i.e. Facebook, Gmail, Twitter, etc) to confirm approval of using
        /// that site's credentials for authentication.
        /// </summary>
        /// <value>
        /// The requires remote authentication.
        /// </value>
        public override bool RequiresRemoteAuthentication
        {
            get { return true; }
        }

        /// <summary>
        /// Initializes the <see cref="SMSAuthentication" /> class.
        /// </summary>
        static SMSAuthentication()
        {
        }

        /// <summary>
        /// Authenticates the specified user name.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="password">The password.</param>
        /// <returns></returns>
        public override Boolean Authenticate( UserLogin user, string password )
        {
            try
            {
                // Security code must only be under 30 minutes old
                if ( user.LastPasswordChangedDateTime == null
                    || user.LastPasswordChangedDateTime < Rock.RockDateTime.Now.AddMinutes( -30 ) )
                {
                    return false;
                }

                var loadedHash = user.Password;
                if ( string.IsNullOrWhiteSpace( loadedHash ) )
                {
                    return false;
                }

                // Limit to 5 attempts per code. The attempt is counted before the code is checked, in one
                // statement, and only while the stored code is still the one being checked.
                if ( ExecuteSql(
                    "UPDATE [UserLogin] SET [FailedPasswordAttemptCount] = ISNULL([FailedPasswordAttemptCount],0) + 1 WHERE [Id] = @id AND [Password] = @loadedHash AND ISNULL([FailedPasswordAttemptCount],0) < 5",
                    new SqlParameter( "@id", user.Id ),
                    new SqlParameter( "@loadedHash", loadedHash ) ) == 0 )
                {
                    return false;
                }

                if ( !AuthenticateBcrypt( user, password ) )
                {
                    return false;
                }

                // A successful login gives its attempt back.
                try
                {
                    ExecuteSql(
                        "UPDATE [UserLogin] SET [FailedPasswordAttemptCount] = [FailedPasswordAttemptCount] - 1 WHERE [Id] = @id AND [Password] = @loadedHash AND [FailedPasswordAttemptCount] > 0",
                        new SqlParameter( "@id", user.Id ),
                        new SqlParameter( "@loadedHash", loadedHash ) );
                }
                catch ( Exception ex )
                {
                    // The attempt stays used; the login still succeeds.
                    ExceptionLogService.LogException( ex );
                }
                return true;
            }
            catch
            {
                return false;
            }

        }

        /// <summary>
        /// Runs a SQL statement against the Rock database and returns the number of rows it affected.
        /// </summary>
        /// <param name="sql">The SQL statement.</param>
        /// <param name="parameters">The statement's parameters.</param>
        /// <returns></returns>
        protected virtual int ExecuteSql( string sql, params SqlParameter[] parameters )
        {
            using ( var rockContext = new RockContext() )
            {
                return rockContext.Database.ExecuteSqlCommand( sql, parameters );
            }
        }

        /// <summary>
        /// Encodes the password.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="password"></param>
        /// <returns></returns>
        public override String EncodePassword( UserLogin user, string password )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Authenticates the user based on a request from a third-party provider.  Will set the username and returnUrl values.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="userName">Name of the user.</param>
        /// <param name="returnUrl">The return URL.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public override bool Authenticate( HttpRequest request, out string userName, out string returnUrl )
        {
            userName = string.Empty;
            returnUrl = request.QueryString["State"];
            return false;
        }

        /// <summary>
        /// Generates the login URL.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public override Uri GenerateLoginUrl( HttpRequest request )
        {
            Uri uri = new Uri( request.Url.ToString() );
            var pageReference = new PageReference( GetAttributeValue( "SMSLoginPage" ), null );
            var host = uri.Scheme + "://" + uri.GetComponents( UriComponents.HostAndPort, UriFormat.UriEscaped );
            return new Uri( host + pageReference.BuildUrl() + uri.Query );
        }

        /// <summary>
        /// Tests the Http Request to determine if authentication should be tested by this
        /// authentication provider.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public override bool IsReturningFromAuthentication( HttpRequest request )
        {
            return false;
        }

        /// <summary>
        /// Gets the URL of an image that should be displayed.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public override string ImageUrl()
        {
            return "";
        }

        /// <summary>
        /// Gets a value indicating whether [supports change password].
        /// </summary>
        /// <value>
        /// <c>true</c> if [supports change password]; otherwise, <c>false</c>.
        /// </value>
        public override bool SupportsChangePassword
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Changes the password.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="oldPassword">The old password.</param>
        /// <param name="newPassword">The new password.</param>
        /// <param name="warningMessage">The warning message.</param>
        /// <returns>
        /// A <see cref="System.Boolean" /> value that indicates if the password change was successful. <c>true</c> if successful; otherwise <c>false</c>.
        /// </returns>
        /// <exception cref="System.Exception">Cannot change password on external service type</exception>
        public override bool ChangePassword( UserLogin user, string oldPassword, string newPassword, out string warningMessage )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sets the password.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="password">The password.</param>
        public override void SetPassword( UserLogin user, string password )
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Methods
        /// <summary>
        /// Creates the user login for the SMS message and send the text message with the code
        /// </summary>
        /// <param name="phoneNumber"></param>
        /// <returns></returns>
        public bool SendSMSAuthentication( string phoneNumber )
        {
            RockContext rockContext = CreateRockContext();

            string error;
            var person = GetNumberOwner( phoneNumber, rockContext, out error );
            if ( person == null )
            {
                return false;
            }

            // Rolling-window cap on code requests, BEFORE any regeneration / counter reset.
            // The budget is kept in memory on each web server, so Max Code Requests is counted per server.
            int maxRequests = GetAttributeValue( "MaxCodeRequests" ).AsIntegerOrNull() ?? 3; 
            int windowMins = GetAttributeValue( "CodeRequestWindowMinutes" ).AsIntegerOrNull() ?? 15;
            
            maxRequests = maxRequests > 0 ? maxRequests : 3;
            windowMins = windowMins > 0 ? windowMins : 15;

            string personKey = "SMS_" + person.Id.ToString();   // matches the UserLogin username
            var ipAddress = GetIpAddress();

            // Check both person and raw number so reformatting the number can't sidestep the cap, and
            // reserve the IP and number, before the login's current code is replaced and the new one sent.
            var issuance = SMSRecords.IssueIfAllowed( personKey, phoneNumber, ipAddress, maxRequests, windowMins, () => ReplaceCodeAndSend( rockContext, person, phoneNumber, ipAddress ) );

            if ( issuance == SMSRecords.IssuanceResult.BudgetExceeded )
            {
                ExceptionLogService.LogException( new Exception( string.Format(
                    "SMS OTP request budget exceeded. PersonId: {0} PhoneNumber: {1} IP: {2}",
                    person.Id, phoneNumber, ipAddress ) ) );
                return false;   // do NOT regenerate, do NOT reset FailedPasswordAttemptCount
            }

            if ( issuance == SMSRecords.IssuanceResult.Reserved )
            {
                // A code is still being sent to this IP or number, so the current code stays as it is.
                ExceptionLogService.LogException( new Exception( string.Format( "Rate limiting reached for SMS authentication: IP: {0} PhoneNumber: {1}", ipAddress, phoneNumber ) ) );
                return true;
            }

            return true;
        }

        /// <summary>
        /// Creates the context that <see cref="SendSMSAuthentication"/> uses.
        /// </summary>
        /// <returns></returns>
        protected virtual RockContext CreateRockContext()
        {
            return new RockContext();
        }

        /// <summary>
        /// Replaces the person's login code with a new one and returns the new code.
        /// </summary>
        /// <param name="rockContext"></param>
        /// <param name="person"></param>
        /// <returns></returns>
        protected virtual string ReplaceCode( RockContext rockContext, Person person )
        {
            UserLoginService userLoginService = new UserLoginService( rockContext );
            var userLogin = userLoginService.Queryable()
                .Where( u => u.UserName == ( "SMS_" + person.Id.ToString() ) )
                .FirstOrDefault();

            //Create user login if does not exist
            if ( userLogin == null )
            {
                var entityTypeId = EntityTypeCache.Get( "Rock.Security.ExternalAuthentication.SMSAuthentication" ).Id;

                userLogin = new UserLogin()
                {
                    UserName = "SMS_" + person.Id.ToString(),
                    EntityTypeId = entityTypeId,
                };
                userLoginService.Add( userLogin );
            }

            //Update user login
            userLogin.PersonId = person.Id;
            userLogin.LastPasswordChangedDateTime = Rock.RockDateTime.Now;
            userLogin.FailedPasswordAttemptWindowStartDateTime = Rock.RockDateTime.Now;
            userLogin.FailedPasswordAttemptCount = 0;
            userLogin.IsConfirmed = true;
            var password = GenerateSecureOtp().ToString();
            userLogin.Password = EncodeBcrypt( password );
            rockContext.SaveChanges();
            return password;
        }

        /// <summary>
        /// Replaces the person's login code with a new one and sends it to the phone number.
        /// </summary>
        /// <param name="rockContext"></param>
        /// <param name="person"></param>
        /// <param name="phoneNumber"></param>
        /// <param name="ipAddress"></param>
        protected virtual void ReplaceCodeAndSend( RockContext rockContext, Person person, string phoneNumber, string ipAddress )
        {
            var password = ReplaceCode( rockContext, person );

            var recipients = new List<RockSMSMessageRecipient>();
            recipients.Add( RockSMSMessageRecipient.CreateAnonymous( phoneNumber, null ) );

            var smsMessage = new RockSMSMessage
            {
                CreateCommunicationRecord = false
            };
            smsMessage.SetRecipients( recipients );

            // Get the From value
            Guid? fromGuid = GetAttributeValue( "From" ).AsGuidOrNull();
            if ( fromGuid.HasValue )
            {
                var systemPhoneNumber = SystemPhoneNumberCache.Get( fromGuid.Value );
                if ( systemPhoneNumber != null )
                {
                    smsMessage.FromSystemPhoneNumber = systemPhoneNumber;
                }
            }

            smsMessage.AdditionalMergeFields = new Dictionary<string, object> { { "password", password } };

            smsMessage.Message = GetAttributeValue( "Message" );

            var delay = SMSRecords.GetDelay( ipAddress, phoneNumber );
            Task.Run( () => { SendSMS( smsMessage, ipAddress, phoneNumber, delay ); } );
        }

        /// <summary>
        /// Generates a cryptographically-secure 6-digit one-time code (100000-999999 inclusive)
        /// using the negligible-bias float-scaling pattern used by Rock core (KeyHelper / Security.Password).
        /// Replaces System.Random, which is non-cryptographic and clock-seeded (ROCK-8764).
        /// </summary>
        private static int GenerateSecureOtp()
        {
            const int minInclusive = 100000;
            const int exclusiveUpperBound = 1000000; // 999999 inclusive
            var bytes = new byte[4];
            using ( var rng = RandomNumberGenerator.Create() )
            {
                rng.GetBytes( bytes );
            }
            var random = BitConverter.ToUInt32( bytes, 0 );
            return (int)( minInclusive + ( exclusiveUpperBound - minInclusive ) * ( random / ( uint.MaxValue + 1.0 ) ) );
        }

        /// <summary>
        /// Finds the owner of the phone number dependant on only one being returned
        /// </summary>
        /// <param name="phoneNumber"></param>
        /// <param name="rockContext"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        public virtual Person GetNumberOwner( string phoneNumber, RockContext rockContext, out string error )
        {
            error = string.Empty;
            PhoneNumberService phoneNumberService = new PhoneNumberService( rockContext );
            var numberOwners = phoneNumberService.Queryable()
                .Where( pn => pn.Number == phoneNumber )
                .Select( pn => pn.Person )
                .DistinctBy( p => p.Id )
                .ToList();

            if ( numberOwners.Count == 0 || numberOwners.Count > 1 )
            {
                error = "There was an issue with your request";
                return null;
            }

            var person = numberOwners.FirstOrDefault();
            if ( person.IsDeceased )
            {
                error = "There was an issue with your request";
                return null;
            }

            var minimumAge = GetAttributeValue( "MinimumAge" ).AsInteger();
            if ( minimumAge != 0 )
            {
                if ( person.Age == null )
                {
                    error = string.Format( "We could not determine your age. You must be at least {0} years old to log in.", minimumAge );
                    return null;
                }
                if ( person.Age.Value < minimumAge )
                {
                    error = string.Format( "You must be at least {0} years old to log in.", minimumAge );
                    return null;
                }
            }

            return person;
        }

        /// <summary>
        /// Hashes the password
        /// </summary>
        /// <param name="password"></param>
        /// <returns></returns>
        private string EncodeBcrypt( string password )
        {
            var workFactor = ( GetAttributeValue( "BCryptCostFactor" ).AsIntegerOrNull() ?? 11 );
            var salt = BCrypt.Net.BCrypt.GenerateSalt( workFactor );
            return BCrypt.Net.BCrypt.HashPassword( password, salt );
        }

        /// <summary>
        /// Tests the bcrypt hash
        /// </summary>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        private bool AuthenticateBcrypt( UserLogin user, string password )
        {
            try
            {
                var hash = user.Password;
                var currentCost = hash.Substring( 4, 2 ).AsInteger();
                return BCrypt.Net.BCrypt.Verify( password, hash );
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Method for getting the user's IP address.
        /// Used in rate limiting.
        /// </summary>
        /// <returns></returns>
        protected virtual string GetIpAddress()
        {
            System.Web.HttpContext context = System.Web.HttpContext.Current;
            string ipAddress = context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

            if ( !string.IsNullOrEmpty( ipAddress ) )
            {
                string[] addresses = ipAddress.Split( ',' );
                if ( addresses.Length != 0 )
                {
                    var forwardedIp = addresses[0];
                    var splitAddresss = forwardedIp.Split( ':' ); //Remove the occasional port
                    return splitAddresss[0];
                }
            }
            return context.Request.ServerVariables["REMOTE_ADDR"];
        }

        /// <summary>
        /// Sends the SMS message so the user can be logged in
        /// </summary>
        /// <param name="smsMessage"></param>
        /// <param name="phoneNumber"></param>
        /// <param name="ipAddress"></param>
        /// <param name="delay"></param>
        public async void SendSMS( RockSMSMessage smsMessage, string phoneNumber, string ipAddress, double delay )
        {
            try
            {
                await Task.Delay( ( int ) delay );
                smsMessage.Send();
            }
            catch ( Exception ex )
            {
                ExceptionLogService.LogException( ex );
            }
            finally
            {
                SMSRecords.ReleaseItems( ipAddress, phoneNumber );
            }
        }
        #endregion
    }
    #region Helper Classes

    /// <summary>
    /// Static class for rate limiting of SMS messages by IP address and phone number
    /// </summary>
    public static class SMSRecords
    {
        /// <summary>
        /// The one lock for all of the state below
        /// </summary>
        private static readonly object _lock = new object();
        /// <summary>
        /// List of SMS Records
        /// </summary>
        private static readonly List<SMSRecord> Records = new List<SMSRecord>();
        /// <summary>
        /// List of Active Items
        /// </summary>
        private static readonly List<string> ActiveItems = new List<string>();

        /// <summary>
        /// Holds IP addressses and phone numbers in reserve
        /// Keeps from flooding SMS messages
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="phoneNumber"></param>
        /// <returns></returns>
        public static bool ReserveItems( string ip, string phoneNumber )
        {
            lock ( _lock )
            {
                if ( ActiveItems.Contains( ip ) || ActiveItems.Contains( phoneNumber ) )
                {
                    //If the list already contains these two items
                    return false;
                }
                ActiveItems.Add( ip );
                ActiveItems.Add( phoneNumber );

                return true;
            }
        }

        /// <summary>
        /// Releases IP addresses and phone numbers from reserve
        /// Allows users to have access to send SMS logins again
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="phoneNumber"></param>
        public static void ReleaseItems( string ip, string phoneNumber )
        {
            lock ( _lock )
            {
                if ( ActiveItems.Contains( ip ) )
                {
                    ActiveItems.Remove( ip );
                }
                if ( ActiveItems.Contains( phoneNumber ) )
                {
                    ActiveItems.Remove( phoneNumber );
                }
            }
        }


        /// <summary>
        /// Calculates a delay for sending the next SMS record
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="phoneNumber"></param>
        /// <returns></returns>
        public static double GetDelay( string ip, string phoneNumber )
        {
            lock ( _lock )
            {
                SMSRecords.Records.Add( new SMSRecord( ip ) );
                SMSRecords.Records.Add( new SMSRecord( phoneNumber ) );
                var hourAgo = Rock.RockDateTime.Now.AddHours( -1 );
                var toRemove = new List<SMSRecord>();
                toRemove.AddRange( Records.Where( r => r.DateTime < hourAgo ).ToList() );
                foreach ( var item in toRemove )
                {
                    Records.Remove( item );
                }
                double delay = 2; //2ms
                                  //Slow down exponentially.
                delay = Math.Pow( delay, Records.Where( r => r.Value == ip || r.Value == phoneNumber ).Count() );

                return delay;
            }
        }

        /// <summary>
        /// Timestamped record of codes issued, keyed by person ("SMS_{personId}") and phone number.
        /// Used to enforce a rolling-window cap on how many codes can be requested.
        /// </summary>
        private static List<SMSRecord> IssuanceRecords = new List<SMSRecord>();

        /// <summary>
        /// Returns true if another code may be issued for this key within the rolling window.
        /// Prunes expired records on each call.
        /// </summary>
        /// <param name="key">"SMS_{personId}" or the raw phone number.</param>
        /// <param name="maxRequests">Max issuances allowed within the window.</param>
        /// <param name="windowMinutes">Rolling window length in minutes.</param>
        private static bool IsRequestAllowed( string key, int maxRequests, int windowMinutes )
        {
            lock ( _lock )
            {
                var windowStart = Rock.RockDateTime.Now.AddMinutes( -windowMinutes );
                IssuanceRecords.RemoveAll( r => r.DateTime < windowStart );
                return IssuanceRecords.Count( r => r.Value == key ) < maxRequests;
            }
        }

        /// <summary>
        /// Records that a code is being issued for this key.
        /// </summary>
        private static void RecordIssuance( string key )
        {
            lock ( _lock )
            {
                IssuanceRecords.Add( new SMSRecord( key ) );
            }
        }

        /// <summary>
        /// Runs inside <see cref="TryBeginIssuance"/> after its checks and before the issuance is recorded. Null unless set by a test.
        /// </summary>
        internal static Action BeforeRecordIssuance;

        /// <summary>
        /// The outcome of <see cref="TryBeginIssuance"/>.
        /// </summary>
        public enum IssuanceResult
        {
            /// <summary>The issuance is counted and the IP and number are reserved.</summary>
            Allowed,
            /// <summary>The person or number has used its requests for the window.</summary>
            BudgetExceeded,
            /// <summary>A code is still being sent to the IP or number.</summary>
            Reserved
        }

        /// <summary>
        /// Decides in one locked step whether a code may be issued: the person and number must be within
        /// their request budget, and the IP and number must not be reserved. Only then is the issuance
        /// counted and the IP and number reserved; a refusal changes nothing. The caller releases the
        /// reservation with <see cref="ReleaseItems"/>.
        /// </summary>
        public static IssuanceResult TryBeginIssuance( string personKey, string phoneNumber, string ipAddress, int maxRequests, int windowMinutes )
        {
            lock ( _lock )
            {
                if ( !IsRequestAllowed( personKey, maxRequests, windowMinutes )
                  || !IsRequestAllowed( phoneNumber, maxRequests, windowMinutes ) )
                {
                    return IssuanceResult.BudgetExceeded;
                }

                if ( !ReserveItems( ipAddress, phoneNumber ) )
                {
                    return IssuanceResult.Reserved;
                }

                BeforeRecordIssuance?.Invoke();
                RecordIssuance( personKey );
                RecordIssuance( phoneNumber );
                return IssuanceResult.Allowed;
            }
        }

        /// <summary>
        /// Runs <paramref name="replaceCodeAndSend"/> only when <see cref="TryBeginIssuance"/> allows the request, so a
        /// refused request never touches the login. If it throws, the reservation is released and the exception rethrown.
        /// </summary>
        public static IssuanceResult IssueIfAllowed( string personKey, string phoneNumber, string ipAddress, int maxRequests, int windowMinutes, Action replaceCodeAndSend )
        {
            var result = TryBeginIssuance( personKey, phoneNumber, ipAddress, maxRequests, windowMinutes );
            if ( result == IssuanceResult.Allowed )
            {
                try
                {
                    replaceCodeAndSend();
                }
                catch
                {
                    ReleaseItems( ipAddress, phoneNumber );
                    throw;
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Helper class for rate limiting
    /// </summary>
    public class SMSRecord
    {
        /// <summary>
        /// Constructor method
        /// </summary>
        /// <param name="value"></param>
        public SMSRecord( string value )
        {
            Value = value;
            DateTime = Rock.RockDateTime.Now;
        }

        /// <summary>
        /// The value of the item being reserved
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Time at which the item was reserved
        /// </summary>
        public DateTime DateTime { get; set; }
    }
    #endregion
}
