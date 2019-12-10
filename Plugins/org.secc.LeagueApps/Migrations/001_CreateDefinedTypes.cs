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
    [MigrationNumber(1, "1.0.1")]
    class CreateDefinedTypes : Migration
    {
        /// <summary>
        /// The commands to run to migrate plugin to the specific version
        /// </summary>
        public override void Up()
        {
            RockMigrationHelper.AddDefinedType(
                "Group",
                "Sport",
                "Sports that originated from LeagueApps.",
                "3fc2b68b-9a24-46e9-b6d9-6c77b98abfb3"
            );

            RockMigrationHelper.AddDefinedType(
                "Group",
                "Season",
                "Seasons that originated from LeagueApps.",
                "cb85b2aa-46e6-4655-802c-4fe33379018d"
            );
        }

    /// <summary>
    /// The commands to undo a migration from a specific version
    /// </summary>
    public override void Down()
        {
            RockMigrationHelper.DeleteDefinedType("3fc2b68b-9a24-46e9-b6d9-6c77b98abfb3");
            RockMigrationHelper.DeleteDefinedType("cb85b2aa-46e6-4655-802c-4fe33379018d");
        }
    }
}
