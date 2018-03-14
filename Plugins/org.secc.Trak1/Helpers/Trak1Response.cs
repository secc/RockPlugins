using System;

namespace org.secc.Trak1.Helpers
{
    public class Trak1Response
    {
        public Guid TransactionId { get; set; }
        public string RedirectUrl { get; set; }
        public ErrorInformation Error { get; set; }
    }
}
