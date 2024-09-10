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
    using org.secc.FamilyCheckin.Utilities;
    using Rock.Plugin;

    [MigrationNumber( 5, "1.10.2" )]
    public partial class KioskCategories : Migration
    {
        public override void Up()
        {
            var sql = string.Format( $@"
DECLARE @entityTypeId INT = (SELECT TOP (1) [Id] FROM [EntityType] WHERE [Name] = 'org.secc.FamilyCheckin.Model.Kiosk')

INSERT INTO [Category] (IsSystem, EntityTypeId, [Name], [Guid], [Order], [Description])
VALUES
(1, @entityTypeId, 'Check-in Station', '{Constants.KIOSK_CATEGORY_STATION}', 0, 'Organizationally owned check-in station');

INSERT INTO [Category] (IsSystem, EntityTypeId, [Name], [Guid], [Order], [Description])
VALUES
(1, @entityTypeId, 'Staff User', '{Constants.KIOSK_CATEGORY_STAFFUSER}', 0, 'Staff member who is using their login to access check-in information');

INSERT INTO [Category] (IsSystem, EntityTypeId, [Name], [Guid], [Order], [Description])
VALUES
(1, @entityTypeId, 'Mobile User', '{Constants.KIOSK_CATEGORY_MOBILEUSER}', 0, 'Church attendee who uses their personal device to check-in');

" );

            Sql( sql );
        }

        public override void Down()
        {

        }
    }
}
