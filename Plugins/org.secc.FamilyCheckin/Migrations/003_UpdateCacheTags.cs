using org.secc.FamilyCheckin.Utilities;
using Rock.Plugin;

namespace org.secc.FamilyCheckin.Migrations
{

    [MigrationNumber( 3, "1.8.0" )]
    public partial class UpdateCacheTags : Migration
    {
        public override void Up()
        {
            RockMigrationHelper.AddDefinedValue( Rock.SystemGuid.DefinedType.CACHE_TAGS, Constants.CACHE_TAG, "Cache tag for SECC Family Checkin" );
        }

        public override void Down()
        {
        }


    }
}
