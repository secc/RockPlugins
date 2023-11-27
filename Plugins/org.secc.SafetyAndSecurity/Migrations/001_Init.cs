namespace org.secc.SafetyAndSecurity.Migrations
{
    using Rock.Plugin;

    [MigrationNumber( 1, "1.10.2" )]
    public partial class Init : Migration
    {
        public override void Up()
        {
            AddTable(
                "dbo._org_secc_SafetyAndSecurity_AlertNotification",
                c => new
                {
                    Id = c.Int( nullable: false, identity: true ),
                    Title = c.String(),
                    IsActive = c.Boolean( nullable: false ),
                    AudienceValueId = c.Int( nullable: false ),
                    AlertNotificationTypeValueId = c.Int( nullable: false ),
                    CreatedDateTime = c.DateTime(),
                    ModifiedDateTime = c.DateTime(),
                    CreatedByPersonAliasId = c.Int(),
                    ModifiedByPersonAliasId = c.Int(),
                    Guid = c.Guid( nullable: false ),
                    ForeignId = c.Int(),
                    ForeignGuid = c.Guid(),
                    ForeignKey = c.String( maxLength: 100 ),
                } );
            AddTable(
                "dbo._org_secc_SafetyAndSecurity_AlertMessage",
                c => new
                {
                    Id = c.Int( nullable: false, identity: true ),
                    AlertNotificationId = c.Int( nullable: false ),
                    Message = c.String(),
                    CreatedDateTime = c.DateTime(),
                    ModifiedDateTime = c.DateTime(),
                    CreatedByPersonAliasId = c.Int(),
                    ModifiedByPersonAliasId = c.Int(),
                    Guid = c.Guid( nullable: false ),
                    ForeignId = c.Int(),
                    ForeignGuid = c.Guid(),
                    ForeignKey = c.String( maxLength: 100 ),
                    AlertNotification_Id = c.Int(),
                } );

            AddPrimaryKey( "dbo._org_secc_SafetyAndSecurity_AlertNotification", "Id" );
            AddPrimaryKey( "dbo._org_secc_SafetyAndSecurity_AlertMessage", "Id" );

            AddForeignKey( "dbo._org_secc_SafetyAndSecurity_AlertNotification", "AlertNotificationTypeValueId", "dbo.DefinedValue" );
            AddForeignKey( "dbo._org_secc_SafetyAndSecurity_AlertNotification", "AudienceValueId", "dbo.DefinedValue" );
            AddForeignKey( "dbo._org_secc_SafetyAndSecurity_AlertNotification", "CreatedByPersonAliasId", "dbo.PersonAlias" );
            AddForeignKey( "dbo._org_secc_SafetyAndSecurity_AlertNotification", "ModifiedByPersonAliasId", "dbo.PersonAlias" );
            AddIndex( "dbo._org_secc_SafetyAndSecurity_AlertNotification", "AudienceValueId" );
            AddIndex( "dbo._org_secc_SafetyAndSecurity_AlertNotification", "AlertNotificationTypeValueId" );
            AddIndex( "dbo._org_secc_SafetyAndSecurity_AlertNotification", "CreatedByPersonAliasId" );
            AddIndex( "dbo._org_secc_SafetyAndSecurity_AlertNotification", "ModifiedByPersonAliasId" );
            AddIndex( "dbo._org_secc_SafetyAndSecurity_AlertNotification", "Guid", unique: true );

            AddForeignKey( "dbo._org_secc_SafetyAndSecurity_AlertMessage", "AlertNotification_Id", "dbo._org_secc_SafetyAndSecurity_AlertNotification" );
            AddForeignKey( "dbo._org_secc_SafetyAndSecurity_AlertMessage", "AlertNotificationId", "dbo._org_secc_SafetyAndSecurity_AlertNotification", cascadeDelete: true );
            AddForeignKey( "dbo._org_secc_SafetyAndSecurity_AlertMessage", "CreatedByPersonAliasId", "dbo.PersonAlias" );
            AddForeignKey( "dbo._org_secc_SafetyAndSecurity_AlertMessage", "ModifiedByPersonAliasId", "dbo.PersonAlias" );
            AddIndex( "dbo._org_secc_SafetyAndSecurity_AlertMessage", "AlertNotificationId" );
            AddIndex( "dbo._org_secc_SafetyAndSecurity_AlertMessage", "CreatedByPersonAliasId" );
            AddIndex( "dbo._org_secc_SafetyAndSecurity_AlertMessage", "ModifiedByPersonAliasId" );
            AddIndex( "dbo._org_secc_SafetyAndSecurity_AlertMessage", "Guid", unique: true );
            AddIndex( "dbo._org_secc_SafetyAndSecurity_AlertMessage", "AlertNotification_Id" );
        }

        public override void Down()
        {
            DropForeignKey( "dbo._org_secc_SafetyAndSecurity_AlertMessage", "ModifiedByPersonAliasId", "dbo.PersonAlias" );
            DropForeignKey( "dbo._org_secc_SafetyAndSecurity_AlertMessage", "CreatedByPersonAliasId", "dbo.PersonAlias" );
            DropForeignKey( "dbo._org_secc_SafetyAndSecurity_AlertNotification", "ModifiedByPersonAliasId", "dbo.PersonAlias" );
            DropForeignKey( "dbo._org_secc_SafetyAndSecurity_AlertNotification", "CreatedByPersonAliasId", "dbo.PersonAlias" );
            DropForeignKey( "dbo._org_secc_SafetyAndSecurity_AlertNotification", "AudienceValueId", "dbo.DefinedValue" );
            DropForeignKey( "dbo._org_secc_SafetyAndSecurity_AlertNotification", "AlertNotificationTypeValueId", "dbo.DefinedValue" );
            DropForeignKey( "dbo._org_secc_SafetyAndSecurity_AlertMessage", "AlertNotificationId", "dbo._org_secc_SafetyAndSecurity_AlertNotification" );
            DropIndex( "dbo._org_secc_SafetyAndSecurity_AlertNotification", new[] { "Guid" } );
            DropIndex( "dbo._org_secc_SafetyAndSecurity_AlertNotification", new[] { "ModifiedByPersonAliasId" } );
            DropIndex( "dbo._org_secc_SafetyAndSecurity_AlertNotification", new[] { "CreatedByPersonAliasId" } );
            DropIndex( "dbo._org_secc_SafetyAndSecurity_AlertNotification", new[] { "AlertNotificationTypeValueId" } );
            DropIndex( "dbo._org_secc_SafetyAndSecurity_AlertNotification", new[] { "AudienceValueId" } );
            DropIndex( "dbo._org_secc_SafetyAndSecurity_AlertMessage", new[] { "Guid" } );
            DropIndex( "dbo._org_secc_SafetyAndSecurity_AlertMessage", new[] { "ModifiedByPersonAliasId" } );
            DropIndex( "dbo._org_secc_SafetyAndSecurity_AlertMessage", new[] { "CreatedByPersonAliasId" } );
            DropIndex( "dbo._org_secc_SafetyAndSecurity_AlertMessage", new[] { "AlertNotificationId" } );
            DropTable( "dbo._org_secc_SafetyAndSecurity_AlertNotification" );
            DropTable( "dbo._org_secc_SafetyAndSecurity_AlertMessage" );

        }
    }
}
