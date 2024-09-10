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
namespace org.secc.Reporting.Migrations
{
    using Rock.Plugin;

    [MigrationNumber( 1, "1.8.0" )]

    public partial class Init : Migration
    {
        public override void Up()
        {
            AddTable(
                "dbo._org_secc_Reporting_DataViewSQLFilterStore",
                c => new
                {
                    Hash = c.String( nullable: false, maxLength: 64 ),
                    EntityId = c.Int( nullable: false ),
                } );
            AddPrimaryKey( "dbo._org_secc_Reporting_DataViewSQLFilterStore", new[] { "Hash", "EntityId" } );
            AddIndex( "dbo._org_secc_Reporting_DataViewSQLFilterStore", "Hash" );

        }

        public override void Down()
        {
        }
    }
}
