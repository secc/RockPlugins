using System;
using Newtonsoft.Json;

namespace org.secc.Jira.Contracts
{
    public class JiraIssue
    {
        [JsonIgnore]
        public DateTime DateTime { get; set; }
        public string Date { get => DateTime.ToString( "yyyy-MM-dd" ); }
        public int Opened { get; set; }
        public int Closed { get; set; }
    }
}
