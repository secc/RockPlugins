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
    using org.secc.DevLib.Extensions.Migration;
    using Rock.Plugin;

    [MigrationNumber( 9, "1.10.2" )]
    public partial class ExpirationTime : Migration
    {
        public override void Up()
        {
            AddColumn( "dbo._org_secc_FamilyCheckin_KioskType", "MinutesValid", c => c.Int() );
            AddColumn( "dbo._org_secc_FamilyCheckin_KioskType", "GraceMinutes", c => c.Int() );
            AddColumn( "dbo._org_secc_FamilyCheckin_KioskType", "IsMobile", c => c.Boolean( defaultValue: false ) );
            this.CreateIndex( "dbo._org_secc_FamilyCheckin_KioskType", "IsMobile" );
            Sql( "UPDATE [_org_secc_FamilyCheckin_KioskType] SET [IsMobile] = 0" );

        }

        public override void Down()
        {

        }
    }
}
