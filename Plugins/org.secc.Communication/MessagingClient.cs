using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using org.secc.Communication.Components;
using org.secc.Communication.Messaging.Model;
using org.secc.DevLib.Components;
using RestSharp;

using Twilio.Http;
using Twilio.Rest.Taskrouter.V1.Workspace.TaskQueue;

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

        public List<TwilioPhoneNumber> GetTwilioNumbers()
        {
            return GetTwilioNumbers( false );
        }


        #region Twilio
        public List<TwilioPhoneNumber> GetTwilioNumbers( bool clearCache = false )
        {

            var url = $"{settings.MessagingUrl}twiliophonenumbers?code={settings.MessagingKey}{( clearCache ? "&nocache=1" : String.Empty )}";
            var restClient = new RestClient( url );
            var request = new RestRequest( Method.GET );
            request.RequestFormat = DataFormat.Json;
            request.AddHeader( "Accept", "application/json" );

            var response = restClient.Execute( request );

            if ( response.StatusCode != HttpStatusCode.OK )
            {
                throw new Exception( $"An error occurred while retrieving Twilio Number List. Status Code {response.StatusCode}. Description: {response.StatusDescription}" );
            }

            return JsonConvert.DeserializeObject<List<TwilioPhoneNumber>>( response.Content );
        }
        #endregion 


        #region Phone Numbers

        public MessagingPhoneNumber AddPhoneNumber( MessagingPhoneNumber number )
        {
            var url = $"{settings.MessagingUrl}phonenumbers?code={settings.MessagingKey}";
            var restClient = new RestClient( url );
            var request = new RestRequest( Method.POST );
            request.RequestFormat = DataFormat.Json;
            request.AddHeader( "Accept", "application/json" );
            request.AddParameter( "application/json", JsonConvert.SerializeObject( number ), ParameterType.RequestBody );
            var response = restClient.Execute( request );

            if ( response.StatusCode == HttpStatusCode.Created )
            {
                return JsonConvert.DeserializeObject<MessagingPhoneNumber>( response.Content );
            }

            return null;

        }

        public void DeletePhoneNumber( string id )
        {
            var url = $"{settings.MessagingUrl}phonenumbers/{id}?code={settings.MessagingKey}";
            var restClient = new RestClient( url );
            var request = new RestRequest( Method.DELETE );
            restClient.Execute( request );

        }

        public MessagingPhoneNumber GetPhoneNumber( string id )
        {
            var url = $"{settings.MessagingUrl}phonenumbers/{id}?code={settings.MessagingKey}";
            var restClient = new RestClient( url );
            var request = new RestRequest( Method.GET );
            request.RequestFormat = DataFormat.Json;
            request.AddHeader( "Accept", "applicaiton/json" );
            var response = restClient.Execute( request );

            if(response.StatusCode == HttpStatusCode.OK)
            {
                return JsonConvert.DeserializeObject<MessagingPhoneNumber>( response.Content );
            }

            return null;
        }

        public List<MessagingPhoneNumber> GetPhoneNumbers()
        {
            var url = $"{settings.MessagingUrl}phonenumbers?code={settings.MessagingKey}";
            var restClient = new RestClient( url );
            var request = new RestRequest( Method.GET );
            request.RequestFormat = DataFormat.Json;
            request.AddHeader( "Accept", "application/json" );
            var response = restClient.Execute( request );

            if(response.StatusCode == HttpStatusCode.OK)
            {
                return JsonConvert.DeserializeObject<List<MessagingPhoneNumber>>( response.Content );
            }

            return null;

        }

        public MessagingPhoneNumber UpdatePhoneNumber( MessagingPhoneNumber number )
        {
            var url = $"{settings.MessagingUrl}phonenumbers?code={settings.MessagingKey}";
            var restClient = new RestClient( url );
            var request = new RestRequest( Method.PUT );
            request.RequestFormat = DataFormat.Json;
            request.AddHeader( "Accept", "application/json" );
            request.AddParameter("application/json", JsonConvert.SerializeObject( number ), ParameterType.RequestBody );
            var response = restClient.Execute( request );

            if(response.StatusCode == HttpStatusCode.OK)
            {
                return JsonConvert.DeserializeObject<MessagingPhoneNumber>( response.Content );
            }

            return null;
        }

        #endregion


    }
}
