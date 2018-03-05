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
using Rock.Plugin;

namespace org.secc.PayPalExpress.Migrations
{
    [MigrationNumber(1, "1.0.1")]
    class CreateCurrencyType : Migration
    {
        /// <summary>
        /// The commands to run to migrate plugin to the specific version
        /// </summary>
        public override void Up()
        {
            RockMigrationHelper.AddDefinedValue(Rock.SystemGuid.DefinedType.FINANCIAL_CURRENCY_TYPE, "PayPal", "PayPal Express", PayPalExpress.Gateway.CURRENCY_TYPE_PAYPAL);
        }

        /// <summary>
        /// The commands to undo a migration from a specific version
        /// </summary>
        public override void Down()
        {
            RockMigrationHelper.DeleteDefinedValue(PayPalExpress.Gateway.CURRENCY_TYPE_PAYPAL);
        }
    }
}
