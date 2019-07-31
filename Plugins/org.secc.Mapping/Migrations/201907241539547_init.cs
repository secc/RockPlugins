namespace org.secc.Mapping.Migrations
{
    using Rock.Plugin;

    [MigrationNumber( 1, "1.8.0" )]

    public partial class Init : Migration
    {
        public override void Up()
        {
            CreateTable(
                "dbo._org_secc_Mapping_LocationDistanceStore",
                c => new
                {
                    Id = c.Int( nullable: false, identity: true ),
                    Origin = c.String(),
                    Destination = c.String(),
                    TravelDistance = c.Double( nullable: false ),
                    TravelDuration = c.Double( nullable: false ),
                    CalculatedBy = c.String(),
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
                .Index( t => t.Origin )
                .Index( t => t.Destination )
                .Index( t => t.CreatedByPersonAliasId )
                .Index( t => t.ModifiedByPersonAliasId )
                .Index( t => t.Guid, unique: true );


        }

        public override void Down()
        {
            DropIndex( "dbo._org_secc_Mapping_LocationDistanceStore", new[] { "Guid" } );
            DropIndex( "dbo._org_secc_Mapping_LocationDistanceStore", new[] { "ModifiedByPersonAliasId" } );
            DropIndex( "dbo._org_secc_Mapping_LocationDistanceStore", new[] { "CreatedByPersonAliasId" } );
            DropIndex( "dbo._org_secc_Mapping_LocationDistanceStore", new[] { "Destination" } );
            DropIndex( "dbo._org_secc_Mapping_LocationDistanceStore", new[] { "Origin" } );
            DropTable( "dbo._org_secc_Mapping_LocationDistanceStore" );
        }
    }
}
