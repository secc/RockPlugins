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


    [MigrationNumber( 1, "1.8.0" )]
    public partial class Init : Migration
    {
        public override void Up()
        {
            AddTable(
                "dbo._org_secc_Microframe_Sign",
                c => new
                {
                    Id = c.Int( nullable: false, identity: true ),
                    Name = c.String( maxLength: 255 ),
                    Description = c.String(),
                    IPAddress = c.String( maxLength: 45 ),
                    Port = c.String( maxLength: 8 ),
                    PIN = c.String( maxLength: 4 ),
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
                } );

            AddTable(
                "dbo._org_secc_Microframe_SignSignCategory",
                c => new
                {
                    SignId = c.Int( nullable: false ),
                    SignCategoryId = c.Int( nullable: false ),
                } );
        }

        public override void Down()
        {
            DropTable( "dbo._org_secc_Microframe_SignSignCategory" );
            DropTable( "dbo._org_secc_Microframe_SignCategory" );
            DropTable( "dbo._org_secc_Microframe_Sign" );
        }
    }
}
