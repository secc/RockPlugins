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

namespace org.secc.Migrations
{
    using Rock.Plugin;

    [MigrationNumber(33, "1.12.9")]
    public partial class Events_CommunityGivesBackCampaignAttribute : Migration
    {
        public override void Up()
        {
            var yearAttributeGuid = "bbe01ea2-357c-4289-aff9-585cb5b3b88c";
            RockMigrationHelper.AddDefinedTypeAttribute("71bb065c-4368-439d-beab-8539c19c8c99", 
                Rock.SystemGuid.FieldType.TEXT, "Year", "Year", "The campaign or year that this school sponsorship is a part of.", 
                2165, "", yearAttributeGuid);

            var definedValues = Rock.Web.Cache.DefinedTypeCache.Get("71bb065c-4368-439d-beab-8539c19c8c99")
                .DefinedValues;


            //update existing schools to use 2024 campaign
            foreach ( var definedValue in definedValues )
            {
                RockMigrationHelper.AddDefinedValueAttributeValue(definedValue.Guid.ToString(), yearAttributeGuid, "2024");
            }
        }
        
        public override void Down()
        {
           
        }


    }
}
