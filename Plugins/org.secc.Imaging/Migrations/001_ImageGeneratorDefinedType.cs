using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.secc.Imaging.Migrations
{
    using Rock.Plugin;

    [MigrationNumber( 1, "1.8.0" )]

    public class _001_ImageGeneratorDefinedType : Migration
    {

        public override void Up()
        {
            RockMigrationHelper.AddDefinedType( "", "Html To Image", "Templates for creating images from Html and Lava", "8CC5A105-E6B9-4F63-89E0-A83F81ACBB21" );
            RockMigrationHelper.AddDefinedTypeAttribute( "8CC5A105-E6B9-4F63-89E0-A83F81ACBB21", "1D0D3794-C210-48A8-8C68-3FBEC08A6BA5", "Template", "Template", "", 0, "", "A247948E-AF88-4622-996D-DBE69CE41832" );
            RockMigrationHelper.AddDefinedTypeAttribute( "8CC5A105-E6B9-4F63-89E0-A83F81ACBB21", "4BD9088F-5CC6-89B1-45FC-A2AAFFC7CC0D", "Enabled Lava Commands", "EnabledLavaCommands", "", 1, "", "40286BE2-F841-423C-B920-78D827C65B81" );
            RockMigrationHelper.AddDefinedTypeAttribute( "8CC5A105-E6B9-4F63-89E0-A83F81ACBB21", "7525C4CB-EE6B-41D4-9B64-A08048D5A5C0", "Image Type", "ImageType", "", 2, "", "3735CF5F-405A-4914-9880-9CC64343CD11" );
            RockMigrationHelper.AddDefinedTypeAttribute( "8CC5A105-E6B9-4F63-89E0-A83F81ACBB21", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Height", "Height", "", 4, "", "D7759B7A-7D4B-4370-A665-18396F7D6768" );
            RockMigrationHelper.AddDefinedTypeAttribute( "8CC5A105-E6B9-4F63-89E0-A83F81ACBB21", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Width", "Width", "", 3, "", "9191028F-A80B-4D1A-B8B8-3B11E4DFDAD5" );
            RockMigrationHelper.AddAttributeQualifier( "3735CF5F-405A-4914-9880-9CC64343CD11", "fieldtype", "ddl", "27978DCD-6C83-4E5C-96CD-32DA5EDDF2F5" );
            RockMigrationHelper.AddAttributeQualifier( "3735CF5F-405A-4914-9880-9CC64343CD11", "repeatColumns", "", "50FF293A-53FB-4BE6-8F84-C6C4F4D118FE" );
            RockMigrationHelper.AddAttributeQualifier( "3735CF5F-405A-4914-9880-9CC64343CD11", "values", "png,jpg", "5BA047C2-B988-492B-8F46-D48FCF5EBDFE" );
            RockMigrationHelper.AddAttributeQualifier( "40286BE2-F841-423C-B920-78D827C65B81", "repeatColumns", "", "0BC7C7E7-5DE6-485D-97F4-6C630D337A81" );
            RockMigrationHelper.AddAttributeQualifier( "A247948E-AF88-4622-996D-DBE69CE41832", "editorHeight", "400", "CE4187E7-D83F-4BAF-8EBC-A1124474959B" );
            RockMigrationHelper.AddAttributeQualifier( "A247948E-AF88-4622-996D-DBE69CE41832", "editorMode", "3", "354730E0-C67A-4686-817F-11FE82A34AE0" );
            RockMigrationHelper.AddAttributeQualifier( "A247948E-AF88-4622-996D-DBE69CE41832", "editorTheme", "0", "FC2F6D93-69AC-4616-BF97-2CD3F6FAF856" );
        }

        public override void Down()
        {
            RockMigrationHelper.DeleteAttribute( "3735CF5F-405A-4914-9880-9CC64343CD11" ); // ImageType
            RockMigrationHelper.DeleteAttribute( "40286BE2-F841-423C-B920-78D827C65B81" ); // EnabledLavaCommands
            RockMigrationHelper.DeleteAttribute( "9191028F-A80B-4D1A-B8B8-3B11E4DFDAD5" ); // Width
            RockMigrationHelper.DeleteAttribute( "A247948E-AF88-4622-996D-DBE69CE41832" ); // Template
            RockMigrationHelper.DeleteAttribute( "D7759B7A-7D4B-4370-A665-18396F7D6768" ); // Height
            RockMigrationHelper.DeleteDefinedType( "8CC5A105-E6B9-4F63-89E0-A83F81ACBB21" ); // Image Generation
        }
    }
}
