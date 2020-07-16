using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rock.CheckIn;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.RedisSession.Converters
{
    public class CheckInStateConverter : Newtonsoft.Json.JsonConverter
    {
        private readonly Type[] _types;
        public CheckInStateConverter( params Type[] types )
        {
            _types = types;
        }

        public override void WriteJson( JsonWriter writer, object value, JsonSerializer serializer )
        {
            serializer.Converters.Add( new ObjectConverter( typeof( Person ), typeof( CheckInStatus ), typeof ( CheckInFamily), typeof(CheckInSchedule), typeof(CheckInPerson), typeof( CheckInLocation ) ) );

            CheckInState state;
            if ( value is CheckInState )
            {
                state = ( CheckInState ) value;
            }
            else
            {
                return;
            }
            var analog = new CheckInStateAnalog
            {
                CheckIn = state.CheckIn,
                CheckinTypeId = state.CheckinTypeId,
                ConfiguredGroupTypes = state.ConfiguredGroupTypes,
                DeviceId = state.DeviceId,
                ManagerLoggedIn = state.ManagerLoggedIn,
                Messages = state.Messages
            };
            serializer.Serialize( writer, analog );
            
        }
        public override object ReadJson( JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer )
        {
            serializer.Converters.Add( new ObjectConverter( typeof( Person ), typeof( CheckInStatus ), typeof( CheckInFamily ), typeof( CheckInSchedule ), typeof( CheckInPerson ), typeof( CheckInLocation ) ) );

            var analog = ( CheckInStateAnalog ) serializer.Deserialize( reader, typeof( CheckInStateAnalog ) );
            var state = new CheckInState( analog.DeviceId, analog.CheckinTypeId, analog.ConfiguredGroupTypes );
            state.CheckInType = new CheckinType( analog.CheckinTypeId ?? 0 );
            state.CheckIn = analog.CheckIn;
            state.ManagerLoggedIn = analog.ManagerLoggedIn;
            state.Messages = analog.Messages;
            return state;
        }
        public override bool CanConvert( Type objectType )
        {
            return _types.Any( t => t == objectType );
        }
        public class CheckInStateAnalog
        {
            public int DeviceId { get; set; }
            public int? CheckinTypeId { get; set; }
            public CheckinType CheckinType { get; set; }
            public bool ManagerLoggedIn { get; set; }
            public List<int> ConfiguredGroupTypes { get; set; }
            public CheckInStatus CheckIn { get; set; }
            public List<CheckInMessage> Messages { get; set; }
        }

    }

}
