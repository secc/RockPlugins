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
                    certificate = p12File.DatabaseData.Content;
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

        public T GetPrivate<T>( string resource )
        {
            //League Apps Settings
            var siteId = Encryption.DecryptString( Settings.GetAttributeValue( Constants.LeagueAppsSiteId ) );
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
                { "exp", timestamp+300  }
            };

            X509Certificate2 cert = new X509Certificate2( Certificate, "notasecret", X509KeyStorageFlags.Exportable );
            object privateKey;
            if ( cert.HasCngKey() )
            {
                privateKey = new RSACng( cert.GetCngPrivateKey() );
            }
            else
            {
                RSACryptoServiceProvider key = ( RSACryptoServiceProvider ) cert.PrivateKey;
                privateKey = new RSACryptoServiceProvider();
                ( ( RSACryptoServiceProvider ) privateKey ).ImportParameters( key.ExportParameters( true ) );
            }

            string assertion = JWT.Encode( payload, privateKey, JwsAlgorithm.RS256 );
            // Using the JWT assertion, get an OAuth Bearer token for subsequent requests
            var client = new HttpClient( new LoggingHandler( new HttpClientHandler() ) );
            client.BaseAddress = new Uri( "https://auth.leagueapps.io" );
            var request = new HttpRequestMessage( HttpMethod.Post, "/v2/auth/token" );
            var keyValues = new List<KeyValuePair<string, string>>();
            keyValues.Add( new KeyValuePair<string, string>( "grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer" ) );
            keyValues.Add( new KeyValuePair<string, string>( "assertion", assertion ) );
            request.Content = new FormUrlEncodedContent( keyValues );
            var response = client.SendAsync( request ).GetAwaiter().GetResult();
            var responseStr = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            if ( !string.IsNullOrWhiteSpace( responseStr ) )
            {
                dynamic obj = JObject.Parse( responseStr );
                string token = obj.access_token;
                var newClient = new HttpClient( new LoggingHandler( new HttpClientHandler() ) );
                newClient.BaseAddress = new Uri( "https://admin.leagueapps.io" );
                newClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue( "Bearer", token );

                //magic string (sorry)
                resource = resource.Replace( "{siteid}", siteId );

                response = newClient.GetAsync( resource ).GetAwaiter().GetResult();
                var export = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();


                try
                {
                    return JsonConvert.DeserializeObject<T>( export );
                }
                catch ( Exception ex )
                {
                    ExceptionLogService.LogException( ex );
                }

            }
            return default( T );
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
