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
    [MigrationNumber( 4, "1.0.4" )]
    class CreateGenderDefinedValues : Migration
    {
        /// <summary>
        /// The commands to run to migrate plugin to the specific version
        /// </summary>
        public override void Up()
        {
            RockMigrationHelper.AddDefinedValue(
                "4FACC42E-8783-4805-859F-AAC6D8951CE6",
                "MALE",
                "Male",
                "9A3C3ED4-48AE-4384-B98D-722539319E84"
            );

            RockMigrationHelper.AddDefinedValue(
                "4FACC42E-8783-4805-859F-AAC6D8951CE6",
                "FEMALE",
                "Female",
                "DDB7721A-DFDC-46EE-83BC-8FA70D99EFD5"
            );

            RockMigrationHelper.AddDefinedValue(
                "4FACC42E-8783-4805-859F-AAC6D8951CE6",
                "ANY",
                "Any",
                "58F93CCD-9AB4-4653-A690-168D42ED17CB"
            );

            RockMigrationHelper.AddDefinedValue(
                "4FACC42E-8783-4805-859F-AAC6D8951CE6",
                "CO_ED",
                "Co_ed",
                "24E07D3E-912E-490F-8932-4A2C27B7244B"
            );

        }

        /// <summary>
        /// The commands to undo a migration from a specific version
        /// </summary>
        public override void Down()
        {
            RockMigrationHelper.DeleteDefinedValue( "9A3C3ED4-48AE-4384-B98D-722539319E84" );
            RockMigrationHelper.DeleteDefinedValue( "DDB7721A-DFDC-46EE-83BC-8FA70D99EFD5" );
            RockMigrationHelper.DeleteDefinedValue( "58F93CCD-9AB4-4653-A690-168D42ED17CB" );
            RockMigrationHelper.DeleteDefinedValue( "24E07D3E-912E-490F-8932-4A2C27B7244B" );
        }
    }
}
