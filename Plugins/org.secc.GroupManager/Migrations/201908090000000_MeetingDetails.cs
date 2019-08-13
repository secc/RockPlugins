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
namespace org.secc.GroupManager.Migrations
{
    using Rock.Plugin;

    [MigrationNumber( 3, "1.8.0" )]
    public partial class MeetingDetails : Migration
    {
        public override void Up()
        {
            AddColumn( "dbo._org_secc_GroupManager_PublishGroup", "WeeklyDayOfWeek", c => c.Int( nullable: true ) );
            Sql( "ALTER TABLE [_org_secc_GroupManager_PublishGroup] ADD [WeeklyTimeOfDay] TIME" );
            AddColumn( "dbo._org_secc_GroupManager_PublishGroup", "StartDate", c => c.DateTime() );
            AddColumn( "dbo._org_secc_GroupManager_PublishGroup", "MeetingLocation", c => c.String() );
        }

        public override void Down()
        {
            DropColumn( "dbo._org_secc_GroupManager_PublishGroup", "WeeklyDayOfWeek" );
            DropColumn( "dbo._org_secc_GroupManager_PublishGroup", "WeeklyTimeOfDay" );
            DropColumn( "dbo._org_secc_GroupManager_PublishGroup", "StartDate" );
            DropColumn( "dbo._org_secc_GroupManager_PublishGroup", "MeetingLocation" );
        }
    }
}
