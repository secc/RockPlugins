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
namespace org.secc.FamilyCheckin.Migrations
{
    using Rock.Plugin;
    using org.secc.DevLib.Extensions.Migration;
    using org.secc.FamilyCheckin.Utilities;

    [MigrationNumber( 21, "1.10.2" )]
    public partial class DisabledGLSsDT : Migration
    {
        public override void Up()
        {
            RockMigrationHelper.AddDefinedType( "Check-in", "Disabled GroupLocationSchedules", "Group location schedules that are disabled through the oz screen.", "4A3255CE-9363-402C-9998-B2998B6712E0", @"" );
        }

        public override void Down()
        {
            RockMigrationHelper.DeleteDefinedType( "4A3255CE-9363-402C-9998-B2998B6712E0" ); // Disabled GroupLocationSchedules
        }
    }
}
