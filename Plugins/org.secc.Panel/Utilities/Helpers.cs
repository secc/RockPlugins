using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.secc.Panel.Utilities
{
    public static class Helpers
    {
        private static readonly DateTime epoch = new DateTime( 1970, 1, 1, 0, 0, 0, DateTimeKind.Utc );
        public static DateTime FromUnixTime( long date )
        {
            return epoch.AddSeconds( date );
        }
    }
}
