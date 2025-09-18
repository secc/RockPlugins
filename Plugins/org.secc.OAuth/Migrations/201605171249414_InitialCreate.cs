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
namespace org.secc.OAuth.Migrations
{
    using Rock.Plugin;


    [MigrationNumber( 1, "1.4.5" )]
    public partial class InitialCreate : Rock.Plugin.Migration
    {
        public override void Up()
        {

            //Adding four tables for OAuth Client, Scope, ClientScope, and Authorization
            AddTable(
                "dbo._org_secc_OAuth_Client",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    ClientName = c.String(maxLength: 255),
                    ApiKey = c.Guid(nullable: false),
                    ApiSecret = c.Guid(nullable: false),
                    CallbackUrl = c.String(),
                    Active = c.Boolean(nullable: false),
                    CreatedDateTime = c.DateTime(),
                    ModifiedDateTime = c.DateTime(),
                    CreatedByPersonAliasId = c.Int(),
                    ModifiedByPersonAliasId = c.Int(),
                    Guid = c.Guid(nullable: false),
                    ForeignId = c.Int(),
                    ForeignGuid = c.Guid(),
                    ForeignKey = c.String(maxLength: 100),
                });
            
            AddTable(
                "dbo._org_secc_OAuth_Scope",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    Identifier = c.String(maxLength: 255),
                    Description = c.String(),
                    Active = c.Boolean(nullable: false),
                    CreatedDateTime = c.DateTime(),
                    ModifiedDateTime = c.DateTime(),
                    CreatedByPersonAliasId = c.Int(),
                    ModifiedByPersonAliasId = c.Int(),
                    Guid = c.Guid(nullable: false),
                    ForeignId = c.Int(),
                    ForeignGuid = c.Guid(),
                    ForeignKey = c.String(maxLength: 100),
                });

            AddTable(
                "dbo._org_secc_OAuth_ClientScope",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    ClientId = c.Int(nullable: false),
                    ScopeId = c.Int(nullable: false),
                    Active = c.Boolean(nullable: false),
                    CreatedDateTime = c.DateTime(),
                    ModifiedDateTime = c.DateTime(),
                    CreatedByPersonAliasId = c.Int(),
                    ModifiedByPersonAliasId = c.Int(),
                    Guid = c.Guid(nullable: false),
                    ForeignId = c.Int(),
                    ForeignGuid = c.Guid(),
                    ForeignKey = c.String(maxLength: 100),
                });

            AddTable(
                "dbo._org_secc_OAuth_Authorization",
                c => new
                {
                    Id = c.Int(nullable: false, identity: true),
                    ClientId = c.Int(nullable: false),
                    ScopeId = c.Int(nullable: false),
                    UserLoginId = c.Int(nullable: false),
                    Active = c.Boolean(nullable: false),
                    CreatedDateTime = c.DateTime(),
                    ModifiedDateTime = c.DateTime(),
                    CreatedByPersonAliasId = c.Int(),
                    ModifiedByPersonAliasId = c.Int(),
                    Guid = c.Guid(nullable: false),
                    ForeignId = c.Int(),
                    ForeignGuid = c.Guid(),
                    ForeignKey = c.String(maxLength: 100),
                });

            //Adding primary keys for all four OAuth tables
            AddPrimaryKey("dbo._org_secc_OAuth_Client", "Id");
            AddPrimaryKey("dbo._org_secc_OAuth_Scope", "Id");
            AddPrimaryKey("dbo._org_secc_OAuth_ClientScope", "Id");
            AddPrimaryKey("dbo._org_secc_OAuth_Authorization", "Id");

            //Adding foreign keys & indices for OAuth Client table
            AddForeignKey("dbo._org_secc_OAuth_Client", "CreatedByPersonAliasId", "dbo.PersonAlias" );
            AddForeignKey( "dbo._org_secc_OAuth_Client", "ModifiedByPersonAliasId", "dbo.PersonAlias" );
            AddIndex( "dbo._org_secc_OAuth_Client", "CreatedByPersonAliasId" );
            AddIndex( "dbo._org_secc_OAuth_Client", "ModifiedByPersonAliasId" );
            AddIndex( "dbo._org_secc_OAuth_Client", "Guid", unique: true );
            AddIndex( "dbo._org_secc_OAuth_Client", "ForeignId" );
            AddIndex( "dbo._org_secc_OAuth_Client", "ForeignGuid" );
            AddIndex( "dbo._org_secc_OAuth_Client", "ForeignKey" );

            //Adding foreign keys & indices for OAuth Scope table
            AddForeignKey( "dbo._org_secc_OAuth_Scope", "CreatedByPersonAliasId", "dbo.PersonAlias" );
            AddForeignKey( "dbo._org_secc_OAuth_Scope", "ModifiedByPersonAliasId", "dbo.PersonAlias");
            AddIndex( "dbo._org_secc_OAuth_Scope", "CreatedByPersonAliasId" );
            AddIndex( "dbo._org_secc_OAuth_Scope", "ModifiedByPersonAliasId" );
            AddIndex( "dbo._org_secc_OAuth_Scope", "Guid", unique: true );
            AddIndex( "dbo._org_secc_OAuth_Scope", "ForeignId" );
            AddIndex( "dbo._org_secc_OAuth_Scope", "ForeignGuid" );
            AddIndex( "dbo._org_secc_OAuth_Scope", "ForeignKey" );

            //Adding foreign keys & indices for OAuth ClientScope table
            AddForeignKey( "dbo._org_secc_OAuth_ClientScope", "CreatedByPersonAliasId", "dbo.PersonAlias" );
            AddForeignKey( "dbo._org_secc_OAuth_ClientScope", "ModifiedByPersonAliasId", "dbo.PersonAlias" );
            AddForeignKey( "dbo._org_secc_OAuth_ClientScope", "ClientId", "_org_secc_OAuth_Client", cascadeDelete: true );
            AddForeignKey( "dbo._org_secc_OAuth_ClientScope", "ScopeId", "_org_secc_OAuth_Scope", cascadeDelete: true );
            AddIndex( "dbo._org_secc_OAuth_ClientScope", "ClientId" );
            AddIndex( "dbo._org_secc_OAuth_ClientScope", "ScopeId" );
            AddIndex( "dbo._org_secc_OAuth_ClientScope", "CreatedByPersonAliasId" );
            AddIndex( "dbo._org_secc_OAuth_ClientScope", "ModifiedByPersonAliasId" );
            AddIndex( "dbo._org_secc_OAuth_ClientScope", "Guid", unique: true );
            AddIndex( "dbo._org_secc_OAuth_ClientScope", "ForeignId" );
            AddIndex( "dbo._org_secc_OAuth_ClientScope", "ForeignGuid" );
            AddIndex( "dbo._org_secc_OAuth_ClientScope", "ForeignKey" );

            //Adding foreign keys & indices for OAuth Authorization table
            AddForeignKey("dbo._org_secc_OAuth_Authorization", "CreatedByPersonAliasId", "dbo.PersonAlias" );
            AddForeignKey( "dbo._org_secc_OAuth_Authorization", "ModifiedByPersonAliasId", "dbo.PersonAlias" );
            AddForeignKey( "dbo._org_secc_OAuth_Authorization", "ClientId", "_org_secc_OAuth_Client", cascadeDelete: true );
            AddForeignKey( "dbo._org_secc_OAuth_Authorization", "ScopeId", "_org_secc_OAuth_Scope", cascadeDelete: true );
            AddForeignKey( "dbo._org_secc_OAuth_Authorization", "UserLoginId", "UserLogin", cascadeDelete: true );
            AddIndex( "dbo._org_secc_OAuth_Authorization", "ClientId" );
            AddIndex( "dbo._org_secc_OAuth_Authorization", "ScopeId" );
            AddIndex( "dbo._org_secc_OAuth_Authorization", "UserLoginId" );
            AddIndex( "dbo._org_secc_OAuth_Authorization", "CreatedByPersonAliasId" );
            AddIndex( "dbo._org_secc_OAuth_Authorization", "ModifiedByPersonAliasId" );
            AddIndex( "dbo._org_secc_OAuth_Authorization", "Guid", unique: true );
            AddIndex( "dbo._org_secc_OAuth_Authorization", "ForeignId" );
            AddIndex( "dbo._org_secc_OAuth_Authorization", "ForeignGuid" );
            AddIndex( "dbo._org_secc_OAuth_Authorization", "ForeignKey" );
        }

        public override void Down()
        {
            DropIndex( "dbo._org_secc_OAuth_Client", new[] { "ForeignKey" } );
            DropIndex( "dbo._org_secc_OAuth_Client", new[] { "ForeignGuid" } );
            DropIndex( "dbo._org_secc_OAuth_Client", new[] { "ForeignId" } );
            DropIndex( "dbo._org_secc_OAuth_Client", new[] { "Guid" } );
            DropIndex( "dbo._org_secc_OAuth_Client", new[] { "ModifiedByPersonAliasId" } );
            DropIndex( "dbo._org_secc_OAuth_Client", new[] { "CreatedByPersonAliasId" } );
            DropIndex( "dbo._org_secc_OAuth_Authorization", new[] { "ForeignKey" } );
            DropIndex( "dbo._org_secc_OAuth_Authorization", new[] { "ForeignGuid" } );
            DropIndex( "dbo._org_secc_OAuth_Authorization", new[] { "ForeignId" } );
            DropIndex( "dbo._org_secc_OAuth_Authorization", new[] { "Guid" } );
            DropIndex( "dbo._org_secc_OAuth_Authorization", new[] { "ModifiedByPersonAliasId" } );
            DropIndex( "dbo._org_secc_OAuth_Authorization", new[] { "CreatedByPersonAliasId" } );
            DropIndex( "dbo._org_secc_OAuth_Authorization", new[] { "UserLoginId" } );
            DropIndex( "dbo._org_secc_OAuth_Authorization", new[] { "ScopeId" } );
            DropIndex( "dbo._org_secc_OAuth_Authorization", new[] { "ClientId" } );

            DropTable( "dbo._org_secc_OAuth_ClientScope" );
            DropTable( "dbo._org_secc_OAuth_Scope" );

            DropTable( "dbo._org_secc_OAuth_Client" );
            DropTable( "dbo._org_secc_OAuth_Authorization" );
        }
    }
}
