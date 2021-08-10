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
namespace org.secc.ChangeManager.Migrations
{
    using Rock.Plugin;

    [MigrationNumber( 2, "1.10.2" )]
    public partial class FamilyGroupOfPerson : Migration
    {
        public override void Up()
        {
            AddColumn( "_org_secc_ChangeManager_ChangeRequest", "FamilyGroupOfPersonAliasId", i => i.Int( true ) );
        }

        public override void Down()
        {
            DropColumn( "_org_secc_ChangeManager_ChangeRequest", "FamilyGroupOfPersonAliasId" );
        }
    }
}
