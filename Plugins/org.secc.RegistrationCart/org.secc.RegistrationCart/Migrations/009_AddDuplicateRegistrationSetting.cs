// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License should be included with this file.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using Rock.Plugin;

namespace org.secc.RegistrationCart.Migrations
{
    /// <summary>
    /// Adds the "Registration Cart: Prevent Duplicate Registrations" global attribute (on by
    /// default): a camper already registered for an option can't be added for it again.
    /// </summary>
    [MigrationNumber( 9, "1.16.0" )]
    public class AddDuplicateRegistrationSetting : Migration
    {
        private const string BooleanFieldTypeGuid = "1EDAFDED-DFE6-4334-B019-6EECBA89E05A"; // Rock.SystemGuid.FieldType.BOOLEAN

        public override void Up()
        {
            RockMigrationHelper.AddGlobalAttribute( BooleanFieldTypeGuid, "", "", "Registration Cart: Prevent Duplicate Registrations",
                "Registration Cart refuses a camper who is already registered for the option (wait list included), at add-to-cart and at checkout.",
                0, "True", SystemGuid.Attribute.PREVENT_DUPLICATE_REGISTRATIONS, Services.CartSettings.PreventDuplicateRegistrationsKey, false );
        }

        public override void Down()
        {
            RockMigrationHelper.DeleteAttribute( SystemGuid.Attribute.PREVENT_DUPLICATE_REGISTRATIONS );
        }
    }
}
