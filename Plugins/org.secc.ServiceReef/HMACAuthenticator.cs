using RestSharp;
using RestSharp.Authenticators;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace org.secc.ServiceReef
{
    public class HMACAuthenticator : IAuthenticator {


        private String _srApiKey = null; 
        private String _srApiSecret = null;

        public HMACAuthenticator(String ApiKey, String ApiSecret)
        {
            _srApiKey = ApiKey;
            _srApiSecret = ApiSecret;
        }
        
        public void Authenticate(IRestClient client, IRestRequest request)
        {

            if (!request.Parameters.Any(p => p.Name.Equals("Authorization", StringComparison.OrdinalIgnoreCase)))
            {
                request.AddHeader("Authorization", GetAuthenticationHeader(client, request));
            }
            else
            {
                request.Parameters.Where(p => p.Name == "Authorization").FirstOrDefault().Value
                    = GetAuthenticationHeader(client, request);
            }

        }

        private string GetAuthenticationHeader(RestSharp.IRestClient client, RestSharp.IRestRequest request)
        {
            string requestContentBase64String = string.Empty;

            //Calculate UNIX time
            DateTime epochStart = new DateTime(1970, 01, 01, 0, 0, 0, 0, DateTimeKind.Utc);
            TimeSpan timeSpan = DateTime.UtcNow - epochStart;
            string requestTimeStamp = Convert.ToUInt64(timeSpan.TotalSeconds).ToString();

            //create random nonce for each request
            string nonce = Guid.NewGuid().ToString("N");

            // URLEncode the request URI
            var requestUri = HttpUtility.UrlEncode(client.BuildUri(request).AbsoluteUri.ToLower());

            //Creating the raw signature string
            string signatureRawData = String.Format("{0}{1}{2}{3}{4}{5}", _srApiKey, request.Method, requestUri, requestTimeStamp, nonce, requestContentBase64String);

            var secretKeyByteArray = Convert.FromBase64String(_srApiSecret);

            byte[] signature = Encoding.UTF8.GetBytes(signatureRawData);

            using (HMACSHA256 hmac = new HMACSHA256(secretKeyByteArray))
            {
                byte[] signatureBytes = hmac.ComputeHash(signature);
                string requestSignatureBase64String = Convert.ToBase64String(signatureBytes);
                //Setting the values in the Authorization header using custom scheme (amx)
                return "amx " +
                       string.Format("{0}:{1}:{2}:{3}", _srApiKey, requestSignatureBase64String, nonce, requestTimeStamp);
            }
        }
    }
}
