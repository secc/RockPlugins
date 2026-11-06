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
    /// One discount code per registration, from the registration template's discounts
    /// (ROCK-9394). Removes the cart-wide discount codes (the Discount Codes admin page, its block
    /// type, and the DiscountCode and CartDiscount tables and entity types) and adds
    /// CartLineItem.IsTemplateDiscountLocked: the line's code came from the discount code
    /// workflow (financial assistance), so the family can't swap it. Existing lines' codes all
    /// came from that workflow. Down only removes the new column; the cart-wide codes are gone
    /// from the plugin.
    /// </summary>
    [MigrationNumber( 12, "1.16.0" )]
    public class OneTemplateDiscountPerRegistration : Migration
    {
        public override void Up()
        {
            Sql( @"
                IF COL_LENGTH('dbo._org_secc_RegistrationCart_CartLineItem', 'IsTemplateDiscountLocked') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[_org_secc_RegistrationCart_CartLineItem]
                        ADD [IsTemplateDiscountLocked] [bit] NOT NULL CONSTRAINT [DF__org_secc_RegistrationCart_CartLineItem_IsTemplateDiscountLocked] DEFAULT 0;
                END" );

            Sql( @"
                UPDATE [dbo].[_org_secc_RegistrationCart_CartLineItem]
                SET [IsTemplateDiscountLocked] = 1
                WHERE ISNULL([TemplateDiscountCode], '') <> '';" );

            RockMigrationHelper.DeleteBlock( SystemGuid.Block.ADMIN_DISCOUNT_CODES );
            RockMigrationHelper.DeletePage( SystemGuid.Page.ADMIN_DISCOUNT_CODES );
            RockMigrationHelper.DeleteBlockType( SystemGuid.BlockType.ADMIN_DISCOUNT_CODES );
            RockMigrationHelper.DeleteEntityType( SystemGuid.EntityType.BLOCK_ADMIN_DISCOUNT_CODES );

            Sql( @"
                DROP TABLE IF EXISTS [dbo].[_org_secc_RegistrationCart_CartDiscount];
                DROP TABLE IF EXISTS [dbo].[_org_secc_RegistrationCart_DiscountCode];" );

            RockMigrationHelper.DeleteEntityType( SystemGuid.EntityType.CART_DISCOUNT );
            RockMigrationHelper.DeleteEntityType( SystemGuid.EntityType.DISCOUNT_CODE );
        }

        public override void Down()
        {
            Sql( @"
                IF COL_LENGTH('dbo._org_secc_RegistrationCart_CartLineItem', 'IsTemplateDiscountLocked') IS NOT NULL
                BEGIN
                    ALTER TABLE [dbo].[_org_secc_RegistrationCart_CartLineItem] DROP CONSTRAINT [DF__org_secc_RegistrationCart_CartLineItem_IsTemplateDiscountLocked];
                    ALTER TABLE [dbo].[_org_secc_RegistrationCart_CartLineItem] DROP COLUMN [IsTemplateDiscountLocked];
                END" );
        }
    }
}
