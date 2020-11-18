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
using System.Data.Entity;
using System.Linq;
using Newtonsoft.Json;
using org.secc.Rise.Model;
using org.secc.xAPI.Model;
using Rock;
using Rock.Data;
using Rock.Web.Cache;

namespace org.secc.Rise.Response.Event
{
    public class CourseCompletedEvent : WebhookEventBase
    {
        [JsonProperty( "data" )]
        public CourseCompletedData Data { get; set; }

        internal void Sync()
        {
            var person = Data.User.GetRockPerson();
            if ( person == null )
            {
                throw new Exception( "Could not find Rock person for Rise Id: " + Data.User.Id );
            }

            var course = Data.Course.GetRockCourse();
            var courseEntityTypeId = EntityTypeCache.Get( typeof( Course ) ).Id;
            var attemptExtension = xAPI.Utilities.ExtensionHelper.GetOrCreateExtension( "http://id.tincanapi.com/extension/attempt-id" );

            RockContext rockContext = new RockContext();
            ExperienceService experienceService = new ExperienceService( rockContext );
            ExperienceQualifierService experienceQualifierService = new ExperienceQualifierService( rockContext );

            //Rise (for some reason) will repeatedly send a course completion event.
            //Check to see if this course completed even has already occurred.
            var existingExperiences = experienceService.Queryable().AsNoTracking()
                .Where( e => e.PersonAlias.PersonId == person.Id
                    && e.xObject.EntityTypeId == courseEntityTypeId
                    && e.xObject.ObjectId == course.Id.ToString() )
                .ToList();

            foreach ( var instance in existingExperiences )
            {
                var instanceContext = instance.GetQualifier( "context" );
                if ( instanceContext != null )
                {
                    var contextExtensions = instanceContext.GetQualifier( "extensions" );
                    if ( contextExtensions != null )
                    {
                        var attemptId = contextExtensions.GetQualifier( attemptExtension.Value );
                        if ( attemptId != null && attemptId.Value == Id )
                        {
                            return;
                        }
                    }
                }
            }

            rockContext.WrapTransaction( () =>
            {
                var experience = new Experience
                {
                    PersonAliasId = person.PrimaryAliasId ?? 0,
                    VerbValueId = xAPI.Utilities.VerbHelper.GetOrCreateVerb( "http://activitystrea.ms/schema/1.0/complete" ).Id,
                    xObject = new ExperienceObject
                    {
                        EntityTypeId = courseEntityTypeId,
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

                var experienceTypeId = EntityTypeCache.Get( typeof( Experience ) ).Id;
                var experienceQualifierTypeId = EntityTypeCache.Get( typeof( ExperienceQualifier ) ).Id;

                var context = new ExperienceQualifier
                {
                    EntityTypeId = experienceTypeId,
                    ParentId = experience.Id,
                    Key = "context",
                };

                experienceQualifierService.Add( context );
                rockContext.SaveChanges();

                var extensions = new ExperienceQualifier
                {
                    EntityTypeId = experienceQualifierTypeId,
                    ParentId = context.Id,
                    Key = "extensions"
                };
                experienceQualifierService.Add( extensions );
                rockContext.SaveChanges();

                var attemptIdQualifier = new ExperienceQualifier
                {
                    EntityTypeId = experienceQualifierTypeId,
                    ParentId = extensions.Id,
                    Key = attemptExtension.Value,
                    Value = Id
                };

                experienceQualifierService.Add( attemptIdQualifier );
                rockContext.SaveChanges();
            } );
        }
    }
}
