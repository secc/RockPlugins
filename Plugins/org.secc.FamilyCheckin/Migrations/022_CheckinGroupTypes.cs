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
    using org.secc.FamilyCheckin.Utilities;
    using Rock.Plugin;

    [MigrationNumber( 22, "1.10.2" )]
    public partial class CheckinGroupTypes : Migration
    {
        public override void Up()
        {
            //Childrens checkin base
            RockMigrationHelper.AddGroupType( "Children Check-In", "Base checkin group for all of check-in.", "Group", "Member", false, false, false,
                "", 100, Constants.GROUP_TYPE_BY_BASE, 0, Rock.SystemGuid.DefinedValue.GROUPTYPE_PURPOSE_CHECKIN_FILTER, Constants.GROUP_TYPE_BY_CHILDREN_CHECKIN_BASE );
            RockMigrationHelper.AddGroupTypeGroupAttribute( "5398A1C2-F422-4ADC-A48B-B9EFFE3598AD", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Link Locations", @"Should all all locations for this group be required to be the same?", 0, "False", "0A8723A4-4AEE-4D21-A32B-350845FC8FA9", "" );
            RockMigrationHelper.AddGroupTypeGroupAttribute( "5398A1C2-F422-4ADC-A48B-B9EFFE3598AD", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Is Volunteer", @"Do people checking into this group count as volunteers?", 1, "", "F5DAD320-B77D-4282-98C9-35414FB0A6DC", "" );
            RockMigrationHelper.AddGroupTypeGroupAttribute( "5398A1C2-F422-4ADC-A48B-B9EFFE3598AD", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Give Priority", @"", 2, "False", "DABEABAC-F052-471B-AC61-A7630465888B", "" );

            //Checkin by Birthday
            RockMigrationHelper.AddGroupType( "Check in by Birthday", "Checkin filter using birthdays.", "Group", "Member", false, false, false,
                "", 101, Constants.GROUP_TYPE_BY_CHILDREN_CHECKIN_BASE, 0, Rock.SystemGuid.DefinedValue.GROUPTYPE_PURPOSE_CHECKIN_FILTER, Constants.GROUP_TYPE_BY_BIRTHDAY );
            RockMigrationHelper.AddGroupTypeGroupAttribute( "3600C17B-1D92-4929-B7B7-BBC156F2D47A", "9C7D431C-875C-4792-9E76-93F3A32BB850", "Date Range", @"", 0, "", "DABCC6EA-29EE-41EA-83DC-C2DF0065631D", "" );
            RockMigrationHelper.AddGroupTypeGroupAttribute( "3600C17B-1D92-4929-B7B7-BBC156F2D47A", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Filter GradeSchool Students", @"", 1, "", "8919515B-D654-43AA-A429-91A2B8A092CF", "" );


            //Checkin by Membership
            RockMigrationHelper.AddGroupType( "Check in by Membership", "Checkin filter using group membership.", "Group", "Member", false, false, false,
              "", 102, Constants.GROUP_TYPE_BY_CHILDREN_CHECKIN_BASE, 0, Rock.SystemGuid.DefinedValue.GROUPTYPE_PURPOSE_CHECKIN_FILTER, Constants.GROUP_TYPE_BY_MEMBERSHIP );
            RockMigrationHelper.AddGroupTypeGroupAttribute( "2098AE58-58D4-4CEF-8D40-C2657D2E7A6A", "F4399CEF-827B-48B2-A735-F7806FCFE8E8", "Group", @"", 0, null, "22C7D86E-ABEA-4DFB-A83C-FC18100C9834", "" );
            RockMigrationHelper.AddGroupTypeGroupAttribute( Constants.GROUP_TYPE_BY_MEMBERSHIP, Rock.SystemGuid.FieldType.SINGLE_SELECT, "Check Requirements",
                "Should the requirements of the group be checked before allowing check-in. This is slower but needed in some situations.",
                5, "0", Constants.GROUP_ATTRIBUTE_CHECK_REQUIREMENTS, false );
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
