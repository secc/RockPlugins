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
    class Events
    {
        public PageInfo PageInfo { get; set; }
        public List<Result> Results { get; set; }

        public class Result
        {
            public int EventId { get; set; }
            public string Name { get; set; }
            public string Url { get; set; }
            public int OrganizationId { get; set; }
            public string OrganizationName { get; set; }
            public string OrganizationProfileUrl { get; set; }
            public string ContactName { get; set; }
            public string ContactEmail { get; set; }
            public double Cost { get; set; }
            public string Description { get; set; }
            public DateTime StartDate { get; set; }
            public DateTime EndDate { get; set; }
            public DateTime RegistrationDeadline { get; set; }
            public DateTime RegistrationStartDate { get; set; }
            public DateTime DatePublished { get; set; }
            public DateTime AvailableDate { get; set; }
            public DateTime DonationDeadlineDate { get; set; }
            public int MaxRegistrations { get; set; }
            public object Level { get; set; }
            public object Visibility { get; set; }
            public object Status { get; set; }
            public object Type { get; set; }
            public int ParentProjectId { get; set; }
            public int SpotsLeft { get; set; }
            public string PictureThumbUrl { get; set; }
            public Address Address { get; set; }
            public List<int> ThirdPartyTags { get; set; }
        }
    }
}
