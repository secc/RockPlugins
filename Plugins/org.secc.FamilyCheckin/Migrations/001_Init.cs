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
            CreateTable(
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
                } )
                .PrimaryKey( t => t.Id )
                .ForeignKey( "dbo.GroupType", t => t.CheckinTemplateId )
                .ForeignKey( "dbo.PersonAlias", t => t.CreatedByPersonAliasId )
                .ForeignKey( "dbo.PersonAlias", t => t.ModifiedByPersonAliasId )
                .Index( t => t.CheckinTemplateId )
                .Index( t => t.CreatedByPersonAliasId )
                .Index( t => t.ModifiedByPersonAliasId )
                .Index( t => t.Guid, unique: true )
                .Index( t => t.ForeignId )
                .Index( t => t.ForeignGuid )
                .Index( t => t.ForeignKey );

            CreateTable(
                "dbo._org_secc_FamilyCheckin_KioskTypeGroupType",
                c => new
                {
                    KioskTypeId = c.Int( nullable: false ),
                    GroupTypeId = c.Int( nullable: false ),
                } )
                .PrimaryKey( t => new { t.KioskTypeId, t.GroupTypeId } )
                .ForeignKey( "dbo._org_secc_FamilyCheckin_KioskType", t => t.KioskTypeId, cascadeDelete: true )
                .ForeignKey( "dbo.GroupType", t => t.GroupTypeId, cascadeDelete: true )
                .Index( t => t.KioskTypeId )
                .Index( t => t.GroupTypeId );

            CreateTable(
                "dbo._org_secc_FamilyCheckin_KioskTypeLocation",
                c => new
                {
                    KioskTypeId = c.Int( nullable: false ),
                    LocationId = c.Int( nullable: false ),
                } )
                .PrimaryKey( t => new { t.KioskTypeId, t.LocationId } )
                .ForeignKey( "dbo._org_secc_FamilyCheckin_KioskType", t => t.KioskTypeId, cascadeDelete: true )
                .ForeignKey( "dbo.Location", t => t.LocationId, cascadeDelete: true )
                .Index( t => t.KioskTypeId )
                .Index( t => t.LocationId );

            CreateTable(
                "dbo._org_secc_FamilyCheckin_KioskTypeSchedule",
                c => new
                {
                    KioskTypeId = c.Int( nullable: false ),
                    ScheduleId = c.Int( nullable: false ),
                } )
                .PrimaryKey( t => new { t.KioskTypeId, t.ScheduleId } )
                .ForeignKey( "dbo._org_secc_FamilyCheckin_KioskType", t => t.KioskTypeId, cascadeDelete: true )
                .ForeignKey( "dbo.Schedule", t => t.ScheduleId, cascadeDelete: true )
                .Index( t => t.KioskTypeId )
                .Index( t => t.ScheduleId );

            CreateTable(
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
                } )
                .PrimaryKey( t => t.Id )
                .ForeignKey( "dbo.PersonAlias", t => t.CreatedByPersonAliasId )
                .ForeignKey( "dbo._org_secc_FamilyCheckin_KioskType", t => t.KioskTypeId )
                .ForeignKey( "dbo.PersonAlias", t => t.ModifiedByPersonAliasId )
                .ForeignKey( "dbo.Device", t => t.PrinterDeviceId )
                .Index( t => t.KioskTypeId )
                .Index( t => t.PrinterDeviceId )
                .Index( t => t.CreatedByPersonAliasId )
                .Index( t => t.ModifiedByPersonAliasId )
                .Index( t => t.Guid, unique: true )
                .Index( t => t.ForeignId )
                .Index( t => t.ForeignGuid )
                .Index( t => t.ForeignKey );
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
