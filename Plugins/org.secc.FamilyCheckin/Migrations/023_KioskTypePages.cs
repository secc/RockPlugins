// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License shoud be included with this file.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
namespace org.secc.FamilyCheckin.Migrations
{
    using Rock.Plugin;
    using org.secc.DevLib.Extensions.Migration;
    using org.secc.FamilyCheckin.Utilities;

    [MigrationNumber( 23, "1.10.2" )]
    public partial class KioskTypePages : Migration
    {
        public override void Up()
        {
            // Page: Kiosk Types
            RockMigrationHelper.AddPage("66C5DD58-094C-4FF9-9AFB-44801FCFCC2D","D65F783D-87A9-4CC9-8110-E83466A0EADB","Kiosk Types","","ED359351-07B1-4317-B461-0A1C9986097D","fa fa-hand-pointer-o"); // Site:Rock RMS
            // Page: Kiosks
            RockMigrationHelper.AddPage("66C5DD58-094C-4FF9-9AFB-44801FCFCC2D","D65F783D-87A9-4CC9-8110-E83466A0EADB","Kiosks","","79B0B175-53FE-46E0-B17B-5DBF8D7342EB","fa fa-power-off"); // Site:Rock RMS
            // Page: Kiosk Type Detail
            RockMigrationHelper.AddPage("ED359351-07B1-4317-B461-0A1C9986097D","D65F783D-87A9-4CC9-8110-E83466A0EADB","Kiosk Type Detail","","87189860-B632-4257-A960-A1EA4C06B329","fa fa-hand-pointer-o"); // Site:Rock RMS
            // Page: Kiosk Detail
            RockMigrationHelper.AddPage("79B0B175-53FE-46E0-B17B-5DBF8D7342EB","D65F783D-87A9-4CC9-8110-E83466A0EADB","Kiosk Detail","","BAE5FEEE-2608-4872-BD50-C86A18CE8441","fa fa-power-off"); // Site:Rock RMS
            RockMigrationHelper.UpdateBlockType( "Kiosk Detail", "Displays the details of the given device.", "~/Plugins/org_secc/FamilyCheckin/KioskDetail.ascx", "SECC > Check-in", "DF7DF5AF-2501-4E21-B1CC-F4F3F2E070B8" );
            RockMigrationHelper.UpdateBlockType( "Kiosk List", "Lists all the kiosks.", "~/Plugins/org_secc/FamilyCheckin/KioskList.ascx", "SECC > Check-in", "6DD6580D-7710-43B1-BE07-4EA9E9409F3A" );
            RockMigrationHelper.UpdateBlockType( "Kiosk Type Detail", "Displays the details of the given device.", "~/Plugins/org_secc/FamilyCheckin/KioskTypeDetail.ascx", "SECC > Check-in", "C8A43A82-222F-49A1-AA1C-98F8774CBCF4" );
            RockMigrationHelper.UpdateBlockType( "Kiosk Type List", "Lists all the kiosk types.", "~/Plugins/org_secc/FamilyCheckin/KioskTypeList.ascx", "SECC > Check-in", "4514023F-9A50-4D42-906D-7858FE4607EC" );
            RockMigrationHelper.AddBlock( "ED359351-07B1-4317-B461-0A1C9986097D", "", "4514023F-9A50-4D42-906D-7858FE4607EC", "Kiosk Type List", "Main", "", "", 0, "C87DE101-B245-4483-B264-020C2738CF9F" );
            RockMigrationHelper.AddBlock( "79B0B175-53FE-46E0-B17B-5DBF8D7342EB", "", "6DD6580D-7710-43B1-BE07-4EA9E9409F3A", "Kiosk List", "Main", "", "", 0, "14A3CCF9-0B60-4AF1-991F-CE61D5CCF706" );
            RockMigrationHelper.AddBlock( "87189860-B632-4257-A960-A1EA4C06B329", "", "C8A43A82-222F-49A1-AA1C-98F8774CBCF4", "Kiosk Type Detail", "Main", "", "", 0, "E29F46FA-E82A-471F-A9E6-B3D9D7BAC2C1" );
            RockMigrationHelper.AddBlock( "BAE5FEEE-2608-4872-BD50-C86A18CE8441", "", "DF7DF5AF-2501-4E21-B1CC-F4F3F2E070B8", "Kiosk Detail", "Main", "", "", 0, "26146D07-FFF1-4281-90A6-FBA1BDE259AE" );
            RockMigrationHelper.AddBlockTypeAttribute( "4514023F-9A50-4D42-906D-7858FE4607EC", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Detail Page", "DetailPage", "", "", 0, @"", "504F73C1-FE34-4343-8BA2-F76AC99D640E" );
            RockMigrationHelper.AddBlockTypeAttribute( "6DD6580D-7710-43B1-BE07-4EA9E9409F3A", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Detail Page", "DetailPage", "", "", 0, @"", "3F985428-0111-41E5-AE3B-2992B2B62525" );
            RockMigrationHelper.AddBlockAttributeValue( "C87DE101-B245-4483-B264-020C2738CF9F", "504F73C1-FE34-4343-8BA2-F76AC99D640E", @"87189860-b632-4257-a960-a1ea4c06b329" ); // Detail Page  
            RockMigrationHelper.AddBlockAttributeValue( "14A3CCF9-0B60-4AF1-991F-CE61D5CCF706", "3F985428-0111-41E5-AE3B-2992B2B62525", @"bae5feee-2608-4872-bd50-c86a18ce8441" ); // Detail Page  
        }

        public override void Down()
        {

        }
    }
}
