namespace org.secc.GroupManager.Migrations
{
    using Rock.Plugin;

    [MigrationNumber( 1, "1.8.0" )]
    public partial class Init : Migration
    {
        public override void Up()
        {
            AddTable(
                "dbo._org_secc_GroupManager_PublishGroup",
                c => new
                {
                    Id = c.Int( nullable: false, identity: true ),
                    GroupId = c.Int( nullable: false ),
                    Description = c.String(),
                    ImageId = c.Int(),
                    StartDateTime = c.DateTime( nullable: false ),
                    EndDateTime = c.DateTime( nullable: false ),
                    RequestorAliasId = c.Int( nullable: false ),
                    ContactPersonAliasId = c.Int( nullable: false ),
                    ContactEmail = c.String(),
                    RequiresRegistration = c.Boolean( nullable: false ),
                    RegistrationLink = c.String(),
                    ChildcareAvailable = c.Boolean( nullable: false ),
                    ChildcareRegistrationLink = c.String(),
                    ContactPhoneNumber = c.String(),
                    ConfirmationFromName = c.String(),
                    ConfirmationEmail = c.String(),
                    ConfirmationSubject = c.String(),
                    ConfirmationBody = c.String(),
                    PublishGroupStatus = c.Int( nullable: false ),
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
                "dbo._org_secc_GroupManager_PublishGroupAudienceValue",
                c => new
                {
                    PublishGroupdId = c.Int( nullable: false ),
                    DefinedValueId = c.Int( nullable: false ),
                } );

            AddPrimaryKey( "dbo._org_secc_GroupManager_PublishGroup", "Id" );
            AddPrimaryKey( "dbo._org_secc_GroupManager_PublishGroupAudienceValue", new string[] { "PublishGroupdId", "DefinedValueId" } );

            AddForeignKey( "dbo._org_secc_GroupManager_PublishGroup", "ContactPersonAliasId", "dbo.PersonAlias", cascadeDelete: true );
            AddForeignKey( "dbo._org_secc_GroupManager_PublishGroup", "CreatedByPersonAliasId", "dbo.PersonAlias" );
            AddForeignKey( "dbo._org_secc_GroupManager_PublishGroup", "GroupId", "dbo.Group", cascadeDelete: true );
            AddForeignKey( "dbo._org_secc_GroupManager_PublishGroup", "ImageId", "dbo.BinaryFile" );
            AddForeignKey( "dbo._org_secc_GroupManager_PublishGroup", "ModifiedByPersonAliasId", "dbo.PersonAlias" );
            AddForeignKey( "dbo._org_secc_GroupManager_PublishGroup", "RequestorAliasId", "dbo.PersonAlias" );
            AddIndex( "dbo._org_secc_GroupManager_PublishGroup", "GroupId" );
            AddIndex( "dbo._org_secc_GroupManager_PublishGroup", "ImageId" );
            AddIndex( "dbo._org_secc_GroupManager_PublishGroup", "StartDateTime" );
            AddIndex( "dbo._org_secc_GroupManager_PublishGroup", "EndDateTime" );
            AddIndex( "dbo._org_secc_GroupManager_PublishGroup", "RequestorAliasId" );
            AddIndex( "dbo._org_secc_GroupManager_PublishGroup", "ContactPersonAliasId" );
            AddIndex( "dbo._org_secc_GroupManager_PublishGroup", "CreatedByPersonAliasId" );
            AddIndex( "dbo._org_secc_GroupManager_PublishGroup", "ModifiedByPersonAliasId" );
            AddIndex( "dbo._org_secc_GroupManager_PublishGroup", "Guid", unique: true );

            AddForeignKey( "dbo._org_secc_GroupManager_PublishGroupAudienceValue", "PublishGroupdId", "dbo._org_secc_GroupManager_PublishGroup", cascadeDelete: true );
            AddForeignKey( "dbo._org_secc_GroupManager_PublishGroupAudienceValue", "DefinedValueId", "dbo.DefinedValue", cascadeDelete: true );
            AddIndex( "dbo._org_secc_GroupManager_PublishGroupAudienceValue", "PublishGroupdId" );


        }

        public override void Down()
        {
            DropForeignKey( "dbo._org_secc_GroupManager_PublishGroupAudienceValue", "DefinedValueId", "dbo.DefinedValue" );
            DropForeignKey( "dbo._org_secc_GroupManager_PublishGroupAudienceValue", "PublishGroupdId", "dbo._org_secc_GroupManager_PublishGroup" );
            DropIndex( "dbo._org_secc_GroupManager_PublishGroupAudienceValue", new[] { "DefinedValueId" } );
            DropIndex( "dbo._org_secc_GroupManager_PublishGroupAudienceValue", new[] { "PublishGroupdId" } );
            DropTable( "dbo._org_secc_GroupManager_PublishGroupAudienceValue" );
            DropTable( "dbo._org_secc_GroupManager_PublishGroup" );
        }
    }
}
