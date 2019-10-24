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

    [MigrationNumber( 5, "1.8.0" )]
    public partial class ChildcareOptions : Migration
    {
        public override void Up()
        {
            AddColumn( "dbo._org_secc_GroupManager_PublishGroup", "ChildcareOptions", c => c.Int( nullable: true ) );
            Sql( @"UPDATE dbo._org_secc_GroupManager_PublishGroup SET ChildcareOptions = 0 WHERE ChildcareAvailable = 0" );
            Sql( @"UPDATE dbo._org_secc_GroupManager_PublishGroup SET ChildcareOptions = 1 WHERE ChildcareAvailable = 1 and ChildcareRegistrationLink=''" );
            Sql( @"UPDATE dbo._org_secc_GroupManager_PublishGroup SET ChildcareOptions = 2 WHERE ChildcareAvailable = 1 and ChildcareRegistrationLink!=''" );
        }

        public override void Down()
        {
            DropColumn( "dbo._org_secc_GroupManager_PublishGroup", "ChildcareOptions" );
        }
    }
}
