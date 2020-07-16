using System.Collections.Specialized;
using System.Text;
using Newtonsoft.Json;
using org.secc.RedisSession.Converters;
using Rock.CheckIn;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;

namespace org.secc.RedisSession
{
    class Serializer : Microsoft.Web.Redis.ISerializer
    {

        JsonSerializerSettings serializerSettings = new JsonSerializerSettings();

        public Serializer()
        {
            serializerSettings.Converters.Add( new NameValueCollectionConverter( typeof( NameValueCollection ) ) );
            serializerSettings.Converters.Add( new AttributeValueCacheConverter( typeof( BreadCrumb ) ) );
            serializerSettings.Converters.Add( new CheckInStateConverter( typeof( CheckInState ) ) );
            serializerSettings.Converters.Add( new AttributeValueCacheConverter( typeof( AttributeValueCache ) ) );
            serializerSettings.Converters.Add( new CacheItemConverter<AttributeCache, Rock.Model.Attribute>( typeof( AttributeCache ) ) );
            serializerSettings.Converters.Add( new CacheItemConverter<DefinedTypeCache, DefinedType>( typeof( DefinedTypeCache ) ) );
            serializerSettings.Converters.Add( new CacheItemConverter<DefinedValueCache, DefinedValue>( typeof( DefinedValueCache ) ) );

        }

        public byte[] Serialize( object data )
        {
            if ( data == null )
            {
                data = new RedisNull();
            }

            SessionObject sessionObject = new SessionObject();
            sessionObject.Type = data.GetType();
            sessionObject.Data = JsonConvert.SerializeObject( data, serializerSettings );
            return Encoding.ASCII.GetBytes( JsonConvert.SerializeObject( sessionObject ) );
        }

        public object Deserialize( byte[] data )
        {
            if ( data == null )
            {
                return null;
            }
            SessionObject sessionObject = ( SessionObject ) JsonConvert.DeserializeObject( Encoding.ASCII.GetString( data, 0, data.Length ), typeof( SessionObject ) );
            object retObject = ( object ) JsonConvert.DeserializeObject( sessionObject.Data, sessionObject.Type, serializerSettings );
            if ( retObject.GetType() == typeof( RedisNull ) )
            {
                return null;
            }
            return retObject;
        }
    }
}
