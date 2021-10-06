namespace org.secc.Equip.Migrations
{
    using Rock.Plugin;

    [MigrationNumber( 1, "1.8.0" )]
    public partial class Init : Migration
    {
        public override void Up()
        {
            CreateTable(
                "dbo._org_secc_Equip_ChapterRecord",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CourseRecordId = c.Int(nullable: false),
                        CompletionDateTime = c.DateTime(),
                        ChapterId = c.Int(nullable: false),
                        Passed = c.Boolean(nullable: false),
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
                .ForeignKey("dbo._org_secc_Equip_Chapter", t => t.ChapterId, cascadeDelete: true)
                .ForeignKey("dbo._org_secc_Equip_CourseRecord", t => t.CourseRecordId, cascadeDelete: true)
                .ForeignKey("dbo.PersonAlias", t => t.CreatedByPersonAliasId)
                .ForeignKey("dbo.PersonAlias", t => t.ModifiedByPersonAliasId)
                .Index(t => t.CourseRecordId)
                .Index(t => t.ChapterId)
                .Index(t => t.CreatedByPersonAliasId)
                .Index(t => t.ModifiedByPersonAliasId)
                .Index(t => t.Guid, unique: true);
            
            CreateTable(
                "dbo._org_secc_Equip_Chapter",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                        Description = c.String(),
                        CourseId = c.Int(nullable: false),
                        Order = c.Int(nullable: false),
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
                .ForeignKey("dbo._org_secc_Equip_Course", t => t.CourseId, cascadeDelete: true)
                .ForeignKey("dbo.PersonAlias", t => t.CreatedByPersonAliasId)
                .ForeignKey("dbo.PersonAlias", t => t.ModifiedByPersonAliasId)
                .Index(t => t.CourseId)
                .Index(t => t.CreatedByPersonAliasId)
                .Index(t => t.ModifiedByPersonAliasId)
                .Index(t => t.Guid, unique: true);
            
            CreateTable(
                "dbo._org_secc_Equip_Course",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                        Description = c.String(),
                        CategoryId = c.Int(),
                        AllowDocumentationMode = c.Boolean(nullable: false),
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
                .ForeignKey("dbo.PersonAlias", t => t.ModifiedByPersonAliasId)
                .Index(t => t.CategoryId)
                .Index(t => t.CreatedByPersonAliasId)
                .Index(t => t.ModifiedByPersonAliasId)
                .Index(t => t.Guid, unique: true);
            
                        
            CreateTable(
                "dbo._org_secc_Equip_CoursePage",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Name = c.String(),
                        ChapterId = c.Int(nullable: false),
                        PassingScore = c.Int(),
                        Configuration = c.String(),
                        EntityTypeId = c.Int(nullable: false),
                        Order = c.Int(nullable: false),
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
                .ForeignKey("dbo._org_secc_Equip_Chapter", t => t.ChapterId, cascadeDelete: true)
                .ForeignKey("dbo.PersonAlias", t => t.CreatedByPersonAliasId)
                .ForeignKey("dbo.EntityType", t => t.EntityTypeId, cascadeDelete: true)
                .ForeignKey("dbo.PersonAlias", t => t.ModifiedByPersonAliasId)
                .Index(t => t.ChapterId)
                .Index(t => t.EntityTypeId)
                .Index(t => t.CreatedByPersonAliasId)
                .Index(t => t.ModifiedByPersonAliasId)
                .Index(t => t.Guid, unique: true);
            
            CreateTable(
                "dbo._org_secc_Equip_CoursePageRecord",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ChapterRecordId = c.Int(nullable: false),
                        CoursePageId = c.Int(nullable: false),
                        Score = c.Int(nullable: false),
                        Passed = c.Boolean(nullable: false),
                        CompletionDetails = c.String(),
                        CompletionDateTime = c.DateTime(),
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
                .ForeignKey("dbo._org_secc_Equip_ChapterRecord", t => t.ChapterRecordId, cascadeDelete: true)
                .ForeignKey("dbo._org_secc_Equip_CoursePage", t => t.CoursePageId, cascadeDelete: true)
                .ForeignKey("dbo.PersonAlias", t => t.CreatedByPersonAliasId)
                .ForeignKey("dbo.PersonAlias", t => t.ModifiedByPersonAliasId)
                .Index(t => t.ChapterRecordId)
                .Index(t => t.CoursePageId)
                .Index(t => t.CreatedByPersonAliasId)
                .Index(t => t.ModifiedByPersonAliasId)
                .Index(t => t.Guid, unique: true);
            
            CreateTable(
                "dbo._org_secc_Equip_CourseRecord",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        PersonAliasId = c.Int(nullable: false),
                        CourseId = c.Int(nullable: false),
                        CompletionDateTime = c.DateTime(),
                        Passed = c.Boolean(nullable: false),
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
                .ForeignKey("dbo._org_secc_Equip_Course", t => t.CourseId, cascadeDelete: true)
                .ForeignKey("dbo.PersonAlias", t => t.CreatedByPersonAliasId)
                .ForeignKey("dbo.PersonAlias", t => t.ModifiedByPersonAliasId)
                .ForeignKey("dbo.PersonAlias", t => t.PersonAliasId, cascadeDelete: true)
                .Index(t => t.PersonAliasId)
                .Index(t => t.CourseId)
                .Index(t => t.CreatedByPersonAliasId)
                .Index(t => t.ModifiedByPersonAliasId)
                .Index(t => t.Guid, unique: true);
            
            CreateTable(
                "dbo._org_secc_Equip_CourseRequirement",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CourseId = c.Int(nullable: false),
                        GroupId = c.Int(),
                        DataViewId = c.Int(),
                        DaysValid = c.Int(),
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
                .ForeignKey("dbo._org_secc_Equip_Course", t => t.CourseId, cascadeDelete: true)
                .ForeignKey("dbo.PersonAlias", t => t.CreatedByPersonAliasId)
                .ForeignKey("dbo.DataView", t => t.DataViewId)
                .ForeignKey("dbo.Group", t => t.GroupId)
                .ForeignKey("dbo.PersonAlias", t => t.ModifiedByPersonAliasId)
                .Index(t => t.CourseId)
                .Index(t => t.GroupId)
                .Index(t => t.DataViewId)
                .Index(t => t.CreatedByPersonAliasId)
                .Index(t => t.ModifiedByPersonAliasId)
                .Index(t => t.Guid, unique: true);
            
            CreateTable(
                "dbo._org_secc_Equip_CourseRequirementStatus",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CourseRequirementId = c.Int(nullable: false),
                        PersonAliasId = c.Int(nullable: false),
                        IsComplete = c.Boolean(nullable: false),
                        ValidUntil = c.DateTime(),
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
                .ForeignKey("dbo._org_secc_Equip_CourseRequirement", t => t.CourseRequirementId, cascadeDelete: true)
                .ForeignKey("dbo.PersonAlias", t => t.CreatedByPersonAliasId)
                .ForeignKey("dbo.PersonAlias", t => t.ModifiedByPersonAliasId)
                .ForeignKey("dbo.PersonAlias", t => t.PersonAliasId, cascadeDelete: true)
                .Index(t => t.CourseRequirementId)
                .Index(t => t.PersonAliasId)
                .Index(t => t.CreatedByPersonAliasId)
                .Index(t => t.ModifiedByPersonAliasId)
                .Index(t => t.Guid, unique: true);
            
            
            
        }
        
        public override void Down()
        {
            
            DropForeignKey("dbo._org_secc_Equip_CourseRequirementStatus", "PersonAliasId", "dbo.PersonAlias");
            DropForeignKey("dbo._org_secc_Equip_CourseRequirementStatus", "ModifiedByPersonAliasId", "dbo.PersonAlias");
            DropForeignKey("dbo._org_secc_Equip_CourseRequirementStatus", "CreatedByPersonAliasId", "dbo.PersonAlias");
            DropForeignKey("dbo._org_secc_Equip_CourseRequirementStatus", "CourseRequirementId", "dbo._org_secc_Equip_CourseRequirement");
            DropForeignKey("dbo._org_secc_Equip_CourseRequirement", "ModifiedByPersonAliasId", "dbo.PersonAlias");
            DropForeignKey("dbo._org_secc_Equip_CourseRequirement", "GroupId", "dbo.Group");
            DropForeignKey("dbo._org_secc_Equip_CourseRequirement", "DataViewId", "dbo.DataView");
            DropForeignKey("dbo._org_secc_Equip_CourseRequirement", "CreatedByPersonAliasId", "dbo.PersonAlias");
            DropForeignKey("dbo._org_secc_Equip_CourseRequirement", "CourseId", "dbo._org_secc_Equip_Course");
            DropForeignKey("dbo._org_secc_Equip_ChapterRecord", "ModifiedByPersonAliasId", "dbo.PersonAlias");
            DropForeignKey("dbo._org_secc_Equip_ChapterRecord", "CreatedByPersonAliasId", "dbo.PersonAlias");
            DropForeignKey("dbo._org_secc_Equip_CourseRecord", "PersonAliasId", "dbo.PersonAlias");
            DropForeignKey("dbo._org_secc_Equip_CourseRecord", "ModifiedByPersonAliasId", "dbo.PersonAlias");
            DropForeignKey("dbo._org_secc_Equip_CourseRecord", "CreatedByPersonAliasId", "dbo.PersonAlias");
            DropForeignKey("dbo._org_secc_Equip_CourseRecord", "CourseId", "dbo._org_secc_Equip_Course");
            DropForeignKey("dbo._org_secc_Equip_ChapterRecord", "CourseRecordId", "dbo._org_secc_Equip_CourseRecord");
            DropForeignKey("dbo._org_secc_Equip_CoursePageRecord", "ModifiedByPersonAliasId", "dbo.PersonAlias");
            DropForeignKey("dbo._org_secc_Equip_CoursePageRecord", "CreatedByPersonAliasId", "dbo.PersonAlias");
            DropForeignKey("dbo._org_secc_Equip_CoursePageRecord", "CoursePageId", "dbo._org_secc_Equip_CoursePage");
            DropForeignKey("dbo._org_secc_Equip_CoursePageRecord", "ChapterRecordId", "dbo._org_secc_Equip_ChapterRecord");
            DropForeignKey("dbo._org_secc_Equip_ChapterRecord", "ChapterId", "dbo._org_secc_Equip_Chapter");
            DropForeignKey("dbo._org_secc_Equip_Chapter", "ModifiedByPersonAliasId", "dbo.PersonAlias");
            DropForeignKey("dbo._org_secc_Equip_Chapter", "CreatedByPersonAliasId", "dbo.PersonAlias");
            DropForeignKey("dbo._org_secc_Equip_CoursePage", "ModifiedByPersonAliasId", "dbo.PersonAlias");
            DropForeignKey("dbo._org_secc_Equip_CoursePage", "EntityTypeId", "dbo.EntityType");
            DropForeignKey("dbo._org_secc_Equip_CoursePage", "CreatedByPersonAliasId", "dbo.PersonAlias");
            DropForeignKey("dbo._org_secc_Equip_CoursePage", "ChapterId", "dbo._org_secc_Equip_Chapter");
            DropForeignKey("dbo._org_secc_Equip_Course", "ModifiedByPersonAliasId", "dbo.PersonAlias");
            DropForeignKey("dbo._org_secc_Equip_Course", "CreatedByPersonAliasId", "dbo.PersonAlias");
            DropForeignKey("dbo._org_secc_Equip_Chapter", "CourseId", "dbo._org_secc_Equip_Course");
            DropForeignKey("dbo._org_secc_Equip_Course", "CategoryId", "dbo.Category");
            DropIndex("dbo._org_secc_Equip_CourseRequirementStatus", new[] { "Guid" });
            DropIndex("dbo._org_secc_Equip_CourseRequirementStatus", new[] { "ModifiedByPersonAliasId" });
            DropIndex("dbo._org_secc_Equip_CourseRequirementStatus", new[] { "CreatedByPersonAliasId" });
            DropIndex("dbo._org_secc_Equip_CourseRequirementStatus", new[] { "PersonAliasId" });
            DropIndex("dbo._org_secc_Equip_CourseRequirementStatus", new[] { "CourseRequirementId" });
            DropIndex("dbo._org_secc_Equip_CourseRequirement", new[] { "Guid" });
            DropIndex("dbo._org_secc_Equip_CourseRequirement", new[] { "ModifiedByPersonAliasId" });
            DropIndex("dbo._org_secc_Equip_CourseRequirement", new[] { "CreatedByPersonAliasId" });
            DropIndex("dbo._org_secc_Equip_CourseRequirement", new[] { "DataViewId" });
            DropIndex("dbo._org_secc_Equip_CourseRequirement", new[] { "GroupId" });
            DropIndex("dbo._org_secc_Equip_CourseRequirement", new[] { "CourseId" });
            DropIndex("dbo._org_secc_Equip_CourseRecord", new[] { "Guid" });
            DropIndex("dbo._org_secc_Equip_CourseRecord", new[] { "ModifiedByPersonAliasId" });
            DropIndex("dbo._org_secc_Equip_CourseRecord", new[] { "CreatedByPersonAliasId" });
            DropIndex("dbo._org_secc_Equip_CourseRecord", new[] { "CourseId" });
            DropIndex("dbo._org_secc_Equip_CourseRecord", new[] { "PersonAliasId" });
            DropIndex("dbo._org_secc_Equip_CoursePageRecord", new[] { "Guid" });
            DropIndex("dbo._org_secc_Equip_CoursePageRecord", new[] { "ModifiedByPersonAliasId" });
            DropIndex("dbo._org_secc_Equip_CoursePageRecord", new[] { "CreatedByPersonAliasId" });
            DropIndex("dbo._org_secc_Equip_CoursePageRecord", new[] { "CoursePageId" });
            DropIndex("dbo._org_secc_Equip_CoursePageRecord", new[] { "ChapterRecordId" });
            DropIndex("dbo._org_secc_Equip_CoursePage", new[] { "Guid" });
            DropIndex("dbo._org_secc_Equip_CoursePage", new[] { "ModifiedByPersonAliasId" });
            DropIndex("dbo._org_secc_Equip_CoursePage", new[] { "CreatedByPersonAliasId" });
            DropIndex("dbo._org_secc_Equip_CoursePage", new[] { "EntityTypeId" });
            DropIndex("dbo._org_secc_Equip_CoursePage", new[] { "ChapterId" });
            DropIndex("dbo._org_secc_Equip_Course", new[] { "Guid" });
            DropIndex("dbo._org_secc_Equip_Course", new[] { "ModifiedByPersonAliasId" });
            DropIndex("dbo._org_secc_Equip_Course", new[] { "CreatedByPersonAliasId" });
            DropIndex("dbo._org_secc_Equip_Course", new[] { "CategoryId" });
            DropIndex("dbo._org_secc_Equip_Chapter", new[] { "Guid" });
            DropIndex("dbo._org_secc_Equip_Chapter", new[] { "ModifiedByPersonAliasId" });
            DropIndex("dbo._org_secc_Equip_Chapter", new[] { "CreatedByPersonAliasId" });
            DropIndex("dbo._org_secc_Equip_Chapter", new[] { "CourseId" });
            DropIndex("dbo._org_secc_Equip_ChapterRecord", new[] { "Guid" });
            DropIndex("dbo._org_secc_Equip_ChapterRecord", new[] { "ModifiedByPersonAliasId" });
            DropIndex("dbo._org_secc_Equip_ChapterRecord", new[] { "CreatedByPersonAliasId" });
            DropIndex("dbo._org_secc_Equip_ChapterRecord", new[] { "ChapterId" });
            DropIndex("dbo._org_secc_Equip_ChapterRecord", new[] { "CourseRecordId" });
            DropTable("dbo._org_secc_Equip_CourseRequirementStatus");
            DropTable("dbo._org_secc_Equip_CourseRequirement");
            DropTable("dbo._org_secc_Equip_CourseRecord");
            DropTable("dbo._org_secc_Equip_CoursePageRecord");
            DropTable("dbo._org_secc_Equip_CoursePage");
            DropTable("dbo._org_secc_Equip_Course");
            DropTable("dbo._org_secc_Equip_Chapter");
            DropTable("dbo._org_secc_Equip_ChapterRecord");
        }
    }
}
