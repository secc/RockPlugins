using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Activation;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace org.secc.Rise.Response
{
    [Url( "users" )]
    public class RiseUser : RiseBase
    {

        [JsonProperty( "email" )]
        public string Email { get; set; }

        [JsonProperty( "role" )]
        public string Role { get; set; }

        [JsonProperty( "firstName" )]
        public string FirstName { get; set; }

        [JsonProperty( "lastName" )]
        public string LastName { get; set; }

        [JsonProperty( "learnerReportUrl" )]
        public string LearnerReportUrl { get; set; }

        [JsonProperty( "url" )]
        public string Url { get; set; }

        [JsonIgnore]
        public List<RiseGroup> Groups
        {
            get
            {
                return ClientManager.GetSet<RiseGroup>( this ).ToList();
            }
        }

        [JsonIgnore]
        public List<RiseLearnerCourse> CourseReports
        {
            get
            {
                return ClientManager.GetSet<RiseLearnerCourse>( this.Id ).ToList();
            }
        }

    }
}
