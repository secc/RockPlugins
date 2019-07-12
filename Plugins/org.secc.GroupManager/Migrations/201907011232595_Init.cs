namespace org.secc.GroupManager.Migrations
{
    using Rock.Plugin;

    [MigrationNumber( 1, "1.8.0" )]
    public partial class Init : Migration
    {
        public override void Up()
        {
            CreateTable(
                            "dbo._org_secc_GroupManager_PublishGroup",
                            c => new
                            {
                                Id = c.Int( nullable: false, identity: true ),
                                GroupId = c.Int( nullable: false ),
                                Description = c.String(),
                                ImageId = c.Int( nullable: false ),
                                StartDateTime = c.DateTime( nullable: false ),
                                EndDateTime = c.DateTime( nullable: false ),
                                RequestorAliasId = c.Int( nullable: false ),
                                ContactPersonAliasId = c.Int( nullable: false ),
                                ContactEmail = c.String(),
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
                            } )
                            .PrimaryKey( t => t.Id )
                            .ForeignKey( "dbo.PersonAlias", t => t.ContactPersonAliasId )
                            .ForeignKey( "dbo.PersonAlias", t => t.CreatedByPersonAliasId )
                            .ForeignKey( "dbo.Group", t => t.GroupId, cascadeDelete: true )
                            .ForeignKey( "dbo.BinaryFile", t => t.ImageId, cascadeDelete: true )
                            .ForeignKey( "dbo.PersonAlias", t => t.ModifiedByPersonAliasId )
                            .ForeignKey( "dbo.PersonAlias", t => t.RequestorAliasId )
                            .Index( t => t.GroupId )
                            .Index( t => t.ImageId )
                            .Index( t => t.StartDateTime )
                            .Index( t => t.EndDateTime )
                            .Index( t => t.RequestorAliasId )
                            .Index( t => t.ContactPersonAliasId )
                            .Index( t => t.CreatedByPersonAliasId )
                            .Index( t => t.ModifiedByPersonAliasId )
                            .Index( t => t.Guid, unique: true );

            CreateTable(
                "dbo._org_secc_GroupManager_PublishGroupAudienceValue",
                c => new
                {
                    PublishGroupdId = c.Int( nullable: false ),
                    DefinedValueId = c.Int( nullable: false ),
                } )
                .PrimaryKey( t => new { t.PublishGroupdId, t.DefinedValueId } )
                .ForeignKey( "dbo._org_secc_GroupManager_PublishGroup", t => t.PublishGroupdId, cascadeDelete: true )
                .ForeignKey( "dbo.DefinedValue", t => t.DefinedValueId, cascadeDelete: true )
                .Index( t => t.PublishGroupdId );


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
