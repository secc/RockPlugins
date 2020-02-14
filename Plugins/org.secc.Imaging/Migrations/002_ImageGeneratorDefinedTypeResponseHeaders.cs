using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.secc.Imaging.Migrations
{
    using Rock.Plugin;

    [MigrationNumber( 2, "1.8.0" )]

    public class _002_ImageGeneratorDefinedTypeResponseHeaders : Migration
    {

        public override void Up()
        {
            RockMigrationHelper.AddDefinedTypeAttribute( "8CC5A105-E6B9-4F63-89E0-A83F81ACBB21", "73B02051-0D38-4AD9-BF81-A2D477DE4F70", "Response Headers", "ResponseHeaders", "", 5, "", "D39AFB27-C4F1-4F3B-992D-6496CC37A826" );
            RockMigrationHelper.AddAttributeQualifier( "D39AFB27-C4F1-4F3B-992D-6496CC37A826", "allowhtml", "False", "10E1EF5E-0BEE-4CD6-BBD9-BF91082D62CB" );
            RockMigrationHelper.AddAttributeQualifier( "D39AFB27-C4F1-4F3B-992D-6496CC37A826", "customvalues", "", "4AF90BEB-B9FB-4622-9203-F03E91D4C341" );
            RockMigrationHelper.AddAttributeQualifier( "D39AFB27-C4F1-4F3B-992D-6496CC37A826", "definedtype", "", "3399647C-8786-4866-8F33-406B445FC3DA" );
            RockMigrationHelper.AddAttributeQualifier( "D39AFB27-C4F1-4F3B-992D-6496CC37A826", "displayvaluefirst", "False", "C7F313BE-55D7-47E7-9E6A-31E4A8F5B7A1" );
            RockMigrationHelper.AddAttributeQualifier( "D39AFB27-C4F1-4F3B-992D-6496CC37A826", "keyprompt", "Name", "2D84C3C9-E338-4431-93DD-57074508432A" );
            RockMigrationHelper.AddAttributeQualifier( "D39AFB27-C4F1-4F3B-992D-6496CC37A826", "valueprompt", "Value", "36A10CB1-90D1-486F-926F-5146B4D96C10" );
        }

        public override void Down()
        {
            RockMigrationHelper.DeleteAttribute( "D39AFB27-C4F1-4F3B-992D-6496CC37A826" ); // ResponseHeaders
        }
    }
}
