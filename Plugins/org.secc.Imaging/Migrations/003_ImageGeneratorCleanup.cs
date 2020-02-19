using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.secc.Imaging.Migrations
{
    using Rock.Plugin;

    [MigrationNumber( 3, "1.8.0" )]

    public class _003_ImageGeneratorCleanup : Migration
    {

        public override void Up()
        {
            RockMigrationHelper.DeleteAttribute( "3735CF5F-405A-4914-9880-9CC64343CD11" ); // ImageType
            RockMigrationHelper.UpdateDefinedTypeAttribute( "8CC5A105-E6B9-4F63-89E0-A83F81ACBB21", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Canvas Height", "CanvasHeight", "This is an optional parameter which will set the height.  If blank, the image generator will attempt to automatically set it based on the HTML template.", 4, false, "", false, false, "D7759B7A-7D4B-4370-A665-18396F7D6768" );
            RockMigrationHelper.UpdateDefinedTypeAttribute( "8CC5A105-E6B9-4F63-89E0-A83F81ACBB21", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Canvas Width", "CanvasWidth", "This sets the minimum width of the canvas for the image.  If blank, the image generator will automatically attempt to determine the width based on the HTML template.", 3, false, "", false, false, "9191028F-A80B-4D1A-B8B8-3B11E4DFDAD5" );
        }

        public override void Down()
        {
        }
    }
}
