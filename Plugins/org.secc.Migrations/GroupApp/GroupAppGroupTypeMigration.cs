using Rock.Plugin;

namespace org.secc.Migrations
{
    [MigrationNumber( 17, "1.12.0" )]
    internal class GroupAppGroupTypeMigration : Migration
    {
        public override void Up()
        {
            // Add a defined type for the group types to be displayed in the Group App
            RockMigrationHelper.AddDefinedType(
                "Group",
                "Group App Group Types",
                "Defined types for the group types to be displayed in the Group App",
                "f75bdfa7-582b-4e0d-9715-5e47b0eb57cf"
            );
        }

        public override void Down()
        {
            // Remove the defined type for the group types
            RockMigrationHelper.DeleteDefinedType( "f75bdfa7-582b-4e0d-9715-5e47b0eb57cf" );
        }
    }
}
