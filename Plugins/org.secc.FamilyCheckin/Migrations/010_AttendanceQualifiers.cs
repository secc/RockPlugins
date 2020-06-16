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

    [MigrationNumber( 10, "1.10.2" )]
    public partial class AttendanceQualifiers : Migration
    {
        public override void Up()
        {
            RockMigrationHelper.AddDefinedType( "Check-in",
                "Mobile Check-in Type", "Used in mobile check-in to tell that an attendance is mobile, and what to do with it once it is completed.",
                Constants.DEFINED_TYPE_MOBILE_ATTENDANCE_TYPE );
            RockMigrationHelper.AddDefinedValue( Constants.DEFINED_TYPE_MOBILE_ATTENDANCE_TYPE, "Mobile Check-in - Set Did Attend",
                "Qualifies an attendance that it is a mobile check-in and SHOULD be marked DidAtted when scanned.", Constants.DEFINED_VALUE_MOBILE_DID_ATTEND );
            RockMigrationHelper.AddDefinedValue( Constants.DEFINED_TYPE_MOBILE_ATTENDANCE_TYPE, "Mobile Check-in - Set Did NOT Attend",
                "Qualifies an attendance that it is a mobile check-in and should NOT be marked DidAtted when scanned.", Constants.DEFINED_VALUE_MOBILE_NOT_ATTEND );
        }

        public override void Down()
        {

        }
    }
}
