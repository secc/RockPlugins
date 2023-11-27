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

    [MigrationNumber( 1, "1.4.0" )]
    public partial class Init : Migration
    {
        public override void Up()
        {
            AddTable(
                "dbo._org_secc_FamilyCheckin_KioskType",
                c => new
                {
                    Id = c.Int( nullable: false, identity: true ),
                    Name = c.String( maxLength: 255 ),
                    Description = c.String(),
                    CheckinTemplateId = c.Int(),
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
                "dbo._org_secc_FamilyCheckin_KioskTypeGroupType",
                c => new
                {
                    KioskTypeId = c.Int( nullable: false ),
                    GroupTypeId = c.Int( nullable: false ),
                } );

            AddTable(
                "dbo._org_secc_FamilyCheckin_KioskTypeLocation",
                c => new
                {
                    KioskTypeId = c.Int( nullable: false ),
                    LocationId = c.Int( nullable: false ),
                } );

            AddTable(
                "dbo._org_secc_FamilyCheckin_KioskTypeSchedule",
                c => new
                {
                    KioskTypeId = c.Int( nullable: false ),
                    ScheduleId = c.Int( nullable: false ),
                } );

            AddTable(
                "dbo._org_secc_FamilyCheckin_Kiosk",
                c => new
                {
                    Id = c.Int( nullable: false, identity: true ),
                    Name = c.String( maxLength: 255 ),
                    Description = c.String(),
                    KioskTypeId = c.Int(),
                    IPAddress = c.String( maxLength: 45 ),
                    PrinterDeviceId = c.Int(),
                    PrintFrom = c.Int( nullable: false ),
                    PrintToOverride = c.Int( nullable: false ),
                    CreatedDateTime = c.DateTime(),
                    ModifiedDateTime = c.DateTime(),
                    CreatedByPersonAliasId = c.Int(),
                    ModifiedByPersonAliasId = c.Int(),
                    Guid = c.Guid( nullable: false ),
                    ForeignId = c.Int(),
                    ForeignGuid = c.Guid(),
                    ForeignKey = c.String( maxLength: 100 ),
                } );

            AddPrimaryKey( "dbo._org_secc_FamilyCheckin_KioskType", "Id" );
            AddPrimaryKey( "dbo._org_secc_FamilyCheckin_KioskTypeGroupType", new string[] { "KioskTypeId", "GroupTypeId" } );
            AddPrimaryKey( "dbo._org_secc_FamilyCheckin_KioskTypeLocation", new string[] { "KioskTypeId", "LocationId" } );
            AddPrimaryKey( "dbo._org_secc_FamilyCheckin_KioskTypeSchedule", new string[] { "KioskTypeId", "ScheduleId" } );
            AddPrimaryKey( "dbo._org_secc_FamilyCheckin_Kiosk", "Id" );

            AddForeignKey( "dbo._org_secc_FamilyCheckin_KioskType", "CheckinTemplateId", "dbo.GroupType" );
            AddForeignKey( "dbo._org_secc_FamilyCheckin_KioskType", "CreatedByPersonAliasId", "dbo.PersonAlias" );
            AddForeignKey( "dbo._org_secc_FamilyCheckin_KioskType", "ModifiedByPersonAliasId", "dbo.PersonAlias" );
            AddIndex( "dbo._org_secc_FamilyCheckin_KioskType", "CheckinTemplateId" );
            AddIndex( "dbo._org_secc_FamilyCheckin_KioskType", "CreatedByPersonAliasId" );
            AddIndex( "dbo._org_secc_FamilyCheckin_KioskType", "ModifiedByPersonAliasId" );
            AddIndex( "dbo._org_secc_FamilyCheckin_KioskType", "Guid", unique: true );
            AddIndex( "dbo._org_secc_FamilyCheckin_KioskType", "ForeignId" );
            AddIndex( "dbo._org_secc_FamilyCheckin_KioskType", "ForeignGuid" );
            AddIndex( "dbo._org_secc_FamilyCheckin_KioskType", "ForeignKey" );

            AddForeignKey( "dbo._org_secc_FamilyCheckin_KioskTypeGroupType", "KioskTypeId", "dbo._org_secc_FamilyCheckin_KioskType", cascadeDelete: true );
            AddForeignKey( "dbo._org_secc_FamilyCheckin_KioskTypeGroupType", "GroupTypeId", "dbo.GroupType", cascadeDelete: true );
            AddIndex( "dbo._org_secc_FamilyCheckin_KioskTypeGroupType", "KioskTypeId" );
            AddIndex( "dbo._org_secc_FamilyCheckin_KioskTypeGroupType", "GroupTypeId" );

            AddForeignKey( "dbo._org_secc_FamilyCheckin_KioskTypeLocation", "KioskTypeId", "dbo._org_secc_FamilyCheckin_KioskType", cascadeDelete: true );
            AddForeignKey( "dbo._org_secc_FamilyCheckin_KioskTypeLocation", "LocationId", "dbo.Location", cascadeDelete: true );
            AddIndex( "dbo._org_secc_FamilyCheckin_KioskTypeLocation", "KioskTypeId" );
            AddIndex( "dbo._org_secc_FamilyCheckin_KioskTypeLocation", "LocationId" );

            AddForeignKey( "dbo._org_secc_FamilyCheckin_KioskTypeSchedule", "KioskTypeId", "dbo._org_secc_FamilyCheckin_KioskType", cascadeDelete: true );
            AddForeignKey( "dbo._org_secc_FamilyCheckin_KioskTypeSchedule", "ScheduleId", "dbo.Schedule", cascadeDelete: true );
            AddIndex( "dbo._org_secc_FamilyCheckin_KioskTypeSchedule", "KioskTypeId" );
            AddIndex( "dbo._org_secc_FamilyCheckin_KioskTypeSchedule", "ScheduleId" );

            AddForeignKey( "dbo._org_secc_FamilyCheckin_Kiosk", "CreatedByPersonAliasId", "dbo.PersonAlias" );
            AddForeignKey( "dbo._org_secc_FamilyCheckin_Kiosk", "KioskTypeId", "dbo._org_secc_FamilyCheckin_KioskType" );
            AddForeignKey( "dbo._org_secc_FamilyCheckin_Kiosk", "ModifiedByPersonAliasId", "dbo.PersonAlias" );
            AddForeignKey( "dbo._org_secc_FamilyCheckin_Kiosk", "PrinterDeviceId", "dbo.Device" );
            AddIndex( "dbo._org_secc_FamilyCheckin_Kiosk", "KioskTypeId" );
            AddIndex( "dbo._org_secc_FamilyCheckin_Kiosk", "PrinterDeviceId" );
            AddIndex( "dbo._org_secc_FamilyCheckin_Kiosk", "CreatedByPersonAliasId" );
            AddIndex( "dbo._org_secc_FamilyCheckin_Kiosk", "ModifiedByPersonAliasId" );
            AddIndex( "dbo._org_secc_FamilyCheckin_Kiosk", "Guid", unique: true );
            AddIndex( "dbo._org_secc_FamilyCheckin_Kiosk", "ForeignId" );
            AddIndex( "dbo._org_secc_FamilyCheckin_Kiosk", "ForeignGuid" );
            AddIndex( "dbo._org_secc_FamilyCheckin_Kiosk", "ForeignKey " );
        }
        public override void Down()
        {

            DropForeignKey( "dbo._org_secc_FamilyCheckin_Kiosk", "PrinterDeviceId", "dbo.Device" );
            DropForeignKey( "dbo._org_secc_FamilyCheckin_Kiosk", "ModifiedByPersonAliasId", "dbo.PersonAlias" );
            DropForeignKey( "dbo._org_secc_FamilyCheckin_Kiosk", "KioskTypeId", "dbo._org_secc_FamilyCheckin_KioskType" );
            DropForeignKey( "dbo._org_secc_FamilyCheckin_KioskTypeSchedule", "ScheduleId", "dbo.Schedule" );
            DropForeignKey( "dbo._org_secc_FamilyCheckin_KioskTypeSchedule", "KioskTypeId", "dbo._org_secc_FamilyCheckin_KioskType" );
            DropForeignKey( "dbo._org_secc_FamilyCheckin_KioskType", "ModifiedByPersonAliasId", "dbo.PersonAlias" );
            DropForeignKey( "dbo._org_secc_FamilyCheckin_KioskTypeLocation", "LocationId", "dbo.Location" );
            DropForeignKey( "dbo._org_secc_FamilyCheckin_KioskTypeLocation", "KioskTypeId", "dbo._org_secc_FamilyCheckin_KioskType" );
            DropForeignKey( "dbo._org_secc_FamilyCheckin_KioskTypeGroupType", "GroupTypeId", "dbo.GroupType" );
            DropForeignKey( "dbo._org_secc_FamilyCheckin_KioskTypeGroupType", "KioskTypeId", "dbo._org_secc_FamilyCheckin_KioskType" );
            DropForeignKey( "dbo._org_secc_FamilyCheckin_KioskType", "CreatedByPersonAliasId", "dbo.PersonAlias" );
            DropForeignKey( "dbo._org_secc_FamilyCheckin_KioskType", "CheckinTemplateId", "dbo.GroupType" );
            DropForeignKey( "dbo._org_secc_FamilyCheckin_Kiosk", "CreatedByPersonAliasId", "dbo.PersonAlias" );
            DropIndex( "dbo._org_secc_FamilyCheckin_KioskTypeSchedule", new[] { "ScheduleId" } );
            DropIndex( "dbo._org_secc_FamilyCheckin_KioskTypeSchedule", new[] { "KioskTypeId" } );
            DropIndex( "dbo._org_secc_FamilyCheckin_KioskTypeLocation", new[] { "LocationId" } );
            DropIndex( "dbo._org_secc_FamilyCheckin_KioskTypeLocation", new[] { "KioskTypeId" } );
            DropIndex( "dbo._org_secc_FamilyCheckin_KioskTypeGroupType", new[] { "GroupTypeId" } );
            DropIndex( "dbo._org_secc_FamilyCheckin_KioskTypeGroupType", new[] { "KioskTypeId" } );
            DropIndex( "dbo._org_secc_FamilyCheckin_KioskType", new[] { "ForeignKey" } );
            DropIndex( "dbo._org_secc_FamilyCheckin_KioskType", new[] { "ForeignGuid" } );
            DropIndex( "dbo._org_secc_FamilyCheckin_KioskType", new[] { "ForeignId" } );
            DropIndex( "dbo._org_secc_FamilyCheckin_KioskType", new[] { "Guid" } );
            DropIndex( "dbo._org_secc_FamilyCheckin_KioskType", new[] { "ModifiedByPersonAliasId" } );
            DropIndex( "dbo._org_secc_FamilyCheckin_KioskType", new[] { "CreatedByPersonAliasId" } );
            DropIndex( "dbo._org_secc_FamilyCheckin_KioskType", new[] { "CheckinTemplateId" } );
            DropIndex( "dbo._org_secc_FamilyCheckin_Kiosk", new[] { "ForeignKey" } );
            DropIndex( "dbo._org_secc_FamilyCheckin_Kiosk", new[] { "ForeignGuid" } );
            DropIndex( "dbo._org_secc_FamilyCheckin_Kiosk", new[] { "ForeignId" } );
            DropIndex( "dbo._org_secc_FamilyCheckin_Kiosk", new[] { "Guid" } );
            DropIndex( "dbo._org_secc_FamilyCheckin_Kiosk", new[] { "ModifiedByPersonAliasId" } );
            DropIndex( "dbo._org_secc_FamilyCheckin_Kiosk", new[] { "CreatedByPersonAliasId" } );
            DropIndex( "dbo._org_secc_FamilyCheckin_Kiosk", new[] { "PrinterDeviceId" } );
            DropIndex( "dbo._org_secc_FamilyCheckin_Kiosk", new[] { "KioskTypeId" } );
            DropTable( "dbo._org_secc_FamilyCheckin_KioskTypeSchedule" );
            DropTable( "dbo._org_secc_FamilyCheckin_KioskTypeLocation" );
            DropTable( "dbo._org_secc_FamilyCheckin_KioskTypeGroupType" );
            DropTable( "dbo._org_secc_FamilyCheckin_KioskType" );
            DropTable( "dbo._org_secc_FamilyCheckin_Kiosk" );
        }
    }
}
