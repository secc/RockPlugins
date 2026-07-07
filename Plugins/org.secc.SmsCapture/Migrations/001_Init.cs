namespace org.secc.SmsCapture.Migrations
{
    using Rock.Plugin;

    [MigrationNumber( 1, "1.13.0" )]
    public partial class Init : Migration
    {
        public override void Up()
        {
            AddTable(
                "dbo._org_secc_SmsCapture_CapturedSms",
                c => new
                {
                    Id = c.Int( nullable: false, identity: true ),
                    FromNumber = c.String( maxLength: 20 ),
                    ToNumber = c.String( maxLength: 20 ),
                    RecipientPersonAliasId = c.Int(),
                    Body = c.String(),
                    AttachmentBinaryFileIds = c.String(),
                    CommunicationId = c.Int(),
                    CommunicationRecipientId = c.Int(),
                    Source = c.String( maxLength: 100 ),
                    CreatedDateTime = c.DateTime(),
                    ModifiedDateTime = c.DateTime(),
                    CreatedByPersonAliasId = c.Int(),
                    ModifiedByPersonAliasId = c.Int(),
                    Guid = c.Guid( nullable: false ),
                    ForeignId = c.Int(),
                    ForeignGuid = c.Guid(),
                    ForeignKey = c.String( maxLength: 100 ),
                } );
            AddPrimaryKey( "dbo._org_secc_SmsCapture_CapturedSms", "Id" );
            AddForeignKey( "dbo._org_secc_SmsCapture_CapturedSms", "RecipientPersonAliasId", "dbo.PersonAlias", "Id" );
            AddForeignKey( "dbo._org_secc_SmsCapture_CapturedSms", "CreatedByPersonAliasId", "dbo.PersonAlias", "Id" );
            AddForeignKey( "dbo._org_secc_SmsCapture_CapturedSms", "ModifiedByPersonAliasId", "dbo.PersonAlias", "Id" );
            AddIndex( "dbo._org_secc_SmsCapture_CapturedSms", "RecipientPersonAliasId" );
            AddIndex( "dbo._org_secc_SmsCapture_CapturedSms", "CreatedByPersonAliasId" );
            AddIndex( "dbo._org_secc_SmsCapture_CapturedSms", "ModifiedByPersonAliasId" );
            AddIndex( "dbo._org_secc_SmsCapture_CapturedSms", "Guid", unique: true );
            AddIndex( "dbo._org_secc_SmsCapture_CapturedSms", "ToNumber" );
            AddIndex( "dbo._org_secc_SmsCapture_CapturedSms", "CreatedDateTime" );
        }

        public override void Down()
        {
            DropTable( "dbo._org_secc_SmsCapture_CapturedSms" );
        }
    }
}
