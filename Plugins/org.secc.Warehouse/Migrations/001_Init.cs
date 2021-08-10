namespace org.secc.Warehouse.Migrations
{
    using org.secc.DevLib.Extensions.Migration;
    using Rock.Plugin;

    [MigrationNumber( 1, "1.10.2" )]
    public partial class Init : Migration
    {
        public override void Up()
        {
            CreateTable(
                "dbo._org_secc_Warehouse_DailyInteraction",
                c => new
                {
                    Id = c.Int( nullable: false, identity: true ),
                    Date = c.DateTime( nullable: false ),
                    Visits = c.Int( nullable: false ),
                    PageId = c.Int( nullable: false ),
                    StaffVisitors = c.Int( nullable: false ),
                    MemberVisitors = c.Int( nullable: false ),
                    AttendeeVisitors = c.Int( nullable: false ),
                    ProspectVisitors = c.Int( nullable: false ),
                    AnonymousVisitors = c.Int(nullable: false ),
                    CreatedDateTime = c.DateTime(),
                    ModifiedDateTime = c.DateTime(),
                    CreatedByPersonAliasId = c.Int(),
                    ModifiedByPersonAliasId = c.Int(),
                    Guid = c.Guid( nullable: false ),
                    ForeignId = c.Int(),
                    ForeignGuid = c.Guid(),
                    ForeignKey = c.String( maxLength: 100 ),
                } )
                .PrimaryKey( t => t.Id )
                .ForeignKey( "dbo.PersonAlias", t => t.CreatedByPersonAliasId )
                .ForeignKey( "dbo.PersonAlias", t => t.ModifiedByPersonAliasId )
                .Index( t => t.Date )
                .Index( t => t.PageId )
                .Index( t => t.CreatedByPersonAliasId )
                .Index( t => t.ModifiedByPersonAliasId )
                .Index( t => t.Guid, unique: true )
                .Run( this );
        }

        public override void Down()
        {
            DropTable( "dbo._org_secc_Warehouse_DailyInteraction" );
        }
    }
}
