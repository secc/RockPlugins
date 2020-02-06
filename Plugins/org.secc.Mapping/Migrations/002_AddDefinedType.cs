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
namespace org.secc.Mapping.Migrations
{
    using org.secc.Mapping.Utilities;
    using Rock.Plugin;

    [MigrationNumber( 2, "1.8.0" )]

    public partial class AddDefinedType : Migration
    {
        public override void Up()
        {
            RockMigrationHelper.AddDefinedType( "Tools", "Distance Mappable Entities", "Endpoint for getting distance for various entities. Route is /api/mapping/distance/{{DefinedValueId}}/{{Address}}", Constants.UniversalDefinedTypeGuid, @"" );
            RockMigrationHelper.AddDefinedTypeAttribute( "7F70ABE9-1705-4DED-BABE-6D720EC52914", "7525C4CB-EE6B-41D4-9B64-A08048D5A5C0", "Entity Type", "EntityType", "", 1248, "", "A4E5D4F4-580D-459E-8C83-C6A92B47396A" );
            RockMigrationHelper.AddDefinedTypeAttribute( "7F70ABE9-1705-4DED-BABE-6D720EC52914", "9C204CD0-1233-41C5-818A-C5DA439445AA", "Entity Id", "EntityId", "The id of the entity (Group Type Id, Parent Group Id or Attribute Id) to calculate distances from.", 1249, "", "1BC6FD9F-DDE2-4083-9539-7821C728014A" );
            RockMigrationHelper.AddAttributeQualifier( "1BC6FD9F-DDE2-4083-9539-7821C728014A", "ispassword", "False", "FDCC5DBA-F3A9-4896-BB35-F93B630612BB" );
            RockMigrationHelper.AddAttributeQualifier( "1BC6FD9F-DDE2-4083-9539-7821C728014A", "maxcharacters", "", "1257C2FD-BB2B-4BBE-992E-54BFCB829FF4" );
            RockMigrationHelper.AddAttributeQualifier( "1BC6FD9F-DDE2-4083-9539-7821C728014A", "showcountdown", "False", "7E762A8E-39D4-4EF2-8092-8C5407127BEA" );
            RockMigrationHelper.AddAttributeQualifier( "A4E5D4F4-580D-459E-8C83-C6A92B47396A", "fieldtype", "ddl", "8AE3FF18-6B1E-4096-8494-3B798AAA471B" );
            RockMigrationHelper.AddAttributeQualifier( "A4E5D4F4-580D-459E-8C83-C6A92B47396A", "repeatColumns", "", "4D143B57-93D5-47EE-9697-2F26879DBF73" );
            RockMigrationHelper.AddAttributeQualifier( "A4E5D4F4-580D-459E-8C83-C6A92B47396A", "values", "GroupType^Group Type,ParentGroup^Child Groups of Group,Campus^Campus,Attribute^Address Attribute", "97BB896B-A83D-4F96-8677-4FCD2609F1D5" );
            RockMigrationHelper.UpdateDefinedValue( "7F70ABE9-1705-4DED-BABE-6D720EC52914", "Blankenbaker Home Groups", "", "2E216362-0B05-4CE7-BBA8-E2C3DCAAEB07", false );
            RockMigrationHelper.UpdateDefinedValue( "7F70ABE9-1705-4DED-BABE-6D720EC52914", "Campus", "", "EB39F521-F9AA-4812-BA8F-102C029C8516", false );
            RockMigrationHelper.UpdateDefinedValue( "7F70ABE9-1705-4DED-BABE-6D720EC52914", "Content Channel Attributes", "", "F4D7D410-5096-460F-B740-4F6E0673CE16", false );
            RockMigrationHelper.UpdateDefinedValue( "7F70ABE9-1705-4DED-BABE-6D720EC52914", "Home Groups", "", "7E83A576-07FC-49EA-98BC-73C123D24DD8", false );
            RockMigrationHelper.AddDefinedValueAttributeValue( "2E216362-0B05-4CE7-BBA8-E2C3DCAAEB07", "1BC6FD9F-DDE2-4083-9539-7821C728014A", @"820057" );
            RockMigrationHelper.AddDefinedValueAttributeValue( "2E216362-0B05-4CE7-BBA8-E2C3DCAAEB07", "A4E5D4F4-580D-459E-8C83-C6A92B47396A", @"ParentGroup" );
            RockMigrationHelper.AddDefinedValueAttributeValue( "7E83A576-07FC-49EA-98BC-73C123D24DD8", "1BC6FD9F-DDE2-4083-9539-7821C728014A", @"60" );
            RockMigrationHelper.AddDefinedValueAttributeValue( "7E83A576-07FC-49EA-98BC-73C123D24DD8", "A4E5D4F4-580D-459E-8C83-C6A92B47396A", @"GroupType" );
            RockMigrationHelper.AddDefinedValueAttributeValue( "EB39F521-F9AA-4812-BA8F-102C029C8516", "A4E5D4F4-580D-459E-8C83-C6A92B47396A", @"Campus" );
            RockMigrationHelper.AddDefinedValueAttributeValue( "F4D7D410-5096-460F-B740-4F6E0673CE16", "1BC6FD9F-DDE2-4083-9539-7821C728014A", @"89281" );
            RockMigrationHelper.AddDefinedValueAttributeValue( "F4D7D410-5096-460F-B740-4F6E0673CE16", "A4E5D4F4-580D-459E-8C83-C6A92B47396A", @"Attribute" );
        }

        public override void Down()
        {
            RockMigrationHelper.DeleteAttribute( "1BC6FD9F-DDE2-4083-9539-7821C728014A" ); // EntityId
            RockMigrationHelper.DeleteAttribute( "A4E5D4F4-580D-459E-8C83-C6A92B47396A" ); // EntityType
            RockMigrationHelper.DeleteDefinedValue( "2E216362-0B05-4CE7-BBA8-E2C3DCAAEB07" ); // Blankenbaker Home Groups
            RockMigrationHelper.DeleteDefinedValue( "7E83A576-07FC-49EA-98BC-73C123D24DD8" ); // Home Groups
            RockMigrationHelper.DeleteDefinedValue( "EB39F521-F9AA-4812-BA8F-102C029C8516" ); // Campus
            RockMigrationHelper.DeleteDefinedValue( "F4D7D410-5096-460F-B740-4F6E0673CE16" ); // Content Channel Attributes
            RockMigrationHelper.DeleteDefinedType( "7F70ABE9-1705-4DED-BABE-6D720EC52914" ); // Distance Mappable Entities
        }
    }
}
