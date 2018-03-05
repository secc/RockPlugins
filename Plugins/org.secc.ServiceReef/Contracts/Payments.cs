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
    class Payments
    {
        public PageInfo PageInfo { get; set; }
        public List<Result> Results { get; set; }

        public class Result
        {
            public int TransactionId { get; set; }
            public Int32 UserId { get; set; }
            public String FirstName { get; set; }
            public String LastName { get; set; }
            public String Email { get; set; }
            public object UserProfileUrl { get; set; }
            public Address Address { get; set; }
            public int EventId { get; set; }
            public string EventName { get; set; }
            public String EventCode { get; set; }
            public string EventUrl { get; set; }
            public Int32 DonatedToUserId { get; set; }
            public string DonatedToFirstName { get; set; }
            public string DonatedToLastName { get; set; }
            public string Description { get; set; }
            public string Type { get; set; }
            public string Method { get; set; }
            public double TransactionFees { get; set; }
            public double Amount { get; set; }
            public DateTime Date { get; set; }
            public object CCLast4 { get; set; }
            public string PaymentProcessorName { get; set; }
            public String PaymentProcessorTransactionId { get; set; }
            public object PaymentProcessorBatchId { get; set; }
        }
    }
}
