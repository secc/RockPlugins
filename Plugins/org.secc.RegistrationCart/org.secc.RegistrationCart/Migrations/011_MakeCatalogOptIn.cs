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
    /// Makes the catalog opt-in. Adds two global attributes: "Registration Cart: Show New
    /// Templates" (off: a registration template appears only once staff turn it on in the
    /// cart's settings) and "Registration Cart: Default Grade Bands" (students only: what an
    /// instance or fee item with no setting of its own is open to). Also hides the catalog page
    /// from the External site's navigation until staff decide to link it.
    /// </summary>
    [MigrationNumber( 11, "1.16.0" )]
    public class MakeCatalogOptIn : Migration
    {
        private const string BooleanFieldTypeGuid = "1EDAFDED-DFE6-4334-B019-6EECBA89E05A";     // Rock.SystemGuid.FieldType.BOOLEAN
        private const string MultiSelectFieldTypeGuid = "BD0D9B57-2A41-4490-89FF-F01DAB7D4904"; // Rock.SystemGuid.FieldType.MULTI_SELECT

        public override void Up()
        {
            RockMigrationHelper.AddGlobalAttribute( BooleanFieldTypeGuid, "", "", "Registration Cart: Show New Templates",
                "Whether a registration template the Registration Cart settings haven't been saved for shows in the catalog. Off: staff turn each template on.",
                0, "False", SystemGuid.Attribute.DEFAULT_VISIBILITY, Services.CartSettings.DefaultVisibilityKey, false );

            RockMigrationHelper.AddGlobalAttribute( MultiSelectFieldTypeGuid, "", "", "Registration Cart: Default Grade Bands",
                "Who a Registration Cart instance or fee item can be registered for when its settings haven't been saved.",
                0, "1,2,4", SystemGuid.Attribute.DEFAULT_GRADE_BANDS, Services.CartSettings.DefaultGradeBandsKey, false );
            RockMigrationHelper.AddAttributeQualifier( SystemGuid.Attribute.DEFAULT_GRADE_BANDS, "values", "1^Elementary,2^Middle School,4^High School,8^Adults",
                SystemGuid.Attribute.DEFAULT_GRADE_BANDS_VALUES_QUALIFIER );

            Sql( $"UPDATE [Page] SET [DisplayInNavWhen] = 2 WHERE [Guid] = '{SystemGuid.Page.CAMP_CART}'" );
        }

        public override void Down()
        {
            Sql( $"UPDATE [Page] SET [DisplayInNavWhen] = 0 WHERE [Guid] = '{SystemGuid.Page.CAMP_CART}'" );
            RockMigrationHelper.DeleteAttribute( SystemGuid.Attribute.DEFAULT_GRADE_BANDS );
            RockMigrationHelper.DeleteAttribute( SystemGuid.Attribute.DEFAULT_VISIBILITY );
        }
    }
}
