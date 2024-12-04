using Rock.Plugin;

namespace org.secc.Migrations.GroupApp
{
    [MigrationNumber( 18, "1.12.0" )]
    internal class GroupAppTableBasedGroupsMigration : Migration
    {
        public override void Up()
        {
            // Add a defined type for the group types to be displayed in the Group App
            RockMigrationHelper.AddDefinedType(
                "Group",
                "Group App Table-Based Group Types",
                "Group types that are used for table-based groups",
                "90526a36-fda6-4c90-997c-636b82b793d8"
            );
        }

        public override void Down()
        {
            // Remove the defined type for the group types
            RockMigrationHelper.DeleteDefinedType( "90526a36-fda6-4c90-997c-636b82b793d8" );
        }
    }
}
