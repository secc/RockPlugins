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
namespace org.secc.Mapping.Migrations
{
    using Rock.Plugin;

    [MigrationNumber( 1, "1.8.0" )]

    public partial class Init : Migration
    {
        public override void Up()
        {
            CreateTable(
                "dbo._org_secc_Mapping_LocationDistanceStore",
                c => new
                {
                    Id = c.Int( nullable: false, identity: true ),
                    Origin = c.String(),
                    Destination = c.String(),
                    TravelDistance = c.Double( nullable: false ),
                    TravelDuration = c.Double( nullable: false ),
                    CalculatedBy = c.String(),
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
                .Index( t => t.Origin )
                .Index( t => t.Destination )
                .Index( t => t.CreatedByPersonAliasId )
                .Index( t => t.ModifiedByPersonAliasId )
                .Index( t => t.Guid, unique: true );


        }

        public override void Down()
        {
            DropIndex( "dbo._org_secc_Mapping_LocationDistanceStore", new[] { "Guid" } );
            DropIndex( "dbo._org_secc_Mapping_LocationDistanceStore", new[] { "ModifiedByPersonAliasId" } );
            DropIndex( "dbo._org_secc_Mapping_LocationDistanceStore", new[] { "CreatedByPersonAliasId" } );
            DropIndex( "dbo._org_secc_Mapping_LocationDistanceStore", new[] { "Destination" } );
            DropIndex( "dbo._org_secc_Mapping_LocationDistanceStore", new[] { "Origin" } );
            DropTable( "dbo._org_secc_Mapping_LocationDistanceStore" );
        }
    }
}
