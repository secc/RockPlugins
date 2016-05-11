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
