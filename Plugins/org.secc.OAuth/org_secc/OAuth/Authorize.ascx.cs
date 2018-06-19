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
//
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
    [TextField("OAuth Config Attribute Key", "The OAuth Configuration Attribute's Key", true, "OAuthSettings")]
    public partial class Authorize : Rock.Web.UI.RockBlock
    {
        Dictionary<string, string> OAuthSettings
        {
            get
            {
                return GlobalAttributesCache.Value(GetAttributeValue("OAuthConfigAttributeKey")).AsDictionary(); ;
            }
        }
        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            if (OAuthSettings["OAuthRequireSsl"].AsBoolean() && Request.Url.Scheme.ToLower() != "https")
            {
                throw new Exception("OAuth requires SSL.");
            }

            // Log the user out
            if (!String.IsNullOrEmpty(PageParameter("OAuthLogout")))
            {
                var authentication = HttpContext.Current.GetOwinContext().Authentication;
                authentication.SignOut("OAuth");
                authentication.Challenge("OAuth");
                Response.Redirect(OAuthSettings["OAuthLoginPath"] +"?logout=true&ReturnUrl=" + Server.UrlEncode(Request.RawUrl.Replace("&OAuthLogout=true", "")));
            }
            if (IsPostBack)
            {
                if (!string.IsNullOrEmpty(Request.Form.Get("__EVENTTARGET")) && Request.Form.Get("__EVENTTARGET") == btnGrant.UniqueID)
                {
                    
                    if (CurrentUser != null)
                    {
                        OAuthContext context = new OAuthContext();
                        ClientService clientService = new ClientService(context);
                        Client OAuthClient = clientService.GetByApiKey(PageParameter("client_id").AsGuid());
                        if (OAuthClient != null && OAuthClient.Active == true)
                        {
                            ClientScopeService clientScopeService = new ClientScopeService(context);
                            AuthorizationService authorizationService = new AuthorizationService(context);

                            foreach (var clientScope in clientScopeService.Queryable().Where(cs => cs.ClientId == OAuthClient.Id && cs.Active == true).Select(cs => cs.Scope))
                            {
                                var authorization = authorizationService.Queryable().Where(a => a.Client.Id == OAuthClient.Id && a.UserLoginId == CurrentUser.Id && a.ScopeId == clientScope.Id).FirstOrDefault();
                                if (authorization == null)
                                {
                                    authorization = new org.secc.OAuth.Model.Authorization();
                                    authorizationService.Add(authorization);
                                }
                                authorization.Active = true;
                                authorization.ClientId = OAuthClient.Id;
                                authorization.UserLoginId = CurrentUser.Id;
                                authorization.ScopeId = clientScope.Id;
                            }
                            context.SaveChanges();
                            Response.Redirect(Request.RawUrl);
                        }
                    }
                }
            }
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
                Response.Redirect(OAuthSettings["OAuthLoginPath"] + "?ReturnUrl=" + Server.UrlEncode(Request.RawUrl), true);
            }
            else if (CurrentUser == null)
            {
                // Kill the OAuth session
                authentication.SignOut("OAuth");
                authentication.Challenge("OAuth");
                Response.Redirect(OAuthSettings["OAuthLoginPath"] + "?ReturnUrl=" + Server.UrlEncode(Request.RawUrl), true);
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
                
                    authorizedScopes = authorizationService.Queryable().Where(a => a.Client.Id == OAuthClient.Id && a.UserLogin.UserName == identity.Name && a.Active == true).Select(a => a.Scope.Identifier).ToArray<string>();
                    if (!clientScopeService.Queryable().Where(cs => cs.ClientId == OAuthClient.Id && cs.Active == true).Any() ||
                        (authorizedScopes != null && scopes.Where(s => !authorizedScopes.Select(a => a.ToLower()).Contains(s.ToLower())).Count() == 0))
                    {
                        scopesApproved = true;
                    }

                    if (scopesApproved)
                    {
                        identity = new ClaimsIdentity(identity.Claims, "Bearer", identity.NameClaimType, identity.RoleClaimType);

                        //only allow claims that have been requested and the client has been authorized for
                        foreach (var scope in scopes.Where(s => clientScopeService.Queryable().Where(cs => cs.ClientId == OAuthClient.Id && cs.Active == true).Select(cs => cs.Scope.Identifier.ToLower()).Contains(s.ToLower())))
                        {
                            identity.AddClaim(new Claim("urn:oauth:scope", scope));
                        }
                        authentication.SignIn(identity);
                    }
                    else
                    {
                        rptScopes.DataSource = clientScopeService.Queryable().Where(cs => cs.ClientId == OAuthClient.Id && cs.Active == true).Select(s => s.Scope).ToList();
                        rptScopes.DataBind();
                    }

                    lClientName.Text = OAuthClient.ClientName;
                    lClientName2.Text = OAuthClient.ClientName;
                    lUsername.Text = CurrentUser.Person.FullName + " ("+userName+")";
                    hlLogout.NavigateUrl = Request.RawUrl + "&OAuthLogout=true";
                }
                else
                {
                    throw new Exception("Invalid Client ID for OAuth authentication.");
                }
            }
        }

        #endregion

        protected void btnGrant_Click(object sender, EventArgs e)
        {
        }
    }
}
