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
using System.Collections.Generic;

namespace org.secc.Rise.Utilities
{
    public static class Constants
    {
        public const string COMPONENT_ATTRIBUTE_KEY_APIKEY = "APIKey";
        public const string COMPONENT_ATTRIBUTE_KEY_SHAREDSECRET = "SharedSecret";

        public const string URL_BASE = "https://api.rise.com";
        public const string API_VERSION = "2020-07-16";

        public const string GROUPTYPE_RISE = "8D704DBD-8B1A-4632-BC84-142F96E95A73";

        public const string GROUP_ATTRIBUTE_RISEID = "FBBDC42C-9E6D-4C5B-B09B-575C2A48DFF4";
        public const string GROUP_ATTRIBUTE_KEY_RISEID = "RiseGroupId";
        public const string GROUP_ATTRIBUTE_KEY_RISEURL = "RiseGroupUrl";

        public const string PERSON_ATTRIBUTE_RISEID = "4C6E9479-1119-47DA-AF35-5DD5BE71F8A1";
        public const string PERSON_ATTRIBUTE_KEY_RISEID = "RiseId";

        public const string EVENT_COURSE_COMPLETED = "course.completed";
        public const string EVENT_COURSE_SUBMITTED = "course.submitted";
        public const string EVENT_ENROLLMENTS_CREATED = "enrollments.created";
        public const string EVENT_USER_CREATED = "user.created";

        public static List<string> WEBHOOK_EVENTS = new List<string>()
        {
            EVENT_COURSE_COMPLETED,
            EVENT_COURSE_SUBMITTED,
            EVENT_ENROLLMENTS_CREATED,
            EVENT_USER_CREATED
        };
    }
}
