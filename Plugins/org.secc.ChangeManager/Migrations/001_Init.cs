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

    [MigrationNumber( 1, "1.8.5" )]
    public partial class Init : Migration
    {
        public override void Up()
        {
            AddTable(
                "dbo._org_secc_ChangeManager_ChangeRecord",
                c => new
                {
                    Id = c.Int( nullable: false, identity: true ),
                    ChangeRequestId = c.Int( nullable: false ),
                    IsRejected = c.Boolean( nullable: false ),
                    WasApplied = c.Boolean( nullable: false ),
                    RelatedEntityTypeId = c.Int(),
                    RelatedEntityId = c.Int(),
                    OldValue = c.String(),
                    NewValue = c.String(),
                    Action = c.Int( nullable: false ),
                    Property = c.String(),
                    Comment = c.String(),
                    CreatedDateTime = c.DateTime(),
                    ModifiedDateTime = c.DateTime(),
                    CreatedByPersonAliasId = c.Int(),
                    ModifiedByPersonAliasId = c.Int(),
                    Guid = c.Guid( nullable: false ),
                    ForeignId = c.Int(),
                    ForeignGuid = c.Guid(),
                    ForeignKey = c.String( maxLength: 100 ),
                } );

            AddTable(
                "dbo._org_secc_ChangeManager_ChangeRequest",
                c => new
                {
                    Id = c.Int( nullable: false, identity: true ),
                    Name = c.String(),
                    EntityTypeId = c.Int( nullable: false ),
                    EntityId = c.Int( nullable: false ),
                    RequestorAliasId = c.Int( nullable: false ),
                    ApproverAliasId = c.Int( nullable: false ),
                    IsComplete = c.Boolean( nullable: false ),
                    RequestorComment = c.String(),
                    ApproverComment = c.String(),
                    CreatedDateTime = c.DateTime(),
                    ModifiedDateTime = c.DateTime(),
                    CreatedByPersonAliasId = c.Int(),
                    ModifiedByPersonAliasId = c.Int(),
                    Guid = c.Guid( nullable: false ),
                    ForeignId = c.Int(),
                    ForeignGuid = c.Guid(),
                    ForeignKey = c.String( maxLength: 100 ),
                } );

            AddPrimaryKey( "dbo._org_secc_ChangeManager_ChangeRecord", "Id" );
            AddPrimaryKey( "dbo._org_secc_ChangeManager_ChangeRequest", "Id" );

            AddForeignKey( "dbo._org_secc_ChangeManager_ChangeRecord", "ChangeRequestId", "dbo._org_secc_ChangeManager_ChangeRequest", "Id", cascadeDelete: true );
            AddForeignKey( "dbo._org_secc_ChangeManager_ChangeRecord", "CreatedByPersonAliasId", "dbo.PersonAlias", "Id" );
            AddForeignKey( "dbo._org_secc_ChangeManager_ChangeRecord", "ModifiedByPersonAliasId", "dbo.PersonAlias", "Id" );
            AddForeignKey( "dbo._org_secc_ChangeManager_ChangeRecord", "RelatedEntityTypeId", "dbo.EntityType", "Id" );
            AddIndex( "dbo._org_secc_ChangeManager_ChangeRecord", "ChangeRequestId" );
            AddIndex( "dbo._org_secc_ChangeManager_ChangeRecord", "IsRejected" );
            AddIndex( "dbo._org_secc_ChangeManager_ChangeRecord", "WasApplied" );
            AddIndex( "dbo._org_secc_ChangeManager_ChangeRecord", "RelatedEntityTypeId" );
            AddIndex( "dbo._org_secc_ChangeManager_ChangeRecord", "RelatedEntityId" );
            AddIndex( "dbo._org_secc_ChangeManager_ChangeRecord", "CreatedByPersonAliasId" );
            AddIndex( "dbo._org_secc_ChangeManager_ChangeRecord", "ModifiedByPersonAliasId" );
            AddIndex( "dbo._org_secc_ChangeManager_ChangeRecord", "Guid", unique: true );

            AddForeignKey( "dbo._org_secc_ChangeManager_ChangeRequest", "CreatedByPersonAliasId", "dbo.PersonAlias", "Id" );
            AddForeignKey( "dbo._org_secc_ChangeManager_ChangeRequest", "ModifiedByPersonAliasId", "dbo.PersonAlias", "Id" );
            AddForeignKey( "dbo._org_secc_ChangeManager_ChangeRequest", "EntityTypeId", "dbo.EntityType", "Id" );
            AddIndex( "dbo._org_secc_ChangeManager_ChangeRequest", "EntityTypeId" );
            AddIndex( "dbo._org_secc_ChangeManager_ChangeRequest", "EntityId" );
            AddIndex( "dbo._org_secc_ChangeManager_ChangeRequest", "RequestorAliasId" );
            AddIndex( "dbo._org_secc_ChangeManager_ChangeRequest", "ApproverAliasId" );
            AddIndex( "dbo._org_secc_ChangeManager_ChangeRequest", "IsComplete" );
            AddIndex( "dbo._org_secc_ChangeManager_ChangeRequest", "CreatedByPersonAliasId" );
            AddIndex( "dbo._org_secc_ChangeManager_ChangeRequest", "ModifiedByPersonAliasId" );
            AddIndex( "dbo._org_secc_ChangeManager_ChangeRequest", "Guid", unique: true );
        }

        public override void Down()
        {
            DropTable( "dbo._org_secc_ChangeManager_ChangeRequest" );
            DropTable( "dbo._org_secc_ChangeManager_ChangeRecord" );
        }
    }
}
