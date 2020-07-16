using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.secc.RedisSession
{
    public class SessionObject
    {
        public Type Type { get; set; }

        public string Data { get; set; }
    }
}
