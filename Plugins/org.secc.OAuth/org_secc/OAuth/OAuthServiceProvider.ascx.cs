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
using System.ComponentModel;
using System.Web;
using org.secc.OAuth;
using org.secc.OAuth.Data;
using org.secc.OAuth.Model;
using Rock;
using Rock.Attribute;
using Rock.Model;
using Rock.Web.UI;

namespace RockWeb.Plugins.org_secc.OAuth
{
    /// <summary>
    /// Prompts user for login credentials.
    /// </summary>
    [DisplayName( "OAuth Service Provider" )]
    [Category( "SECC > OAuth" )]
    [Description( "Block to authenticate to authenticate to the OAuth portal" )]


    [CustomDropdownListField( "OAuth Client",
        "The client to use in authenticating the authentication response",
        "select [guid] as [Value],ClientName as [Text]from _org_secc_OAuth_Client",
        Key = AttributeKeys.OAuthClient,
        Order = 0
        )]

    [TextField( "Authorization URI",
        Key = AttributeKeys.AuthorizationURI,
        Order = 1 )]

    [TextField( "Token URI",
        Key = AttributeKeys.TokenURI,
        Order = 2 )]

    [TextField( "UserLogin API Endpoint",
        Key = AttributeKeys.UserLoginEndpoint,
        Order = 2 )]

    public partial class OAuthServiceProvider : RockBlock
    {
        internal static class AttributeKeys
        {
            internal const string OAuthClient = "OAuthClient";
            internal const string AuthorizationURI = "AuthorizationURI";
            internal const string TokenURI = "TokenURI";
            internal const string UserLoginEndpoint = "UserLoginEndpoint";
        }
        internal static class PageParameterKeys
        {
            internal const string ReturnUrl = "returnurl";
            internal const string Code = "code";
        }

        #region Base Control Methods

        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            //We have to be sure to have a session
            //https://stackoverflow.com/questions/14225840/session-change-in-between-request-and-process-user-authorization
            Session["MockData"] = true;
        }

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            var returnurl = PageParameter( PageParameterKeys.ReturnUrl );
            returnurl = ExtensionMethods.ScrubEncodedStringForXSSObjects( returnurl );
            returnurl = Server.UrlDecode( returnurl );

            //If already logged in send them back
            if ( CurrentUser != null )
            {
                if ( returnurl.IsNullOrWhiteSpace() )
                {
                    returnurl = "/";
                }
                Response.Redirect( returnurl, true );
            }


            //If we have redirect cookie store it and reload the page for a clear url
            if ( returnurl.IsNotNullOrWhiteSpace() )
            {
                HttpCookie redirectCookie = new HttpCookie( "AuthRedirectCookie" );
                redirectCookie.Value = returnurl;
                this.Page.Response.Cookies.Set( redirectCookie );
                var client = GetClient();
                Response.Redirect( client.CallbackUrl, true );
            }

            var code = PageParameter( PageParameterKeys.Code );

            if ( code.IsNotNullOrWhiteSpace() )
            {
                Client client = GetClient();

                var oauthClient = new OAuthClient(
                    GetAttributeValue( AttributeKeys.AuthorizationURI ),
                    GetAttributeValue( AttributeKeys.TokenURI ),
                    client.ApiKey.ToString(),
                    client.ApiSecret.ToString() );

                var userLogin = oauthClient.GetUser( code, client, GetAttributeValue( AttributeKeys.TokenURI ), GetAttributeValue( AttributeKeys.UserLoginEndpoint ) );

                Login( userLogin );
            }
            else
            {
                var client = GetClient();

                var oauthClient = new OAuthClient(
                   GetAttributeValue( AttributeKeys.AuthorizationURI ),
                   GetAttributeValue( AttributeKeys.TokenURI ),
                   client.ApiKey.ToString(),
                   client.ApiSecret.ToString() );

                oauthClient.SendAuthRequest( Context, new Uri( client.CallbackUrl ) );
                Response.End();
            }

        }

        private Client GetClient()
        {
            OAuthContext rockContext = new OAuthContext();
            ClientService clientService = new ClientService( rockContext );
            Client client = clientService.Get( GetAttributeValue( AttributeKeys.OAuthClient ).AsGuid() );
            return client;
        }

        private void Login( UserLogin userLogin )
        {
            Rock.Security.Authorization.SetAuthCookie( userLogin.UserName, true, false );

            string callback = null;

            var authCookie = Request.Cookies["AuthRedirectCookie"];

            if ( authCookie != null )
            {
                callback = authCookie.Value;
            }

            if ( callback.IsNullOrWhiteSpace() )
            {
                callback = "/";
            }
            Response.Redirect( callback, true );

        }

        #endregion
    }
}