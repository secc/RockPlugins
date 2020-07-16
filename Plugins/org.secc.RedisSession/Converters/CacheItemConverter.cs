using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rock.Data;
using Rock.Web.Cache;
using Rock.Web.UI;

namespace org.secc.RedisSession.Converters
{

    public class CacheItemConverter<T,TT> : Newtonsoft.Json.JsonConverter where T : IEntityCache, new() where TT : Model<TT>, new()
    {
        private readonly Type[] _types;
        public CacheItemConverter( params Type[] types )
        {
            _types = types;
        }

        public override void WriteJson( JsonWriter writer, object value, JsonSerializer serializer )
        {
            T cacheItem;
            if ( value is T )
            {
                cacheItem = ( T ) value;
            }
            else
            {
                return;
            }
            var analog = new CacheItemAnalog() { Id = cacheItem.Id };

            serializer.Serialize( writer, analog );
        }
        public override object ReadJson( JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer )
        {
            var deserializedobject = ( CacheItemAnalog ) serializer.Deserialize( reader, typeof( CacheItemAnalog ) );
            return ModelCache<T, TT>.Get( deserializedobject.Id );
        }
        public override bool CanConvert( Type objectType )
        {
            return _types.Any( t => t == objectType );
        }
        public class CacheItemAnalog
        {
            public int Id { get; set; }

        }

    }

}
