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
