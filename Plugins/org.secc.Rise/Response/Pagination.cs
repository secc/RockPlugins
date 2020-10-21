using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace org.secc.Rise.Response
{
    public class Pagination<T>
    {
        [JsonProperty( "nextUrl" )]
        public string NextUrl { get; set; }

        public List<T> Resources { get; set; }

    }
}
