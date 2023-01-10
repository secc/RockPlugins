using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using org.secc.Communication.Components;
using org.secc.Communication.Messaging.Model;
using org.secc.DevLib.Components;

namespace org.secc.Communication
{
    public class MessagingClient
    {
        private SECCMessagingSettings settings;

        public MessagingClient()
        {
            var settingsComponent = SettingsComponent.GetComponent<MessagingServiceSettings>();
            settings = settingsComponent.GetSettings();
        }

        public MessagingClient( SECCMessagingSettings settings )
        {
            this.settings = settings;
        }

        public Task<List<TwilioPhoneNumber>> GetTwilioNumbers()
        {
            return GetTwilioNumbers( false );
        }


        #region Twilio
        public async Task<List<TwilioPhoneNumber>> GetTwilioNumbers( bool clearCache = false )
        {
            string method = "twiliophonenumbers";
            var queryValues = new Dictionary<string, string>();

            if ( clearCache )
            {
                queryValues.Add( "nocache", "1" );
            }

            var twilioNumberUri = BuildUrl( method, queryValues );

            using ( HttpClient httpClient = new HttpClient() )
            {
                var responseString = await httpClient.GetStringAsync( twilioNumberUri );

                return JsonConvert.DeserializeObject<List<TwilioPhoneNumber>>( responseString );
            }
        }
        #endregion 


        #region Phone Numbers

        public async Task AddPhoneNumber( MessagingPhoneNumber number )
        {
            var method = "phonenumbers";
            var uri = BuildUrl( method, new Dictionary<string, string>() );

            var body = JsonConvert.SerializeObject( number );

            var httpContent = new StringContent( body, Encoding.UTF8, "application/json" );
            using ( HttpClient httpClient = new HttpClient() )
            {
                var responseString = await httpClient.PostAsync( uri, httpContent );
            }

        }

        public async Task<MessagingPhoneNumber> GetPhoneNumber( string id )
        {
            var method = $"phonenumbers/{id}";
            var uri = BuildUrl( method, new Dictionary<string, string>() );

            using ( HttpClient httpClient = new HttpClient() )
            {
                var responseString = await httpClient.GetStringAsync( uri );
                return JsonConvert.DeserializeObject<MessagingPhoneNumber>( responseString );
            }
        }

        public async Task<List<MessagingPhoneNumber>> GetPhoneNumbers()
        {
            var method = "phonenumbers";
            var numberUri = BuildUrl( method, new Dictionary<string, string>() );

            using ( HttpClient httpClient = new HttpClient() )
            {
                var responseString = await httpClient.GetStringAsync( numberUri );
                return JsonConvert.DeserializeObject<List<MessagingPhoneNumber>>( responseString );
            }

        }

        public async Task UpdatePhoneNumber( MessagingPhoneNumber number )
        {
            var method = "phonenumbers";
            var numberUri = BuildUrl( method, new Dictionary<string, string>() );
            var body = JsonConvert.SerializeObject( number );
            var content = new StringContent( body, Encoding.UTF8, "application/json" );

            using ( HttpClient httpClient = new HttpClient() )
            {
                var responseString = await httpClient.PutAsync( numberUri, content );
            }
        }

        #endregion


        private Uri BuildUrl( string requestPath, Dictionary<string, string> queryString )
        {
            Uri requestUri = new Uri( $"{settings.MessagingUrl}{( settings.MessagingUrl.EndsWith( "/" ) ? String.Empty : "/" )}{requestPath}" );
            UriBuilder builder = new UriBuilder( requestUri );

            var queryBuilder = new StringBuilder( $"code={settings.MessagingKey}" );

            foreach ( var item in queryString )
            {
                queryBuilder.Append( $"&{item.Key}={item.Value}" );
            }
            builder.Query = queryBuilder.ToString();

            return builder.Uri;


        }

    }
}
