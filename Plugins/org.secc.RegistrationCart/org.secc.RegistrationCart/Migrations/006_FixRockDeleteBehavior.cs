// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License should be included with this file.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using Rock.Plugin;

namespace org.secc.RegistrationCart.Migrations
{
    /// <summary>
    /// Lets Rock delete what the cart points at. Migration 001 made every foreign key to a Rock
    /// table NO ACTION, so deleting a registration, instance, template, fee or transaction that a
    /// cart had ever used failed with a SQL reference error. Now:
    ///   - deleting an instance deletes its cart lines;
    ///   - deleting a registration or a transaction clears the cart's link to it;
    ///   - deleting a template clears an offering-scoped discount code's template;
    ///   - a cart add-on no longer references the fee item (its name and price are snapshotted).
    /// SQL Server allows only one cascade path into a table: template → instance → line → add-on
    /// is that path, so the add-on's fee item key is dropped rather than cascaded (template →
    /// fee → fee item → add-on would be a second one), and the line's template key stays NO
    /// ACTION, which SQL Server checks after the instance cascade has removed the lines.
    /// </summary>
    [MigrationNumber( 6, "1.16.0" )]
    public class FixRockDeleteBehavior : Migration
    {
        public override void Up()
        {
            Sql( ReplaceFk( "CartLineItem", "RegistrationInstanceId", "RegistrationInstance", "CASCADE" ) );
            Sql( ReplaceFk( "CartLineItem", "RegistrationId", "Registration", "SET NULL" ) );
            Sql( ReplaceFk( "Cart", "FinancialTransactionId", "FinancialTransaction", "SET NULL" ) );
            Sql( ReplaceFk( "DiscountCode", "RegistrationTemplateId", "RegistrationTemplate", "SET NULL" ) );
            Sql( DropFk( "CartLineItemAddOn", "RegistrationTemplateFeeItemId" ) );

            // Seat counts and the instance cascade look lines up by instance.
            Sql( @"
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX__org_secc_RegistrationCart_CartLineItem_Instance')
                    CREATE INDEX [IX__org_secc_RegistrationCart_CartLineItem_Instance]
                        ON [dbo].[_org_secc_RegistrationCart_CartLineItem] ([RegistrationInstanceId]) INCLUDE ([CartId], [IsWaitList]);" );
        }

        public override void Down()
        {
            Sql( @"DROP INDEX IF EXISTS [IX__org_secc_RegistrationCart_CartLineItem_Instance] ON [dbo].[_org_secc_RegistrationCart_CartLineItem];" );

            Sql( ReplaceFk( "CartLineItem", "RegistrationInstanceId", "RegistrationInstance", "NO ACTION" ) );
            Sql( ReplaceFk( "CartLineItem", "RegistrationId", "Registration", "NO ACTION" ) );
            Sql( ReplaceFk( "Cart", "FinancialTransactionId", "FinancialTransaction", "NO ACTION" ) );
            Sql( ReplaceFk( "DiscountCode", "RegistrationTemplateId", "RegistrationTemplate", "NO ACTION" ) );

            // Add-ons whose fee item was deleted in the meantime would fail a checked constraint.
            var table = Table( "CartLineItemAddOn" );
            Sql( $@"
                IF OBJECT_ID(N'[dbo].[FK_{table}_RegistrationTemplateFeeItemId]', 'F') IS NULL
                    ALTER TABLE [dbo].[{table}] WITH NOCHECK ADD CONSTRAINT [FK_{table}_RegistrationTemplateFeeItemId]
                        FOREIGN KEY ([RegistrationTemplateFeeItemId]) REFERENCES [dbo].[RegistrationTemplateFeeItem] ([Id]);" );
        }

        private static string Table( string shortName ) => "_org_secc_RegistrationCart_" + shortName;

        /// <summary>Drops the migration 001 constraint (same name) and adds it back with <paramref name="onDelete"/>.</summary>
        private static string ReplaceFk( string shortName, string column, string referencedTable, string onDelete )
        {
            var table = Table( shortName );
            return $@"
                {DropFk( shortName, column )}
                ALTER TABLE [dbo].[{table}] ADD CONSTRAINT [FK_{table}_{column}]
                    FOREIGN KEY ([{column}]) REFERENCES [dbo].[{referencedTable}] ([Id]) ON DELETE {onDelete};";
        }

        private static string DropFk( string shortName, string column )
        {
            var table = Table( shortName );
            return $@"
                IF OBJECT_ID(N'[dbo].[FK_{table}_{column}]', 'F') IS NOT NULL
                    ALTER TABLE [dbo].[{table}] DROP CONSTRAINT [FK_{table}_{column}];";
        }
    }
}
