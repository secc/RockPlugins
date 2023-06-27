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
namespace org.secc.FamilyCheckin.Migrations
{
    using Rock.Plugin;

    [MigrationNumber( 8, "1.10.2" )]
    public partial class MobileCheckinRecordMigration : Migration
    {
        public override void Up()
        {
            AddTable(
                "dbo._org_secc_FamilyCheckin_MobileCheckinRecord",
                c => new
                {
                    Id = c.Int( nullable: false, identity: true ),
                    AccessKey = c.String( nullable: false, maxLength: 255 ),
                    UserName = c.String( nullable: false, maxLength: 255 ),
                    FamilyGroupId = c.Int( nullable: false ),
                    SerializedCheckInState = c.String(),
                    ReservedUntilDateTime = c.DateTime(),
                    ExpirationDateTime = c.DateTime(),
                    CampusId = c.Int( nullable: false ),
                    Status = c.Int( nullable: false ),
                    IsDirty = c.Boolean(),
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
                "dbo._org_secc_FamilyCheckin_MobileCheckinRecordAttendances",
                c => new
                {
                    MobileCheckinRecordId = c.Int( nullable: false ),
                    AttendanceId = c.Int( nullable: false ),
                } );

            AddPrimaryKey( "dbo._org_secc_FamilyCheckin_MobileCheckinRecord", "Id" );
            AddPrimaryKey( "dbo._org_secc_FamilyCheckin_MobileCheckinRecordAttendances", new string[] { "MobileCheckinRecordId", "AttendanceId" } );

            AddForeignKey( "dbo._org_secc_FamilyCheckin_MobileCheckinRecord", "CampusId", "dbo.Campus" );
            AddForeignKey( "dbo._org_secc_FamilyCheckin_MobileCheckinRecord", "FamilyGroupId", "dbo.Group" );
            AddForeignKey( "dbo._org_secc_FamilyCheckin_MobileCheckinRecord", "CreatedByPersonAliasId", "dbo.PersonAlias" );
            AddForeignKey( "dbo._org_secc_FamilyCheckin_MobileCheckinRecord", "ModifiedByPersonAliasId", "dbo.PersonAlias" );
            AddIndex( "dbo._org_secc_FamilyCheckin_MobileCheckinRecord", "AccessKey", unique: true );
            AddIndex( "dbo._org_secc_FamilyCheckin_MobileCheckinRecord", "UserName" );
            AddIndex( "dbo._org_secc_FamilyCheckin_MobileCheckinRecord", "CampusId" );
            AddIndex( "dbo._org_secc_FamilyCheckin_MobileCheckinRecord", "FamilyGroupId" );
            AddIndex( "dbo._org_secc_FamilyCheckin_MobileCheckinRecord", "Status" );
            AddIndex( "dbo._org_secc_FamilyCheckin_MobileCheckinRecord", "CreatedByPersonAliasId" );
            AddIndex( "dbo._org_secc_FamilyCheckin_MobileCheckinRecord", "ModifiedByPersonAliasId" );
            AddIndex( "dbo._org_secc_FamilyCheckin_MobileCheckinRecord", "Guid", unique: true );

            AddForeignKey( "dbo._org_secc_FamilyCheckin_MobileCheckinRecordAttendances", "MobileCheckinRecordId", "dbo._org_secc_FamilyCheckin_MobileCheckinRecord" );
            AddForeignKey( "dbo._org_secc_FamilyCheckin_MobileCheckinRecordAttendances", "AttendanceId", "dbo.Attendance" );
            AddIndex( "dbo._org_secc_FamilyCheckin_MobileCheckinRecordAttendances", "MobileCheckinRecordId" );
            AddIndex( "dbo._org_secc_FamilyCheckin_MobileCheckinRecordAttendances", "AttendanceId" );
        }

        public override void Down()
        {

        }
    }
}
