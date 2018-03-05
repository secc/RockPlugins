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
namespace org.secc.Microframe.Migrations
{
    using Rock.Plugin;


    [MigrationNumber( 1, "1.4.0" )]
    public partial class Init : Migration
    {
        public override void Up()
        {
            CreateTable(
                "dbo._org_secc_Microframe_Sign",
                c => new
                {
                    Id = c.Int( nullable: false, identity: true ),
                    Name = c.String( maxLength: 255 ),
                    Description = c.String(),
                    IPAddress = c.String( maxLength: 45 ),
                    PIN = c.String( maxLength: 4 ),
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
                .Index( t => t.CreatedByPersonAliasId )
                .Index( t => t.ModifiedByPersonAliasId )
                .Index( t => t.Guid, unique: true )
                .Index( t => t.ForeignId )
                .Index( t => t.ForeignGuid )
                .Index( t => t.ForeignKey );

            CreateTable(
                "dbo._org_secc_Microframe_SignCategory",
                c => new
                {
                    Id = c.Int( nullable: false, identity: true ),
                    Name = c.String( maxLength: 255 ),
                    Description = c.String(),
                    Codes = c.String(),
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
                .Index( t => t.CreatedByPersonAliasId )
                .Index( t => t.ModifiedByPersonAliasId )
                .Index( t => t.Guid, unique: true )
                .Index( t => t.ForeignId )
                .Index( t => t.ForeignGuid )
                .Index( t => t.ForeignKey );

            CreateTable(
                "dbo._org_secc_Microframe_SignSignCategory",
                c => new
                {
                    SignId = c.Int( nullable: false ),
                    SignCategoryId = c.Int( nullable: false ),
                } )
                .PrimaryKey( t => new { t.SignId, t.SignCategoryId } )
                .ForeignKey( "dbo._org_secc_Microframe_Sign", t => t.SignId, cascadeDelete: true )
                .ForeignKey( "dbo._org_secc_Microframe_SignCategory", t => t.SignCategoryId, cascadeDelete: true )
                .Index( t => t.SignId )
                .Index( t => t.SignCategoryId );
        }

        public override void Down()
        {
            DropForeignKey( "dbo._org_secc_Microframe_SignSignCategory", "SignCategoryId", "dbo._org_secc_Microframe_SignCategory" );
            DropForeignKey( "dbo._org_secc_Microframe_SignSignCategory", "SignId", "dbo._org_secc_Microframe_Sign" );
            DropForeignKey( "dbo._org_secc_Microframe_SignCategory", "ModifiedByPersonAliasId", "dbo.PersonAlias" );
            DropForeignKey( "dbo._org_secc_Microframe_SignCategory", "CreatedByPersonAliasId", "dbo.PersonAlias" );
            DropForeignKey( "dbo._org_secc_Microframe_Sign", "ModifiedByPersonAliasId", "dbo.PersonAlias" );
            DropForeignKey( "dbo._org_secc_Microframe_Sign", "CreatedByPersonAliasId", "dbo.PersonAlias" );
            DropIndex( "dbo._org_secc_Microframe_SignSignCategory", new[] { "SignCategoryId" } );
            DropIndex( "dbo._org_secc_Microframe_SignSignCategory", new[] { "SignId" } );
            DropIndex( "dbo._org_secc_Microframe_SignCategory", new[] { "ForeignKey" } );
            DropIndex( "dbo._org_secc_Microframe_SignCategory", new[] { "ForeignGuid" } );
            DropIndex( "dbo._org_secc_Microframe_SignCategory", new[] { "ForeignId" } );
            DropIndex( "dbo._org_secc_Microframe_SignCategory", new[] { "Guid" } );
            DropIndex( "dbo._org_secc_Microframe_SignCategory", new[] { "ModifiedByPersonAliasId" } );
            DropIndex( "dbo._org_secc_Microframe_SignCategory", new[] { "CreatedByPersonAliasId" } );
            DropIndex( "dbo._org_secc_Microframe_Sign", new[] { "ForeignKey" } );
            DropIndex( "dbo._org_secc_Microframe_Sign", new[] { "ForeignGuid" } );
            DropIndex( "dbo._org_secc_Microframe_Sign", new[] { "ForeignId" } );
            DropIndex( "dbo._org_secc_Microframe_Sign", new[] { "Guid" } );
            DropIndex( "dbo._org_secc_Microframe_Sign", new[] { "ModifiedByPersonAliasId" } );
            DropIndex( "dbo._org_secc_Microframe_Sign", new[] { "CreatedByPersonAliasId" } );
            DropTable( "dbo._org_secc_Microframe_SignSignCategory" );
            DropTable( "dbo._org_secc_Microframe_SignCategory" );
            DropTable( "dbo._org_secc_Microframe_Sign" );
        }
    }
}
