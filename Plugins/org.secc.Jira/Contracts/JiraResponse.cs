using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace org.secc.Jira.Contracts
{
    public class JiraResponse
    {
        [JsonProperty( "total" )]
        public int Total { get; set; }

        [JsonProperty( "issues" )]
        public List<Issue> Issues { get; set; }
    }
}
