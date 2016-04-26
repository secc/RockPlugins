using org.secc.PayPalReporting.Data;
using System.Linq;

namespace org.secc.PayPalReporting.Model
{
    public class TransactionService : PayPalReportingService<Transaction>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionService"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        public TransactionService(PayPalReportingContext context) : base( context ) { }

        public Transaction Get(string GatewayTransactionId)
        {
            return Queryable().FirstOrDefault(t => t.GatewayTransactionId == GatewayTransactionId);
        }
    }
}
