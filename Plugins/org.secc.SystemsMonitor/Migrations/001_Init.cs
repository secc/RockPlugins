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
namespace org.secc.SystemsMonitor.Migrations
{
    using Rock.Plugin;
    using Rock.Transactions;

    [MigrationNumber( 1, "1.10.0" )]
    public partial class Init : Migration
    {
        public override void Up()
        {
            //Adding two tables for SystemTest & SystemTestHistory
            AddTable(
             "dbo._org_secc_SystemsMonitor_SystemTest",
             c => new
             {
                 Id = c.Int( nullable: false, identity: true ),
                 Name = c.String(),
                 Description = c.String(),
                 RunIntervalMinutes = c.Int(),
                 AlarmCondition = c.Int( nullable: false ),
                 AlarmScore = c.Int(),
                 EntityTypeId = c.Int(),
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
                "dbo._org_secc_SystemsMonitor_SystemTestHistory",
                c => new
                {
                    Id = c.Int( nullable: false, identity: true ),
                    SystemTestId = c.Int( nullable: false ),
                    Score = c.Int( nullable: false ),
                    Passed = c.Boolean( nullable: false ),
                    Message = c.String(),
                    CreatedDateTime = c.DateTime(),
                    ModifiedDateTime = c.DateTime(),
                    CreatedByPersonAliasId = c.Int(),
                    ModifiedByPersonAliasId = c.Int(),
                    Guid = c.Guid( nullable: false ),
                    ForeignId = c.Int(),
                    ForeignGuid = c.Guid(),
                    ForeignKey = c.String( maxLength: 100 ),
                } );

            //Adding primary keys for both tables
            AddPrimaryKey( "dbo._org_secc_SystemsMonitor_SystemTest", "Id" );
            AddPrimaryKey( "dbo._org_secc_SystemsMonitor_SystemTestHistory", "Id" );

            //Adding foreign keys & indices for SystemTest table
            AddForeignKey( "dbo._org_secc_SystemsMonitor_SystemTest", "CreatedByPersonAliasId", "dbo.PersonAlias" );
            AddForeignKey( "dbo._org_secc_SystemsMonitor_SystemTest", "EntityTypeId", "dbo.EntityType" );
            AddForeignKey( "dbo._org_secc_SystemsMonitor_SystemTest", "ModifiedByPersonAliasId", "dbo.PersonAlias" );
            AddIndex( "dbo._org_secc_SystemsMonitor_SystemTest", "EntityTypeId" );
            AddIndex( "dbo._org_secc_SystemsMonitor_SystemTest", "CreatedByPersonAliasId" );
            AddIndex( "dbo._org_secc_SystemsMonitor_SystemTest", "ModifiedByPersonAliasId" );
            AddIndex( "dbo._org_secc_SystemsMonitor_SystemTest", "Guid", unique: true );

            //Adding foreign keys & indices for SystemTestHistory table
            AddForeignKey( "dbo._org_secc_SystemsMonitor_SystemTestHistory", "CreatedByPersonAliasId", "dbo.PersonAlias" );
            AddForeignKey( "dbo._org_secc_SystemsMonitor_SystemTestHistory", "ModifiedByPersonAliasId", "dbo.PersonAlias" );
            AddForeignKey( "dbo._org_secc_SystemsMonitor_SystemTestHistory", "SystemTestId", "dbo._org_secc_SystemsMonitor_SystemTest" );
            AddIndex( "dbo._org_secc_SystemsMonitor_SystemTestHistory", "SystemTestId" );
            AddIndex( "dbo._org_secc_SystemsMonitor_SystemTestHistory", "CreatedByPersonAliasId" );
            AddIndex( "dbo._org_secc_SystemsMonitor_SystemTestHistory", "ModifiedByPersonAliasId" );
            AddIndex( "dbo._org_secc_SystemsMonitor_SystemTestHistory", "Guid", unique: true );

        }

        public override void Down()
        {
        }
    }
}
