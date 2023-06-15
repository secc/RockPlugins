namespace org.secc.Equip.Migrations
{
    using Rock.Plugin;

    [MigrationNumber( 1, "1.8.0" )]
    public partial class Init : Migration
    {
        public override void Up()
        {
            AddTable(
                "dbo._org_secc_Equip_ChapterRecord",
                c => new
                {
                    Id = c.Int( nullable: false, identity: true ),
                    CourseRecordId = c.Int( nullable: false ),
                    CompletionDateTime = c.DateTime(),
                    ChapterId = c.Int( nullable: false ),
                    Passed = c.Boolean( nullable: false ),
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
                "dbo._org_secc_Equip_Chapter",
                c => new
                {
                    Id = c.Int( nullable: false, identity: true ),
                    Name = c.String(),
                    Description = c.String(),
                    CourseId = c.Int( nullable: false ),
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
                "dbo._org_secc_Equip_Course",
                c => new
                {
                    Id = c.Int( nullable: false, identity: true ),
                    Name = c.String(),
                    Description = c.String(),
                    CategoryId = c.Int(),
                    AllowDocumentationMode = c.Boolean( nullable: false ),
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
                "dbo._org_secc_Equip_CoursePage",
                c => new
                {
                    Id = c.Int( nullable: false, identity: true ),
                    Name = c.String(),
                    ChapterId = c.Int( nullable: false ),
                    PassingScore = c.Int(),
                    Configuration = c.String(),
                    EntityTypeId = c.Int( nullable: false ),
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
                "dbo._org_secc_Equip_CoursePageRecord",
                c => new
                {
                    Id = c.Int( nullable: false, identity: true ),
                    ChapterRecordId = c.Int( nullable: false ),
                    CoursePageId = c.Int( nullable: false ),
                    Score = c.Int( nullable: false ),
                    Passed = c.Boolean( nullable: false ),
                    CompletionDetails = c.String(),
                    CompletionDateTime = c.DateTime(),
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
                "dbo._org_secc_Equip_CourseRecord",
                c => new
                {
                    Id = c.Int( nullable: false, identity: true ),
                    PersonAliasId = c.Int( nullable: false ),
                    CourseId = c.Int( nullable: false ),
                    CompletionDateTime = c.DateTime(),
                    Passed = c.Boolean( nullable: false ),
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
                "dbo._org_secc_Equip_CourseRequirement",
                c => new
                {
                    Id = c.Int( nullable: false, identity: true ),
                    CourseId = c.Int( nullable: false ),
                    GroupId = c.Int(),
                    DataViewId = c.Int(),
                    DaysValid = c.Int(),
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
                "dbo._org_secc_Equip_CourseRequirementStatus",
                c => new
                {
                    Id = c.Int( nullable: false, identity: true ),
                    CourseRequirementId = c.Int( nullable: false ),
                    PersonAliasId = c.Int( nullable: false ),
                    IsComplete = c.Boolean( nullable: false ),
                    ValidUntil = c.DateTime(),
                    CreatedDateTime = c.DateTime(),
                    ModifiedDateTime = c.DateTime(),
                    CreatedByPersonAliasId = c.Int(),
                    ModifiedByPersonAliasId = c.Int(),
                    Guid = c.Guid( nullable: false ),
                    ForeignId = c.Int(),
                    ForeignGuid = c.Guid(),
                    ForeignKey = c.String( maxLength: 100 ),
                } );

            AddPrimaryKey( "dbo._org_secc_Equip_ChapterRecord", "Id" );
            AddPrimaryKey( "dbo._org_secc_Equip_Chapter", "Id" );
            AddPrimaryKey( "dbo._org_secc_Equip_Course", "Id" );
            AddPrimaryKey( "dbo._org_secc_Equip_CoursePage", "Id" );
            AddPrimaryKey( "dbo._org_secc_Equip_CoursePageRecord", "Id" );
            AddPrimaryKey( "dbo._org_secc_Equip_CourseRecord", "Id" );
            AddPrimaryKey( "dbo._org_secc_Equip_CourseRequirement", "Id" );
            AddPrimaryKey( "dbo._org_secc_Equip_CourseRequirementStatus", "Id" );

            AddForeignKey( "dbo._org_secc_Equip_ChapterRecord", "ChapterId", "dbo._org_secc_Equip_Chapter", "Id", cascadeDelete: true );
            AddForeignKey( "dbo._org_secc_Equip_ChapterRecord", "CourseRecordId", "dbo._org_secc_Equip_CourseRecord", "Id", cascadeDelete: true );
            AddForeignKey( "dbo._org_secc_Equip_ChapterRecord", "CreatedByPersonAliasId", "dbo.PersonAlias", "Id" );
            AddForeignKey( "dbo._org_secc_Equip_ChapterRecord", "ModifiedByPersonAliasId", "dbo.PersonAlias", "Id" );
            AddIndex( "dbo._org_secc_Equip_ChapterRecord", "CourseRecordId" );
            AddIndex( "dbo._org_secc_Equip_ChapterRecord", "ChapterId" );
            AddIndex( "dbo._org_secc_Equip_ChapterRecord", "CreatedByPersonAliasId" );
            AddIndex( "dbo._org_secc_Equip_ChapterRecord", "ModifiedByPersonAliasId" );
            AddIndex( "dbo._org_secc_Equip_ChapterRecord", "Guid", unique: true );

            AddForeignKey( "dbo._org_secc_Equip_Chapter", "CourseId", "dbo._org_secc_Equip_Course", "Id", cascadeDelete: true );
            AddForeignKey( "dbo._org_secc_Equip_Chapter", "CreatedByPersonAliasId", "dbo.PersonAlias", "Id" );
            AddForeignKey( "dbo._org_secc_Equip_Chapter", "ModifiedByPersonAliasId", "dbo.PersonAlias", "Id" );
            AddIndex( "dbo._org_secc_Equip_Chapter", "CourseId" );
            AddIndex( "dbo._org_secc_Equip_Chapter", "CreatedByPersonAliasId" );
            AddIndex( "dbo._org_secc_Equip_Chapter", "ModifiedByPersonAliasId" );
            AddIndex( "dbo._org_secc_Equip_Chapter", "Guid", unique: true );

            AddForeignKey( "dbo._org_secc_Equip_Course", "CategoryId", "dbo.Category", "Id" );
            AddForeignKey( "dbo._org_secc_Equip_Course", "CreatedByPersonAliasId", "dbo.PersonAlias", "Id" );
            AddForeignKey( "dbo._org_secc_Equip_Course", "ModifiedByPersonAliasId", "dbo.PersonAlias", "Id" );
            AddIndex( "dbo._org_secc_Equip_Course", "CategoryId" );
            AddIndex( "dbo._org_secc_Equip_Course", "CreatedByPersonAliasId" );
            AddIndex( "dbo._org_secc_Equip_Course", "ModifiedByPersonAliasId" );
            AddIndex( "dbo._org_secc_Equip_Course", "Guid", unique: true );

            AddForeignKey( "dbo._org_secc_Equip_CoursePage", "ChapterId", "dbo._org_secc_Equip_Chapter", "Id", cascadeDelete: true );
            AddForeignKey( "dbo._org_secc_Equip_CoursePage", "CreatedByPersonAliasId", "dbo.PersonAlias", "Id" );
            AddForeignKey( "dbo._org_secc_Equip_CoursePage", "EntityTypeId", "dbo.EntityType", "Id", cascadeDelete: true );
            AddForeignKey( "dbo._org_secc_Equip_CoursePage", "ModifiedByPersonAliasId", "dbo.PersonAlias", "Id" );
            AddIndex( "dbo._org_secc_Equip_CoursePage", "ChapterId" );
            AddIndex( "dbo._org_secc_Equip_CoursePage", "EntityTypeId" );
            AddIndex( "dbo._org_secc_Equip_CoursePage", "CreatedByPersonAliasId" );
            AddIndex( "dbo._org_secc_Equip_CoursePage", "ModifiedByPersonAliasId" );
            AddIndex( "dbo._org_secc_Equip_CoursePage", "Guid", unique: true );

            AddForeignKey( "dbo._org_secc_Equip_CoursePageRecord", "ChapterRecordId", "dbo._org_secc_Equip_ChapterRecord", "Id", cascadeDelete: true );
            AddForeignKey( "dbo._org_secc_Equip_CoursePageRecord", "CoursePageId", "dbo._org_secc_Equip_CoursePage", "Id" );
            AddForeignKey( "dbo._org_secc_Equip_CoursePageRecord", "CreatedByPersonAliasId", "dbo.PersonAlias", "Id" );
            AddForeignKey( "dbo._org_secc_Equip_CoursePageRecord", "ModifiedByPersonAliasId", "dbo.PersonAlias", "Id" );
            AddIndex( "dbo._org_secc_Equip_CoursePageRecord", "ChapterRecordId" );
            AddIndex( "dbo._org_secc_Equip_CoursePageRecord", "CoursePageId" );
            AddIndex( "dbo._org_secc_Equip_CoursePageRecord", "CreatedByPersonAliasId" );
            AddIndex( "dbo._org_secc_Equip_CoursePageRecord", "ModifiedByPersonAliasId" );
            AddIndex( "dbo._org_secc_Equip_CoursePageRecord", "Guid", unique: true );

            AddForeignKey( "dbo._org_secc_Equip_CourseRecord", "CourseId", "dbo._org_secc_Equip_Course", "Id" );
            AddForeignKey( "dbo._org_secc_Equip_CourseRecord", "CreatedByPersonAliasId", "dbo.PersonAlias", "Id" );
            AddForeignKey( "dbo._org_secc_Equip_CourseRecord", "ModifiedByPersonAliasId", "dbo.PersonAlias", "Id" );
            AddForeignKey( "dbo._org_secc_Equip_CourseRecord", "PersonAliasId", "dbo.PersonAlias", "Id", cascadeDelete: true );
            AddIndex( "dbo._org_secc_Equip_CourseRecord", "PersonAliasId" );
            AddIndex( "dbo._org_secc_Equip_CourseRecord", "CourseId" );
            AddIndex( "dbo._org_secc_Equip_CourseRecord", "CreatedByPersonAliasId" );
            AddIndex( "dbo._org_secc_Equip_CourseRecord", "ModifiedByPersonAliasId" );
            AddIndex( "dbo._org_secc_Equip_CourseRecord", "Guid", unique: true );

            AddForeignKey( "dbo._org_secc_Equip_CourseRequirement", "CourseId", "dbo._org_secc_Equip_Course", "Id", cascadeDelete: true );
            AddForeignKey( "dbo._org_secc_Equip_CourseRequirement", "CreatedByPersonAliasId", "dbo.PersonAlias", "Id" );
            AddForeignKey( "dbo._org_secc_Equip_CourseRequirement", "DataViewId", "dbo.DataView", "Id" );
            AddForeignKey( "dbo._org_secc_Equip_CourseRequirement", "GroupId", "dbo.Group", "Id" );
            AddForeignKey( "dbo._org_secc_Equip_CourseRequirement", "ModifiedByPersonAliasId", "dbo.PersonAlias", "Id" );
            AddIndex( "dbo._org_secc_Equip_CourseRequirement", "CourseId" );
            AddIndex( "dbo._org_secc_Equip_CourseRequirement", "GroupId" );
            AddIndex( "dbo._org_secc_Equip_CourseRequirement", "DataViewId" );
            AddIndex( "dbo._org_secc_Equip_CourseRequirement", "CreatedByPersonAliasId" );
            AddIndex( "dbo._org_secc_Equip_CourseRequirement", "ModifiedByPersonAliasId" );
            AddIndex( "dbo._org_secc_Equip_CourseRequirement", "Guid", unique: true );

            AddForeignKey( "dbo._org_secc_Equip_CourseRequirementStatus", "CourseRequirementId", "dbo._org_secc_Equip_CourseRequirement", "Id", cascadeDelete: true );
            AddForeignKey( "dbo._org_secc_Equip_CourseRequirementStatus", "CreatedByPersonAliasId", "dbo.PersonAlias", "Id" );
            AddForeignKey( "dbo._org_secc_Equip_CourseRequirementStatus", "ModifiedByPersonAliasId", "dbo.PersonAlias", "Id" );
            AddForeignKey( "dbo._org_secc_Equip_CourseRequirementStatus", "PersonAliasId", "dbo.PersonAlias", "Id", cascadeDelete: true );
            AddIndex( "dbo._org_secc_Equip_CourseRequirementStatus", "CourseRequirementId" );
            AddIndex( "dbo._org_secc_Equip_CourseRequirementStatus", "PersonAliasId" );
            AddIndex( "dbo._org_secc_Equip_CourseRequirementStatus", "CreatedByPersonAliasId" );
            AddIndex( "dbo._org_secc_Equip_CourseRequirementStatus", "ModifiedByPersonAliasId" );
            AddIndex( "dbo._org_secc_Equip_CourseRequirementStatus", "Guid", unique: true );
        }

        public override void Down()
        {

            DropForeignKey( "dbo._org_secc_Equip_CourseRequirementStatus", "PersonAliasId", "dbo.PersonAlias" );
            DropForeignKey( "dbo._org_secc_Equip_CourseRequirementStatus", "ModifiedByPersonAliasId", "dbo.PersonAlias" );
            DropForeignKey( "dbo._org_secc_Equip_CourseRequirementStatus", "CreatedByPersonAliasId", "dbo.PersonAlias" );
            DropForeignKey( "dbo._org_secc_Equip_CourseRequirementStatus", "CourseRequirementId", "dbo._org_secc_Equip_CourseRequirement" );
            DropForeignKey( "dbo._org_secc_Equip_CourseRequirement", "ModifiedByPersonAliasId", "dbo.PersonAlias" );
            DropForeignKey( "dbo._org_secc_Equip_CourseRequirement", "GroupId", "dbo.Group" );
            DropForeignKey( "dbo._org_secc_Equip_CourseRequirement", "DataViewId", "dbo.DataView" );
            DropForeignKey( "dbo._org_secc_Equip_CourseRequirement", "CreatedByPersonAliasId", "dbo.PersonAlias" );
            DropForeignKey( "dbo._org_secc_Equip_CourseRequirement", "CourseId", "dbo._org_secc_Equip_Course" );
            DropForeignKey( "dbo._org_secc_Equip_ChapterRecord", "ModifiedByPersonAliasId", "dbo.PersonAlias" );
            DropForeignKey( "dbo._org_secc_Equip_ChapterRecord", "CreatedByPersonAliasId", "dbo.PersonAlias" );
            DropForeignKey( "dbo._org_secc_Equip_CourseRecord", "PersonAliasId", "dbo.PersonAlias" );
            DropForeignKey( "dbo._org_secc_Equip_CourseRecord", "ModifiedByPersonAliasId", "dbo.PersonAlias" );
            DropForeignKey( "dbo._org_secc_Equip_CourseRecord", "CreatedByPersonAliasId", "dbo.PersonAlias" );
            DropForeignKey( "dbo._org_secc_Equip_CourseRecord", "CourseId", "dbo._org_secc_Equip_Course" );
            DropForeignKey( "dbo._org_secc_Equip_ChapterRecord", "CourseRecordId", "dbo._org_secc_Equip_CourseRecord" );
            DropForeignKey( "dbo._org_secc_Equip_CoursePageRecord", "ModifiedByPersonAliasId", "dbo.PersonAlias" );
            DropForeignKey( "dbo._org_secc_Equip_CoursePageRecord", "CreatedByPersonAliasId", "dbo.PersonAlias" );
            DropForeignKey( "dbo._org_secc_Equip_CoursePageRecord", "CoursePageId", "dbo._org_secc_Equip_CoursePage" );
            DropForeignKey( "dbo._org_secc_Equip_CoursePageRecord", "ChapterRecordId", "dbo._org_secc_Equip_ChapterRecord" );
            DropForeignKey( "dbo._org_secc_Equip_ChapterRecord", "ChapterId", "dbo._org_secc_Equip_Chapter" );
            DropForeignKey( "dbo._org_secc_Equip_Chapter", "ModifiedByPersonAliasId", "dbo.PersonAlias" );
            DropForeignKey( "dbo._org_secc_Equip_Chapter", "CreatedByPersonAliasId", "dbo.PersonAlias" );
            DropForeignKey( "dbo._org_secc_Equip_CoursePage", "ModifiedByPersonAliasId", "dbo.PersonAlias" );
            DropForeignKey( "dbo._org_secc_Equip_CoursePage", "EntityTypeId", "dbo.EntityType" );
            DropForeignKey( "dbo._org_secc_Equip_CoursePage", "CreatedByPersonAliasId", "dbo.PersonAlias" );
            DropForeignKey( "dbo._org_secc_Equip_CoursePage", "ChapterId", "dbo._org_secc_Equip_Chapter" );
            DropForeignKey( "dbo._org_secc_Equip_Course", "ModifiedByPersonAliasId", "dbo.PersonAlias" );
            DropForeignKey( "dbo._org_secc_Equip_Course", "CreatedByPersonAliasId", "dbo.PersonAlias" );
            DropForeignKey( "dbo._org_secc_Equip_Chapter", "CourseId", "dbo._org_secc_Equip_Course" );
            DropForeignKey( "dbo._org_secc_Equip_Course", "CategoryId", "dbo.Category" );
            DropIndex( "dbo._org_secc_Equip_CourseRequirementStatus", new[] { "Guid" } );
            DropIndex( "dbo._org_secc_Equip_CourseRequirementStatus", new[] { "ModifiedByPersonAliasId" } );
            DropIndex( "dbo._org_secc_Equip_CourseRequirementStatus", new[] { "CreatedByPersonAliasId" } );
            DropIndex( "dbo._org_secc_Equip_CourseRequirementStatus", new[] { "PersonAliasId" } );
            DropIndex( "dbo._org_secc_Equip_CourseRequirementStatus", new[] { "CourseRequirementId" } );
            DropIndex( "dbo._org_secc_Equip_CourseRequirement", new[] { "Guid" } );
            DropIndex( "dbo._org_secc_Equip_CourseRequirement", new[] { "ModifiedByPersonAliasId" } );
            DropIndex( "dbo._org_secc_Equip_CourseRequirement", new[] { "CreatedByPersonAliasId" } );
            DropIndex( "dbo._org_secc_Equip_CourseRequirement", new[] { "DataViewId" } );
            DropIndex( "dbo._org_secc_Equip_CourseRequirement", new[] { "GroupId" } );
            DropIndex( "dbo._org_secc_Equip_CourseRequirement", new[] { "CourseId" } );
            DropIndex( "dbo._org_secc_Equip_CourseRecord", new[] { "Guid" } );
            DropIndex( "dbo._org_secc_Equip_CourseRecord", new[] { "ModifiedByPersonAliasId" } );
            DropIndex( "dbo._org_secc_Equip_CourseRecord", new[] { "CreatedByPersonAliasId" } );
            DropIndex( "dbo._org_secc_Equip_CourseRecord", new[] { "CourseId" } );
            DropIndex( "dbo._org_secc_Equip_CourseRecord", new[] { "PersonAliasId" } );
            DropIndex( "dbo._org_secc_Equip_CoursePageRecord", new[] { "Guid" } );
            DropIndex( "dbo._org_secc_Equip_CoursePageRecord", new[] { "ModifiedByPersonAliasId" } );
            DropIndex( "dbo._org_secc_Equip_CoursePageRecord", new[] { "CreatedByPersonAliasId" } );
            DropIndex( "dbo._org_secc_Equip_CoursePageRecord", new[] { "CoursePageId" } );
            DropIndex( "dbo._org_secc_Equip_CoursePageRecord", new[] { "ChapterRecordId" } );
            DropIndex( "dbo._org_secc_Equip_CoursePage", new[] { "Guid" } );
            DropIndex( "dbo._org_secc_Equip_CoursePage", new[] { "ModifiedByPersonAliasId" } );
            DropIndex( "dbo._org_secc_Equip_CoursePage", new[] { "CreatedByPersonAliasId" } );
            DropIndex( "dbo._org_secc_Equip_CoursePage", new[] { "EntityTypeId" } );
            DropIndex( "dbo._org_secc_Equip_CoursePage", new[] { "ChapterId" } );
            DropIndex( "dbo._org_secc_Equip_Course", new[] { "Guid" } );
            DropIndex( "dbo._org_secc_Equip_Course", new[] { "ModifiedByPersonAliasId" } );
            DropIndex( "dbo._org_secc_Equip_Course", new[] { "CreatedByPersonAliasId" } );
            DropIndex( "dbo._org_secc_Equip_Course", new[] { "CategoryId" } );
            DropIndex( "dbo._org_secc_Equip_Chapter", new[] { "Guid" } );
            DropIndex( "dbo._org_secc_Equip_Chapter", new[] { "ModifiedByPersonAliasId" } );
            DropIndex( "dbo._org_secc_Equip_Chapter", new[] { "CreatedByPersonAliasId" } );
            DropIndex( "dbo._org_secc_Equip_Chapter", new[] { "CourseId" } );
            DropIndex( "dbo._org_secc_Equip_ChapterRecord", new[] { "Guid" } );
            DropIndex( "dbo._org_secc_Equip_ChapterRecord", new[] { "ModifiedByPersonAliasId" } );
            DropIndex( "dbo._org_secc_Equip_ChapterRecord", new[] { "CreatedByPersonAliasId" } );
            DropIndex( "dbo._org_secc_Equip_ChapterRecord", new[] { "ChapterId" } );
            DropIndex( "dbo._org_secc_Equip_ChapterRecord", new[] { "CourseRecordId" } );
            DropTable( "dbo._org_secc_Equip_CourseRequirementStatus" );
            DropTable( "dbo._org_secc_Equip_CourseRequirement" );
            DropTable( "dbo._org_secc_Equip_CourseRecord" );
            DropTable( "dbo._org_secc_Equip_CoursePageRecord" );
            DropTable( "dbo._org_secc_Equip_CoursePage" );
            DropTable( "dbo._org_secc_Equip_Course" );
            DropTable( "dbo._org_secc_Equip_Chapter" );
            DropTable( "dbo._org_secc_Equip_ChapterRecord" );
        }
    }
}
