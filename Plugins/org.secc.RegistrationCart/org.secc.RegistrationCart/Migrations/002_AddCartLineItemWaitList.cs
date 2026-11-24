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
    /// Adds CartLineItem.IsWaitList: the line is for a full camp whose template allows a wait
    /// list, so it costs nothing now and becomes a wait-listed registrant at checkout.
    /// </summary>
    [MigrationNumber( 2, "1.16.0" )]
    public class AddCartLineItemWaitList : Migration
    {
        public override void Up()
        {
            Sql( @"
                IF COL_LENGTH('dbo._org_secc_RegistrationCart_CartLineItem', 'IsWaitList') IS NULL
                BEGIN
                    ALTER TABLE [dbo].[_org_secc_RegistrationCart_CartLineItem]
                        ADD [IsWaitList] [bit] NOT NULL CONSTRAINT [DF__org_secc_RegistrationCart_CartLineItem_IsWaitList] DEFAULT 0;
                END" );
        }

        public override void Down()
        {
            Sql( @"
                IF COL_LENGTH('dbo._org_secc_RegistrationCart_CartLineItem', 'IsWaitList') IS NOT NULL
                BEGIN
                    ALTER TABLE [dbo].[_org_secc_RegistrationCart_CartLineItem] DROP CONSTRAINT [DF__org_secc_RegistrationCart_CartLineItem_IsWaitList];
                    ALTER TABLE [dbo].[_org_secc_RegistrationCart_CartLineItem] DROP COLUMN [IsWaitList];
                END" );
        }
    }
}
