using Rock.Plugin;

namespace org.secc.Jobs.Migrations
{
    [MigrationNumber(1, "1.13.0")]
    public partial class AddOtherPhoneTypes : Migration
    {
        public override void Up()
        {
            RockMigrationHelper.UpdateDefinedValue( Rock.SystemGuid.DefinedType.PERSON_PHONE_TYPE, "Other1", "", "f5e13eeb-a98e-4bf8-a60f-14e4dc0ae662" );
            RockMigrationHelper.UpdateDefinedValue( Rock.SystemGuid.DefinedType.PERSON_PHONE_TYPE, "Other2", "", "48f77164-a742-45fb-beec-7d91c81442de" );

        }

        public override void Down()
        {
            //blank
        }
    }
}
