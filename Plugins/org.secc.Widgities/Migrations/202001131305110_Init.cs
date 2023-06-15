namespace org.secc.Widgities.Migrations
{
    using Rock.Plugin;

    [MigrationNumber( 1, "1.8.0" )]
    public partial class Init : Migration
    {
        public override void Up()
        {
            AddTable(
                "dbo._org_secc_Widgities_Widgity",
                c => new
                {
                    Id = c.Int( nullable: false, identity: true ),
                    WidgityTypeId = c.Int( nullable: false ),
                    EntityTypeId = c.Int( nullable: false ),
                    EntityGuid = c.Guid( nullable: false ),
                    Order = c.Int( nullable: false ),
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
                "dbo._org_secc_Widgities_WidgityItem",
                c => new
                {
                    Id = c.Int( nullable: false, identity: true ),
                    WidgityId = c.Int( nullable: false ),
                    WidgityTypeId = c.Int( nullable: false ),
                    Order = c.Int( nullable: false ),
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
                "dbo._org_secc_Widgities_WidgityType",
                c => new
                {
                    Id = c.Int( nullable: false, identity: true ),
                    Name = c.String(),
                    IsSystem = c.Boolean( nullable: false ),
                    Icon = c.String(),
                    HasItems = c.Boolean( nullable: false ),
                    EnabledLavaCommands = c.String( maxLength: 500 ),
                    Description = c.String(),
                    Markdown = c.String(),
                    CategoryId = c.Int(),
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
                "dbo._org_secc_Widgities_WidgityTypeEntityType",
                c => new
                {
                    WidgityTypeId = c.Int( nullable: false ),
                    EntityTypeId = c.Int( nullable: false ),
                } );

            AddPrimaryKey( "dbo._org_secc_Widgities_Widgity", "Id" );
            AddPrimaryKey( "dbo._org_secc_Widgities_WidgityItem", "Id" );
            AddPrimaryKey( "dbo._org_secc_Widgities_WidgityType", "Id" );
            AddPrimaryKey( "dbo._org_secc_Widgities_WidgityTypeEntityType", new string[] { "WidgityTypeId", "EntityTypeId" } );


            AddForeignKey( "dbo._org_secc_Widgities_Widgity", "CreatedByPersonAliasId", "dbo.PersonAlias", "Id" );
            AddForeignKey( "dbo._org_secc_Widgities_Widgity", "EntityTypeId", "dbo.EntityType", "Id", cascadeDelete: true );
            AddForeignKey( "dbo._org_secc_Widgities_Widgity", "ModifiedByPersonAliasId", "dbo.PersonAlias", "Id" );
            AddForeignKey( "dbo._org_secc_Widgities_Widgity", "WidgityTypeId", "dbo._org_secc_Widgities_WidgityType", "Id", cascadeDelete: true );
            AddIndex( "dbo._org_secc_Widgities_Widgity", "WidgityTypeId" );
            AddIndex( "dbo._org_secc_Widgities_Widgity", "EntityTypeId" );
            AddIndex( "dbo._org_secc_Widgities_Widgity", "CreatedByPersonAliasId" );
            AddIndex( "dbo._org_secc_Widgities_Widgity", "ModifiedByPersonAliasId" );
            AddIndex( "dbo._org_secc_Widgities_Widgity", "Guid", unique: true );


            AddForeignKey( "dbo._org_secc_Widgities_WidgityItem", "CreatedByPersonAliasId", "dbo.PersonAlias", "Id" );
            AddForeignKey( "dbo._org_secc_Widgities_WidgityItem", "ModifiedByPersonAliasId", "dbo.PersonAlias", "Id" );
            AddForeignKey( "dbo._org_secc_Widgities_WidgityItem", "WidgityId", "dbo._org_secc_Widgities_Widgity", "Id", cascadeDelete: true );
            AddForeignKey( "dbo._org_secc_Widgities_WidgityItem", "WidgityTypeId", "dbo._org_secc_Widgities_WidgityType", "Id", cascadeDelete: true );
            AddIndex( "dbo._org_secc_Widgities_WidgityItem", "WidgityId" );
            AddIndex( "dbo._org_secc_Widgities_WidgityItem", "WidgityTypeId" );
            AddIndex( "dbo._org_secc_Widgities_WidgityItem", "CreatedByPersonAliasId" );
            AddIndex( "dbo._org_secc_Widgities_WidgityItem", "ModifiedByPersonAliasId" );
            AddIndex( "dbo._org_secc_Widgities_WidgityItem", "Guid", unique: true );

            AddForeignKey( "dbo._org_secc_Widgities_WidgityType", "CategoryId", "dbo.Category", "Id" );
            AddForeignKey( "dbo._org_secc_Widgities_WidgityType", "CreatedByPersonAliasId", "dbo.PersonAlias", "Id" );
            AddForeignKey( "dbo._org_secc_Widgities_WidgityType", "ModifiedByPersonAliasId", "dbo.PersonAlias", "Id" );
            AddIndex( "dbo._org_secc_Widgities_WidgityType", "CategoryId" );
            AddIndex( "dbo._org_secc_Widgities_WidgityType", "CreatedByPersonAliasId" );
            AddIndex( "dbo._org_secc_Widgities_WidgityType", "ModifiedByPersonAliasId" );
            AddIndex( "dbo._org_secc_Widgities_WidgityType", "Guid", unique: true );

            AddForeignKey( "dbo._org_secc_Widgities_WidgityTypeEntityType", "WidgityTypeId", "dbo._org_secc_Widgities_WidgityType", "Id", cascadeDelete: true );
            AddForeignKey( "dbo._org_secc_Widgities_WidgityTypeEntityType", "EntityTypeId", "dbo.EntityType", "Id", cascadeDelete: true );
            AddIndex( "dbo._org_secc_Widgities_WidgityTypeEntityType", "WidgityTypeId" );
            AddIndex( "dbo._org_secc_Widgities_WidgityTypeEntityType", "EntityTypeId" );
        }

        public override void Down()
        {

        }
    }
}
