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

namespace org.secc.PayPalReporting.Migrations
{
    [MigrationNumber(2, "1.0.2")]
    class CreateAccountType : Migration
    {
        /// <summary>
        /// The commands to run to migrate plugin to the specific version
        /// </summary>
        public override void Up()
        {
            RockMigrationHelper.AddDefinedValue(
                Rock.SystemGuid.DefinedType.FINANCIAL_ACCOUNT_TYPE,
                "Service Reef",
                "Transactions that originated from ServiceReef.",
                "51DC439B-2931-47CE-8FA8-C6DA1451B633"
            );
        }

    /// <summary>
    /// The commands to undo a migration from a specific version
    /// </summary>
    public override void Down()
        {
            RockMigrationHelper.DeleteDefinedValue("51DC439B-2931-47CE-8FA8-C6DA1451B633");
           
        }
    }
}
