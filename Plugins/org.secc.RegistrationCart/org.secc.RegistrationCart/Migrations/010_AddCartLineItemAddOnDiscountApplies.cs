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
    /// Adds CartLineItemAddOn.DiscountApplies: the add-on's Rock fee "Discount Applies" setting,
    /// snapshotted, so a template discount (the line's own code, e.g. financial assistance)
    /// applies only to the base price and the fees that allow it, as in Rock. Existing add-ons
    /// take their fee's current setting.
    /// </summary>
    [MigrationNumber( 10, "1.16.0" )]
    public class AddCartLineItemAddOnDiscountApplies : Migration
    {
        public override void Up()
        {
            Sql( @"
                IF COL_LENGTH('dbo._org_secc_RegistrationCart_CartLineItemAddOn', 'DiscountApplies') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[_org_secc_RegistrationCart_CartLineItemAddOn]
                        ADD [DiscountApplies] [bit] NOT NULL CONSTRAINT [DF__org_secc_RegistrationCart_CartLineItemAddOn_DiscountApplies] DEFAULT 0;
                END" );

            Sql( @"
                UPDATE a SET a.[DiscountApplies] = f.[DiscountApplies]
                FROM [dbo].[_org_secc_RegistrationCart_CartLineItemAddOn] a
                JOIN [dbo].[RegistrationTemplateFeeItem] fi ON fi.[Id] = a.[RegistrationTemplateFeeItemId]
                JOIN [dbo].[RegistrationTemplateFee] f ON f.[Id] = fi.[RegistrationTemplateFeeId];" );
        }

        public override void Down()
        {
            Sql( @"
                IF COL_LENGTH('dbo._org_secc_RegistrationCart_CartLineItemAddOn', 'DiscountApplies') IS NOT NULL
                BEGIN
                    ALTER TABLE [dbo].[_org_secc_RegistrationCart_CartLineItemAddOn] DROP CONSTRAINT [DF__org_secc_RegistrationCart_CartLineItemAddOn_DiscountApplies];
                    ALTER TABLE [dbo].[_org_secc_RegistrationCart_CartLineItemAddOn] DROP COLUMN [DiscountApplies];
                END" );
        }
    }
}
