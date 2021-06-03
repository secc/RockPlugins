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
using System.Security.Claims;
using System.Web;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;
using org.secc.OAuth.Data;
using org.secc.OAuth.Model;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace RockWeb.Plugins.org_secc.OAuth
{
    /// <summary>
    /// OAuth login prompts user for login credentials.
    /// </summary>
    [DisplayName( "OAuth Authorize" )]
    [Category( "SECC > Security" )]
    [Description( "Check to make sure the user has authorized this OAuth request (or prompt for permissions)." )]


    [TextField( "OAuth Config Attribute Key",
        Description = "The OAuth Configuration Attribute's Key",
        IsRequired = true,
        Order = 0,
        DefaultValue = "OAuthSettings",
        Key = AttributeKeys.OAuthConfigAttributeKey )]

    [CustomEnhancedListField( "Restricted Security Groups",
        "Security groups that are restricted to using certain authentication methods",
        "SELECT g.[Name] AS [Text],	g.[Guid] AS [Value] FROM [Group] g WHERE GroupTypeId = 1 ORDER BY [Name]",
        IsRequired = false,
        Order = 1,
        Key = AttributeKeys.RestrictedSecurityGroups
        )]

    [ComponentsField( "Rock.Security.AuthenticationContainer",
        Name = "Approved Authentication Providers",
        Description = "Authentication providers that are permitted for those in the rescricted security groups",
        IsRequired = false,
        Order = 2,
        Key = AttributeKeys.ApprovedAuthenticationProviders
        )]

    [LinkedPage( "Rejected Authentication Page",
        Description = "Page to send the user to if they are not permitted to authenticate in the manner attempted.",
        Order = 3,
        Key = AttributeKeys.RejectedAuthenticationPage )]

    public partial class Authorize : Rock.Web.UI.RockBlock
    {
        internal static class AttributeKeys
        {
            internal const string OAuthConfigAttributeKey = "OAuthConfigAttributeKey";
            internal const string RestrictedSecurityGroups = "RestrictedSecurityGroups";
            internal const string ApprovedAuthenticationProviders = "ApprovedAuthenticationProviders";
            internal const string RejectedAuthenticationPage = "RejectedAuthenticationPage";
        }


        Dictionary<string, string> OAuthSettings
        {
            get
            {
                return GlobalAttributesCache.Value( GetAttributeValue( "OAuthConfigAttributeKey" ) ).AsDictionary();
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

            if ( OAuthSettings["OAuthRequireSsl"].AsBoolean() && Request.Url.Scheme.ToLower() != "https" )
            {
                throw new Exception( "OAuth requires SSL." );
            }

            // Log the user out
            if ( !String.IsNullOrEmpty( PageParameter( "OAuthLogout" ) ) )
            {
                var authentication = HttpContext.Current.GetOwinContext().Authentication;
                authentication.SignOut( DefaultAuthenticationTypes.ApplicationCookie );
                authentication.Challenge( DefaultAuthenticationTypes.ApplicationCookie );
                Response.Redirect( OAuthSettings["OAuthLoginPath"] + "?logout=true&ReturnUrl=" + Server.UrlEncode( Request.RawUrl.Replace( "&OAuthLogout=true", "" ) ) );
            }
            if ( IsPostBack )
            {
                if ( !string.IsNullOrEmpty( Request.Form.Get( "__EVENTTARGET" ) ) && Request.Form.Get( "__EVENTTARGET" ) == btnGrant.UniqueID )
                {

                    if ( CurrentUser != null )
                    {
                        OAuthContext context = new OAuthContext();
                        ClientService clientService = new ClientService( context );
                        Client OAuthClient = clientService.GetByApiKey( PageParameter( "client_id" ).AsGuid() );
                        if ( OAuthClient != null && OAuthClient.Active == true )
                        {
                            ClientScopeService clientScopeService = new ClientScopeService( context );
                            AuthorizationService authorizationService = new AuthorizationService( context );

                            foreach ( var clientScope in clientScopeService.Queryable().Where( cs => cs.ClientId == OAuthClient.Id && cs.Active == true ).Select( cs => cs.Scope ) )
                            {
                                var authorization = authorizationService.Queryable().Where( a => a.Client.Id == OAuthClient.Id && a.UserLoginId == CurrentUser.Id && a.ScopeId == clientScope.Id ).FirstOrDefault();
                                if ( authorization == null )
                                {
                                    authorization = new org.secc.OAuth.Model.Authorization();
                                    authorizationService.Add( authorization );
                                }
                                authorization.Active = true;
                                authorization.ClientId = OAuthClient.Id;
                                authorization.UserLoginId = CurrentUser.Id;
                                authorization.ScopeId = clientScope.Id;
                            }
                            context.SaveChanges();
                            Response.Redirect( Request.RawUrl );
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

            if ( IsAuthenticationNotPermitted() )
            {
                Rock.Security.Authorization.SignOut();
                authentication.SignOut( "OAuth" );
                authentication.SignOut( DefaultAuthenticationTypes.ApplicationCookie );
                NavigateToLinkedPage( AttributeKeys.RejectedAuthenticationPage );
                return;
            }

            var ticket = authentication.AuthenticateAsync( DefaultAuthenticationTypes.ApplicationCookie ).Result;
            var identity = ticket != null ? ticket.Identity : null;
            string userName = null;
            string[] authorizedScopes = null;
            var scopes = ( Request.QueryString.Get( "scope" ) ?? "" ).Split( ' ' );
            bool scopesApproved = false;

            if ( CurrentUser != null && identity == null )
            {
                CreateOAuthIdentity( authentication );
                Response.Redirect( Request.RawUrl, true );
            }
            else if ( identity == null )
            {
                authentication.Challenge( DefaultAuthenticationTypes.ApplicationCookie );
                Response.Redirect( OAuthSettings["OAuthLoginPath"] + "?ReturnUrl=" + Server.UrlEncode( Request.RawUrl ), true );
            }
            else
            {
                OAuthContext context = new OAuthContext();
                ClientService clientService = new ClientService( context );
                Client OAuthClient = clientService.GetByApiKey( PageParameter( "client_id" ).AsGuid() );
                if ( OAuthClient != null )
                {
                    ClientScopeService clientScopeService = new ClientScopeService( context );

                    userName = identity.Name;
                    AuthorizationService authorizationService = new AuthorizationService( context );

                    authorizedScopes = authorizationService.Queryable().Where( a => a.Client.Id == OAuthClient.Id && a.UserLogin.UserName == identity.Name && a.Active == true ).Select( a => a.Scope.Identifier ).ToArray<string>();
                    if ( !clientScopeService.Queryable().Where( cs => cs.ClientId == OAuthClient.Id && cs.Active == true ).Any() ||
                        ( authorizedScopes != null && scopes.Where( s => !authorizedScopes.Select( a => a.ToLower() ).Contains( s.ToLower() ) ).Count() == 0 ) )
                    {
                        scopesApproved = true;
                    }

                    if ( scopesApproved )
                    {
                        identity = new ClaimsIdentity( identity.Claims, "Bearer", identity.NameClaimType, identity.RoleClaimType );

                        //only allow claims that have been requested and the client has been authorized for
                        foreach ( var scope in scopes.Where( s => clientScopeService.Queryable().Where( cs => cs.ClientId == OAuthClient.Id && cs.Active == true ).Select( cs => cs.Scope.Identifier.ToLower() ).Contains( s.ToLower() ) ) )
                        {
                            identity.AddClaim( new Claim( "urn:oauth:scope", scope ) );
                        }
                        authentication.SignIn( identity );
                    }
                    else
                    {
                        rptScopes.DataSource = clientScopeService.Queryable().Where( cs => cs.ClientId == OAuthClient.Id && cs.Active == true ).Select( s => s.Scope ).ToList();
                        rptScopes.DataBind();
                    }

                    lClientName.Text = OAuthClient.ClientName;
                    lClientName2.Text = OAuthClient.ClientName;
                    lUsername.Text = CurrentUser.Person.FullName + " (" + userName + ")";
                    hlLogout.NavigateUrl = Request.RawUrl + "&OAuthLogout=true";
                }
                else
                {
                    throw new Exception( "Invalid Client ID for OAuth authentication." );
                }
            }
        }

        private bool IsAuthenticationNotPermitted()
        {
            if ( CurrentUser == null )
            {
                return false;
            }

            RockContext rockContext = new RockContext();
            GroupService groupService = new GroupService( rockContext );
            var restrictedGroupGuids = GetAttributeValues( AttributeKeys.RestrictedSecurityGroups ).Select( g => g.AsGuid() ).ToList();

            var isMember = groupService.Queryable()
                .Where( g => restrictedGroupGuids.Contains( g.Guid ) )
                .SelectMany( g => g.Members )
                .Where( gm => gm.PersonId == CurrentUser.PersonId )
                .Any();

            if ( isMember )
            {
                var permittedAuthMethods = GetAttributeValues( AttributeKeys.ApprovedAuthenticationProviders );
                if ( !permittedAuthMethods.Select( m => m.AsGuid() ).Contains( CurrentUser.EntityType.Guid ) )
                {
                    return true;
                }
            }
            return false;
        }

        #endregion

        private void CreateOAuthIdentity( IAuthenticationManager authentication )
        {
            List<Claim> claims = new List<Claim>
            {
                new Claim(ClaimsIdentity.DefaultNameClaimType, CurrentUser.UserName),
                new Claim("First_Name", CurrentUser.Person.FirstName),
                new Claim("Nick_Name", CurrentUser.Person.NickName),
                new Claim("Last_Name", CurrentUser.Person.LastName),
            };

            authentication.SignIn(
                new AuthenticationProperties { IsPersistent = true },
                new ClaimsIdentity( claims.ToArray(), DefaultAuthenticationTypes.ApplicationCookie ) );
        }

        protected void btnGrant_Click( object sender, EventArgs e )
        {
        }
    }
}
