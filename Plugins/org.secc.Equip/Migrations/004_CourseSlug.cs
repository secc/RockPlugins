namespace org.secc.Equip.Migrations
{
    using Rock.Plugin;

    [MigrationNumber( 4, "1.8.0" )]
    public partial class CourseSlug : Migration
    {
        public override void Up()
        {
            AddColumn( "dbo._org_secc_Equip_Course", "Slug", c => c.String() );
        }

        public override void Down()
        {
            DropColumn( "dbo._org_secc_Equip_Course", "Slug" );
        }
    }
}
