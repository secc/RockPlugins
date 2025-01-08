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

    [MigrationNumber(31,"1.12.9")]
    internal class MedicalConsentKioskTypeAttributes : Migration
    {
        public override void Up()
        {
            /* Show Medical Consent */
            RockMigrationHelper.UpdateEntityAttribute( "org.secc.FamilyCheckin.Model.KioskType",
                Rock.SystemGuid.FieldType.BOOLEAN,
                "", "",
                "Show Medical Consent", 
                "Should the medical consent message be shown as part of check in.", 
                0, 
                "False", 
                "ce48a6d4-c087-4a5c-84db-668a236c045c", 
                "ShowMedicalConsent" );

            RockMigrationHelper.UpdateAttributeQualifier( "ce48a6d4-c087-4a5c-84db-668a236c045c",
                "BooleanControlType",
                "0",
                "01e5280f-2f77-46d1-9832-eefe6a7f3a18" );

            RockMigrationHelper.UpdateAttributeQualifier( "ce48a6d4-c087-4a5c-84db-668a236c045c",
                "falsetext",
                "No",
                "f927b1f7-74a2-4793-8788-9da9961e49c1" );

            RockMigrationHelper.UpdateAttributeQualifier( "ce48a6d4-c087-4a5c-84db-668a236c045c",
                "truetext",
                "Yes",
                "67bc9c81-c50b-4da4-ad2b-addb2705f662" );

            Sql( @"UPDATE dbo.[AttributeQualifier] 
                 SET [IsSystem] = 0 
                 WHERE [Guid] in ('01e5280f-2f77-46d1-9832-eefe6a7f3a18','f927b1f7-74a2-4793-8788-9da9961e49c1','67bc9c81-c50b-4da4-ad2b-addb2705f662')
            " );

            /* Skips */
            RockMigrationHelper.UpdateEntityAttribute( "org.secc.FamilyCheckin.Model.KioskType",
                Rock.SystemGuid.FieldType.INTEGER,
                "", "",
                "Medical Consent Skips Allowed",
                "The number of skips that are allowed for the medical consent page.  -1 means unlimited. Default Value = -1",
                1,
                "-1",
                "85a96bff-ed13-4602-9673-174623d82ce3",
                "MedicalConsentSkipsAllowed" );

        }

        public override void Down()
        {
            /* INTENTIONALLY LEFT BLANK */
        }
    }
}
