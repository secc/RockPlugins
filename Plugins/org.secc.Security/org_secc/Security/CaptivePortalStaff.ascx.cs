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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using System.Security.Cryptography;
using System.Text;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;
using System.Net;
using System.Text.RegularExpressions;
using Rock.Security;

namespace RockWeb.Plugins.org_secc.Security
{
    [DisplayName( "Staff Captive Portal" )]
    [Category( "SECC > Security" )]
    [Description( "Controls access to Staff Wi-Fi." )]

    #region Block Settings

    [EncryptedTextField(
           name: "SECC Secure",
           description: "This is the key used for the Sha256Hash",
           defaultValue: "",
           order: 1,
           key: "SECCSecure",
           isPassword: true )]
    [TextField( "MAC Address Paramameter", "The query string parameter used for the MAC Address", true, "client_mac", "", 0, "MacAddressParam" )]
    [TextField(
        name: "Network SSID",
        description: "SSID of network connecting to",
        defaultValue: "Southeast%20SPD",
        order: 2,
        key: "NetworkSSID" )]
    [TextField(
        name: "SeccFPUrl",
        description: "SECC frontPorchThe <span class='tip tip-lava'></span>.",
        defaultValue: "http://secc.frontporch.cloud/captivePortal",
        order: 3 )]
    [CustomDropdownListField( "Redirect When", "When the redirect will occur.", "1^Always,2^When On Provided Network,3^When NOT On Provided Network", true, "1", order: 4 )]
    [TextField( "Network", "The network to compare to in the format of '192.168.0.0/24'. See http://www.ipaddressguide.com/cidr for assistance in calculating CIDR addresses.", false, "", order: 5 )]
    [BooleanField( "TestingEnabled", "Set to true for test network", false, "", 7 )]

    #endregion Block Settings
    public partial class CaptivePortalStaff : RockBlock
    {

        /// <summary>
        /// The user agents to ignore. UA strings that begin with one of these will be ignored.
        /// This is to fix Apple devices loading the page with its CaptiveNetwork WISPr UA and messing
        /// up the device info, which is parsed from the UA. Ignoring "CaptiveNetworkSupport*"
        /// will fix 100% of current known issues, if more than a few come up we should put this
        /// into the DB as DefinedType/DefinedValues.
        /// http://secc.frontporch.cloud/captivePortal?fpid=111&secureNetworkSSID=Southeast%20SPD&secureNetworkHash=23fe51f23a6729ee5f43753a502ca7333320bd4cdeb411b87c82461da25003a8&frontporch=check&fppostback=https://MacBook.frontporch.cloud/captivePortal&client_mac=28cfdaee1df0
        /// </summary>

        /// <summary>
        /// Calculate Hash for front porch connection
        /// </summary>
        /// <param name="seccSecure"></param>
        /// <returns></returns>

        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );
        }

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack )
            {
                string macAttr = GetAttributeValue( "MacAddressParam" );
                string macAddress = RockPage.PageParameter( GetAttributeValue( "MacAddressParam" ) );
                Regex regex = new Regex( "^([0-9a-fA-F]{2}(?:[:-]?[0-9a-fA-F]{2}){5})$" );

                if ( string.IsNullOrWhiteSpace( macAddress ) || !macAddress.IsValidMacAddress() )
                {
                    nbAlert.Text = "Missing or invalid MAC Address";
                    nbAlert.Visible = true;
                    return;
                }
                // Save the supplied MAC address to the page removing any non-Alphanumeric characters
                macAddress = macAddress.RemoveAllNonAlphaNumericCharacters();
                hfMacAddress.Value = macAddress;

                // See if user is logged and link the alias to the device.
                if ( CurrentPerson == null )
                {
                    nbAlert.Text = "You are not logged in";
                    nbAlert.Visible = true;
                    return;
                }
                hfPersonAliasId.Value = CurrentPersonAliasId.ToString();

                RefreshContent();
            }
        }

        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            RefreshContent();
        }

        private void RefreshContent()
        {

            int redirectOption = GetAttributeValue( "RedirectWhen" ).AsInteger();

            // if always redirect 
            if ( redirectOption == 1 )
            {
                RedirectToUrl( CreateRedirectUrl() );
                return;
            }

            // check network to determine redirect
            string network = GetAttributeValue( "Network" );

            if ( network.IsNullOrWhiteSpace() )
            {
                nbAlert.Text = "No network was provided to test against.";
            }

            var userIP = Request.UserHostAddress;

            if ( userIP == "::1" )
            {
                userIP = "127.0.0.1";
            }

            var isOnNetwork = IsInRange( userIP, network );

            if ( ( redirectOption == 2 && isOnNetwork ) || ( redirectOption == 3 && !isOnNetwork ) )
            {
                RedirectToUrl( CreateRedirectUrl() );
                return;
            }

            return;


        }
        protected string CreateRedirectUrl()
        {

            string seccFPurl = GetAttributeValue( "SeccFPUrl" );
            string networkSSID = GetAttributeValue( "NetworkSSID" );
            string hashAttr = Encryption.DecryptString( GetAttributeValue( "SECCSecure" ) );

            var builder = new UriBuilder( seccFPurl );
            builder.Port = -1;
            var query = HttpUtility.ParseQueryString( builder.Query );
            query["fpid"] = hfPersonAliasId.Value;
            query["secureNetworkSSID"] = networkSSID;
            query["secureNetworkHash"] = ComputeSha256Hash( networkSSID + "-" + hfPersonAliasId.Value + "-" + hashAttr );
            if ( GetAttributeValue( "TestingEnabled" ).AsBoolean() )
            {
                query["test"] = "true";
            }
            builder.Query = string.Format( "{0}&{1}", query.ToString(), Request.QueryString);
            string url = builder.ToString();

            return url.ToString();


        }
        private void RedirectToUrl( string url )
        {
            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, CurrentPerson );
            string resolvedUrl = url.ResolveMergeFields( mergeFields );

            // create or get device
            if ( hfMacAddress.Value.IsNotNullOrWhiteSpace() )
            {
                string macAddress = hfMacAddress.Value;
                PersonalDeviceService personalDeviceService = new PersonalDeviceService( new RockContext() );
                PersonalDevice personalDevice = null;

                bool isAnExistingDevice = DoesPersonalDeviceExist( macAddress );
                if (isAnExistingDevice)
                {
                    personalDevice = VerifyDeviceInfo( macAddress );
                }
                else
                {
                    personalDevice = CreateDevice( macAddress );
                    CreateDeviceCookie( macAddress );
                }

                // Time to link this device to the person.
                if (personalDevice.PersonAliasId != CurrentPersonAliasId && CurrentPersonAliasId.HasValue)
                {
                    RockPage.LinkPersonAliasToDevice( CurrentPersonAliasId.Value, macAddress );
                }
            }

            if ( IsUserAuthorized( Rock.Security.Authorization.ADMINISTRATE ) )
            {
                nbAlert.Text = string.Format( "If you did not have Administrate permissions on this block, you would have been redirected to here: <a href='{0}'>{0}</a>.", Page.ResolveUrl( resolvedUrl ) );
            }
            else
            {
                Response.Redirect( resolvedUrl, false );
                Context.ApplicationInstance.CompleteRequest();
                return;
            }
        }

        // true if ipAddress falls inside the CIDR range, example
        // bool result = IsInRange("10.50.30.7", "10.0.0.0/8");
        private bool IsInRange( string ipAddress, string cIDRmask )
        {
            string[] parts = cIDRmask.Split( '/' );

            int iP_addr = BitConverter.ToInt32( IPAddress.Parse( parts[0] ).GetAddressBytes(), 0 );
            int cIDR_addr = BitConverter.ToInt32( IPAddress.Parse( ipAddress ).GetAddressBytes(), 0 );
            int cIDR_mask = IPAddress.HostToNetworkOrder( -1 << ( 32 - int.Parse( parts[1] ) ) );

            return ( ( iP_addr & cIDR_mask ) == ( cIDR_addr & cIDR_mask ) );
        }
        /// <summary>
        /// Calculate Hash for front porch connection
        /// </summary>
        /// <param name="SECCSecure"></param>
        /// <returns></returns>
        protected string ComputeSha256Hash( string rawData )
        {

            // Create a SHA256   
            using ( SHA256 sha256Hash = SHA256.Create() )
            {
                // ComputeHash - returns byte array  
                byte[] bytes = sha256Hash.ComputeHash( Encoding.UTF8.GetBytes( rawData ) );

                // Convert byte array to a string   
                StringBuilder builder = new StringBuilder();
                for ( int i = 0; i < bytes.Length; i++ )
                {
                    builder.Append( bytes[i].ToString( "x2" ) );
                }
                return builder.ToString();
            }
        }


        /// <summary>
        /// Doeses a personal device exist for the provided MAC address
        /// </summary>
        /// <param name="macAddress">The mac address.</param>
        /// <returns></returns>
        private bool DoesPersonalDeviceExist( string macAddress )
        {
            PersonalDeviceService personalDeviceService = new PersonalDeviceService( new RockContext() );
            return personalDeviceService.GetByMACAddress( macAddress ) == null ? false : true;
        }



        /// <summary>
        /// Creates the device if new.
        /// </summary>
        /// <returns>Returns true if the device was created, false it already existed</returns>
        private PersonalDevice CreateDevice( string macAddress )
        {
            UAParser.ClientInfo client = UAParser.Parser.GetDefault().Parse( Request.UserAgent );

            RockContext rockContext = new RockContext();
            PersonalDeviceService personalDeviceService = new PersonalDeviceService( rockContext );

            PersonalDevice personalDevice = new PersonalDevice();
            personalDevice.MACAddress = macAddress;

            personalDevice.PersonalDeviceTypeValueId = GetDeviceTypeValueId();
            personalDevice.PlatformValueId = GetDevicePlatformValueId( client );
            personalDevice.DeviceVersion = GetDeviceOsVersion( client );

            personalDeviceService.Add( personalDevice );
            rockContext.SaveChanges();

            return personalDevice;
        }

        /// <summary>
        /// Gets the current device platform info and updates the obj if needed.
        /// </summary>
        /// <param name="personalDevice">The personal device.</param>
        private PersonalDevice VerifyDeviceInfo( string macAddress )
        {
            UAParser.ClientInfo client = UAParser.Parser.GetDefault().Parse( Request.UserAgent );

            RockContext rockContext = new RockContext();
            PersonalDeviceService personalDeviceService = new PersonalDeviceService( rockContext );

            PersonalDevice personalDevice = personalDeviceService.GetByMACAddress( macAddress );
            personalDevice.PersonalDeviceTypeValueId = GetDeviceTypeValueId();
            personalDevice.PlatformValueId = GetDevicePlatformValueId( client );
            personalDevice.DeviceVersion = GetDeviceOsVersion( client );

            rockContext.SaveChanges();

            return personalDevice;
        }

        /// <summary>
        /// Uses the Request information to determine if the device is mobile or not
        /// </summary>
        /// <returns>DevinedValueId for "Mobile" or "Computer", Mobile includes Tablet. Null if there is a data issue and the DefinedType is missing</returns>
        private int? GetDeviceTypeValueId()
        {
            // Get the device type Mobile or Computer
            DefinedTypeCache definedTypeCache = DefinedTypeCache.Read( Rock.SystemGuid.DefinedType.PERSONAL_DEVICE_TYPE.AsGuid() );
            DefinedValueCache definedValueCache = null;

            var clientType = InteractionDeviceType.GetClientType( Request.UserAgent );
            clientType = clientType == "Mobile" || clientType == "Tablet" ? "Mobile" : "Computer";

            if (definedTypeCache != null)
            {
                definedValueCache = definedTypeCache.DefinedValues.FirstOrDefault( v => v.Value == clientType );

                if (definedValueCache == null)
                {
                    definedValueCache = DefinedValueCache.Read( "828ADECE-EFE7-49DF-BA8C-B3F132509A95" );
                }

                return definedValueCache.Id;
            }

            return null;
        }

        /// <summary>
        /// Parses ClientInfo to find the OS family
        /// </summary>
        /// <param name="client">The client.</param>
        /// <returns>DefinedValueId for the found OS. Uses "Other" if the OS is not in DefinedValue. Null if there is a data issue and the DefinedType is missing</returns>
        private int? GetDevicePlatformValueId( UAParser.ClientInfo client )
        {
            // get the OS
            string platform = client.OS.Family.Split( ' ' ).First();

            DefinedTypeCache definedTypeCache = DefinedTypeCache.Read( Rock.SystemGuid.DefinedType.PERSONAL_DEVICE_PLATFORM.AsGuid() );
            DefinedValueCache definedValueCache = null;
            if (definedTypeCache != null)
            {
                definedValueCache = definedTypeCache.DefinedValues.FirstOrDefault( v => v.Value == platform );

                if (definedValueCache == null)
                {
                    definedValueCache = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSONAL_DEVICE_PLATFORM_OTHER.AsGuid() );
                }

                return definedValueCache.Id;
            }

            return null;
        }

        /// <summary>
        /// Parses ClientInfo and gets the device os version. If it cannot be determined returns the OS family string without the platform
        /// </summary>
        /// <param name="client">The client.</param>
        /// <returns></returns>
        private string GetDeviceOsVersion( UAParser.ClientInfo client )
        {
            if (client.OS.Major == null)
            {
                string platform = client.OS.Family.Split( ' ' ).First();
                return client.OS.Family.Replace( platform, string.Empty ).Trim();
            }

            return string.Format(
                "{0}.{1}.{2}.{3}",
                client.OS.Major ?? "0",
                client.OS.Minor ?? "0",
                client.OS.Patch ?? "0",
                client.OS.PatchMinor ?? "0" );
        }

        /// <summary>
        /// Creates the device cookie if it does not exist.
        /// </summary>
        private void CreateDeviceCookie( string macAddress )
        {
            if (Request.Cookies["rock_wifi"] == null)
            {
                HttpCookie httpcookie = new HttpCookie( "rock_wifi" );
                httpcookie.Expires = DateTime.MaxValue;
                httpcookie.Values.Add( "ROCK_PERSONALDEVICE_ADDRESS", macAddress );
                Response.Cookies.Add( httpcookie );
            }
        }
    }
}