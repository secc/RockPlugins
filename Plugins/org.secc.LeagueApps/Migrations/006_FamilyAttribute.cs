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
using org.secc.LeagueApps.Utilities;
using Rock.Plugin;

namespace org.secc.LeagueApps.Migrations
{
    [MigrationNumber(6, "1.0.5")]
    class FamilyAttribute : Migration
    {
        /// <summary>
        /// The commands to run to migrate plugin to the specific version
        /// </summary>
        public override void Up()
        {
            RockMigrationHelper.AddGroupTypeGroupAttribute(
                Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY,
                Rock.SystemGuid.FieldType.TEXT,
                "League Apps Family Id",
                "Family id for league apps",
                999,
                "",
                Constants.ATTRIBUTE_GROUP_FAMILY_ID,
                false
                );

            

        }

    /// <summary>
    /// The commands to undo a migration from a specific version
    /// </summary>
    public override void Down()
        {
         
        }
    }
}
