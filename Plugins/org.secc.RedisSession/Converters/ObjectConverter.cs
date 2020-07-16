using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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
            foreach(var converter in serializer.Converters )
            {
                // Don't include the Object Converter at this level
                if ( converter.GetType() != GetType())
                {
                    serializerSettings.Converters.Add( converter );
                }
            }

            SessionObject sessionObject = new SessionObject();
            sessionObject.Type = value.GetType();
            sessionObject.Data = JsonConvert.SerializeObject( writer, serializerSettings );

            // Now put this into the original 
            serializer.Serialize( writer, sessionObject );
                

        }
        public override object ReadJson( JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer )
        {
            var deserializedobject = ( SessionObject ) serializer.Deserialize( reader, typeof( SessionObject ) );
            if ( deserializedobject != null )
            {
                return serializer.Deserialize( new JsonTextReader( new StringReader( deserializedobject.Data ) ), deserializedobject.Type );
            }
            return null;

        }
        public override bool CanConvert( Type objectType )
        {
            return _types.Any( t => t == objectType );
        }
    }

}
