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
    /// Adds CartLineItem.FieldValuesJson (the registrant's answers to the template's forms,
    /// keyed by form field Guid) and CartLineItem.SignatureData (the signed inline signature
    /// document, as Rock's electronic signature returns it).
    /// </summary>
    [MigrationNumber( 3, "1.16.0" )]
    public class AddCartLineItemFormAnswers : Migration
    {
        public override void Up()
        {
            Sql( @"
                IF COL_LENGTH('dbo._org_secc_RegistrationCart_CartLineItem', 'FieldValuesJson') IS NULL
                    ALTER TABLE [dbo].[_org_secc_RegistrationCart_CartLineItem] ADD [FieldValuesJson] [nvarchar](max) NULL;
                IF COL_LENGTH('dbo._org_secc_RegistrationCart_CartLineItem', 'SignatureData') IS NULL
                    ALTER TABLE [dbo].[_org_secc_RegistrationCart_CartLineItem] ADD [SignatureData] [nvarchar](max) NULL;" );
        }

        public override void Down()
        {
            Sql( @"
                IF COL_LENGTH('dbo._org_secc_RegistrationCart_CartLineItem', 'SignatureData') IS NOT NULL
                    ALTER TABLE [dbo].[_org_secc_RegistrationCart_CartLineItem] DROP COLUMN [SignatureData];
                IF COL_LENGTH('dbo._org_secc_RegistrationCart_CartLineItem', 'FieldValuesJson') IS NOT NULL
                    ALTER TABLE [dbo].[_org_secc_RegistrationCart_CartLineItem] DROP COLUMN [FieldValuesJson];" );
        }
    }
}
