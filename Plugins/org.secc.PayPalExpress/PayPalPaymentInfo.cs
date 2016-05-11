using Rock.Financial;
using Rock.Web.Cache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.secc.PayPalExpress
{
    class PayPalPaymentInfo : PaymentInfo
    {

        /// <summary>
        /// The paypal express token
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// The payer id from PayPal
        /// </summary>
        public string PayerId { get; set; }

        /// <summary>
        /// Gets the currency type value.
        /// </summary>
        public override DefinedValueCache CurrencyTypeValue
        {
            get { return DefinedValueCache.Read(new Guid(PayPalExpress.Gateway.CURRENCY_TYPE_PAYPAL)); }
        }

    }
}
