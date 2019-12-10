using Jose;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using org.secc.LeagueApps.Contracts;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace org.secc.LeagueApps
{
    public class APIClient
    {
        internal static List<Registrations> names { get; private set; }
        internal static Member user { get; private set; }

        public static async System.Threading.Tasks.Task RunAsync(Byte[] rawdata, String client_id, Boolean registrations, String resource)
        {
            // Get a Unix Timestamp
            TimeSpan t = (DateTime.UtcNow - new DateTime(1970, 1, 1));
            int timestamp = (int)t.TotalSeconds;
            var payload = new Dictionary<string, object>()
            {
                { "aud", "https://auth.leagueapps.io/v2/auth/token" },
                { "iss", client_id },
                { "sub", client_id },
                { "iat", timestamp },
                { "exp", timestamp+300  }
            };
            X509Certificate2 cert = new X509Certificate2(rawdata, "notasecret", X509KeyStorageFlags.Exportable);
            RSACryptoServiceProvider key = (RSACryptoServiceProvider)cert.PrivateKey;
            RSACryptoServiceProvider privateKey = new RSACryptoServiceProvider();
            privateKey.ImportParameters(key.ExportParameters(true));
            byte[] data = Encoding.UTF8.GetBytes("Data to be signed");
            privateKey.SignData(data, "SHA256");
            string assertion = JWT.Encode(payload, privateKey, JwsAlgorithm.RS256);
            // Using the JWT assertion, get an OAuth Bearer token for subsequent requests
            var client = new HttpClient(new LoggingHandler(new HttpClientHandler()));
            client.BaseAddress = new Uri("https://auth.leagueapps.io");
            var request = new HttpRequestMessage(HttpMethod.Post, "/v2/auth/token");
            var keyValues = new List<KeyValuePair<string, string>>();
            keyValues.Add(new KeyValuePair<string, string>("grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"));
            keyValues.Add(new KeyValuePair<string, string>("assertion", assertion));
            request.Content = new FormUrlEncodedContent(keyValues);
            var response = await client.SendAsync(request);
            var responseStr = await response.Content.ReadAsStringAsync();

            if (!string.IsNullOrWhiteSpace(responseStr))
            {
                dynamic obj = JObject.Parse(responseStr);
                string token = obj.access_token;
                var newClient = new HttpClient(new LoggingHandler(new HttpClientHandler()));
                newClient.BaseAddress = new Uri("https://admin.leagueapps.io");
                newClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                response = await newClient.GetAsync(resource);
                var export = await response.Content.ReadAsStringAsync();
                if (registrations)
                    names = JsonConvert.DeserializeObject<List<Contracts.Registrations>>(export);
                else
                    user = JsonConvert.DeserializeObject<Contracts.Member>(export);

            }
        }
    }

    public class LoggingHandler : DelegatingHandler
    {
        public LoggingHandler(HttpMessageHandler innerHandler): base(innerHandler)
        {
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage response = await base.SendAsync(request, cancellationToken);
            return response;
        }
    }
}
