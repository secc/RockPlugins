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
