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

namespace org.secc.xAPI.Migrations
{
    using org.secc.DevLib.Extensions.Migration;
    using Rock.Plugin;

    [MigrationNumber( 2, "1.10.2" )]
    public partial class BuildDB : Migration
    {
        public override void Up()
        {
            CreateTable(
                "dbo._org_secc_xAPI_ExperienceObject",
                c => new
                {
                    Id = c.Int( nullable: false, identity: true ),
                    EntityTypeId = c.Int( nullable: false ),
                    ObjectId = c.String( maxLength: 100 ),
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
                .Index( t => t.ObjectId )
                .Index( t => t.CreatedByPersonAliasId )
                .Index( t => t.ModifiedByPersonAliasId )
                .Index( t => t.Guid, unique: true )
                .Run( this );


            CreateTable(
                "dbo._org_secc_xAPI_ExperienceQualifier",
                c => new
                {
                    Id = c.Int( nullable: false, identity: true ),
                    ParentId = c.Int( nullable: false ),
                    EntityTypeId = c.Int( nullable: false ),
                    Key = c.String(),
                    Value = c.String(),
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
                .Index( t => t.ParentId )
                .Index( t => t.EntityTypeId )
                .Index( t => t.CreatedByPersonAliasId )
                .Index( t => t.ModifiedByPersonAliasId )
                .Index( t => t.Guid, unique: true )
                .Run( this );

            CreateTable(
                "dbo._org_secc_xAPI_ExperienceResult",
                c => new
                {
                    Id = c.Int( nullable: false, identity: true ),
                    IsComplete = c.Boolean( nullable: false ),
                    WasSuccess = c.Boolean( nullable: false ),
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
                .ForeignKey( "dbo.PersonAlias", t => t.ModifiedByPersonAliasId )
                .Index( t => t.IsComplete )
                .Index( t => t.WasSuccess )
                .Index( t => t.CreatedByPersonAliasId )
                .Index( t => t.ModifiedByPersonAliasId )
                .Index( t => t.Guid, unique: true )
                .Run( this );

            CreateTable(
                "dbo._org_secc_xAPI_Experience",
                c => new
                {
                    Id = c.Int( nullable: false, identity: true ),
                    PersonAliasId = c.Int( nullable: false ),
                    VerbValueId = c.Int( nullable: false ),
                    xObjectId = c.Int(),
                    ResultId = c.Int(),
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
                .ForeignKey( "dbo.PersonAlias", t => t.ModifiedByPersonAliasId )
                .ForeignKey( "dbo._org_secc_xAPI_ExperienceObject", t => t.xObjectId )
                .ForeignKey( "dbo.PersonAlias", t => t.PersonAliasId )
                .ForeignKey( "dbo.DefinedValue", t => t.VerbValueId )
                .Index( t => t.PersonAliasId )
                .Index( t => t.VerbValueId )
                .Index( t => t.xObjectId )
                .Index( t => t.ResultId )
                .Index( t => t.CreatedByPersonAliasId )
                .Index( t => t.ModifiedByPersonAliasId )
                .Index( t => t.Guid, unique: true )
                .Run( this );
        }

        public override void Down()
        {

        }
    }
}
