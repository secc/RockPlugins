// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License shoud be included with this file.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System.Linq;
using Newtonsoft.Json;
using org.secc.Rise.Model;
using Rock.Data;
using Rock.Model;

namespace org.secc.Rise.Response.Event
{
    public class EnrollmentsCreatedEvent : WebhookEventBase
    {
        [JsonProperty( "data" )]
        public EnrollmentsCreatedData Data { get; set; }

        internal void Sync()
        {
            RockContext rockContext = new RockContext();
            CourseService courseService = new CourseService( rockContext );
            GroupService groupService = new GroupService( rockContext );

            var course = Data.Course.GetRockCourse( courseService );

            //fun fact: the rise webhook only handles enrolling not unenrolling :/
            foreach ( var riseGroup in Data.Groups )
            {
                var group = riseGroup.GetRockGroup( groupService );
                if ( !course.EnrolledGroups.Select( g => g.Id ).Contains( group.Id ) )
                {
                    course.EnrolledGroups.Add( group );
                }
            }
            rockContext.SaveChanges();
        }
    }
}
