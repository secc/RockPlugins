using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Configuration;
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

namespace org.secc.OAuth
{
    public partial class Startup : Rock.Plugin.IStartup
    {
        public void ConfigureAuth( IAppBuilder app )
        {

            var settings = GlobalAttributesCache.Value("OAuthSettings").AsDictionary();

            int tokenLifespan = 0;

            if (int.TryParse(settings["OAuthTokenLifespan"], out tokenLifespan))
            {
                tokenLifespan = 10;
            }

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
                    AuthorizationCodeExpireTimeSpan = new TimeSpan(0,tokenLifespan, 0),
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
            var identity = new ClaimsIdentity( new GenericIdentity( context.UserName, OAuthDefaults.AuthenticationType ), context.Scope.Select( x => new Claim( "urn:oauth:scope", x ) ) );
            context.Validated( identity );
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

        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}