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
    /// Adds CartLineItem.TemplateDiscountCode / TemplateDiscountAmount / TemplateDiscountPercentage:
    /// the registration template's own code (the financial-assistance code) that the discount
    /// code workflow applied to a line at add-to-cart.
    /// </summary>
    [MigrationNumber( 4, "1.16.0" )]
    public class AddCartLineItemTemplateDiscount : Migration
    {
        public override void Up()
        {
            Sql( @"
                IF COL_LENGTH('dbo._org_secc_RegistrationCart_CartLineItem', 'TemplateDiscountCode') IS NULL
                    ALTER TABLE [dbo].[_org_secc_RegistrationCart_CartLineItem] ADD [TemplateDiscountCode] [nvarchar](100) NULL;
                IF COL_LENGTH('dbo._org_secc_RegistrationCart_CartLineItem', 'TemplateDiscountAmount') IS NULL
                    ALTER TABLE [dbo].[_org_secc_RegistrationCart_CartLineItem] ADD [TemplateDiscountAmount] [decimal](18, 2) NOT NULL CONSTRAINT [DF__org_secc_RegistrationCart_CartLineItem_TemplateDiscountAmount] DEFAULT (0);
                IF COL_LENGTH('dbo._org_secc_RegistrationCart_CartLineItem', 'TemplateDiscountPercentage') IS NULL
                    ALTER TABLE [dbo].[_org_secc_RegistrationCart_CartLineItem] ADD [TemplateDiscountPercentage] [decimal](18, 2) NOT NULL CONSTRAINT [DF__org_secc_RegistrationCart_CartLineItem_TemplateDiscountPercentage] DEFAULT (0);" );
        }

        public override void Down()
        {
            Sql( @"
                IF COL_LENGTH('dbo._org_secc_RegistrationCart_CartLineItem', 'TemplateDiscountPercentage') IS NOT NULL
                BEGIN
                    ALTER TABLE [dbo].[_org_secc_RegistrationCart_CartLineItem] DROP CONSTRAINT [DF__org_secc_RegistrationCart_CartLineItem_TemplateDiscountPercentage];
                    ALTER TABLE [dbo].[_org_secc_RegistrationCart_CartLineItem] DROP COLUMN [TemplateDiscountPercentage];
                END
                IF COL_LENGTH('dbo._org_secc_RegistrationCart_CartLineItem', 'TemplateDiscountAmount') IS NOT NULL
                BEGIN
                    ALTER TABLE [dbo].[_org_secc_RegistrationCart_CartLineItem] DROP CONSTRAINT [DF__org_secc_RegistrationCart_CartLineItem_TemplateDiscountAmount];
                    ALTER TABLE [dbo].[_org_secc_RegistrationCart_CartLineItem] DROP COLUMN [TemplateDiscountAmount];
                END
                IF COL_LENGTH('dbo._org_secc_RegistrationCart_CartLineItem', 'TemplateDiscountCode') IS NOT NULL
                    ALTER TABLE [dbo].[_org_secc_RegistrationCart_CartLineItem] DROP COLUMN [TemplateDiscountCode];" );
        }
    }
}
