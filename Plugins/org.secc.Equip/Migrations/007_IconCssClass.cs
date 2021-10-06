namespace org.secc.Equip.Migrations
{
    using Rock.Plugin;

    [MigrationNumber(7 , "1.8.0" )]
    public partial class IconCssClass : Migration
    {
        public override void Up()
        {
            AddColumn( "dbo._org_secc_Equip_Course", "IconCssClass", c => c.String(maxLength:100) );
        }

        public override void Down()
        {
            DropColumn( "dbo._org_secc_Equip_Course", "IconCssClass" );
        }
    }
}
