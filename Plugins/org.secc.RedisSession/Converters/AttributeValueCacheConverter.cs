using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rock.Web.Cache;
using Rock.Web.UI;

namespace org.secc.RedisSession.Converters
{

    public class AttributeValueCacheConverter : Newtonsoft.Json.JsonConverter
    {
        private readonly Type[] _types;
        public AttributeValueCacheConverter( params Type[] types )
        {
            _types = types;
        }

        public override void WriteJson( JsonWriter writer, object value, JsonSerializer serializer )
        {
            AttributeValueCache avc;
            if ( value is AttributeValueCache )
            {
                avc = ( AttributeValueCache ) value;
            }
            else
            {
                return;
            }
            var analog = new AttributeValueCacheAnalog() { AttributeId = avc.AttributeId, EntityId = avc.EntityId, Value = avc.Value };

            serializer.Serialize( writer, analog );
        }
        public override object ReadJson( JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer )
        {
            var deserializedobject = ( AttributeValueCacheAnalog ) serializer.Deserialize( reader, typeof( AttributeValueCacheAnalog ) );
            return new AttributeValueCache() { AttributeId = deserializedobject.AttributeId, EntityId = deserializedobject.EntityId, Value = deserializedobject.Value };
        }
        public override bool CanConvert( Type objectType )
        {
                return _types.Any( t => t == objectType );
        }
        public class AttributeValueCacheAnalog
        {
            public int AttributeId { get; set; }

            public int? EntityId { get; set; }

            public string Value { get; set; }

        }

    }

}
