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
using System.Collections.Generic;
using Newtonsoft.Json;

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

            var attemptExtension = xAPI.Utilities.ExtensionHelper.GetOrCreateExtension( "http://id.tincanapi.com/extension/attempt-id" );
            var dateTimeExtension = xAPI.Utilities.ExtensionHelper.GetOrCreateExtension( "http://id.tincanapi.com/extension/datetime" );

            var contextExtentions = new Dictionary<string, string> {
                { attemptExtension.Value, Id },
                { dateTimeExtension.Value, CreatedAtServerTime.ToString() } };

            course.UpdateCourseStatus( person, true, null, contextExtentions );
        }
    }
}
