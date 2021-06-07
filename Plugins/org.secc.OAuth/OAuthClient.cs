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
using System.Net.Http;
using System.Web;
using DotNetOpenAuth.OAuth2;
using Newtonsoft.Json;
using Rock.Data;
using Rock.Model;

namespace org.secc.OAuth
{
    public class OAuthClient
    {
        WebServerClient webServerClient;

        public OAuthClient( string authorizationEndpoint, string tokenEndpoint, string clientId, string clientSecret )
        {
            var authServer = new AuthorizationServerDescription
            {
                AuthorizationEndpoint = new Uri( authorizationEndpoint ),
                TokenEndpoint = new Uri( tokenEndpoint )
            };

            webServerClient = new WebServerClient( authServer, clientId, clientSecret );
        }

        public UserLogin GetUser( HttpRequestBase request, string endPoint )
        {
            try
            {
                var authState = webServerClient.ProcessUserAuthorization( request );

                var accessToken = authState.AccessToken;

                var authorizingHandler = webServerClient.CreateAuthorizingHandler( accessToken );

                var client = new HttpClient( authorizingHandler );
                var body = client.GetStringAsync( new Uri( endPoint ) ).Result;

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
                return null;
            }

        }

        public void SendAuthRequest( HttpContext context, Uri returnTo = null )
        {
            var userAuth = webServerClient.PrepareRequestUserAuthorization( returnTo: returnTo );
            userAuth.Send( context );
        }

    }
}
