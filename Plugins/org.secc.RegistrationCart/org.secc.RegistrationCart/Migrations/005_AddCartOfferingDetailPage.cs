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
    /// Adds the Cart block's "Offering Detail Page" setting (where a line's Edit link goes) and
    /// points the cart page's block at the offering page created by migration 001.
    /// </summary>
    [MigrationNumber( 5, "1.16.0" )]
    public class AddCartOfferingDetailPage : Migration
    {
        private const string PageReferenceFieldTypeGuid = "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108";  // Rock.SystemGuid.FieldType.PAGE_REFERENCE

        public override void Up()
        {
            RockMigrationHelper.UpdateBlockTypeAttribute( SystemGuid.BlockType.CART, PageReferenceFieldTypeGuid, "Offering Detail Page", "OfferingDetailPage", "", "", 2, "",
                SystemGuid.BlockTypeAttribute.CART_OFFERING_DETAIL_PAGE );

            // Only where migration 001 created the pages (it skips them without an External site).
            var hasPages = SqlScalar( $@"
                SELECT COUNT(*) FROM [Block] b, [Page] p
                WHERE b.[Guid] = '{SystemGuid.Block.CART}' AND p.[Guid] = '{SystemGuid.Page.OFFERING}'" );
            if ( hasPages is int count && count > 0 )
            {
                RockMigrationHelper.AddBlockAttributeValue( SystemGuid.Block.CART, SystemGuid.BlockTypeAttribute.CART_OFFERING_DETAIL_PAGE,
                    SystemGuid.Page.OFFERING + "," + SystemGuid.PageRoute.OFFERING );
            }
        }

        public override void Down()
        {
            RockMigrationHelper.DeleteAttribute( SystemGuid.BlockTypeAttribute.CART_OFFERING_DETAIL_PAGE );
        }
    }
}
