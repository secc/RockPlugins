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
                "Attendance Qualifiers", "Used to note special properties of an attendance record, such as mobile or with parent.",
                Constants.DEFINED_TYPE_ATTENDANCE_QUALIFIERS );
            RockMigrationHelper.AddDefinedValue( Constants.DEFINED_TYPE_ATTENDANCE_QUALIFIERS, "Mobile Check-in - Set Did Attend",
                "Qualifies an attendance that it is a mobile check-in and SHOULD be marked DidAtted when scanned.", Constants.DEFINED_VALUE_MOBILE_DID_ATTEND );
            RockMigrationHelper.AddDefinedValue( Constants.DEFINED_TYPE_ATTENDANCE_QUALIFIERS, "Mobile Check-in - Set Did NOT Attend",
                "Qualifies an attendance that it is a mobile check-in and should NOT be marked DidAtted when scanned.", Constants.DEFINED_VALUE_MOBILE_NOT_ATTEND );
            RockMigrationHelper.AddDefinedValue( Constants.DEFINED_TYPE_ATTENDANCE_QUALIFIERS, "Attendance Status - With Parent",
               "Qualifies an attendance that a child has temporarily left the class room with a parent and is expected to return.", Constants.DEFINED_VALUE_ATTENDANCE_STATUS_WITH_PARENT );
        }

        public override void Down()
        {

        }
    }
}
