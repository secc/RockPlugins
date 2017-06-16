using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Infrastructure;
using Microsoft.Owin.Security.OAuth;
using Owin;
using Rock.Web.Cache;
using Rock;
using org.secc.OAuth.Model;
using org.secc.OAuth.Data;
using Rock.Model;
using Rock.Data;
using Rock.Security;

namespace org.secc.OAuth
{
    public partial class Startup : Rock.Utility.IRockOwinStartup
    {
        /// <summary>
        /// Specify the startup order (This doesn't matter so return zero)
        /// </summary>
        public int StartupOrder {
            get {
                return 0;
            }
        }

        /// <summary>
        /// This actually runs the startup for the OAuth implementation in OWIN
        /// </summary>
        /// <param name="app">The OWIN Builder object</param>
        public void OnStartup( IAppBuilder app )
        {

            var settings = GlobalAttributesCache.Value("OAuthSettings").AsDictionary();

            int tokenLifespan = settings["OAuthTokenLifespan"].AsIntegerOrNull() ?? 10;
            
            //Enable Application Sign In Cookie
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = "OAuth",
                AuthenticationMode = AuthenticationMode.Passive,
                LoginPath = new PathString("/" + settings["OAuthLoginPath"].Trim('/')),
                LogoutPath = new PathString("/" + settings["OAuthLogoutPath"].Trim('/')),
                SlidingExpiration = false,
                ExpireTimeSpan = new TimeSpan(0, tokenLifespan, 0)
            });


            //Setup Authorization Server
            app.UseOAuthAuthorizationServer( new OAuthAuthorizationServerOptions
                {
                    AuthorizeEndpointPath = new PathString("/" + settings["OAuthAuthorizePath"].Trim('/')),
                    AccessTokenExpireTimeSpan = new TimeSpan(0, tokenLifespan, 0),
                    TokenEndpointPath = new PathString("/" + settings["OAuthTokenPath"].Trim('/')),
                    ApplicationCanDisplayErrors = false,
                    AllowInsecureHttp = AllowInsecureHttp(),
                     
                    //Authorization server provider which controls the lifecycle fo the Authorization Server
                    Provider = new OAuthAuthorizationServerProvider
                    {
                        OnValidateClientRedirectUri = ValidateClientRedirectUri,
                        OnValidateClientAuthentication = ValidateClientAuthentication,
                        OnGrantResourceOwnerCredentials = GrantResourceOwnerCredentials,
                        OnGrantClientCredentials = GrantClientCredentials,
                    },

                    //Authorization code provider who creates and receives authorization code
                    AuthorizationCodeProvider = new AuthenticationTokenProvider
                    {
                        OnCreate = CreateAuthenticationCode,
                        OnReceive = ReceiveAuthenticationCode,
                    },

                    //Refresh token provider which creates and receives refresh token
                    RefreshTokenProvider = new AuthenticationTokenProvider
                    {
                        OnCreate = CreateRefreshToken,
                        OnReceive = ReceiveRefreshToken
                    }

                } );

            app.UseOAuthBearerAuthentication(new OAuthBearerAuthenticationOptions());
        }

        #region Refresh Token Provider
        private void CreateRefreshToken( AuthenticationTokenCreateContext context )
        {
            var settings = GlobalAttributesCache.Value("OAuthSettings").AsDictionary();
            if (settings.ContainsKey("OAuthRefreshTokenLifespan") && settings["OAuthTokenLifespan"].AsInteger() > 0)
            {
                context.Ticket.Properties.ExpiresUtc = new DateTimeOffset(DateTime.Now.AddHours(settings["OAuthTokenLifespan"].AsInteger()));
            }
            context.SetToken( context.SerializeTicket() );
        }

        private void ReceiveRefreshToken( AuthenticationTokenReceiveContext context )
        {
            context.DeserializeTicket( context.Token );
        }
        #endregion

        #region Authentication Token Provider

        private readonly ConcurrentDictionary<string, string> mAuthenticationCodes =
            new ConcurrentDictionary<string, string>( StringComparer.Ordinal );

        private void CreateAuthenticationCode( AuthenticationTokenCreateContext context )
        {
            context.SetToken( Guid.NewGuid().ToString( "n" ) + Guid.NewGuid().ToString( "n" ) );
            mAuthenticationCodes[context.Token] = context.SerializeTicket();

        }

        private void ReceiveAuthenticationCode( AuthenticationTokenReceiveContext context )
        {
            string value;
            if ( mAuthenticationCodes.TryRemove( context.Token, out value ) )
            {
                context.DeserializeTicket( value );
            }
        }
        #endregion


        #region Auth Server Provider
        private System.Threading.Tasks.Task GrantClientCredentials( OAuthGrantClientCredentialsContext context )
        {
            var identity = new ClaimsIdentity( new GenericIdentity( context.ClientId, OAuthDefaults.AuthenticationType ), context.Scope.Select( x => new Claim( "urn:oauth:scope", x ) ) );
            context.Validated( identity );

            return System.Threading.Tasks.Task.FromResult( 0 );
        }

        private System.Threading.Tasks.Task GrantResourceOwnerCredentials( OAuthGrantResourceOwnerCredentialsContext context )
        {
            if (!string.IsNullOrEmpty(context.UserName) && !string.IsNullOrEmpty(context.Password))
            {
                var userLoginService = new UserLoginService(new RockContext());
                var userLogin = userLoginService.GetByUserName(context.UserName);
                if (userLogin != null && userLogin.EntityType != null)
                {
                    var component = AuthenticationContainer.GetComponent(userLogin.EntityType.Name);
                    if (component != null && component.IsActive && !component.RequiresRemoteAuthentication)
                    {
                        if (component.Authenticate(userLogin, context.Password))
                        {
                            if ((userLogin.IsConfirmed ?? true) && !(userLogin.IsLockedOut ?? false))
                            {
                                OAuthContext oAuthContext = new OAuthContext();
                                ClientScopeService clientScopeService = new ClientScopeService(oAuthContext);
                                AuthorizationService authorizationService = new AuthorizationService(oAuthContext);
                                ClientService clientService = new ClientService(oAuthContext);

                                var scopes = (context.Scope.FirstOrDefault() ?? "").Split(',');

                                bool scopesApproved = false;
                                Client OAuthClient = clientService.GetByApiKey(context.ClientId.AsGuid());
                                string[] authorizedScopes = authorizationService.Queryable().Where(a => a.Client.Id == OAuthClient.Id && a.UserLogin.UserName == context.UserName && a.Active == true).Select(a => a.Scope.Identifier).ToArray<string>();
                                if (!clientScopeService.Queryable().Where(cs => cs.ClientId == OAuthClient.Id && cs.Active == true).Any() ||
                                    (authorizedScopes != null && scopes.Where(s => !authorizedScopes.Select(a => a.ToLower()).Contains(s.ToLower())).Count() == 0))
                                {
                                    scopesApproved = true;
                                }

                                if (scopesApproved)
                                {
                                    var identity = new ClaimsIdentity(new GenericIdentity(context.UserName, OAuthDefaults.AuthenticationType));

                                    //only allow claims that have been requested and the client has been authorized for
                                    foreach (var scope in scopes.Where(s => clientScopeService.Queryable().Where(cs => cs.ClientId == OAuthClient.Id && cs.Active == true).Select(cs => cs.Scope.Identifier.ToLower()).Contains(s.ToLower())))
                                    {
                                        identity.AddClaim(new Claim("urn:oauth:scope", scope));
                                    }
                                    UserLoginService.UpdateLastLogin(context.UserName);
                                    context.Validated(identity);
                                    return System.Threading.Tasks.Task.FromResult(0);
                                }
                                else
                                {
                                    context.SetError("Authentication Error", "All scopes are not authorized for this user.");
                                }
                            }
                            if (!userLogin.IsConfirmed ?? true)
                            {
                                context.SetError("Authentication Error", "Account email is unconfirmed.");
                            }
                            if (userLogin.IsLockedOut ?? false)
                            {
                                context.SetError("Authentication Error", "Account is locked.");
                            }
                        }
                        else
                        {
                            context.SetError("Authentication Error", "Invalid Username/Password.");
                        }
                    }
                    else
                    {
                        context.SetError("Authentication Error", "Invalid Authentication Configuration.");
                    }
                }
                else
                {
                    context.SetError("Authentication Error", "Invalid Username/Password.");
                }
            }
            else
            {
                context.SetError("Authentication Error", "Invalid Username/Password.");
            }

            context.Rejected();
            return System.Threading.Tasks.Task.FromResult( 0 );
        }

        private System.Threading.Tasks.Task ValidateClientAuthentication( OAuthValidateClientAuthenticationContext context )
        {
            string clientId;
            string clientSecret;

            if (context.TryGetBasicCredentials(out clientId, out clientSecret) ||
                context.TryGetFormCredentials(out clientId, out clientSecret))
            {

                try
                {
                    ClientService clientService = new ClientService(new OAuthContext());
                    Client client = clientService.Queryable().Where(c => c.ApiKey.ToString() == clientId && c.ApiSecret.ToString() == clientSecret).FirstOrDefault();

                    if (client != null && client.Active)
                    {
                        context.Validated();
                    }
                    else
                    {
                        context.Rejected();
                        context.SetError( "Authentication Error", "Client is not active." );
                    }
                }
                catch ( System.Security.Authentication.AuthenticationException authEx )
                {
                    context.Rejected();
                    context.SetError( "Authentication Error", authEx.Message);
                }

                
            }
            return System.Threading.Tasks.Task.FromResult( 0 );
        }

        private System.Threading.Tasks.Task ValidateClientRedirectUri( OAuthValidateClientRedirectUriContext context )
        {
            

            try
            {
                ClientService clientService = new ClientService(new OAuthContext());
                Client client = clientService.Queryable().Where(c => c.ApiKey.ToString() == context.ClientId).FirstOrDefault();

                if (client != null && client.Active )
                {
                    if (client.CallbackUrl.Equals( context.RedirectUri, StringComparison.OrdinalIgnoreCase ) )
                    {
                        context.Validated();
                    }
                    else
                    {
                        
                        context.Rejected();
                        context.SetError( "Authentication Error", "Incorrect CallbackUrl provided." );
                    }
                }
                else
                {

                    context.Rejected();
                    context.SetError( "Authentication Error", "Invalid Client" );
                }
            }
            catch ( System.Security.Authentication.AuthenticationException authEx )
            {
                context.SetError( "Authentication Error", authEx.Message );
            }


            return System.Threading.Tasks.Task.FromResult( 0 );

        }


        #endregion

        private bool AllowInsecureHttp()
        {
            bool allowInsecure = false;

            var settings = GlobalAttributesCache.Value("OAuthSettings").AsDictionary();
            if (settings["OAuthRequireSsl"] != null )
            {
                allowInsecure = !settings["OAuthRequireSsl"].AsBoolean();
            }

            return allowInsecure;
        }
        
    }


}