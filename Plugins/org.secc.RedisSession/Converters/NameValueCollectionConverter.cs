using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace org.secc.RedisSession.Converters
{

    public class NameValueCollectionConverter : Newtonsoft.Json.JsonConverter
    {
        private readonly Type[] _types;
        public NameValueCollectionConverter( params Type[] types )
        {
            _types = types;
        }

        public override void WriteJson( JsonWriter writer, object value, JsonSerializer serializer )
        {
            NameValueCollection nvc;
            if ( value is NameValueCollection )
            {
                nvc = ( NameValueCollection ) value;
            }
            else
            {
                return;
            }

            serializer.Serialize( writer, nvc.AllKeys.ToDictionary( k => k, k => nvc.GetValues( k ) ) );
        }
        public override object ReadJson( JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer )
        {

            var deserializedobject = ( Dictionary<string, string[]> ) serializer.Deserialize( reader, typeof( Dictionary<string, string[]> ) );
            NameValueCollection nvc = new NameValueCollection();
            foreach ( var strCol in deserializedobject.Values )
                foreach ( var str in strCol )
                {
                    nvc.Add( deserializedobject.FirstOrDefault( x => x.Value.Contains( str ) ).Key, str );
                }
            return nvc;
        }
        public override bool CanConvert( Type objectType )
        {
            return _types.Any( t => t == objectType );
        }
    }

}
