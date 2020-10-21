using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Activation;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace org.secc.Rise.Response
{
    [Url( "reports/learners" )]
    public class RiseLearnerCourse : RiseBase
    {

        [JsonProperty( "courseId" )]
        public string CourseId { get; set; }

        [JsonProperty( "courseTitle" )]
        public string CourseTitle { get; set; }

        [JsonProperty( "courseReportUrl" )]
        public string CourseReportUrl { get; set; }

        [JsonProperty( "courseUrl" )]
        public string CourseUrl { get; set; }

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
