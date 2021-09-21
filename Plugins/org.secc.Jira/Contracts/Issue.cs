using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace org.secc.Jira.Contracts
{
    public class Issue
    {
        [JsonProperty( "id" )]
        public int Id { get; set; }

        [JsonProperty( "key" )]
        public string Key { get; set; }

        [JsonProperty( "fields" )]
        public Fields Fields { get; set; }
    }
}
