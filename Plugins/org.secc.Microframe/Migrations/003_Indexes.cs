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
    using org.secc.DevLib.Extensions.Migration;
    using Rock.Plugin;


    [MigrationNumber( 3, "1.8.0" )]
    public partial class Indexes : Migration
    {
        public override void Up()
        {
            try
            {
                this.AddPrimaryKey( "dbo._org_secc_Microframe_Sign", "Id" );
                this.AddForeignKey( "dbo._org_secc_Microframe_Sign", "CreatedByPersonAliasId", "dbo.PersonAlias" );
                this.AddForeignKey( "dbo._org_secc_Microframe_Sign", "ModifiedByPersonAliasId", "dbo.PersonAlias" );
                this.CreateIndex( "dbo._org_secc_Microframe_Sign", "CreatedByPersonAliasId" );
                this.CreateIndex( "dbo._org_secc_Microframe_Sign", "ModifiedByPersonAliasId" );
                this.CreateIndex( "dbo._org_secc_Microframe_Sign", "Guid", true );
                this.CreateIndex( "dbo._org_secc_Microframe_Sign", "ForeignId" );
                this.CreateIndex( "dbo._org_secc_Microframe_Sign", "ForeignGuid" );
                this.CreateIndex( "dbo._org_secc_Microframe_Sign", "ForeignKey" );
            }
            catch { }

            try
            {
                this.AddPrimaryKey( "dbo._org_secc_Microframe_SignCategory", "Id" );
                this.AddForeignKey( "dbo._org_secc_Microframe_SignCategory", "CreatedByPersonAliasId", "dbo.PersonAlias" );
                this.AddForeignKey( "dbo._org_secc_Microframe_SignCategory", "ModifiedByPersonAliasId", "dbo.PersonAlias" );
                this.CreateIndex( "dbo._org_secc_Microframe_SignCategory", "CreatedByPersonAliasId" );
                this.CreateIndex( "dbo._org_secc_Microframe_SignCategory", "ModifiedByPersonAliasId" );
                this.CreateIndex( "dbo._org_secc_Microframe_SignCategory", "Guid", true );
                this.CreateIndex( "dbo._org_secc_Microframe_SignCategory", "ForeignId" );
                this.CreateIndex( "dbo._org_secc_Microframe_SignCategory", "ForeignGuid" );
                this.CreateIndex( "dbo._org_secc_Microframe_SignCategory", "ForeignKey" );
            }
            catch { }

            try
            {
                this.AddPrimaryKey( "dbo._org_secc_Microframe_SignSignCategory", new string[] { "SignId", "SignCategoryId" } );
                this.AddForeignKey( "dbo._org_secc_Microframe_SignSignCategory", "SignId", "dbo._org_secc_Microframe_Sign", cascadeDelete: true );
                this.AddForeignKey( "dbo._org_secc_Microframe_SignSignCategory", "SignCategoryId", "dbo._org_secc_Microframe_SignCategory", cascadeDelete: true );
                this.CreateIndex( "dbo._org_secc_Microframe_SignSignCategory", "SignId" );
                this.CreateIndex( "dbo._org_secc_Microframe_SignSignCategory", "SignCategoryId" );
            }
            catch { }
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
        }
    }
}
