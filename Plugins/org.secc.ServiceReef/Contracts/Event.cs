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
    public class Event
    {
        public DateTime PaymentDueDate { get; set; }
        public string TimeZone { get; set; }
        public bool ApprovalRequired { get; set; }
        public bool ApplicationEnabled { get; set; }
        public bool UseMasterApplication { get; set; }       
        public int OrganizationFormId { get; set; }
        public double ApplicationFee { get; set; }
        public bool AcceptFundRaising { get; set; }
        public double FundRaisingGoal { get; set; }
        public List<object> Media { get; set; }
        public int RegistrantsWaitingApproval { get; set; }
        public int ApprovedRegistrants { get; set; }
        public int Stories { get; set; }
        public double TotalAmountCollected { get; set; }
        public double TotalAmountEarned { get; set; }
        public List<object> ApplicationQuestions { get; set; }
        public List<object> IntegrationIdentifiers { get; set; }
        public List<CategorySimple> Categories { get; set; }
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

        public class CategorySimple
        {
            public int CategoryId { get; set; }
            public string Name { get; set; }
            public List<CategoryOption> Options { get; set; }

            public class CategoryOption
            {
                public int CategoryOptionId { get; set; }
                public string Name { get; set; }
            }
        }
    }
}
