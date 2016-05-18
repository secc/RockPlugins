namespace org.secc.OAuth.Migrations
{
    using Rock.Migrations;
    using Rock.Plugin;
    using System.Data.Entity.Migrations;
    [MigrationNumber(1, "1.4.5")]
    public partial class InitialCreate : Rock.Plugin.Migration
    {
        public override void Up()
        {
            CreateTable(
                "dbo._org_secc_OAuth_Client",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ClientName = c.String(maxLength: 255),
                        ApiKey = c.Guid(nullable: false),
                        ApiSecret = c.Guid(nullable: false),
                        CallbackUrl = c.String(),
                        Active = c.Boolean(nullable: false),
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
                .ForeignKey("PersonAlias", t => t.CreatedByPersonAliasId)
                .ForeignKey("PersonAlias", t => t.ModifiedByPersonAliasId)
                .Index(t => t.CreatedByPersonAliasId)
                .Index(t => t.ModifiedByPersonAliasId)
                .Index(t => t.Guid, unique: true)
                .Index(t => t.ForeignId)
                .Index(t => t.ForeignGuid)
                .Index(t => t.ForeignKey);

            CreateTable(
                "dbo._org_secc_OAuth_Scope",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Identifier = c.String(maxLength: 255),
                        Description = c.String(),
                        Active = c.Boolean(nullable: false),
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
                .ForeignKey("PersonAlias", t => t.CreatedByPersonAliasId)
                .ForeignKey("PersonAlias", t => t.ModifiedByPersonAliasId)
                .Index(t => t.CreatedByPersonAliasId)
                .Index(t => t.ModifiedByPersonAliasId)
                .Index(t => t.Guid, unique: true)
                .Index(t => t.ForeignId)
                .Index(t => t.ForeignGuid)
                .Index(t => t.ForeignKey);
            


            CreateTable(
                "dbo._org_secc_OAuth_ClientScope",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ClientId = c.Int(nullable: false),
                        ScopeId = c.Int(nullable: false),
                        Active = c.Boolean(nullable: false),
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
                .ForeignKey("PersonAlias", t => t.CreatedByPersonAliasId)
                .ForeignKey("PersonAlias", t => t.ModifiedByPersonAliasId)
                .ForeignKey("_org_secc_OAuth_Client", t => t.ClientId, cascadeDelete: true)
                .ForeignKey("_org_secc_OAuth_Scope", t => t.ScopeId, cascadeDelete: true)
                .Index(t => t.ClientId)
                .Index(t => t.ScopeId)
                .Index(t => t.CreatedByPersonAliasId)
                .Index(t => t.ModifiedByPersonAliasId)
                .Index(t => t.Guid, unique: true)
                .Index(t => t.ForeignId)
                .Index(t => t.ForeignGuid)
                .Index(t => t.ForeignKey);

            CreateTable(
                "dbo._org_secc_OAuth_Authorization",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    ClientId = c.Int(nullable: false),
                    ScopeId = c.Int(nullable: false),
                    UserLoginId = c.Int(nullable: false),
                    Active = c.Boolean(nullable: false),
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
                .ForeignKey("PersonAlias", t => t.CreatedByPersonAliasId)
                .ForeignKey("PersonAlias", t => t.ModifiedByPersonAliasId)
                .ForeignKey("_org_secc_OAuth_Client", t=>t.ClientId, cascadeDelete: true)
                .ForeignKey("_org_secc_OAuth_Scope", t => t.ScopeId, cascadeDelete: true)
                .ForeignKey("UserLogin", t => t.UserLoginId, cascadeDelete: true)
                .Index(t => t.ClientId)
                .Index(t => t.ScopeId)
                .Index(t => t.UserLoginId)
                .Index(t => t.CreatedByPersonAliasId)
                .Index(t => t.ModifiedByPersonAliasId)
                .Index(t => t.Guid, unique: true)
                .Index(t => t.ForeignId)
                .Index(t => t.ForeignGuid)
                .Index(t => t.ForeignKey);

        }

        public override void Down()
        {
            DropIndex("dbo._org_secc_OAuth_Client", new[] { "ForeignKey" });
            DropIndex("dbo._org_secc_OAuth_Client", new[] { "ForeignGuid" });
            DropIndex("dbo._org_secc_OAuth_Client", new[] { "ForeignId" });
            DropIndex("dbo._org_secc_OAuth_Client", new[] { "Guid" });
            DropIndex("dbo._org_secc_OAuth_Client", new[] { "ModifiedByPersonAliasId" });
            DropIndex("dbo._org_secc_OAuth_Client", new[] { "CreatedByPersonAliasId" });
            DropIndex("dbo._org_secc_OAuth_Authorization", new[] { "ForeignKey" });
            DropIndex("dbo._org_secc_OAuth_Authorization", new[] { "ForeignGuid" });
            DropIndex("dbo._org_secc_OAuth_Authorization", new[] { "ForeignId" });
            DropIndex("dbo._org_secc_OAuth_Authorization", new[] { "Guid" });
            DropIndex("dbo._org_secc_OAuth_Authorization", new[] { "ModifiedByPersonAliasId" });
            DropIndex("dbo._org_secc_OAuth_Authorization", new[] { "CreatedByPersonAliasId" });
            DropIndex("dbo._org_secc_OAuth_Authorization", new[] { "UserLoginId" });
            DropIndex("dbo._org_secc_OAuth_Authorization", new[] { "ScopeId" });
            DropIndex("dbo._org_secc_OAuth_Authorization", new[] { "ClientId" });
            
            DropTable("dbo._org_secc_OAuth_ClientScope");
            DropTable("dbo._org_secc_OAuth_Scope");
            
            DropTable("dbo._org_secc_OAuth_Client");
            DropTable("dbo._org_secc_OAuth_Authorization");
        }
    }
}
