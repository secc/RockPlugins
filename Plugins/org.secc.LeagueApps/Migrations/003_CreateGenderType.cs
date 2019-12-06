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
    [MigrationNumber(3, "1.0.3")]
    class CreateGenderType : Migration
    {
        /// <summary>
        /// The commands to run to migrate plugin to the specific version
        /// </summary>
        public override void Up()
        {
            RockMigrationHelper.AddDefinedType(
                "Group",
                "Gender",
                "Gender for groups.",
                "4FACC42E-8783-4805-859F-AAC6D8951CE6"
            );
        }

    /// <summary>
    /// The commands to undo a migration from a specific version
    /// </summary>
    public override void Down()
        {
            RockMigrationHelper.DeleteDefinedType("4FACC42E-8783-4805-859F-AAC6D8951CE6");
        }
    }
}
