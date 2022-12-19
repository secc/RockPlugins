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

namespace org.secc.ChangeManager.Migrations
{
    using Rock.Plugin;
    [MigrationNumber(3, "1.12.1")]
    public class InternalCampusTypeValue : Migration
    {
        public override void Up()
        {
            RockMigrationHelper.UpdateDefinedValue( Rock.SystemGuid.DefinedType.CAMPUS_TYPE, "Internal", "", 
                "DF089AAE-C10D-4240-BB67-99C46E89B23B", true, null, "", 3 );

        }

        public override void Down()
        {
            RockMigrationHelper.DeleteDefinedValue( "DF089AAE-C10D-4240-BB67-99C46E89B23B" );
        }

    }
}
