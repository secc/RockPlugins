namespace org.secc.Widgities.Migrations
{
    using Rock.Plugin;

    [MigrationNumber( 1, "1.8.0" )]
    public partial class Init : Migration
    {
        public override void Up()
        {
            CreateTable(
                "dbo._org_secc_Widgity_Widgity",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        WidgityTypeId = c.Int(nullable: false),
                        EntityId = c.Int(nullable: false),
                        Order = c.Int( nullable: false ),
                        CreatedDateTime = c.DateTime(),
                        ModifiedDateTime = c.DateTime(),
                        CreatedByPersonAliasId = c.Int(),
                        ModifiedByPersonAliasId = c.Int(),
                        Guid = c.Guid(nullable: false),
                        ForeignId = c.Int(),
                        ForeignGuid = c.Guid(),
                        ForeignKey = c.String(maxLength: 100),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.PersonAlias", t => t.CreatedByPersonAliasId)
                .ForeignKey("dbo.PersonAlias", t => t.ModifiedByPersonAliasId)
                .ForeignKey("dbo._org_secc_Widgity_WidgityType", t => t.WidgityTypeId, cascadeDelete: true)
                .Index(t => t.WidgityTypeId)
                .Index(t => t.CreatedByPersonAliasId)
                .Index(t => t.ModifiedByPersonAliasId)
                .Index(t => t.Guid, unique: true);

            CreateTable(
                "dbo._org_secc_Widgity_WidgityItem",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        WidgityId = c.Int(nullable: false),
                        WidgityTypeId = c.Int(nullable: false),
                        Order = c.Int( nullable: false ),
                        CreatedDateTime = c.DateTime(),
                        ModifiedDateTime = c.DateTime(),
                        CreatedByPersonAliasId = c.Int(),
                        ModifiedByPersonAliasId = c.Int(),
                        Guid = c.Guid(nullable: false),
                        ForeignId = c.Int(),
                        ForeignGuid = c.Guid(),
                        ForeignKey = c.String(maxLength: 100),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.PersonAlias", t => t.CreatedByPersonAliasId)
                .ForeignKey("dbo.PersonAlias", t => t.ModifiedByPersonAliasId)
                .ForeignKey("dbo._org_secc_Widgity_Widgity", t => t.WidgityId, cascadeDelete: true)
                .ForeignKey("dbo._org_secc_Widgity_WidgityType", t => t.WidgityTypeId, cascadeDelete: true)
                .Index(t => t.WidgityId)
                .Index(t => t.WidgityTypeId)
                .Index(t => t.CreatedByPersonAliasId)
                .Index(t => t.ModifiedByPersonAliasId)
                .Index(t => t.Guid, unique: true);
            
            CreateTable(
                "dbo._org_secc_Widgity_WidgityType",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                        IsSystem = c.Boolean(nullable: false),
                        Icon = c.String(),
                        HasItems = c.Boolean( nullable: false ),
                        EnabledLavaCommands = c.String(maxLength: 500),
                        Description = c.String(),
                        Markdown = c.String(),
                        CategoryId = c.Int(),
                        EntityTypeId = c.Int(nullable: false),
                        CreatedDateTime = c.DateTime(),
                        ModifiedDateTime = c.DateTime(),
                        CreatedByPersonAliasId = c.Int(),
                        ModifiedByPersonAliasId = c.Int(),
                        Guid = c.Guid(nullable: false),
                        ForeignId = c.Int(),
                        ForeignGuid = c.Guid(),
                        ForeignKey = c.String(maxLength: 100),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.Category", t => t.CategoryId)
                .ForeignKey("dbo.PersonAlias", t => t.CreatedByPersonAliasId)
                .ForeignKey("dbo.EntityType", t => t.EntityTypeId, cascadeDelete: true)
                .ForeignKey("dbo.PersonAlias", t => t.ModifiedByPersonAliasId)
                .Index(t => t.CategoryId)
                .Index(t => t.EntityTypeId)
                .Index(t => t.CreatedByPersonAliasId)
                .Index(t => t.ModifiedByPersonAliasId)
                .Index(t => t.Guid, unique: true);
            
            
        }
        
        public override void Down()
        {
            
        }
    }
}
