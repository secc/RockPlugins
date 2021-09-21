using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace org.secc.Jira.Contracts
{
    public class Fields
    {
        [JsonProperty( "created" )]
        public DateTime? Created { get; set; }

        [JsonProperty( "resolutiondate" )]
        public DateTime? ResolutionDate { get; set; }

        [JsonProperty( "issuetype" )]
        public IssueType IssueType { get; set; }

        [JsonProperty( "summary" )]
        public string Summary { get; set; }

        [JsonProperty( "description" )]
        public string Description { get; set; }
    }
}
