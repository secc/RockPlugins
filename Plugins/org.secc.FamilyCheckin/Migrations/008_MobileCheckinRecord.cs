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
    using org.secc.DevLib.Extensions.Migration;

    [MigrationNumber( 8, "1.10.2" )]
    public partial class MobileCheckinRecordMigration : Migration
    {
        public override void Up()
        {
            CreateTable(
                "dbo._org_secc_FamilyCheckin_MobileCheckinRecord",
                c => new
                {
                    Id = c.Int( nullable: false, identity: true ),
                    AccessKey = c.String( nullable: false, maxLength: 255 ),
                    UserName = c.String( nullable: false, maxLength: 255 ),
                    FamilyGroupId = c.Int( nullable: false ),
                    SerializedCheckInState = c.String(),
                    ExpirationDateTime = c.DateTime(),
                    CampusId = c.Int( nullable: false ),
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
                .ForeignKey( "dbo.Campus", t => t.CampusId )
                .ForeignKey( "dbo.Group", t => t.FamilyGroupId )
                .ForeignKey( "dbo.PersonAlias", t => t.CreatedByPersonAliasId )
                .ForeignKey( "dbo.PersonAlias", t => t.ModifiedByPersonAliasId )
                .Index( t => t.AccessKey, unique: true )
                .Index( t => t.UserName, unique: true )
                .Index( t => t.CampusId )
                .Index( t => t.FamilyGroupId )
                .Index( t => t.CreatedByPersonAliasId )
                .Index( t => t.ModifiedByPersonAliasId )
                .Index( t => t.Guid, unique: true )
                .Run( this );

            CreateTable(
                "dbo._org_secc_FamilyCheckin_MobileCheckinRecordAttendances",
                c => new
                {
                    MobileCheckinRecordId = c.Int( nullable: false ),
                    AttendanceId = c.Int( nullable: false ),
                } )
                .PrimaryKey( t => new { t.MobileCheckinRecordId, t.AttendanceId } )
                .ForeignKey( "dbo._org_secc_FamilyCheckin_MobileCheckinRecord", t => t.MobileCheckinRecordId )
                .ForeignKey( "dbo.Attendance", t => t.AttendanceId )
                .Index( t => t.MobileCheckinRecordId )
                .Index( t => t.AttendanceId )
                .Run( this );

        }

        public override void Down()
        {

        }
    }
}
