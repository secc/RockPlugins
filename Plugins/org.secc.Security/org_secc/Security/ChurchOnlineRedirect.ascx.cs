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
    [DisplayName( "ChurchOnline SSO Redirect" )]
    [Category( "SECC > Security" )]
    [Description( "Authentication and redirect for Church Online for users who are logged in." )]

    [UrlLinkField( "Redirect URL", "URL to redirect user to upon successful login.  Example: http://online.rocksoliddemochurch.com", true, "", "", 10 )]
    [GroupTypeGroupField( "Check In Group", "The group online campus guests should check into. Your checkin in grouptype will need 'Display Options > Show in Group Lists' enabled", "Check In Group", true, order: 11 )]
    [CampusField( "Online Campus Location", "The campus people will be checked into", true, "", "", 12 )]
    [SchedulesField( "Online Campus Schedules", "The schedules church online is available for", true, order: 13 )]
    [TextField( "Church Online SSO Key", "Your SSO key from church online", true, isPassword: true, key: "SSOKey", order: 14 )]
    public partial class ChurchOnlineRedirect : RockBlock
    {
        #region Base Control Methods
        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );
            if ( CurrentUser != null )
            {
                PostAttendance( CurrentUser.Person );
                AuthChurchOnline( CurrentUser.Person );
            }
        }

        #endregion



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
                    .Where( s => s.WasCheckInActive( RockDateTime.Now ) && s.IsCheckInEnabled )
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
                    byte[] hashmessage = hmacsha1.ComputeHash( Encoding.UTF8.GetBytes( cipherText ) );
                    sha1Hash = Convert.ToBase64String( hashmessage );
                }

                string urlRedirect = GetAttributeValue( "RedirectURL" );
                var url = ( urlRedirect + "/sso?sso=" + cipherText.UrlEncode() + "&signature=" + sha1Hash.UrlEncode() );
                if ( IsUserAuthorized( Rock.Security.Authorization.ADMINISTRATE ) )
                {
                    nbAdminRedirect.Visible = true;
                    nbAdminRedirect.Text = string.Format( "If you were not an administrator you would have been redirected to <a href='{0}'>{0}</a>", url );
                }
                else
                {
                    Response.Redirect( url, false );
                    Context.ApplicationInstance.CompleteRequest();
                }
                return;
            }
            if ( IsUserAuthorized( Rock.Security.Authorization.ADMINISTRATE ) )
            {
                nbAdminRedirect.Visible = true;
                nbAdminRedirect.Text = "Error building authentication code for Church Online";
            }
            else
            {
                throw new Exception( "Error building authentication code for Church Online" );
            }
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