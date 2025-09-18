namespace org.secc.Communication.Migrations
{
    using Rock.Plugin;

    [MigrationNumber( 1, "1.10.0" )]
    public partial class Init : Migration
    {
        public override void Up()
        {
            AddTable(
                "dbo._org_secc_Communication_TwilioHistory",
                c => new
                {
                    Id = c.Int( nullable: false, identity: true ),
                    SID = c.String( nullable: false, maxLength: 64 ),
                    Body = c.String(),
                    DateCreated = c.DateTime(),
                    DateSent = c.DateTime(),
                    Direction = c.Int( nullable: false ),
                    To = c.String(),
                    From = c.String(),
                    Status = c.Int( nullable: false ),
                    Price = c.Decimal( nullable: false, precision: 12, scale: 10 ),
                    NumberOfSegments = c.Int( nullable: false ),
                    CreatedDateTime = c.DateTime(),
                    ModifiedDateTime = c.DateTime(),
                    CreatedByPersonAliasId = c.Int(),
                    ModifiedByPersonAliasId = c.Int(),
                    Guid = c.Guid( nullable: false ),
                    ForeignId = c.Int(),
                    ForeignGuid = c.Guid(),
                    ForeignKey = c.String( maxLength: 100 ),
                } );
            AddPrimaryKey( "dbo._org_secc_Communication_TwilioHistory", "Id" );
            AddForeignKey( "dbo._org_secc_Communication_TwilioHistory", "CreatedByPersonAliasId", "dbo.PersonAlias", "Id" );
            AddForeignKey( "dbo._org_secc_Communication_TwilioHistory", "ModifiedByPersonAliasId", "dbo.PersonAlias", "Id" );
            AddIndex( "dbo._org_secc_Communication_TwilioHistory", "CreatedByPersonAliasId" );
            AddIndex( "dbo._org_secc_Communication_TwilioHistory", "ModifiedByPersonAliasId" );
            AddIndex( "dbo._org_secc_Communication_TwilioHistory", "Guid", unique: true );
        }

        public override void Down()
        {
            DropTable( "dbo._org_secc_Communication_TwilioHistory" );
        }
    }
}
