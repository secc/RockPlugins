using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

using Newtonsoft.Json;

using Rock;
using Rock.Attribute;
using Rock.Communication;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Security.ExternalAuthentication;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.org_secc.Security
{
    /// <summary>
    /// Prompts user for login credentials.
    /// </summary>
    [DisplayName( "ChurchOnline SSO SMS Login" )]
    [Category( "SECC > Security" )]
    [Description( "Prompts user for login using SMS." )]

    [CodeEditorField( "Prompt Message", "Message to show before logging in.", CodeEditorMode.Html, CodeEditorTheme.Rock, 100, true, @"
Please enter your cell phone number and we will text you a code to log in with. <br /><i>Text and data rates may apply</i>.
", "", 0 )]
    [LinkedPage( "Resolve Number Page", "Page to resolve duplicate or non-existant mobile numbers.", true, "", "", 1 )]
    [CodeEditorField( "Resolve Message", "Message to show if a single mobile number could not be located.", CodeEditorMode.Html, CodeEditorTheme.Rock, 100, true, defaultValue: @"
We are sorry, but we could not determine which account this number belongs to. 
        ", order: 2 )]
    [CodeEditorField( "Prompt Message", "Optional text (HTML) to display above username and password fields.", CodeEditorMode.Html, CodeEditorTheme.Rock, 100, false, @"", "", 9 )]
    [UrlLinkField( "Redirect URL", "URL to redirect user to upon successful login.  Example: http://online.rocksoliddemochurch.com", true, "", "", 10 )]
    [GroupTypeGroupField( "Check In Group", "The group online campus guests should check into. Your checkin in grouptype will need 'Display Options > Show in Group Lists' enabled", "Check In Group", true, order:11 )]
    [CampusField( "Online Campus Location", "The campus people will be checked into", true, "", "", 12 )]
    [SchedulesField( "Online Campus Schedules", "The schedules church online is available for", true, order: 13 )]
    [TextField( "Church Online SSO Key", "Your SSO key from church online", true, isPassword: true, key: "SSOKey", order: 14 )]
    public partial class ChurchOnlineSMSLogin : RockBlock
    {
        #region Base Control Methods
        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack )
            {
                if ( !Page.IsPostBack )
                {
                    lbPrompt.Text = GetAttributeValue( "PromptMessage" );
                }
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Handles the click event of the btnGenerate control
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void btnGenerate_Click( object sender, EventArgs e )
        {
            if ( !Page.IsValid )
            {
                return;
            }

            pnlPhoneNumber.Visible = false;

            var smsAuthentication = new SMSAuthentication();
            var success = smsAuthentication.SendSMSAuthentication( GetPhoneNumber() );

            if ( success )
            {
                pnlCode.Visible = true;
            }
            else
            {
                lbResolve.Text = GetAttributeValue( "ResolveMessage" );
                pnlResolve.Visible = true;
                if ( !string.IsNullOrWhiteSpace( GetAttributeValue( "ResolveNumberPage" ) ) )
                {
                    btnResolve.Visible = true;
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the btnLogin control.
        /// NOTE: This is the btnLogin for Internal Auth
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        protected void btnLogin_Click( object sender, EventArgs e )
        {
            nbError.Visible = false;
            if ( Page.IsValid )
            {
                RockContext rockContext = new RockContext();
                var smsAuthentication = new SMSAuthentication();
                string error;
                var person = smsAuthentication.GetNumberOwner( GetPhoneNumber(), rockContext, out error );
                if ( person == null )
                {
                    nbError.Text = error;
                    nbError.Visible = true;
                    return;
                }

                var userLoginService = new UserLoginService( rockContext );
                var userLogin = userLoginService.GetByUserName( "SMS_" + person.Id.ToString() );
                if ( userLogin != null && userLogin.EntityType != null )
                {
                    if ( smsAuthentication.Authenticate( userLogin, tbCode.Text ) )
                    {
                        CheckUser( userLogin, Request.QueryString["returnurl"], true );
                        return;
                    }
                }
            }
            nbError.Text = "Sorry, the code you entered did not match the code we generated.";
            nbError.Visible = true;
        }

        /// <summary>
        /// Handles the Click event of the btnLogin control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        protected void btnNewAccount_Click( object sender, EventArgs e )
        {
            string returnUrl = Request.QueryString["returnurl"];

            if ( !string.IsNullOrWhiteSpace( GetAttributeValue( "NewAccountPage" ) ) )
            {
                var parms = new Dictionary<string, string>();

                if ( !string.IsNullOrWhiteSpace( returnUrl ) )
                {
                    parms.Add( "returnurl", returnUrl );
                }

                NavigateToLinkedPage( "NewAccountPage", parms );
            }
            else
            {
                string url = "~/NewAccount";

                if ( !string.IsNullOrWhiteSpace( returnUrl ) )
                {
                    url += "?returnurl=" + returnUrl;
                }

                Response.Redirect( url, false );
                Context.ApplicationInstance.CompleteRequest();
            }
        }

        /// <summary>
        /// Handles the Click event of the btnHelp control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        protected void btnHelp_Click( object sender, EventArgs e )
        {
            if ( !string.IsNullOrWhiteSpace( GetAttributeValue( "HelpPage" ) ) )
            {
                NavigateToLinkedPage( "HelpPage" );
            }
            else
            {
                Response.Redirect( "~/ForgotUserName", false );
                Context.ApplicationInstance.CompleteRequest();
            }
        }


        /// <summary>
        /// Handles the Click event for the btnResolve control
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void btnResolve_Click( object sender, EventArgs e )
        {
            NavigateToPage( GetAttributeValue( "ResolveNumberPage" ).AsGuid(), new Dictionary<string, string>() { { "MobilePhoneNumber", tbPhoneNumber.Text } } );
        }

        /// <summary>
        /// Handles the Click event for the btnCancel control
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void btnCancel_Click( object sender, EventArgs e )
        {
            pnlResolve.Visible = false;
            pnlPhoneNumber.Visible = true;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Parses the formatted phonenumber into just numbers
        /// </summary>
        /// <returns></returns>
        private string GetPhoneNumber()
        {
            return Regex.Replace( tbPhoneNumber.Text, @"^(\+)|\D", "$1" );
        }

        /// <summary>
        /// Checks to see if the user can be authenticated
        /// </summary>
        /// <param name="userLogin"></param>
        /// <param name="returnUrl"></param>
        /// <param name="rememberMe"></param>
        private void CheckUser( UserLogin userLogin, string returnUrl, bool rememberMe )
        {
            if ( userLogin != null )
            {
                if ( ( userLogin.IsConfirmed ?? true ) && !( userLogin.IsLockedOut ?? false ) )
                {
                    LoginUser( userLogin.UserName, returnUrl, rememberMe );
                    PostAttendance( userLogin.Person );
                    AuthChurchOnline( userLogin.Person );

                }
                else
                {
                    var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( RockPage, CurrentPerson );

                    if ( userLogin.FailedPasswordAttemptCount > 5 )
                    {
                        pnlCode.Visible = false;
                        pnlPhoneNumber.Visible = true;
                    }
                    else
                    {
                        nbError.Visible = true;
                    }
                }
            }
        }


        /// <summary>
        /// Logs in the authenticated user
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="returnUrl"></param>
        /// <param name="rememberMe"></param>
        private void LoginUser( string userName, string returnUrl, bool rememberMe )
        {
            string redirectUrlSetting = LinkedPageUrl( "RedirectPage" );

            UserLoginService.UpdateLastLogin( userName );

            Authorization.SetAuthCookie( userName, rememberMe, false );

            if ( !string.IsNullOrWhiteSpace( returnUrl ) )
            {
                string redirectUrl = Server.UrlDecode( returnUrl );
                Response.Redirect( redirectUrl );
                Context.ApplicationInstance.CompleteRequest();
            }
            else if ( !string.IsNullOrWhiteSpace( redirectUrlSetting ) )
            {
                Response.Redirect( redirectUrlSetting );
                Context.ApplicationInstance.CompleteRequest();
            }
            else
            {
                RockPage.Layout.Site.RedirectToDefaultPage();
            }
        }

        public void PostAttendance( Person person )
        {
            var checkinGroupValue = GetAttributeValue( "CheckInGroup" );
            var checkinLocationValue = GetAttributeValue( "OnlineCampusLocation" );
            var checkinScheduleValues = GetAttributeValue( "OnlineCampusSchedules" );

            if ( !string.IsNullOrWhiteSpace( checkinGroupValue ) && !string.IsNullOrWhiteSpace( checkinLocationValue ) && !string.IsNullOrWhiteSpace( checkinScheduleValues ) )
            {
                var rockContext = new RockContext();

                var group = new GroupService( rockContext ).Get( checkinGroupValue.Split( '|' )[1].AsGuid() );
                var campus = new CampusService( rockContext ).Get( checkinLocationValue.AsGuid() );

                var scheduleGuids = checkinScheduleValues.Split( ',' ).AsGuidList();
                var schedule = new ScheduleService( rockContext )
                    .GetByGuids( scheduleGuids )
                    .ToList()
                    .Where( s => s.WasCheckInActive(RockDateTime.Now) && s.IsCheckInEnabled )
                    .FirstOrDefault();

                if ( group != null && campus != null && schedule != null )
                {
                    var attendanceService = new AttendanceService( rockContext );
                    attendanceService.AddOrUpdate( person.PrimaryAliasId ?? person.PrimaryAlias.Id, RockDateTime.Now, group.Id, null, schedule.Id, campus.Id );
                    rockContext.SaveChanges();
                }
            }
        }

        private MultiPass BuildMultipass( Person person )
        {
            var multiPass = new MultiPass();
            multiPass.Email = person.Email.ToLowerInvariant();
            multiPass.FirstName = person.FirstName;
            multiPass.LastName = person.LastName;
            multiPass.Nickname = !string.IsNullOrWhiteSpace( person.NickName ) ? person.NickName : person.FirstName;
            multiPass.Expires = RockDateTime.Now.AddMinutes( 5 ).ToUniversalTime().ToString( "yyyy-MM-ddTHH:mm:ssZ" );
            return multiPass;
        }
        private void AuthChurchOnline( Person person )
        {
            var ssoKey = GetAttributeValue( "SSOKey" );
            var multipass = BuildMultipass( person );
            var json = JsonConvert.SerializeObject( multipass );
            if ( !string.IsNullOrWhiteSpace( json ) && !string.IsNullOrWhiteSpace( ssoKey ) )
            {
                // Sha256 hash of the SSO key                
                var sha256 = SHA256.Create();
                byte[] keyByte = Encoding.UTF8.GetBytes( ssoKey );
                byte[] hashKey = sha256.ComputeHash( keyByte );
                string initVector = "OpenSSL for Ruby"; 

                byte[] initVectorBytes = Encoding.UTF8.GetBytes( initVector );
                byte[] toEncrypt = Encoding.UTF8.GetBytes( initVector + json );
                byte[] encryptedData = encryptStringToBytes_AES( toEncrypt, hashKey, initVectorBytes );

                // Convert plain text to bytes
                var cipherTextWithSpaces = Convert.ToBase64String( encryptedData )
                    .ToCharArray()
                    .Where( c => !Char.IsWhiteSpace( c ) ).ToArray();
                string cipherText = new string( cipherTextWithSpaces );

                string sha1Hash;
                using ( var hmacsha1 = new HMACSHA1( keyByte ) )
                {
                    byte[] hashmessage = hmacsha1.ComputeHash( Encoding.UTF8.GetBytes( cipherText) );
                    sha1Hash = Convert.ToBase64String( hashmessage );
                }

                string urlRedirect = GetAttributeValue( "RedirectURL" );
                Response.Redirect( ( urlRedirect + "/sso?sso=" + cipherText.UrlEncode() + "&signature=" + sha1Hash.UrlEncode() ), false );
                Context.ApplicationInstance.CompleteRequest();
                return;
            }
            throw new Exception( "Error building authentication code for Church Online" );
        }

        static byte[] encryptStringToBytes_AES( byte[] textBytes, byte[] key, byte[] iv )
        {
            // Declare the stream used to encrypt to an in memory
            // array of bytes and the RijndaelManaged object
            // used to encrypt the data.
            using ( MemoryStream msEncrypt = new MemoryStream() )
            using ( RijndaelManaged aesAlg = new RijndaelManaged() )
            {
                // Provide the RijndaelManaged object with the specified key and IV.
                aesAlg.Mode = CipherMode.CBC;
                aesAlg.Padding = PaddingMode.PKCS7;
                aesAlg.KeySize = 256;
                aesAlg.BlockSize = 128;
                aesAlg.Key = key;
                aesAlg.IV = iv;
                // Create an encrytor to perform the stream transform.
                ICryptoTransform encryptor = aesAlg.CreateEncryptor();

                // Create the streams used for encryption.
                using ( CryptoStream csEncrypt = new CryptoStream( msEncrypt, encryptor, CryptoStreamMode.Write ) )
                {
                    csEncrypt.Write( textBytes, 0, textBytes.Length );
                    csEncrypt.FlushFinalBlock();
                }

                byte[] encrypted = msEncrypt.ToArray();
                // Return the encrypted bytes from the memory stream.
                return encrypted;
            }
        }

        #endregion

        internal class MultiPass
        {
            [JsonProperty( PropertyName = "email" )]
            public string Email { get; set; }
            [JsonProperty( PropertyName = "expires" )]
            public string Expires { get; set; }
            [JsonProperty( PropertyName = "first_name" )]
            public string FirstName { get; set; }
            [JsonProperty( PropertyName = "last_name" )]
            public string LastName { get; set; }
            [JsonProperty( PropertyName = "nickname" )]
            public string Nickname { get; set; }
        }
        
    }
}