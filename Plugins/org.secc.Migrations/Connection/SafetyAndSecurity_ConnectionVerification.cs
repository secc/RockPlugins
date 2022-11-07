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
using Rock.Plugin;

namespace org.secc.Migrations
{
    [MigrationNumber(16, "1.12.0")]
    class SafetyAndSecurity_ConnectionVerification : Migration
    {

        public override void Up()
        {
            // Entity: Rock.Model.ConnectionOpportunity Attribute: Require Safety and Security to Connect
            RockMigrationHelper.AddOrUpdateEntityAttribute( "Rock.Model.ConnectionOpportunity", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "ConnectionTypeId", "1", "Require Safety and Security to Connect", "Require Safety and Security to Connect", @"", 4, @"True", "929B0CB3-0223-44C9-8CC8-3799E5211723", "SecurityToConnect");
            // Qualifier for attribute: SecurityToConnect
            RockMigrationHelper.UpdateAttributeQualifier( "929B0CB3-0223-44C9-8CC8-3799E5211723", "truetext", @"Yes", "C076547E-3DD4-4BD0-AA98-D0AEF92D3770" );
            // Qualifier for attribute: SecurityToConnect
            RockMigrationHelper.UpdateAttributeQualifier( "929B0CB3-0223-44C9-8CC8-3799E5211723", "falsetext", @"No", "70666A67-C5DF-4877-A044-7093115F6A02" );
            // Qualifier for attribute: SecurityToConnect
            RockMigrationHelper.UpdateAttributeQualifier( "929B0CB3-0223-44C9-8CC8-3799E5211723", "BooleanControlType", @"0", "5098E8B0-8120-4F28-8913-75A63D874F13" );

            // Entity: Rock.Model.ConnectionOpportunity Attribute: Connectable Statuses
            RockMigrationHelper.AddOrUpdateEntityAttribute( "Rock.Model.ConnectionOpportunity", "BD0D9B57-2A41-4490-89FF-F01DAB7D4904", "ConnectionTypeId", "1", "Connectable Statuses", "Connectable Statuses", @"", 5, @"", "D5AEE44C-D000-4C4C-B4E3-5EB366AB0A4D", "ConnectableStatuses");
            // Qualifier for attribute: ConnectableStatuses
            RockMigrationHelper.UpdateAttributeQualifier( "D5AEE44C-D000-4C4C-B4E3-5EB366AB0A4D", "values", @"SELECT Id as Value, Name as Text FROM ConnectionStatus WHERE ConnectionTypeId = 1  ORDER BY [Order]", "F38CC061-E145-495A-9355-CEDF6CB5C82A"); 
            // Qualifier for attribute: ConnectableStatuses
            RockMigrationHelper.UpdateAttributeQualifier( "D5AEE44C-D000-4C4C-B4E3-5EB366AB0A4D", "enhancedselection", @"False", "F156E070-1078-4E74-B162-8905A3A41811"); 
            // Qualifier for attribute: ConnectableStatuses
            RockMigrationHelper.UpdateAttributeQualifier( "D5AEE44C-D000-4C4C-B4E3-5EB366AB0A4D", "repeatColumns", @"", "A2144AAE-623D-4D46-BBF0-372C30BDC470"); 
        }

        public override void Down()
        {
            RockMigrationHelper.DeleteAttribute( "929B0CB3-0223-44C9-8CC8-3799E5211723" ); // Rock.Model.ConnectionOpportunity: Require Safety and Security to Connect 
            RockMigrationHelper.DeleteAttribute( "D5AEE44C-D000-4C4C-B4E3-5EB366AB0A4D" ); // Rock.Model.ConnectionOpportunity: Connectable Statuses 
        }


    }
}
