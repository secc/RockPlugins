using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Remoting.Activation;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using org.secc.Rise.Components;
using org.secc.Rise.Response;
using org.secc.xAPI.Component;
using Rock;

namespace org.secc.Rise
{
    public static class ClientManager
    {
        const string rootUrl = "https://api.rise.com";

        public static string ApiKey
        {
            get
            {
                var component = ( xAPIComponent ) xAPIContainer.Instance.Dictionary
               .Where( c => c.Value.Value.EntityType.Name == typeof( RiseComponent ).FullName )
               .Select( c => c.Value.Value )
               .FirstOrDefault();

                return component.GetAttributeValue( "APIKey" );
            }
        }

        private static List<string> _paginableProperties;
        public static List<string> PaginableProperties
        {
            get
            {
                if ( _paginableProperties == null )
                {
                    List<Type> riseTypes = new List<Type>();
                    foreach ( Type type in
                        Assembly.GetAssembly( typeof( RiseBase ) ).GetTypes()
                        .Where( t => t.IsClass && !t.IsAbstract && t.IsSubclassOf( typeof( RiseBase ) ) ) )
                    {
                        riseTypes.Add( type );
                    }
                    _paginableProperties = new List<string>();
                    foreach ( var type in riseTypes )
                    {
                        _paginableProperties.Add( GetUrlForType( type ) );
                    }
                }
                return _paginableProperties;
            }
        }


        internal static T Get<T>( string id, Dictionary<string, string> parameters = null )
        {
            var resourse = GetAsync<T>( id, parameters );
            Task.WaitAll( resourse );
            return resourse.Result;
        }

        internal static async Task<T> GetAsync<T>( string id, Dictionary<string, string> parameters = null )
        {
            return await GetResource<T>( id, parameters );
        }

        internal static IEnumerable<T> GetSet<T>( Dictionary<string, string> parameters = null )
        {
            return GetPagenatedResource<T>( GetUrl<T>(), parameters );
        }


        internal static IEnumerable<T> GetSet<T>( string id, Dictionary<string, string> parameters = null )
        {
            return GetPagenatedResource<T>( GetUrl<T>( id ), parameters );
        }

        internal static IEnumerable<T> GetSet<T>( RiseBase riseObject, Dictionary<string, string> parameters = null )
        {
            return GetPagenatedResource<T>( GetUrl<T>( riseObject ), parameters );
        }

        private static IEnumerable<T> GetPagenatedResource<T>( string url, Dictionary<string, string> parameters = null )
        {
            while ( !string.IsNullOrWhiteSpace( url ) )
            {
                var call = ApiGet( url, parameters );
                call.Wait();
                var pagination = JsonConvert.DeserializeObject<Pagination<T>>( call.Result, new PaginationJsonConverter<T>() );
                url = pagination.NextUrl;
                foreach ( var resource in pagination.Resources )
                {
                    yield return resource;
                }
            }
        }

        private static async Task<T> GetResource<T>( string id, Dictionary<string, string> parameters = null )
        {
            var response = await ApiGet( GetUrl<T>( id ), parameters );
            return JsonConvert.DeserializeObject<T>( response );
        }


        private static async Task<string> ApiGet( string resource, Dictionary<string, string> parameters = null )
        {
            if ( parameters != null )
            {
                string delimitor = "?";
                foreach ( KeyValuePair<string, string> parm in parameters )
                {
                    resource += delimitor + HttpUtility.UrlEncode( parm.Key ) + "=" + HttpUtility.UrlEncode( parm.Value );
                    delimitor = "&";
                }
            }

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add( "Authorization", "Bearer " + ApiKey );
            client.DefaultRequestHeaders.Add( "Rise-API-Version", "2020-07-16" );
            return await client.GetStringAsync( resource );
        }

        public static T Post<T>( Dictionary<string, string> parameters )
        {
            var resource = GetUrl<T>();
            var result = ApiPost<T>( resource, parameters );
            result.Wait();
            return JsonConvert.DeserializeObject<T>( result.Result );
        }

        internal static async Task<string> ApiPost<T>( string resource, Dictionary<string, string> parameters )
        {
            var content = new StringContent( JsonConvert.SerializeObject( parameters ),Encoding.UTF8, "application/json" );

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add( "Authorization", "Bearer " + ApiKey );
            client.DefaultRequestHeaders.Add( "Rise-API-Version", "2020-07-16" );
            var result = await client.PostAsync( resource, content );
            return await result.Content.ReadAsStringAsync();
        }

        internal static void Delete<T>( RiseBase riseBase, string childId )
        {
            var resource = GetUrl<T>( riseBase, childId );
            ApiDelete( resource );
        }

        private static void ApiDelete( string resource )
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add( "Authorization", "Bearer " + ApiKey );
            client.DefaultRequestHeaders.Add( "Rise-API-Version", "2020-07-16" );
            var result = client.DeleteAsync( resource );
            result.Wait();
        }

        internal static void Put<T>( RiseBase riseBase, string childId )
        {
            var resource = GetUrl<T>( riseBase, childId );
            ApiPut( resource );
        }

        private static void ApiPut( string resource )
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add( "Authorization", "Bearer " + ApiKey );
            client.DefaultRequestHeaders.Add( "Rise-API-Version", "2020-07-16" );
            var result = client.PutAsync( resource, new StringContent( "" ) );
            result.Wait();
        }


        private static string GetUrl<T>()
        {
            return $"{rootUrl}/{GetUrlForType<T>()}";
        }

        private static string GetUrl<T>( string id )
        {
            return $"{rootUrl}/{GetUrlForType<T>()}/{id}";
        }

        private static string GetUrl<T>( RiseBase riseBase )
        {
            var type = riseBase.GetType();
            UrlAttribute urlAttribute = ( UrlAttribute ) Attribute.GetCustomAttribute( type, typeof( UrlAttribute ) );

            if ( urlAttribute == null || string.IsNullOrWhiteSpace( urlAttribute.UrlValue ) )
            {
                throw new Exception( $"Resource for {type.Name} does not have defined url." );
            }

            return $"{rootUrl}/{urlAttribute.UrlValue}/{riseBase.Id}/{GetUrlForType<T>()}";
        }

        private static string GetUrl<T>( RiseBase riseBase, string childId )
        {
            return $"{GetUrl<T>( riseBase )}/{childId}";
        }

        private static string GetUrlForType<T>()
        {
            var type = typeof( T );
            return GetUrlForType( type );

        }

        private static string GetUrlForType( Type type )
        {
            UrlAttribute urlAttribute = ( UrlAttribute ) Attribute.GetCustomAttribute( type, typeof( UrlAttribute ) );

            if ( urlAttribute == null || string.IsNullOrWhiteSpace( urlAttribute.UrlValue ) )
            {
                throw new Exception( $"Resource for {type.Name} does not have defined url." );
            }
            return urlAttribute.UrlValue;
        }
    }
}
