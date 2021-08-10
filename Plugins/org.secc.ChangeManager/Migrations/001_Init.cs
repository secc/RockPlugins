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
            CreateTable(
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
                } )
                .PrimaryKey( t => t.Id )
                .ForeignKey( "dbo._org_secc_ChangeManager_ChangeRequest", t => t.ChangeRequestId, cascadeDelete: true )
                .ForeignKey( "dbo.PersonAlias", t => t.CreatedByPersonAliasId )
                .ForeignKey( "dbo.PersonAlias", t => t.ModifiedByPersonAliasId )
                .ForeignKey( "dbo.EntityType", t => t.RelatedEntityTypeId )
                .Index( t => t.ChangeRequestId )
                .Index( t => t.IsRejected )
                .Index( t => t.WasApplied )
                .Index( t => t.RelatedEntityTypeId )
                .Index( t => t.RelatedEntityId )
                .Index( t => t.CreatedByPersonAliasId )
                .Index( t => t.ModifiedByPersonAliasId )
                .Index( t => t.Guid, unique: true );

            CreateTable(
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
                } )
                .PrimaryKey( t => t.Id )
                .ForeignKey( "dbo.PersonAlias", t => t.CreatedByPersonAliasId )
                .ForeignKey( "dbo.EntityType", t => t.EntityTypeId )
                .ForeignKey( "dbo.PersonAlias", t => t.ModifiedByPersonAliasId )
                .Index( t => t.EntityTypeId )
                .Index( t => t.EntityId )
                .Index( t => t.RequestorAliasId )
                .Index( t => t.ApproverAliasId )
                .Index( t => t.IsComplete )
                .Index( t => t.CreatedByPersonAliasId )
                .Index( t => t.ModifiedByPersonAliasId )
                .Index( t => t.Guid, unique: true );
        }

        public override void Down()
        {
            DropTable( "dbo._org_secc_ChangeManager_ChangeRequest" );
            DropTable( "dbo._org_secc_ChangeManager_ChangeRecord" );
        }
    }
}
