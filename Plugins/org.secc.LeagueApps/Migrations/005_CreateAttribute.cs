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

namespace org.secc.LeagueApps.Migrations
{
    [MigrationNumber(5, "1.0.5")]
    class CreateAttribute : Migration
    {
        /// <summary>
        /// The commands to run to migrate plugin to the specific version
        /// </summary>
        public override void Up()
        {
            RockMigrationHelper.UpdatePersonAttribute(
                "7BDAE237-6E49-47AC-9961-A45AFB69E240",
                "4D1E1EBA-ABF2-4A7C-8ADF-65CB5AAE94E2",
                "LeagueApps UserId",
                "LeagueAppsUserId",
                "",
                "The LeagueApps user id that is associated with this person.",
                0,
                "",
                "FA679B84-53A4-4DE9-84B5-D4EAFCC52E75"
            );

        }

    /// <summary>
    /// The commands to undo a migration from a specific version
    /// </summary>
    public override void Down()
        {
            RockMigrationHelper.DeleteAttribute("FA679B84-53A4-4DE9-84B5-D4EAFCC52E75");
        }
    }
}
