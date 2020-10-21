using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Activation;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
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


        internal static T Get<T>( string id )
        {
            var resourse = GetAsync<T>( id );
            Task.WaitAll( resourse );
            return resourse.Result;
        }

        internal static async Task<T> GetAsync<T>( string id )
        {
            return await GetResource<T>( id );
        }

        internal static IEnumerable<T> GetSet<T>()
        {
            return GetPagenatedResource<T>( GetUrl<T>() );
        }

        internal static IEnumerable<T> GetSet<T>( string id )
        {
            return GetPagenatedResource<T>( GetUrl<T>( id ) );
        }

        internal static IEnumerable<T> GetSet<T>( RiseBase riseObject )
        {
            return GetPagenatedResource<T>( GetUrl<T>( riseObject ) );
        }

        private static IEnumerable<T> GetPagenatedResource<T>( string url )
        {
            while ( !string.IsNullOrWhiteSpace( url ) )
            {
                var call = MakeApiCall( url );
                Task.WaitAll( call );
                var pagination = JsonConvert.DeserializeObject<Pagination<T>>( call.Result, new PaginationJsonConverter<T>() );
                url = pagination.NextUrl;
                foreach ( var resource in pagination.Resources )
                {
                    yield return resource;
                }
            }
        }

        private static async Task<T> GetResource<T>( string id )
        {
            var response = await MakeApiCall( GetUrl<T>( id ) );
            return JsonConvert.DeserializeObject<T>( response );
        }


        private static async Task<string> MakeApiCall( string resource )
        {
            var component = ( xAPIComponent ) xAPIContainer.Instance.Dictionary
               .Where( c => c.Value.Value.EntityType.Name == typeof(RiseComponent).Name )
               .Select( c => c.Value.Value )
               .FirstOrDefault();

            var apiKey = component.GetAttributeValue( "APIKey" );

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add( "Authorization", "Bearer " + apiKey );
            client.DefaultRequestHeaders.Add( "Rise-API-Version", "2020-07-16" );
            return await client.GetStringAsync( resource );
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
