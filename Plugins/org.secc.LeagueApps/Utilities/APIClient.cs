using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
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
using org.secc.DevLib.Extensions;
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
                    // ROCK-9041: Read through a fresh provider stream (disposed); throws a clear error when the
                    // service account file is missing or empty. See BinaryFileExtensions.ReadContentBytes.
                    certificate = p12File.ReadContentBytes( "LeagueApps service account file" );
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


            // An empty body deserializes to null; callers decide whether that is an error.
            var export = response.Content;
            if ( string.IsNullOrWhiteSpace( export ) )
            {
                return default( T );
            }
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

        // Admin error bodies are kept short in messages; they end up in ExceptionLog and job status.
        private const int MaxErrorBodyLength = 200;

        /// <summary>
        /// Calls an authenticated LeagueApps admin API resource and deserializes the JSON body.
        /// Returns <c>default(T)</c> (null for reference types) when the body is empty or the JSON literal
        /// <c>null</c>; callers that page through export endpoints rely on this as the end-of-data signal.
        /// Throws <see cref="LeagueAppsAuthException"/> when a bearer token cannot be obtained or is rejected
        /// even after a refresh, and a plain <see cref="Exception"/> carrying status and resource for any other failure.
        /// </summary>
        public T GetPrivate<T>( string resource )
        {
            var siteId = Encryption.DecryptString( Settings.GetAttributeValue( Constants.LeagueAppsSiteId ) );

            //magic string (sorry)
            resource = resource.Replace( "{siteid}", siteId );

            string export;
            string reasonPhrase;
            var statusCode = SendAdminRequest( resource, GetBearerToken(), out reasonPhrase, out export );

            if ( statusCode == HttpStatusCode.Unauthorized )
            {
                // The cached token can be rejected before its local expiry (revocation, clock skew).
                // Drop it and retry once with a fresh one before declaring the run's credentials dead.
                bearerToken = null;
                statusCode = SendAdminRequest( resource, GetBearerToken(), out reasonPhrase, out export );

                if ( statusCode == HttpStatusCode.Unauthorized )
                {
                    throw new LeagueAppsAuthException( "LeagueApps rejected a freshly issued bearer token: 401 " + reasonPhrase + " for " + resource );
                }
            }

            if ( ( int ) statusCode < 200 || ( int ) statusCode > 299 )
            {
                throw new LeagueAppsApiException( statusCode, "LeagueApps API Response: " + ( int ) statusCode + " " + reasonPhrase + " for " + resource + " " + export.Truncate( MaxErrorBodyLength ) );
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
                // A successful body is member data; report its size, not its contents.
                throw new Exception( "LeagueApps API returned a " + export.Length + "-character body that could not be parsed as " + typeof( T ).Name + " for " + resource + ": " + ex.Message, ex );
            }
        }

        private static HttpStatusCode SendAdminRequest( string resource, string token, out string reasonPhrase, out string body )
        {
            using ( var request = new HttpRequestMessage( HttpMethod.Get, resource ) )
            {
                request.Headers.Authorization = new AuthenticationHeaderValue( "Bearer", token );
                using ( var response = adminClient.SendAsync( request ).GetAwaiter().GetResult() )
                {
                    body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    reasonPhrase = response.ReasonPhrase;
                    return response.StatusCode;
                }
            }
        }

        /// <summary>
        /// Returns a cached OAuth bearer token, exchanging a fresh JWT assertion only when the cached token
        /// is missing or about to expire. Any failure to obtain a token surfaces as <see cref="LeagueAppsAuthException"/>.
        /// </summary>
        private string GetBearerToken()
        {
            if ( bearerToken != null && DateTime.UtcNow < bearerTokenExpiresUtc.AddSeconds( -TokenRefreshMarginSeconds ) )
            {
                return bearerToken;
            }

            try
            {
                return RequestBearerToken();
            }
            catch ( LeagueAppsAuthException )
            {
                throw;
            }
            catch ( Exception ex )
            {
                // Network errors, timeouts and certificate problems all mean no call in this run can succeed.
                throw new LeagueAppsAuthException( "LeagueApps auth failed: " + ex.Message, ex );
            }
        }

        private string RequestBearerToken()
        {
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
                        // Never echo the token endpoint's body; report only the standard OAuth error code if it sent one.
                        throw new LeagueAppsAuthException( "LeagueApps auth failed: " + ( int ) response.StatusCode + " " + response.ReasonPhrase + GetOAuthErrorCode( responseStr ) );
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
                // The token response carries the token itself, so never echo the body.
                throw new LeagueAppsAuthException( "LeagueApps auth failed: token response was not JSON: " + ex.Message, ex );
            }

            string token = obj.Value<string>( "access_token" );
            if ( string.IsNullOrWhiteSpace( token ) )
            {
                throw new LeagueAppsAuthException( "LeagueApps auth failed: no access_token in response" );
            }

            // Honor expires_in if LeagueApps sends it (as an integer, decimal or numeric string);
            // otherwise assume the JWT lifetime.
            double expiresIn;
            var expiresInValue = obj["expires_in"] as JValue;
            if ( !double.TryParse( Convert.ToString( expiresInValue?.Value, CultureInfo.InvariantCulture ), NumberStyles.Float,CultureInfo.InvariantCulture, out expiresIn ) || expiresIn <= 0 )
            {
                expiresIn = JwtLifetimeSeconds;
            }
            bearerToken = token;
            bearerTokenExpiresUtc = DateTime.UtcNow.AddSeconds( expiresIn );
            return bearerToken;
        }

        /// <summary>
        /// Returns " (error: code)" for an RFC 6749 error response such as <c>{"error":"invalid_grant"}</c>,
        /// or an empty string when the body is not one. The code is a short fixed token, never the body itself.
        /// </summary>
        private static string GetOAuthErrorCode( string body )
        {
            try
            {
                var code = JObject.Parse( body ).Value<string>( "error" );
                if ( !string.IsNullOrWhiteSpace( code ) && code.Length <= 64 && System.Text.RegularExpressions.Regex.IsMatch( code, "^[A-Za-z0-9_.-]+$" ) )
                {
                    return " (error: " + code + ")";
                }
            }
            catch ( Exception )
            {
                // Not a JSON error object; report status only.
            }
            return string.Empty;
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

    /// <summary>
    /// Raised for a non-2xx admin API response other than an unrecoverable 401, so callers can tell
    /// an ordinary per-record miss (404) from an API that is failing.
    /// </summary>
    public class LeagueAppsApiException : Exception
    {
        public HttpStatusCode StatusCode { get; }

        public LeagueAppsApiException( HttpStatusCode statusCode, string message ) : base( message )
        {
            StatusCode = statusCode;
        }
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
