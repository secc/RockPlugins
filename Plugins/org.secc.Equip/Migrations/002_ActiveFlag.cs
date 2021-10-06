namespace org.secc.Equip.Migrations
{
    using Rock.Plugin;

    [MigrationNumber( 2, "1.8.0" )]
    public partial class ActiveFlag : Migration
    {
        public override void Up()
        {
            AddColumn( "dbo._org_secc_Equip_Course", "IsActive", c => c.Boolean() );
        }
        
        public override void Down()
        {
            DropColumn( "dbo._org_secc_Equip_Course", "IsActive" );
        }
    }
}
