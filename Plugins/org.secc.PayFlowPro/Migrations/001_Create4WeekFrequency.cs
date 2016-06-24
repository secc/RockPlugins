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
