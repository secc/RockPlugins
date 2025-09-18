using Rock.Plugin;

namespace org.secc.Migrations
{
    [MigrationNumber( 19, "1.12.0" )]
    internal class GroupAppStudentGroupsMigration : Migration
    {
        public override void Up()
        {
            // Add a defined type for the group types to be displayed in the Group App
            RockMigrationHelper.AddDefinedType(
                "Group",
                "Group App Student Group Types",
                "Group types that are used for student groups",
                "3780965b-3da0-4609-9577-cf8d39ec601a"
            );
        }

        public override void Down()
        {
            // Remove the defined type for the group types
            RockMigrationHelper.DeleteDefinedType( "3780965b-3da0-4609-9577-cf8d39ec601a" );
        }
    }
}
