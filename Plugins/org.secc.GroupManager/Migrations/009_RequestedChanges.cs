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

    [MigrationNumber( 9, "1.8.0" )]
    public partial class RequestedChanges : Migration
    {
        public override void Up()
        {
            AddColumn( "dbo._org_secc_GroupManager_PublishGroup", "IsHidden", c => c.Boolean() );
            Sql( "CREATE INDEX[IX_IsHidden] ON[dbo].[_org_secc_GroupManager_PublishGroup]([IsHidden] )" );
            AddColumn( "dbo._org_secc_GroupManager_PublishGroup", "Slug", c => c.String( maxLength: 75 ) );
            AddColumn( "dbo._org_secc_GroupManager_PublishGroup", "Name", c => c.String() );
            AddColumn( "dbo._org_secc_GroupManager_PublishGroup", "CustomSchedule", c => c.String() );
            Sql( "CREATE INDEX[IX_Slug] ON[dbo].[_org_secc_GroupManager_PublishGroup]([Slug])" );
            Sql( "UPDATE [dbo].[_org_secc_GroupManager_PublishGroup] SET IsHidden = 0" );
            Sql( "UPDATE pg SET [Slug] = LEFT( LOWER( REPLACE( g.Name, ' ', '' ) ), 75) FROM [dbo].[_org_secc_GroupManager_PublishGroup] pg INNER JOIN [group] g ON pg.groupId = g.id" );
        }

        public override void Down()
        {
            DropColumn( "dbo._org_secc_GroupManager_PublishGroup", "IsHidden" );
            DropColumn( "dbo._org_secc_GroupManager_PublishGroup", "Slug" );
            DropColumn( "dbo._org_secc_GroupManager_PublishGroup", "Name" );
        }
    }
}
