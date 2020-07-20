using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rock.CheckIn;
using static org.secc.RedisSession.Converters.CheckInStateConverter;

namespace org.secc.RedisSession
{
    public static class CheckInStateSessionManager
    {
        private static ConcurrentDictionary<string, CheckInStateAnalog> Data { get; set; } = new ConcurrentDictionary<string, CheckInStateAnalog>();

        public static void Set( CheckInStateAnalog stateAnalog )
        {
            Data.AddOrUpdate( stateAnalog.LocalKey, stateAnalog, ( l, analog ) => stateAnalog );
            Clean();
        }

        public static CheckInStateAnalog Get( string localKey )
        {
            CheckInStateAnalog stateAnalog;
            Data.TryGetValue( localKey, out stateAnalog );
            return stateAnalog;
        }

        public static void Clean()
        {
            Task.Run( () =>
            {
                var keys = Data.Keys;
                foreach(var key in keys )
                {
                    var item = Get( key );
                    if (item.ExpiresDateTime < Rock.RockDateTime.Now )
                    {
                        Data.TryRemove( key, out item );
                    }
                }
            }
            );
        }
    }
}
