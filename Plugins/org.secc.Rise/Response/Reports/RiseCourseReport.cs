using System.Runtime.Remoting.Activation;
using Newtonsoft.Json;

namespace org.secc.Rise.Response
{
    [Url( "reports/courses" )]
    public class RiseCourseReport : RiseBase
    {
        [JsonProperty( "userId" )]
        public string UserId { get; set; }

        [JsonProperty( "firstName" )]
        public string FirstName { get; set; }

        [JsonProperty( "lastName" )]
        public string LastName { get; set; }

        [JsonProperty( "learnerReportUrl" )]
        public string LearnerReportUrl { get; set; }

        [JsonProperty( "userUrl" )]
        public string UserUrl { get; set; }

        [JsonProperty( "duration" )]
        public string Duration { get; set; }

        [JsonProperty( "quizScorePercent" )]
        public int? QuizScorePercent { get; set; }

        [JsonProperty( "dueAt" )]
        public string DueAt { get; set; }

        [JsonProperty( "status" )]
        public string Status { get; set; }

    }
}
