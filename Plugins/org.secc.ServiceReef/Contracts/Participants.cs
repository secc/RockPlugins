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
    class Participants
    {
        public PageInfo PageInfo { get; set; }
        public List<Result> Results { get; set; }

        public class Result
        {
            public string RegistrationStatus { get; set; }
            public double ParticipantFundraisingGoal { get; set; }
            public double AmountRaised { get; set; }
            public double BalanceDue { get; set; }
            public DateTime DateRegistered { get; set; }
            public List<ApplicationAnswer> MasterApplicationAnswers { get; set; }
            public List<ApplicationAnswer> ApplicationAnswers { get; set; }
            public DateTime BackgroundCheckDate { get; set; }
            public int UserId { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }
            public string Phone { get; set; }
            public Address Address { get; set; }
            public string ProfileUrl { get; set; }
            public string PassportStatus { get; set; }
            public string FirstNameOnPassport { get; set; }
            public string MiddleNameOnPassport { get; set; }
            public string LastNameOnPassport { get; set; }
            public string FullNameOnPassport { get; set; }
            public string PassportSex { get; set; }
            public string PassportPlaceOfBirth { get; set; }
            public DateTime PassportDateOfBirth { get; set; }
            public string PassportNumber { get; set; }
            public DateTime PassportIssued { get; set; }
            public string PassportIssuedBy { get; set; }
            public DateTime PassportExpirationDate { get; set; }
            public List<object> IntegrationIdentifiers { get; set; }
            public int FellowshipOnePersonId { get; set; }

            public class ApplicationAnswer
            {
                public int QuestionId { get; set; }
                public string Question { get; set; }
                public string Answer { get; set; }
            }
        }
    }
}
