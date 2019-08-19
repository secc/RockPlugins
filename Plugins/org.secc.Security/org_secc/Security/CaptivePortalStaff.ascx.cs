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
    [TextField(
        name: "FPpostbackUrl",
        description: "The postback URL <span class='tip tip-lava'></span>.",
        defaultValue: "fppostback=https://MacBook.frontporch.cloud/captivePortal",
        order: 4 )]
    [CustomDropdownListField( "Redirect When", "When the redirect will occur.", "1^Always,2^When On Provided Network,3^When NOT On Provided Network", true, "1", order: 5 )]
    [TextField( "Network", "The network to compare to in the format of '192.168.0.0/24'. See http://www.ipaddressguide.com/cidr for assistance in calculating CIDR addresses.", false, "", order: 6 )]
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
            string fpid = "?fpid=" + hfPersonAliasId.Value;
            string networkSSID = GetAttributeValue( "NetworkSSID" );
            string hashPrefix = "Southeast SPD";
            string hashAttr = Encryption.DecryptString( GetAttributeValue( "SECCSecure" ) );
            string hash = ComputeSha256Hash( hashPrefix + "-" + hfPersonAliasId.Value + "-" + hashAttr );
            string fpPostBack = GetAttributeValue( "FPpostbackUrl" );
            string testing = GetAttributeValue( "TestingEnabled" );
            string clientMac = "client_mac=" + hfMacAddress.Value;


            var fpPbBuild = new UriBuilder( fpPostBack );
            fpPbBuild.Port = -1;




            var builder = new UriBuilder( seccFPurl );
            builder.Port = -1;
            var query = HttpUtility.ParseQueryString( builder.Query );
            query["fpid"] = hfPersonAliasId.Value;
            query["secureNetworkSSID"] = networkSSID;
            query["secureNetworkHash"] = hash;
            query["frontporch"] = "check";
            query["fpPostBack"] = fpPbBuild.ToString();
            query["client_mac"] = hfMacAddress.Value;
            if ( GetAttributeValue( "TestingEnabled" ).AsBoolean() )
            {
                query["test"] = "true";
            }
            builder.Query = query.ToString();
            string url = builder.ToString();

            return url.ToString();


        }
        private void RedirectToUrl( string url )
        {
            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, CurrentPerson );
            string resolvedUrl = url.ResolveMergeFields( mergeFields );

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

    }
}