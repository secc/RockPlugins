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
