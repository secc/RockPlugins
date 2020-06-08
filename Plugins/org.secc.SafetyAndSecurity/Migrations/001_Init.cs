namespace org.secc.SafetyAndSecurity.Migrations
{
    using org.secc.DevLib.Extensions.Migration;
    using Rock.Plugin;

    [MigrationNumber( 1, "1.10.2" )]
    public partial class Init : Migration
    {
        public override void Up()
        {
            CreateTable(
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
                } )
                .PrimaryKey( t => t.Id )
                .ForeignKey( "dbo.DefinedValue", t => t.AlertNotificationTypeValueId )
                .ForeignKey( "dbo.DefinedValue", t => t.AudienceValueId )
                .ForeignKey( "dbo.PersonAlias", t => t.CreatedByPersonAliasId )
                .ForeignKey( "dbo.PersonAlias", t => t.ModifiedByPersonAliasId )
                .Index( t => t.AudienceValueId )
                .Index( t => t.AlertNotificationTypeValueId )
                .Index( t => t.CreatedByPersonAliasId )
                .Index( t => t.ModifiedByPersonAliasId )
                .Index( t => t.Guid, unique: true )
                .Run( this );

            CreateTable(
                "dbo._org_secc_SafetyAndSecurity_AlertMessage",
                c => new
                {
                    Id = c.Int( nullable: false, identity: true ),
                    AlertNotificationId = c.Int( nullable: false ),
                    Message = c.String(),
                    CommunicationId = c.Int( nullable: false ),
                    CreatedDateTime = c.DateTime(),
                    ModifiedDateTime = c.DateTime(),
                    CreatedByPersonAliasId = c.Int(),
                    ModifiedByPersonAliasId = c.Int(),
                    Guid = c.Guid( nullable: false ),
                    ForeignId = c.Int(),
                    ForeignGuid = c.Guid(),
                    ForeignKey = c.String( maxLength: 100 ),
                    AlertNotification_Id = c.Int(),
                } )
                .PrimaryKey( t => t.Id )
                .ForeignKey( "dbo._org_secc_SafetyAndSecurity_AlertNotification", t => t.AlertNotification_Id )
                .ForeignKey( "dbo._org_secc_SafetyAndSecurity_AlertNotification", t => t.AlertNotificationId, cascadeDelete: true )
                .ForeignKey( "dbo.Communication", t => t.CommunicationId )
                .ForeignKey( "dbo.PersonAlias", t => t.CreatedByPersonAliasId )
                .ForeignKey( "dbo.PersonAlias", t => t.ModifiedByPersonAliasId )
                .Index( t => t.AlertNotificationId )
                .Index( t => t.CommunicationId )
                .Index( t => t.CreatedByPersonAliasId )
                .Index( t => t.ModifiedByPersonAliasId )
                .Index( t => t.Guid, unique: true )
                .Index( t => t.AlertNotification_Id )
                .Run( this );
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
