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

namespace org.secc.Rise.Migrations
{
    using org.secc.DevLib.Extensions.Migration;
    using org.secc.Rise.Utilities;
    using Rock.Plugin;
    using Rock.Web.Cache;

    [MigrationNumber( 2, "1.10.2" )]
    public partial class RiseGroupType : Migration
    {
        public override void Up()
        {
            RockMigrationHelper.AddGroupType( "Rise Learning Group", "Groups that are mirrored in the Rise learning management system.",
                "Group", "Member", false, true, true, "fa fa-chalkboard", 100, "", 0, "", Constants.GROUPTYPE_RISE );
            RockMigrationHelper.AddGroupTypeRole( Constants.GROUPTYPE_RISE, "Learner", "", 0, null, null,
                "41DF5756-4D66-4BDE-841A-300C25680354", true, false, true );
            RockMigrationHelper.AddGroupTypeGroupAttribute( Constants.GROUPTYPE_RISE, Rock.SystemGuid.FieldType.TEXT, "Rise Group Id",
                "The id of the group in the Rise learning system.", 0, "", Constants.GROUP_ATTRIBUTE_RISEID );
            RockMigrationHelper.AddGroupTypeGroupAttribute( Constants.GROUPTYPE_RISE, Rock.SystemGuid.FieldType.TEXT, "Rise Group Url",
                "The url of the group in the Rise learning system.", 1, "", "ABF5A7DA-B8B7-4446-8124-139CE3E90870" );
        }


        public override void Down()
        {

        }
    }
}
