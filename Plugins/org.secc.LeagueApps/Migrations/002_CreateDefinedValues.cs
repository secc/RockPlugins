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
    [MigrationNumber( 2, "1.0.2" )]
    class CreateDefinedValues : Migration
    {
        /// <summary>
        /// The commands to run to migrate plugin to the specific version
        /// </summary>
        public override void Up()
        {
            RockMigrationHelper.AddDefinedValue(
                "3fc2b68b-9a24-46e9-b6d9-6c77b98abfb3",
                "Baseball",
                "Baseball",
                "46cefe70-1d37-493b-8582-5a0a97bc9370"
            );

            RockMigrationHelper.AddDefinedValue(
                "3fc2b68b-9a24-46e9-b6d9-6c77b98abfb3",
                "Basketball",
                "Basketball",
                "b6fa2d52-5fd9-48c6-98fd-3c14784dd83e"
            );

            RockMigrationHelper.AddDefinedValue(
                "3fc2b68b-9a24-46e9-b6d9-6c77b98abfb3",
                "Fitness",
                "Fitness",
                "009adb76-9a30-4763-a86f-e62dea0a24eb"
            );

            RockMigrationHelper.AddDefinedValue(
                "3fc2b68b-9a24-46e9-b6d9-6c77b98abfb3",
                "Golf",
                "Golf",
                "03cceb5d-2c85-4f72-95f8-29fe4361c594"
            );

            RockMigrationHelper.AddDefinedValue(
                "3fc2b68b-9a24-46e9-b6d9-6c77b98abfb3",
                "Running",
                "Running",
                "00553266-f10a-48c1-b6d3-b52d569091ff"
            );

            RockMigrationHelper.AddDefinedValue(
                "3fc2b68b-9a24-46e9-b6d9-6c77b98abfb3",
                "Soccer",
                "Soccer",
                "11bac8f5-752e-4f4f-a8d0-72586ab3566f"
            );

            RockMigrationHelper.AddDefinedValue(
                "3fc2b68b-9a24-46e9-b6d9-6c77b98abfb3",
                "Softball",
                "Softball",
                "bf75c1c5-2b35-46b1-846c-9459d6fc6f9c"
            );

            RockMigrationHelper.AddDefinedValue(
                "3fc2b68b-9a24-46e9-b6d9-6c77b98abfb3",
                "Volleyball (Indoor)",
                "Volleyball (Indoor)",
                "f1a25704-f955-4bca-b3a9-e88a504c2c9f"
            );

            RockMigrationHelper.AddDefinedValue(
                "3fc2b68b-9a24-46e9-b6d9-6c77b98abfb3",
                "Volleyball (Sand)",
                "Volleyball (Sand)",
                "22522520-c47e-4a3f-9c36-167426aa0136"
            );


            RockMigrationHelper.AddDefinedValue(
                "cb85b2aa-46e6-4655-802c-4fe33379018d",
                "Fall",
                "Fall",
                "1a03c95b-fc59-466f-80ab-861fcbcf589e"
            );

            RockMigrationHelper.AddDefinedValue(
                "cb85b2aa-46e6-4655-802c-4fe33379018d",
                "Spring",
                "Spring",
                "1b3526ab-80c3-4b42-8978-d22acb6c703a"
            );

            RockMigrationHelper.AddDefinedValue(
                "cb85b2aa-46e6-4655-802c-4fe33379018d",
                "Summer",
                "Summer",
                "f34cc715-da42-4ca2-abec-25cd22dc337d"
            );

            RockMigrationHelper.AddDefinedValue(
                "cb85b2aa-46e6-4655-802c-4fe33379018d",
                "Winter",
                "Winter",
                "0e5dec46-8f3a-402f-b8b3-aa4c62fd1d3d"
            );
        }

        /// <summary>
        /// The commands to undo a migration from a specific version
        /// </summary>
        public override void Down()
        {
            RockMigrationHelper.DeleteDefinedValue( "46cefe70-1d37-493b-8582-5a0a97bc9370" );
            RockMigrationHelper.DeleteDefinedValue( "b6fa2d52-5fd9-48c6-98fd-3c14784dd83e" );
            RockMigrationHelper.DeleteDefinedValue( "009adb76-9a30-4763-a86f-e62dea0a24eb" );
            RockMigrationHelper.DeleteDefinedValue( "03cceb5d-2c85-4f72-95f8-29fe4361c594" );
            RockMigrationHelper.DeleteDefinedValue( "00553266-f10a-48c1-b6d3-b52d569091ff" );
            RockMigrationHelper.DeleteDefinedValue( "11bac8f5-752e-4f4f-a8d0-72586ab3566f" );
            RockMigrationHelper.DeleteDefinedValue( "bf75c1c5-2b35-46b1-846c-9459d6fc6f9c" );
            RockMigrationHelper.DeleteDefinedValue( "f1a25704-f955-4bca-b3a9-e88a504c2c9f" );
            RockMigrationHelper.DeleteDefinedValue( "22522520-c47e-4a3f-9c36-167426aa0136" );

            RockMigrationHelper.DeleteDefinedValue( "1a03c95b-fc59-466f-80ab-861fcbcf589e" );
            RockMigrationHelper.DeleteDefinedValue( "1b3526ab-80c3-4b42-8978-d22acb6c703a" );
            RockMigrationHelper.DeleteDefinedValue( "f34cc715-da42-4ca2-abec-25cd22dc337d" );
            RockMigrationHelper.DeleteDefinedValue( "0e5dec46-8f3a-402f-b8b3-aa4c62fd1d3d" );
        }
    }
}
