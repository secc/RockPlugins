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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.secc.ServiceReef.Contracts
{
    public class Member
    {
        public DateTime Birthdate { get; set; }
        public object Website { get; set; }
        public object Facebook { get; set; }
        public object Twitter { get; set; }
        public string ThumbUrl { get; set; }
        public string ImageUrl { get; set; }
        public List<Trip> Trips { get; set; }
        public List<object> MasterApplicationAnswers { get; set; }
        public List<string> Badges { get; set; }
        public int ArenaId { get; set; }
        public object FacebookId { get; set; }
        public int UserId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public object Phone { get; set; }
        public Address Address { get; set; }
        public object BackgroundCheckDate { get; set; }
        public string ProfileUrl { get; set; }

        public class Trip
        {
            public int EventId { get; set; }
            public string Name { get; set; }
            public string Url { get; set; }
            public string StartDate { get; set; }
        }
    }
}
