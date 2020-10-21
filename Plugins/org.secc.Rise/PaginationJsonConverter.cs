using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using org.secc.Rise.Response;

namespace org.secc.Rise
{
    public class PaginationJsonConverter<T> : JsonConverter
    {
        private readonly Type[] _types;

        public PaginationJsonConverter( params Type[] types )
        {
            _types = types;
        }

        public override void WriteJson( JsonWriter writer, object value, JsonSerializer serializer )
        {

        }

        public override object ReadJson( JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer )
        {
            var pagination = new Pagination<T>();
            JObject items = JObject.Load( reader );

            foreach ( var item in items )
            {
                if ( item.Key == "nextUrl" )
                {
                    pagination.NextUrl = item.Value.ToString();
                }
                else if ( ClientManager.PaginableProperties.Contains( item.Key.ToLower() ) )
                {
                    try
                    {
                        var payload = JsonConvert.DeserializeObject<List<T>>( item.Value.ToString() );
                        pagination.Resources = payload;
                    }
                    catch ( Exception e )
                    {
                        Console.WriteLine( e );
                    }
                }
            }

            return pagination;
        }

        public override bool CanRead
        {
            get { return true; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override bool CanConvert( Type objectType )
        {
            return true;
        }
    }
}
