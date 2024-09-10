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

    [MigrationNumber( 8, "1.8.0" )]
    public partial class AddKeys : Migration
    {
        public override void Up()
        {
            Sql( @"ALTER TABLE _org_secc_GroupManager_PublishGroup ADD CONSTRAINT FK__org_secc_GroupManager_PublishGroup_GroupId FOREIGN KEY (GroupId) REFERENCES [Group](Id);
ALTER TABLE _org_secc_GroupManager_PublishGroup ADD CONSTRAINT FK__org_secc_GroupManager_PublishGroup_ContactPersonAliasId FOREIGN KEY (ContactPersonAliasId) REFERENCES [PersonAlias](Id);
ALTER TABLE _org_secc_GroupManager_PublishGroup ADD CONSTRAINT FK__org_secc_GroupManager_PublishGroup_RequestorAliasId FOREIGN KEY (RequestorAliasId) REFERENCES [PersonAlias](Id);
ALTER TABLE _org_secc_GroupManager_PublishGroup ADD CONSTRAINT FK__org_secc_GroupManager_PublishGroup_CreatedByPersonAliasId FOREIGN KEY (CreatedByPersonAliasId) REFERENCES [PersonAlias](Id);
ALTER TABLE _org_secc_GroupManager_PublishGroup ADD CONSTRAINT FK__org_secc_GroupManager_PublishGroup_ModifiedByPersonAliasId FOREIGN KEY (ModifiedByPersonAliasId) REFERENCES [PersonAlias](Id);

" );
        }

        public override void Down()
        {
        }
    }
}
