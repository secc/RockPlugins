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

[assembly: OwinStartup("OAuthPlugin", typeof(org.secc.OAuth.Startup))]
namespace org.secc.OAuth
{
    public partial class Startup
    {
        public void ConfigureAuth( IAppBuilder app )
        {
            //Enable Application Sign In Cookie
            app.UseCookieAuthentication( new CookieAuthenticationOptions
                {
                    AuthenticationType = "OAuth",
                    AuthenticationMode = AuthenticationMode.Active,
                    LoginPath = new PathString( AppSettingValue("OAuthLoginPath") ),
                    LogoutPath = new PathString( AppSettingValue("OAuthLogoutPath") ),
                    SlidingExpiration = false,
                    ExpireTimeSpan = new TimeSpan(0, 0, 30)
                } );

            int tokenLifespan = 0;

            if ( int.TryParse( AppSettingValue("OAuthTokenLifespan"), out tokenLifespan ) )
            {
                tokenLifespan = 10;
            }

            //Setup Authorization Server
            app.UseOAuthAuthorizationServer( new OAuthAuthorizationServerOptions
                {
                    AuthorizeEndpointPath = new PathString( AppSettingValue("OAuthAuthorizePath") ),
                    AuthorizationCodeExpireTimeSpan = new TimeSpan(0,tokenLifespan, 0),
                    TokenEndpointPath = new PathString( AppSettingValue("OAuthTokenPath") ),
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

            if ( context.TryGetBasicCredentials( out clientId, out clientSecret ) ||
                context.TryGetFormCredentials( out clientId, out clientSecret ) )
            {

                try
                {
                    // TODO:  Check to see if the person is authenticated to Rock
                    if (false )
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
                // TODO: Implement client stuff here (A new entity).
                

                if ( true) // client.ClientID > 0 && client.Active )
                {
                    if ( true )//client.CallbackURL.Equals( context.RedirectUri, StringComparison.OrdinalIgnoreCase ) )
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

            if (ConfigurationManager.AppSettings["OAuthRequireSsl"] != null )
            {
                allowInsecure = !bool.Parse(ConfigurationManager.AppSettings["OAuthRequireSsl"] );
            }

            return allowInsecure;
        }

        private string AppSettingValue( string key )
        {
            if (ConfigurationManager.AppSettings[key] == null )
            {
                return null;
            }
            else
            {
                return ConfigurationManager.AppSettings[key];
            }
        }

        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
            app.MapSignalR();
        }
    }
}