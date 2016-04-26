using Rock.Plugin;

namespace org.secc.PayPalReporting.Migrations
{
    [MigrationNumber(1, "1.0.1")]
    class CreateAttributes : Migration
    {
        /// <summary>
        /// The commands to run to migrate plugin to the specific version
        /// </summary>
        public override void Up()
        {
            RockMigrationHelper.AddDefinedValue(
                Rock.SystemGuid.DefinedType.FINANCIAL_SOURCE_TYPE,
                "Service Reef",
                "Transactions that originated from ServiceReef.",
                "9a3e36fa-634e-45e4-9244-d3d21646dba4"
            );
        }

    /// <summary>
    /// The commands to undo a migration from a specific version
    /// </summary>
    public override void Down()
        {
            RockMigrationHelper.DeleteDefinedValue("9a3e36fa-634e-45e4-9244-d3d21646dba4");
           
        }
    }
}
