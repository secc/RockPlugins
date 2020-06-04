using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using org.secc.Purchasing.Intacct.Api;
using org.secc.Purchasing.Intacct.Auth;
using org.secc.Purchasing.Intacct.Functions;
using org.secc.Purchasing.Intacct.Model;
using RestSharp;
using Rock.Web.Cache;

namespace org.secc.Purchasing.Intacct
{
    /// <summary>
    /// An API client for Intacct:  https://developer.intacct.com/api/
    /// </summary>
    public class ApiClient
    {

        /// <summary>
        /// The API Endpoint
        /// </summary>
        const string INTACCT_API_ENDPOINT = "https://api.intacct.com/ia/xml/xmlgw.phtml";

        const string INTACCT_CACHE_TAG = "IntacctAPI";

        const string INTACCT_CACHE_PREFIX = "org.secc.Purchasing.Intacct.";

        RestClient client = null;
        public string SenderId { get; set; }
        public string SenderPassword { get; set; }
        public string CompanyId { get; set; }
        public string UserId { get; set; }
        public string Password { get; set; }

        string Session { get; set; }

        DateTime CacheTimeout { get; set; }

        public ApiClient()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Ssl3 |
                                                   SecurityProtocolType.Tls | SecurityProtocolType.Tls11;

            client = new RestClient( INTACCT_API_ENDPOINT );
        }

        private Request GetRequest( bool includeSession = true )
        {
            var requestBody = new Request();
            requestBody.Control.SenderId = SenderId;
            requestBody.Control.Password = SenderPassword;
            if ( includeSession )
            {
                if (string.IsNullOrWhiteSpace( Session ) )
                {
                    StartApiSession();
                }

                requestBody.Operation.Authentication.SessionId = Session;
            } 

            return requestBody;
        }

        private void CheckResponse(IRestResponse response)
        {
            var xmlDeserializer = new RestSharp.Deserializers.XmlDeserializer();
            var responseObj = xmlDeserializer.Deserialize<Response>( response );
            if ( responseObj?.Operation?.Result?.Status != "success" )
            {
                string errorMessage = "Intacct API Error " + responseObj?.ErrorMessage?.Error?.ErrorNo + ":";
                if ( !string.IsNullOrWhiteSpace( responseObj?.ErrorMessage?.Error?.Description ) )
                {
                    errorMessage += " " + responseObj?.ErrorMessage?.Error?.Description;
                }

                if ( !string.IsNullOrWhiteSpace( responseObj?.ErrorMessage?.Error?.Description2 ) )
                {
                    errorMessage += " " + responseObj?.ErrorMessage?.Error?.Description2;
                }
                
                throw new Exception( errorMessage );
            }

        }

        /// <summary>
        /// This fetches a dimension's related data.
        /// </summary>
        /// <returns>List of RestrictedData objects.</returns>
        public List<RestrictedData> GetDimensionRestrictedData( IntacctModel model )
        {
            string cacheKey = INTACCT_CACHE_PREFIX + model.GetType().Name.ToUpper() + "_" + model.Id;
            List<RestrictedData> items = RockCache.Get( cacheKey ) as List<RestrictedData>;

            if ( items == null )
            {
                var request = GetRequest();
                var getDimensionRestrictedData = new GetDimensionRestrictedData();
                getDimensionRestrictedData.Function.DimensionValue.Dimension = model.GetType().Name.ToUpper();
                getDimensionRestrictedData.Function.DimensionValue.Value = model.ApiId;
                request.Operation.Content.Function = getDimensionRestrictedData;

                var restRequest = new RestRequest( "", Method.POST );
                restRequest.AddHeader( "Content-Type", "application/xml" );
                restRequest.RequestFormat = DataFormat.Xml;
                restRequest.AddBody( request );

                var response = client.Execute<List<RestrictedData>>( restRequest );

                var xmlDeserializer = new RestSharp.Deserializers.XmlDeserializer();
                var responseObj = xmlDeserializer.Deserialize<Response>( response );
                if ( responseObj?.Operation?.Result?.Status != "success" && !string.IsNullOrWhiteSpace( responseObj?.ErrorMessage?.Error?.Description ) )
                {
                    CheckResponse( response );
                }

                items = response.Data;

                RockCache.AddOrUpdate( cacheKey, items );
            }
            return items;
        }

        /// <summary>
        /// This gets all Departments from Intacct.
        /// </summary>
        /// <returns>List of LocationEntity objects.</returns>
        public List<Department> GetDepartments()
        {
            return ReadByQuery<Department>();
        }

        /// <summary>
        /// This gets all GL Accounts from Intacct.
        /// </summary>
        /// <returns>List of LocationEntity objects.</returns>
        public List<GLAccount> GetGLAccounts()
        {
            return ReadByQuery<GLAccount>();
        }

        /// <summary>
        /// This gets all Location Entities from Intacct.
        /// </summary>
        /// <returns>List of LocationEntity objects.</returns>
        public List<LocationEntity> GetLocationEntities()
        {
            return ReadByQuery<LocationEntity>();
        }

        /// <summary>
        /// This gets all Locations from Intacct.
        /// </summary>
        /// <returns>List of Location objects.</returns>
        public List<Location> GetLocations()
        {
            return ReadByQuery<Location>();
        }


        /// <summary>
        /// This gets all projects from Intacct.
        /// </summary>
        /// <returns>List of Project objects.</returns>
        public List<Project> GetProjects()
        {
            return ReadByQuery<Project>();
        }

        private List<T> ReadByQuery<T>() where T : IntacctModel
        {
            string operation = typeof( T ).Name.ToUpper();

            string cacheKey = INTACCT_CACHE_PREFIX + operation;
            List<T> items = RockCache.Get( cacheKey ) as List<T>;

            if ( items == null )
            {
                items = new List<T>();

                var request = GetRequest();
                request.Operation.Content.Function = new ReadByQuery( operation );

                var restRequest = new RestRequest( "", Method.POST );
                restRequest.AddHeader( "Content-Type", "application/xml" );
                restRequest.RequestFormat = DataFormat.Xml;
                restRequest.AddBody( request );

                var response = client.Execute<List<T>>( restRequest );
                CheckResponse( response );

                var responseObj = new RestSharp.Deserializers.XmlDeserializer().Deserialize<Response>( response );
                int? numRemaining = responseObj?.Operation?.Result?.Data?.NumRemaining ?? 0;
                string resultId = responseObj?.Operation?.Result?.Data?.ResultId;
                items.AddRange( response.Data );

                while ( numRemaining != 0 && !string.IsNullOrWhiteSpace( resultId ) )
                {
                    restRequest = new RestRequest( "", Method.POST );
                    restRequest.AddHeader( "Content-Type", "application/xml" );
                    restRequest.RequestFormat = DataFormat.Xml;

                    request.Operation.Content.Function = new ReadMore( resultId );
                    restRequest.AddBody( request );

                    response = client.Execute<List<T>>( restRequest );
                    CheckResponse( response );

                    responseObj = new RestSharp.Deserializers.XmlDeserializer().Deserialize<Response>( response );
                    numRemaining = responseObj?.Operation?.Result?.Data?.NumRemaining ?? 0;
                    items.AddRange( response.Data );
                }


                RockCache.AddOrUpdate( cacheKey, items );
            }
            return items;
        }


        protected void StartApiSession()
        {

            string cacheKey = "org.secc.Purchasing.Intacct.ApiSession";
            ApiSession apiSession = RockCache.Get( cacheKey ) as ApiSession;

            if ( apiSession == null )
            {
                var request = GetRequest( false );
                request.Operation.Content.Function = new GetAPISession();

                // Set the login information in the request
                Login login = new Login();
                login.CompanyId = CompanyId;
                login.UserId = UserId;
                login.Password = Password;
                request.Operation.Authentication.Login = login;

                var restRequest = new RestRequest( "", Method.POST );
                restRequest.AddHeader( "Content-Type", "application/xml" );
                restRequest.RequestFormat = DataFormat.Xml;
                restRequest.AddBody( request );


                var response = client.Execute<ApiSession>( restRequest );
                CheckResponse( response );

                apiSession = response.Data;

                // Fetch the expiration date too (expire cache 30 minutes before the session ends)
                var xmlDeserializer = new RestSharp.Deserializers.XmlDeserializer();
                var auth = xmlDeserializer.Deserialize<Authentication>( response );

                CacheTimeout = auth?.SessionTimeout?.AddMinutes( -30 )??DateTime.Now.AddHours(1);

                RockCache.AddOrUpdate( cacheKey, null, apiSession, CacheTimeout, INTACCT_CACHE_TAG ); ;
            }
            Session = apiSession.SessionId;
        }
    }

    public class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding => Encoding.UTF8;
    }

}
