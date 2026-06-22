using System;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json;
using org.secc.Communication.Components;
using org.secc.Communication.Messaging;
using org.secc.Communication.Messaging.Model;
using org.secc.DevLib.Components;
using RestSharp;

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

            var url = $"{settings.MessagingUrl}twiliophonenumbers{(clearCache ? "?nocache=1" : String.Empty)}";
            var restClient = new RestClient( url );
            var request = new RestRequest( Method.GET );
            request.RequestFormat = DataFormat.Json;
            request.AddHeader( "Accept", "application/json" );
            request.AddHeader( "x-functions-key", settings.MessagingKey );

            var response = restClient.Execute( request );

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception( $"An error occurred while retrieving Twilio Number List. Status Code {response.StatusCode}. Description: {response.StatusDescription}" );
            }

            return JsonConvert.DeserializeObject<List<TwilioPhoneNumber>>( response.Content );
        }
        #endregion 


        #region Phone Numbers

        public MessagingPhoneNumber AddPhoneNumber( MessagingPhoneNumber number )
        {
            var url = $"{settings.MessagingUrl}phonenumbers";
            var restClient = new RestClient( url );
            var request = new RestRequest( Method.POST );
            request.RequestFormat = DataFormat.Json;
            request.AddHeader( "Accept", "application/json" );
            request.AddHeader( "x-functions-key", settings.MessagingKey );
            request.AddParameter( "application/json", JsonConvert.SerializeObject( number ), ParameterType.RequestBody );
            var response = restClient.Execute( request );

            if (response.StatusCode == HttpStatusCode.Created)
            {
                return JsonConvert.DeserializeObject<MessagingPhoneNumber>( response.Content );
            }

            return null;

        }

        public void DeletePhoneNumber( string id )
        {
            var url = $"{settings.MessagingUrl}phonenumbers/{id}";
            var restClient = new RestClient( url );
            var request = new RestRequest( Method.DELETE );
            request.AddHeader( "x-functions-key", settings.MessagingKey );
            restClient.Execute( request );

        }

        public MessagingPhoneNumber GetPhoneNumber( string id )
        {
            var url = $"{settings.MessagingUrl}phonenumbers/{id}";
            var restClient = new RestClient( url );
            var request = new RestRequest( Method.GET );
            request.RequestFormat = DataFormat.Json;
            request.AddHeader( "Accept", "application/json" );
            request.AddHeader( "x-functions-key", settings.MessagingKey );
            var response = restClient.Execute( request );

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return JsonConvert.DeserializeObject<MessagingPhoneNumber>( response.Content );
            }

            return null;
        }

        public List<MessagingPhoneNumber> GetPhoneNumbers()
        {
            var url = $"{settings.MessagingUrl}phonenumbers";
            var restClient = new RestClient( url );
            var request = new RestRequest( Method.GET );
            request.RequestFormat = DataFormat.Json;
            request.AddHeader( "Accept", "application/json" );
            request.AddHeader( "x-functions-key", settings.MessagingKey );
            var response = restClient.Execute( request );

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return JsonConvert.DeserializeObject<List<MessagingPhoneNumber>>( response.Content );
            }

            return null;

        }

        public MessagingPhoneNumber UpdatePhoneNumber( MessagingPhoneNumber number )
        {
            var url = $"{settings.MessagingUrl}phonenumbers";
            var restClient = new RestClient( url );
            var request = new RestRequest( Method.PUT );
            request.RequestFormat = DataFormat.Json;
            request.AddHeader( "Accept", "application/json" );
            request.AddHeader( "x-functions-key", settings.MessagingKey );
            request.AddParameter( "application/json", JsonConvert.SerializeObject( number ), ParameterType.RequestBody );
            var response = restClient.Execute( request );

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return JsonConvert.DeserializeObject<MessagingPhoneNumber>( response.Content );
            }

            return null;
        }

        #endregion


        #region Keywords

        public void AddKeyword( string phoneId, Keyword k )
        {
            var url = $"{settings.MessagingUrl}phonenumbers/{phoneId}/keywords";
            var restClient = new RestClient( url );
            var request = new RestRequest( Method.POST );
            request.RequestFormat = DataFormat.Json;
            request.AddHeader( "Accept", "application/json" );
            request.AddHeader( "x-functions-key", settings.MessagingKey );
            request.AddParameter( "application/json", JsonConvert.SerializeObject( k ), ParameterType.RequestBody );

            var response = restClient.Execute( request );

            if (response.StatusCode != HttpStatusCode.Created)
            {
                throw new Exception( "Keyword not created" );
            }
        }

        public void DeleteKeyword( string phoneId, string keywordId )
        {
            var url = $"{settings.MessagingUrl}phonenumbers/{phoneId}/keywords/{keywordId}";
            var restClient = new RestClient( url );
            var request = new RestRequest( Method.DELETE );
            request.RequestFormat = DataFormat.Json;
            request.AddHeader( "x-functions-key", settings.MessagingKey );
            var response = restClient.Execute( request );

            if (response.StatusCode != HttpStatusCode.Gone)
            {
                throw new Exception( "Keyword not deleted." );
            }

        }

        public Keyword GetKeyword( string phoneId, string keywordId )
        {
            var url = $"{settings.MessagingUrl}phonenumbers/{phoneId}/keywords/{keywordId}";
            var restClient = new RestClient( url );
            var request = new RestRequest( Method.GET );
            request.RequestFormat = DataFormat.Json;
            request.AddHeader( "Accept", "application/json" );
            request.AddHeader( "x-functions-key", settings.MessagingKey );
            var response = restClient.Execute( request );

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new Exception( $"Keyword {keywordId} not found." );
            }

            var k = JsonConvert.DeserializeObject<Keyword>( response.Content );
            return k;
        }

        public void ReorderKeyword( KeywordReorderItem item, string phoneId )
        {
            var url = $"{settings.MessagingUrl}phonenumbers/{phoneId}/keywords/reorder";
            var restClient = new RestClient( url );
            var request = new RestRequest( Method.PATCH );
            request.RequestFormat = DataFormat.Json;
            request.AddHeader( "Accept", "application/json" );
            request.AddHeader( "x-functions-key", settings.MessagingKey );
            request.AddParameter( "application/json", JsonConvert.SerializeObject( item ), ParameterType.RequestBody );
            var response = restClient.Execute( request );

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception( "An error occurred reordering keywords." );
            }

        }

        public void UpdateKeyword( string phoneId, Keyword k )
        {
            var url = $"{settings.MessagingUrl}phonenumbers/{phoneId}/keywords";
            var restClient = new RestClient( url );
            var request = new RestRequest( Method.PUT );
            request.RequestFormat = DataFormat.Json;
            request.AddHeader( "Accept", "application/json" );
            request.AddHeader( "x-functions-key", settings.MessagingKey );
            request.AddParameter( "application/json", JsonConvert.SerializeObject( k ), ParameterType.RequestBody );
            var response = restClient.Execute( request );

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception( "An error occurred updating keyword." );
            }
        }
        #endregion

    }
}
