using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Activation;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace org.secc.Rise.Response
{
    [Url("groups")]
    public class RiseGroup : RiseBase
    {
        [JsonProperty( "name" )]
        public string Name { get; set; }

        [JsonProperty( "url" )]
        public string Url { get; set; }
    }
}
