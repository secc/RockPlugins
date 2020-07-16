using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rock.Web.Cache;

namespace org.secc.RedisSession.Converters
{

    public class ObjectConverter : Newtonsoft.Json.JsonConverter
    {
        private readonly Type[] _types;
        public ObjectConverter( params Type[] types )
        {
            _types = types;
        }

        public override void WriteJson( JsonWriter writer, object value, JsonSerializer serializer )
        {
            JsonSerializerSettings serializerSettings = new JsonSerializerSettings();
            serializerSettings.Converters.Add( new AttributeValueCacheConverter( typeof( AttributeValueCache ) ) );
            serializerSettings.Converters.Add( new CacheItemConverter<AttributeCache, Rock.Model.Attribute>( typeof( AttributeCache ) ) );
            serializerSettings.Converters.Add( new CacheItemConverter<DefinedTypeCache, Rock.Model.DefinedType>( typeof( DefinedTypeCache ) ) );
            serializerSettings.Converters.Add( new CacheItemConverter<DefinedValueCache, Rock.Model.DefinedValue>( typeof( DefinedValueCache ) ) );

            foreach ( var converter in serializer.Converters )
            {
                // Don't include the Object Converter at this level
                if ( converter.GetType() != GetType() )
                {
                    serializerSettings.Converters.Add( converter );
                }
            }

            SessionObject sessionObject = new SessionObject();
            sessionObject.Type = value.GetType();
            sessionObject.Data = JsonConvert.SerializeObject( value, serializerSettings );

            // Now put this into the original 
            serializer.Serialize( writer, sessionObject );


        }
        public override object ReadJson( JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer )
        {
            var deserializedobject = ( SessionObject ) serializer.Deserialize( reader, typeof( SessionObject ) );
            if ( deserializedobject != null && deserializedobject.Data != null )
            {
                JsonSerializerSettings serializerSettings = new JsonSerializerSettings();
                serializerSettings.Converters.Add( new AttributeValueCacheConverter( typeof( AttributeValueCache ) ) );
                serializerSettings.Converters.Add( new CacheItemConverter<AttributeCache, Rock.Model.Attribute>( typeof( AttributeCache ) ) );
                serializerSettings.Converters.Add( new CacheItemConverter<DefinedTypeCache, Rock.Model.DefinedType>( typeof( DefinedTypeCache ) ) );
                serializerSettings.Converters.Add( new CacheItemConverter<DefinedValueCache, Rock.Model.DefinedValue>( typeof( DefinedValueCache ) ) );


                var obj = JsonConvert.DeserializeObject( deserializedobject.Data, deserializedobject.Type, serializerSettings );
                return obj;
            }
            return null;

        }
        public override bool CanConvert( Type objectType )
        {
            return _types.Any( t => t == objectType );
        }
    }

}
