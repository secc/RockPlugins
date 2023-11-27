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

    [MigrationNumber( 30, "1.10.2" )]
    public partial class LastMedicationCheckinAttribute : Migration
    {
        public override void Up()
        {
            RockMigrationHelper.AddOrUpdateEntityAttribute( "Rock.Model.Person",
                Rock.SystemGuid.FieldType.DATE,
                "", "",
                "Last Medication Checkin",
                "",
                "Date of the last medication check-in",
                0,
                "",
                Utilities.Constants.PERSON_ATTRIBUTE_LASTMEDICATIONCHECKIN,
                Utilities.Constants.PERSON_ATTRIBUTE_KEY_LASTMEDICATIONCHECKIN
                );
        }

        public override void Down()
        {

        }
    }
}
