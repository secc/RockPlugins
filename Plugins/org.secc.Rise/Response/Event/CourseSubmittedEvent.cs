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
using System;
using Newtonsoft.Json;
using org.secc.Rise.Model;
using org.secc.xAPI.Model;
using Rock.Data;
using Rock.Web.Cache;

namespace org.secc.Rise.Response.Event
{
    public class CourseSubmittedEvent : WebhookEventBase
    {
        [JsonProperty( "data" )]
        public CourseSubmittedData Data { get; set; }

        internal void Sync()
        {
            var person = Data.Submitter.GetRockPerson();
            if ( person == null )
            {
                throw new Exception( "Could not find Rock person for Rise Id: " + Data.Submitter.Id );
            }

            RiseClient riseClient = new RiseClient();
            var course = riseClient.SyncCourse( Data.Course );


            RockContext rockContext = new RockContext();
            ExperienceService experienceService = new ExperienceService( rockContext );

            var experience = new Experience
            {
                PersonAliasId = person.PrimaryAliasId ?? 0,
                VerbValueId = xAPI.Utilities.VerbHelper.GetOrCreateVerb( "http://activitystrea.ms/schema/1.0/author" ).Id,
                xObject = new ExperienceObject
                {
                    EntityTypeId = EntityTypeCache.Get( typeof( Course ) ).Id,
                    ObjectId = course.Id.ToString()
                },
                Result = new ExperienceResult
                {
                    IsComplete = true,
                    WasSuccess = true
                }
            };

            experienceService.Add( experience );
            rockContext.SaveChanges();
        }
    }
}
