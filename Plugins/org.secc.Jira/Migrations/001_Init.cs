namespace org.secc.Jira.Migrations
{
    using org.secc.DevLib.Extensions.Migration;

    using Rock.Plugin;

    [MigrationNumber( 1, "1.12.0" )]
    public partial class Init : Migration
    {
        public override void Up()
        {
            CreateTable(
                "dbo._org_secc_Jira_JiraTopic",
                c => new
                {
                    Id = c.Int( nullable: false, identity: true ),
                    Name = c.String(),
                    JQL = c.String(),
                    Order = c.Int( nullable: false ),
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
                .ForeignKey( "dbo.PersonAlias", t => t.CreatedByPersonAliasId )
                .ForeignKey( "dbo.PersonAlias", t => t.ModifiedByPersonAliasId )
                .Index( t => t.CreatedByPersonAliasId )
                .Index( t => t.ModifiedByPersonAliasId )
                .Index( t => t.Guid, unique: true )
                .Run( this );

            CreateTable(
                "dbo._org_secc_Jira_JiraTicket",
                c => new
                {
                    Id = c.Int( nullable: false, identity: true ),
                    JiraTopicId = c.Int( nullable: false ),
                    JiraId = c.Int( nullable: false ),
                    Key = c.String( maxLength: 100 ),
                    Title = c.String(),
                    Description = c.String(),
                    TicketType = c.Int( nullable: false ),
                    CreatedDateTime = c.DateTime(),
                    ModifiedDateTime = c.DateTime(),
                    CreatedByPersonAliasId = c.Int(),
                    ModifiedByPersonAliasId = c.Int(),
                    Guid = c.Guid( nullable: false ),
                    ForeignId = c.Int(),
                    ForeignGuid = c.Guid(),
                    ForeignKey = c.String( maxLength: 100 ),
                    JiraTopic_Id = c.Int(),
                } )
                .PrimaryKey( t => t.Id )
                .ForeignKey( "dbo.PersonAlias", t => t.CreatedByPersonAliasId )
                .ForeignKey( "dbo._org_secc_Jira_JiraTopic", t => t.JiraTopicId )
                .ForeignKey( "dbo.PersonAlias", t => t.ModifiedByPersonAliasId )
                .ForeignKey( "dbo._org_secc_Jira_JiraTopic", t => t.JiraTopic_Id )
                .Index( t => t.JiraTopicId )
                .Index( t => t.Key, unique: true )
                .Index( t => t.CreatedByPersonAliasId )
                .Index( t => t.ModifiedByPersonAliasId )
                .Index( t => t.Guid, unique: true )
                .Index( t => t.JiraTopic_Id )
                .Run( this );

        }

        public override void Down()
        {
            DropForeignKey( "dbo._org_secc_Jira_JiraTopic", "ModifiedByPersonAliasId", "dbo.PersonAlias" );
            DropForeignKey( "dbo._org_secc_Jira_JiraTicket", "JiraTopic_Id", "dbo._org_secc_Jira_JiraTopic" );
            DropForeignKey( "dbo._org_secc_Jira_JiraTicket", "ModifiedByPersonAliasId", "dbo.PersonAlias" );
            DropForeignKey( "dbo._org_secc_Jira_JiraTicket", "JiraTopicId", "dbo._org_secc_Jira_JiraTopic" );
            DropForeignKey( "dbo._org_secc_Jira_JiraTicket", "CreatedByPersonAliasId", "dbo.PersonAlias" );
            DropForeignKey( "dbo._org_secc_Jira_JiraTopic", "CreatedByPersonAliasId", "dbo.PersonAlias" );
            DropIndex( "dbo._org_secc_Jira_JiraTicket", new[] { "JiraTopic_Id" } );
            DropIndex( "dbo._org_secc_Jira_JiraTicket", new[] { "Guid" } );
            DropIndex( "dbo._org_secc_Jira_JiraTicket", new[] { "ModifiedByPersonAliasId" } );
            DropIndex( "dbo._org_secc_Jira_JiraTicket", new[] { "CreatedByPersonAliasId" } );
            DropIndex( "dbo._org_secc_Jira_JiraTicket", new[] { "Key" } );
            DropIndex( "dbo._org_secc_Jira_JiraTicket", new[] { "JiraTopicId" } );
            DropIndex( "dbo._org_secc_Jira_JiraTopic", new[] { "Guid" } );
            DropIndex( "dbo._org_secc_Jira_JiraTopic", new[] { "ModifiedByPersonAliasId" } );
            DropIndex( "dbo._org_secc_Jira_JiraTopic", new[] { "CreatedByPersonAliasId" } );
            DropTable( "dbo._org_secc_Jira_JiraTicket" );
            DropTable( "dbo._org_secc_Jira_JiraTopic" );
        }
    }
}
