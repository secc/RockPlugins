namespace org.secc.FamilyCheckin.Migrations
{
    using Rock.Plugin;

    [MigrationNumber( 2, "1.4.0" )]
    public partial class Message : Migration
    {
        public override void Up()
        {
            AddColumn("dbo._org_secc_FamilyCheckin_KioskType", "Message", c => c.String());
        }
        
        public override void Down()
        {
            DropColumn("dbo._org_secc_FamilyCheckin_KioskType", "Message");
        }
    }
}
