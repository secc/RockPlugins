using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rock.Web.UI;

namespace org.secc.RedisSession.Converters
{

    public class BreadCrumbConverter : Newtonsoft.Json.JsonConverter
    {
        private readonly Type[] _types;
        public BreadCrumbConverter( params Type[] types )
        {
            _types = types;
        }

        public override void WriteJson( JsonWriter writer, object value, JsonSerializer serializer )
        {
            List<BreadCrumb> bc;
            if ( value is List<BreadCrumb> )
            {
                bc = ( List<BreadCrumb> ) value;
            }
            else
            {
                return;
            }
            var analog = bc.Select( b => new BreadCrumbAnalog { Active = b.Active, Name = b.Name, Url = b.Url } ).ToList();

            serializer.Serialize( writer, analog );
        }
        public override object ReadJson( JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer )
        {
            var bc = new List<BreadCrumb>();
            var deserializedobject = ( List<BreadCrumbAnalog> ) serializer.Deserialize( reader, typeof( List<BreadCrumbAnalog> ) );
            foreach ( var item in deserializedobject )
            {
                bc.Add( new BreadCrumb( item.Name, item.Url, item.Active ) );
            }
            return bc;
        }
        public override bool CanConvert( Type objectType )
        {
            return _types.Any( t => t == objectType );
        }
        public class BreadCrumbAnalog
        {
            public string Name { get; set; }
            public bool Active { get; set; }
            public string Url { get; set; }
        }

    }

}
