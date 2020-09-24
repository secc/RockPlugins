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
    using System;

    [MigrationNumber( 13, "1.10.2" )]
    public partial class GroupFilterAttributes : Migration
    {
        public override void Up()
        {
            RockMigrationHelper.AddGroupTypeGroupAttribute(Constants.GROUP_TYPE_BY_MEMBERSHIP, Rock.SystemGuid.FieldType.SINGLE_SELECT, "Check Requirements",
                "Should the requirements of the group be checked before allowing check-in. This is slower but needed in some situations.",
                5, "0", Constants.GROUP_ATTRIBUTE_CHECK_REQUIREMENTS, false);
            RockMigrationHelper.AddAttributeQualifier( Constants.GROUP_ATTRIBUTE_CHECK_REQUIREMENTS, "values", "0^Do Not Check,1^Check Required Only,2^Check Required and Warning", "9E659023-6860-4872-B7C9-EB019FF76145" );

            RockMigrationHelper.AddGroupTypeGroupAttribute( Constants.GROUP_TYPE_BY_MEMBERSHIP, Rock.SystemGuid.FieldType.SINGLE_SELECT, "Member Role",
                "Optional filter to only allow people if they are a member or leader.",
                6, "0", Constants.GROUP_ATTRIBUTE_MEMBER_ROLE, false );
            RockMigrationHelper.AddAttributeQualifier( Constants.GROUP_ATTRIBUTE_MEMBER_ROLE, "values", "0^Any,1^Leaders Only,2^Non Leaders Only", "840B04D7-DC1B-452B-A869-0AEB4904DC26" );

            RockMigrationHelper.AddGroupTypeGroupAttribute( Constants.GROUP_TYPE_BY_MEMBERSHIP, Rock.SystemGuid.FieldType.BOOLEAN, "Save Attendance To Group",
                "If selected, the attendance will be set on the group the person is a member of instead of on the check-in group.",
                7, "False", Constants.GROUP_ATTRIBUTE_ATTENDANCE_ON_GROUP );
        }

        public override void Down()
        {

        }
    }
}
