using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Jose;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using org.secc.LeagueApps.Components;
using org.secc.LeagueApps.Utilities;
using RestSharp;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Security.Cryptography;
using Security.Cryptography.X509Certificates;

namespace org.secc.LeagueApps
{
    public class APIClient
    {
        private LeagueAppsSettings settings;
        public LeagueAppsSettings Settings
        {
            get
            {
                if ( settings == null )
                {
                    settings = LeagueAppsSettings.GetComponent<LeagueAppsSettings>();
                }
                return settings;
            }
        }

        private byte[] certificate;
        public byte[] Certificate
        {
            get
            {
                if ( certificate == null )
                {
                    RockContext rockContext = new RockContext();
                    BinaryFileService binaryFileService = new BinaryFileService( rockContext );
                    var p12File = binaryFileService.GetNoTracking( Settings.GetAttributeValue( Constants.LeagueAppsServiceAccountFile ).AsGuid() );
                    certificate = p12File.ContentStream.ReadBytesToEnd();
                }
                return certificate;
            }
        }

        public T GetPublic<T>( string resource )
        {
            //League Apps Settings
            var siteId = Encryption.DecryptString( Settings.GetAttributeValue( Constants.LeagueAppsSiteId ) );
            var clientId = Encryption.DecryptString( Settings.GetAttributeValue( Constants.LeagueAppsClientId ) );

            var client = new RestClient( "https://public.leagueapps.io" );

            //Magic string (sorry)
            resource = resource.Replace( "{siteid}", siteId );

            var request = new RestRequest( resource, Method.GET );
            request.AddHeader( "la-api-key", clientId );
            var response = client.Get( request );

            if ( response.StatusCode != System.Net.HttpStatusCode.OK )
            {
                throw new Exception( "LeagueApps API Response: " + response.StatusDescription + " Content Length: " + response.ContentLength );
            }


            var export = response.Content.ToString();
            return JsonConvert.DeserializeObject<T>( export );
        }

        // HttpClient is designed to be shared; one per base address for the life of the process.
        private static readonly HttpClient authClient = new HttpClient( new LoggingHandler( new HttpClientHandler() ) )
        {
            BaseAddress = new Uri( "https://auth.leagueapps.io" )
        };

        private static readonly HttpClient adminClient = new HttpClient( new LoggingHandler( new HttpClientHandler() ) )
        {
            BaseAddress = new Uri( "https://admin.leagueapps.io" )
        };

        // Bearer token cache. Refreshed when within TokenRefreshMarginSeconds of expiry.
        private string bearerToken;
        private DateTime bearerTokenExpiresUtc = DateTime.MinValue;
        private const int JwtLifetimeSeconds = 300;
        private const int TokenRefreshMarginSeconds = 30;

        /// <summary>
        /// Calls an authenticated LeagueApps admin API resource and deserializes the JSON body.
        /// Returns <c>default(T)</c> (null for reference types) when the body is empty or the JSON literal
        /// <c>null</c>; callers that page through export endpoints rely on this as the end-of-data signal.
        /// Throws <see cref="LeagueAppsAuthException"/> when a bearer token cannot be obtained, and a plain
        /// <see cref="Exception"/> carrying status, resource and a truncated body for any other failure.
        /// </summary>
        public T GetPrivate<T>( string resource )
        {
            var siteId = Encryption.DecryptString( Settings.GetAttributeValue( Constants.LeagueAppsSiteId ) );
            var token = GetBearerToken();

            //magic string (sorry)
            resource = resource.Replace( "{siteid}", siteId );

            string export;
            HttpResponseMessage response;
            using ( var request = new HttpRequestMessage( HttpMethod.Get, resource ) )
            {
                request.Headers.Authorization = new AuthenticationHeaderValue( "Bearer", token );
                response = adminClient.SendAsync( request ).GetAwaiter().GetResult();
            }
            using ( response )
            {
                export = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                if ( !response.IsSuccessStatusCode )
                {
                    throw new Exception( "LeagueApps API Response: " + ( int ) response.StatusCode + " " + response.ReasonPhrase + " for " + resource + " " + Truncate( export ) );
                }
            }

            if ( string.IsNullOrWhiteSpace( export ) )
            {
                return default( T );
            }

            try
            {
                return JsonConvert.DeserializeObject<T>( export );
            }
            catch ( JsonException ex )
            {
                throw new Exception( "LeagueApps API returned a body that could not be parsed as " + typeof( T ).Name + " for " + resource + ": " + ex.Message + " Body: " + Truncate( export ), ex );
            }
        }

        /// <summary>
        /// Returns a cached OAuth bearer token, exchanging a fresh JWT assertion only when the cached token
        /// is missing or about to expire.
        /// </summary>
        private string GetBearerToken()
        {
            if ( bearerToken != null && DateTime.UtcNow < bearerTokenExpiresUtc.AddSeconds( -TokenRefreshMarginSeconds ) )
            {
                return bearerToken;
            }

            var clientId = Encryption.DecryptString( Settings.GetAttributeValue( Constants.LeagueAppsClientId ) );

            // Get a Unix Timestamp
            TimeSpan t = ( DateTime.UtcNow - new DateTime( 1970, 1, 1 ) );
            int timestamp = ( int ) t.TotalSeconds;
            var payload = new Dictionary<string, object>()
            {
                { "aud", "https://auth.leagueapps.io/v2/auth/token" },
                { "iss", clientId },
                { "sub", clientId },
                { "iat", timestamp },
                { "exp", timestamp + JwtLifetimeSeconds }
            };

            X509Certificate2 cert = new X509Certificate2( Certificate, "notasecret", X509KeyStorageFlags.Exportable );
            object privateKey;
            if ( cert.HasCngKey() )
            {
                privateKey = new Security.Cryptography.RSACng( cert.GetCngPrivateKey() );
            }
            else
            {
                RSACryptoServiceProvider key = ( RSACryptoServiceProvider ) cert.PrivateKey;
                privateKey = new RSACryptoServiceProvider();
                ( ( RSACryptoServiceProvider ) privateKey ).ImportParameters( key.ExportParameters( true ) );
            }

            string assertion = JWT.Encode( payload, privateKey, JwsAlgorithm.RS256 );

            // Using the JWT assertion, get an OAuth Bearer token for subsequent requests
            string responseStr;
            using ( var request = new HttpRequestMessage( HttpMethod.Post, "/v2/auth/token" ) )
            {
                var keyValues = new List<KeyValuePair<string, string>>();
                keyValues.Add( new KeyValuePair<string, string>( "grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer" ) );
                keyValues.Add( new KeyValuePair<string, string>( "assertion", assertion ) );
                request.Content = new FormUrlEncodedContent( keyValues );

                using ( var response = authClient.SendAsync( request ).GetAwaiter().GetResult() )
                {
                    responseStr = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

                    if ( !response.IsSuccessStatusCode || string.IsNullOrWhiteSpace( responseStr ) )
                    {
                        throw new LeagueAppsAuthException( "LeagueApps auth failed: " + ( int ) response.StatusCode + " " + response.ReasonPhrase + " " + Truncate( responseStr ) );
                    }
                }
            }

            JObject obj;
            try
            {
                obj = JObject.Parse( responseStr );
            }
            catch ( JsonException ex )
            {
                throw new LeagueAppsAuthException( "LeagueApps auth failed: token response was not JSON: " + ex.Message + " Body: " + Truncate( responseStr ), ex );
            }

            string token = obj.Value<string>( "access_token" );
            if ( string.IsNullOrWhiteSpace( token ) )
            {
                throw new LeagueAppsAuthException( "LeagueApps auth failed: no access_token in response " + Truncate( responseStr ) );
            }

            // Honor expires_in if LeagueApps sends it; otherwise assume the JWT lifetime.
            var expiresIn = obj.Value<int?>( "expires_in" ) ?? JwtLifetimeSeconds;
            bearerToken = token;
            bearerTokenExpiresUtc = DateTime.UtcNow.AddSeconds( expiresIn );
            return bearerToken;
        }

        private static string Truncate( string value, int maxLength = 500 )
        {
            if ( string.IsNullOrEmpty( value ) || value.Length <= maxLength )
            {
                return value;
            }
            return value.Substring( 0, maxLength ) + "...";
        }
    }

    /// <summary>
    /// Raised when a LeagueApps bearer token cannot be obtained. Callers should treat this as fatal for the
    /// whole run rather than retrying per record, since every subsequent call will fail the same way.
    /// </summary>
    public class LeagueAppsAuthException : Exception
    {
        public LeagueAppsAuthException( string message ) : base( message ) { }
        public LeagueAppsAuthException( string message, Exception innerException ) : base( message, innerException ) { }
    }

    public class LoggingHandler : DelegatingHandler
    {
        public LoggingHandler( HttpMessageHandler innerHandler ) : base( innerHandler )
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync( HttpRequestMessage request, CancellationToken cancellationToken )
        {
            HttpResponseMessage response = await base.SendAsync( request, cancellationToken );
            return response;
        }
    }
}
