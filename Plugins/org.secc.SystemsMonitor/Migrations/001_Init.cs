namespace org.secc.SystemsMonitor.Migrations
{
    using org.secc.DevLib.Extensions.Migration;
    using Rock.Plugin;

    [MigrationNumber( 1, "1.10.0" )]
    public partial class Init : Migration
    {
        public override void Up()
        {
            CreateTable(
             "dbo._org_secc_SystemsMonitor_SystemTest",
             c => new
             {
                 Id = c.Int( nullable: false, identity: true ),
                 Name = c.String(),
                 Description = c.String(),
                 RunIntervalMinutes = c.Int(),
                 AlarmCondition = c.Int( nullable: false ),
                 AlarmScore = c.Int(),
                 EntityTypeId = c.Int(),
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
             .ForeignKey( "dbo.EntityType", t => t.EntityTypeId )
             .ForeignKey( "dbo.PersonAlias", t => t.ModifiedByPersonAliasId )
             .Index( t => t.EntityTypeId )
             .Index( t => t.CreatedByPersonAliasId )
             .Index( t => t.ModifiedByPersonAliasId )
             .Index( t => t.Guid, unique: true )
             .Run( this );


            CreateTable(
                "dbo._org_secc_SystemsMonitor_SystemTestHistory",
                c => new
                {
                    Id = c.Int( nullable: false, identity: true ),
                    SystemTestId = c.Int( nullable: false ),
                    Score = c.Int( nullable: false ),
                    Passed = c.Boolean( nullable: false ),
                    Message = c.String(),
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
                .ForeignKey( "dbo._org_secc_SystemsMonitor_SystemTest", t => t.SystemTestId )
                .Index( t => t.SystemTestId )
                .Index( t => t.CreatedByPersonAliasId )
                .Index( t => t.ModifiedByPersonAliasId )
                .Index( t => t.Guid, unique: true )
                .Run( this );
        }

        public override void Down()
        {
        }
    }
}
