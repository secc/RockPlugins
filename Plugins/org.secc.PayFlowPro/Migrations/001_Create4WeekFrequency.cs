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

namespace org.secc.PayFloPro.Migrations
{
    [MigrationNumber( 1, "1.0.0" )]
    class CreateCurrencyType : Migration
    {
        /// <summary>
        /// The commands to run to migrate plugin to the specific version
        /// </summary>
        public override void Up()
        {
            RockMigrationHelper.AddDefinedValue( Rock.SystemGuid.DefinedType.FINANCIAL_FREQUENCY, "Every 4 Weeks", "Every 4 Weeks", org.secc.PayFlowPro.Gateway.TRANSACTION_FREQUENCY_FOUR_WEEKS );
        }

        /// <summary>
        /// The commands to undo a migration from a specific version
        /// </summary>
        public override void Down()
        {
            RockMigrationHelper.DeleteDefinedValue( org.secc.PayFlowPro.Gateway.TRANSACTION_FREQUENCY_FOUR_WEEKS );
        }
    }
}
