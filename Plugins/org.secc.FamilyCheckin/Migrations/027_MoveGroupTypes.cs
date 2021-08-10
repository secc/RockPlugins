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

    [MigrationNumber( 27, "1.10.2" )]
    public partial class MoveGroupTypes : Migration
    {
        public override void Up()
        {
            Sql( $"UPDATE [GroupType] " +
                $"SET [InheritedGroupTypeId] = (SELECT Id FROM [GroupType] WHERE [Guid] = '{Constants.GROUP_TYPE_BY_CHILDREN_CHECKIN_BASE}' )  " +
                $"WHERE [Guid] IN ('{Constants.GROUP_TYPE_BY_DATAVIEW}', '{Constants.GROUP_TYPE_BY_GRADE}', '{Constants.GROUP_TYPE_BY_AGE}', '{Constants.GROUP_TYPE_BY_ABILITY_LEVEL}') " );
        }

        public override void Down()
        {

        }
    }
}
