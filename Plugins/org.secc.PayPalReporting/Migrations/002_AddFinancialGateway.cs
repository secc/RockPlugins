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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.secc.PayPalReporting.Migrations
{
    [MigrationNumber(2, "1.0.1")]
    class AddFinancialGateway : Migration
    {
        /// <summary>
        /// The commands to run to migrate plugin to the specific version
        /// </summary>
        public override void Up()
        {
            Sql( @"
            ALTER TABLE dbo._org_secc_PayPalReporting_Transaction ADD
	            FinancialGatewayId int NULL
            ALTER TABLE dbo._org_secc_PayPalReporting_Transaction ADD CONSTRAINT
	            FK__org_secc_PayPalReporting_Transaction_FinancialGateway FOREIGN KEY
	            (
	            FinancialGatewayId
	            ) REFERENCES dbo.FinancialGateway
	            (
	            Id
	            ) ON UPDATE  NO ACTION 
	             ON DELETE  NO ACTION 
            " );


        }

    /// <summary>
    /// The commands to undo a migration from a specific version
    /// </summary>
    public override void Down()
        {
            Sql( @"
            ALTER TABLE [dbo].[_org_secc_PayPalReporting_Transaction] DROP CONSTRAINT [FK__org_secc_PayPalReporting_Transaction_FinancialGateway]
            ALTER TABLE [dbo].[_org_secc_PayPalReporting_Transaction] DROP COLUMN [FinancialGatewayId]
            " );
        }
    }
}
