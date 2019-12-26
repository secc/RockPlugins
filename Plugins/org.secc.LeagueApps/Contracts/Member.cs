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

namespace org.secc.LeagueApps.Contracts
{
    class Member
    {
        public int userId { get; set; }
        public string email { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        private DateTime? _birthDate = null;
        [JsonConverter( typeof( MillisecondEpochConverter ) )]
        public DateTime? birthDate {
            get {
                return _birthDate;
            }
            set {
                if ( value.HasValue )
                {
                    _birthDate = value.Value.Date;
                }
            }
        }
        [JsonConverter( typeof( MillisecondEpochConverter ) )]
        public DateTime dateJoined { get; set; }
        public string gender { get; set; }
        public string address1 { get; set; }
        public string address2 { get; set; }
        public string city { get; set; }
        public string state { get; set; }
        public string country { get; set; }
        public string zipCode { get; set; }
    }
}
