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
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using DotNetOpenAuth.OAuth2;
using Newtonsoft.Json;
using org.secc.OAuth.Model;
using org.secc.OAuth.Utilities;
using Rock.Data;
using Rock.Model;

namespace org.secc.OAuth
{
    public class OAuthClient
    {
        WebServerClient _webServerClient;


        public OAuthClient( string authorizationEndpoint, string tokenEndpoint, string clientId, string clientSecret )
        {
            var authServer = new AuthorizationServerDescription
            {
                AuthorizationEndpoint = new Uri( authorizationEndpoint ),
                TokenEndpoint = new Uri( tokenEndpoint )
            };

            _webServerClient = new WebServerClient( authServer, clientId, clientSecret );
        }

        public UserLogin GetUser( string code, Client client, string tokenEndpoint, string userLoginEndpoint )
        {
            try
            {
                var keysCombined = System.Text.Encoding.UTF8.GetBytes( $"{client.ApiKey.ToString()}:{client.ApiSecret.ToString()}" );
                var auth = System.Convert.ToBase64String( keysCombined );

                WebClient webclient = new WebClient();
                webclient.Headers.Add( HttpRequestHeader.Authorization, "Basic " + auth );
                webclient.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                webclient.Headers["Cache-Control"] = "no-store,no-cache";
                var reqparm = new System.Collections.Specialized.NameValueCollection();
                reqparm.Add( "code", code );
                reqparm.Add( "grant_type", "authorization_code" );
                reqparm.Add( "redirect_uri", client.CallbackUrl );
                byte[] responsebytes = webclient.UploadValues( tokenEndpoint, "POST", reqparm );
                string responsebody = Encoding.UTF8.GetString( responsebytes );

                var tokens = JsonConvert.DeserializeObject<TokenResponse>( responsebody );

                var authorizingHandler = _webServerClient.CreateAuthorizingHandler( tokens.AccessToken );

                var httpClient = new HttpClient( authorizingHandler );
                var body = httpClient.GetStringAsync( new Uri( userLoginEndpoint ) ).Result;

                var data = JsonConvert.DeserializeObject<Dictionary<string, string>>( body );

                if ( data.ContainsKey( "Value" ) )
                {
                    var userName = data["Value"];
                    RockContext rockContext = new RockContext();
                    UserLoginService userLoginService = new UserLoginService( rockContext );
                    return userLoginService.GetByUserName( userName );
                }
                return null;
            }
            catch ( Exception ex )
            {
                ExceptionLogService.LogException( new Exception( "Exception Getting OAuth Token", ex ) );
                return null;
            }

        }

        public void SendAuthRequest( HttpContext context, Uri returnTo = null )
        {
            var userAuth = _webServerClient.PrepareRequestUserAuthorization( returnTo: returnTo );
            userAuth.Send( context );
        }

    }
}
