namespace org.secc.Equip.Migrations
{
    using Rock.Plugin;

    [MigrationNumber( 5, "1.8.0" )]
    public partial class AllowedViewers : Migration
    {
        public override void Up()
        {
            AddColumn( "dbo._org_secc_Equip_Course", "AllowedGroupId", c => c.Int( nullable: true ) );
            AddColumn( "dbo._org_secc_Equip_Course", "AllowedDataViewId", c => c.Int( nullable: true ) );
        }

        public override void Down()
        {
            DropColumn( "dbo._org_secc_Equip_Course", "AllowedGroupId" );
            DropColumn( "dbo._org_secc_Equip_Course", "AllowedDataViewId" );
        }
    }
}
