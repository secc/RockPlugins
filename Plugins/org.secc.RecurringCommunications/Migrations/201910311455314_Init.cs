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
namespace org.secc.RecurringCommunications.Migrations
{
    using Rock.Plugin;

    [MigrationNumber( 1, "1.8.0" )]
    public partial class Init : Migration
    {
        public override void Up()
        {
            AddTable(
                "dbo._org_secc_RecurringCommunications_RecurringCommunication",
                c => new
                {
                    Id = c.Int( nullable: false, identity: true ),
                    Name = c.String(),
                    DataViewId = c.Int( nullable: false ),
                    ScheduleId = c.Int( nullable: false ),
                    LastRunDateTime = c.DateTime(),
                    CommunicationType = c.Int( nullable: false ),
                    FromName = c.String(),
                    FromEmail = c.String(),
                    Subject = c.String(),
                    EmailBody = c.String(),
                    SMSBody = c.String(),
                    PhoneNumberValueId = c.Int(),
                    PushTitle = c.String(),
                    PushMessage = c.String(),
                    PushSound = c.String(),
                    ScheduleDescription = c.String(),
                    CreatedDateTime = c.DateTime(),
                    ModifiedDateTime = c.DateTime(),
                    CreatedByPersonAliasId = c.Int(),
                    ModifiedByPersonAliasId = c.Int(),
                    Guid = c.Guid( nullable: false ),
                    ForeignId = c.Int(),
                    ForeignGuid = c.Guid(),
                    ForeignKey = c.String( maxLength: 100 ),
                } );
            AddPrimaryKey( "dbo._org_secc_RecurringCommunications_RecurringCommunication", "Id" );
            AddForeignKey( "dbo._org_secc_RecurringCommunications_RecurringCommunication", "CreatedByPersonAliasId", "dbo.PersonAlias" );
            AddForeignKey( "dbo._org_secc_RecurringCommunications_RecurringCommunication", "DataViewId", "dbo.DataView" );
            AddForeignKey( "dbo._org_secc_RecurringCommunications_RecurringCommunication", "ModifiedByPersonAliasId", "dbo.PersonAlias" );
            AddForeignKey( "dbo._org_secc_RecurringCommunications_RecurringCommunication", "PhoneNumberValueId", "dbo.DefinedValue" );
            AddForeignKey( "dbo._org_secc_RecurringCommunications_RecurringCommunication", "ScheduleId", "dbo.Schedule", cascadeDelete: true );
            AddIndex( "dbo._org_secc_RecurringCommunications_RecurringCommunication", "DataViewId" );
            AddIndex( "dbo._org_secc_RecurringCommunications_RecurringCommunication", "ScheduleId" );
            AddIndex( "dbo._org_secc_RecurringCommunications_RecurringCommunication", "PhoneNumberValueId" );
            AddIndex( "dbo._org_secc_RecurringCommunications_RecurringCommunication", "CreatedByPersonAliasId" );
            AddIndex( "dbo._org_secc_RecurringCommunications_RecurringCommunication", "ModifiedByPersonAliasId" );
            AddIndex( "dbo._org_secc_RecurringCommunications_RecurringCommunication", "Guid", unique: true );
        }

        public override void Down()
        {
            DropTable( "dbo.RecurringCommunication" );
        }
    }
}
