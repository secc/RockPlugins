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
using Newtonsoft.Json.Converters;

namespace org.secc.LeagueApps.Contracts
{
    public class Programs
    {
        public int programId { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        [JsonConverter( typeof( MillisecondEpochConverter ) )]
        public DateTime startTime { get; set; }
        [JsonConverter( typeof( MillisecondEpochConverter ) )]
        public DateTime publicRegistrationTime { get; set; }
        public string gender { get; set; }
        public string mode { get; set; }
        public string sport { get; set; }
        public string season { get; set; }
        public string experienceLevel { get; set; }
        [JsonConverter( typeof( MillisecondEpochConverter ) )]
        public DateTime ageLimitEffectiveDate { get; set; }
        public string programUrlHtml { get; set; }
        public string registerUrlHtml { get; set; }
        public string scheduleUrlHtml { get; set; }
        public string standingsUrlHtml { get; set; }
        public string programLogo150 { get; set; }
    }

    public class MillisecondEpochConverter : DateTimeConverterBase
    {
        private static readonly DateTime _epoch = new DateTime( 1970, 1, 1, 0, 0, 0, DateTimeKind.Utc );

        public override void WriteJson( JsonWriter writer, object value, JsonSerializer serializer )
        {
            writer.WriteRawValue( ( ( DateTime ) value - _epoch ).TotalMilliseconds.ToString() );
        }

        public override object ReadJson( JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer )
        {
            if ( reader.Value == null )
            { return null; }
            return _epoch.AddMilliseconds( ( long ) reader.Value );
        }
    }

}
