// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
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
using System.Web.UI;
using Rock;
using Rock.Attribute;
using Rock.Communication;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;
using System.Web;
using System.Security.Claims;
using Microsoft.Owin.Security;
using System.Linq;
using org.secc.OAuth.Model;
using org.secc.OAuth.Data;

namespace RockWeb.Plugins.org_secc.OAuth
{
    /// <summary>
    /// OAuth login prompts user for login credentials.
    /// </summary>
    [DisplayName( "OAuth Authorize" )]
    [Category( "SECC > Security" )]
    [Description( "Check to make sure the user has authorized this OAuth request (or prompt for permissions)." )]
    public partial class Authorize : Rock.Web.UI.RockBlock
    {
        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
            
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            var authentication = HttpContext.Current.GetOwinContext().Authentication;
            var ticket = authentication.AuthenticateAsync("OAuth").Result;
            var identity = ticket != null ? ticket.Identity : null;
            string userName = null;
            string[] authorizedScopes = null;
            var scopes = (Request.QueryString.Get("scope") ?? "").Split(' ');
            bool scopesApproved = false;
            if (identity == null)
            {
                authentication.Challenge("OAuth");
                Response.Redirect("/page/4569?ReturnUrl="+ Server.UrlEncode(Request.RawUrl), true);
            }
            else
            {
                OAuthContext context = new OAuthContext();
                ClientService clientService = new ClientService(context);
                Client OAuthClient = clientService.GetByApiKey(PageParameter("client_id").AsGuid());
                if (OAuthClient != null)
                {
                    ClientScopeService clientScopeService = new ClientScopeService(context);

                    userName = identity.Name;
                    AuthorizationService authorizationService = new AuthorizationService(context);
                
                    authorizedScopes = authorizationService.Queryable().Where(a => a.Client.Id == OAuthClient.Id && a.UserLogin.UserName == identity.Name).Select(a => a.Scope.Identifier).ToArray<string>();
                    if (authorizedScopes != null && scopes.Where(s => !authorizedScopes.Select(a => a.ToLower()).Contains(s.ToLower())).Count() == 0)
                    {
                        scopesApproved = true;
                    }

                    if (scopesApproved)
                    {
                        identity = new ClaimsIdentity(identity.Claims, "Bearer", identity.NameClaimType, identity.RoleClaimType);

                        //only allow claims that have been requested and the client has been authorized for
                        foreach (var scope in scopes.Where(s => clientScopeService.Queryable().Where(cs => cs.ClientId == OAuthClient.Id).Select(cs => cs.Scope.Identifier.ToLower()).Contains(s.ToLower())))
                        {
                            identity.AddClaim(new Claim("urn:oauth:scope", scope));
                        }
                        authentication.SignIn(identity);
                    }
                    else
                    {
                        rptScopes.DataSource = clientScopeService.Queryable().Where(cs => cs.ClientId == OAuthClient.Id).Select(s => s.Scope).ToList();
                        rptScopes.DataBind();
                    }
                        


                    lClientName.Text = OAuthClient.ClientName;
                    lClientName2.Text = OAuthClient.ClientName;
                    lUsername.Text = CurrentUser.Person.FullName + " ("+userName+")";
                } else
                {
                    throw new Exception("Invalid Client ID for OAuth authentication.");
                }
            }

            if (Request.HttpMethod == "POST")
            {
                if (!string.IsNullOrEmpty(Request.Form.Get("submit.Grant")))
                {
                    /*if (oauthClient != null)
                    {
                        foreach (var clientScope in oauthClient.Scopes.Where(cs => scopes.Select(s => s.ToLower()).Contains(cs.Identifier.ToLower())))
                        {
                            if (!authorizedScopes.Select(s => s.ToLower()).Contains(clientScope.Identifier.ToLower()))
                            {
                                UpdateUserAuthorization(apiSession, clientScope, oauthClient.ClientID, userName);
                            }

                        }
                        scopesApproved = true;
                    }
                    */
                }
                if (!string.IsNullOrEmpty(Request.Form.Get("submit.Login")))
                {
                    authentication.SignOut("OAuth");
                    authentication.Challenge("OAuth");
                    Response.Redirect("/page/4569?ReturnUrl=" + Server.UrlEncode(Request.RawUrl), true);
                }
            }

            //ViewBag.ScopesNeedingApproval = ScopesNeedingApproval;

        }

        #endregion

        
    }
}
