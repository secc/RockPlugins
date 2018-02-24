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
using Rock.Plugin;
using Rock;

namespace org.secc.OAuth.Migrations
{

    [MigrationNumber(3, "1.4.5")]
    class CreateAdminPage : Rock.Plugin.Migration
    {
        public override void Up()
        {            
            // Page: OAuth Configuration
            RockMigrationHelper.AddPage("91CCB1C9-5F9F-44F5-8BE2-9EC3A3CFD46F", "D65F783D-87A9-4CC9-8110-E83466A0EADB", "OAuth Configuration", "", "C2B15044-B63C-43EB-BADC-9730890F4621", "fa fa-cloud"); // Site:Rock RMS
            RockMigrationHelper.UpdateBlockType("OAuth Configuration", "Configuration settings for OAuth.", "~/Plugins/org_secc/OAuth/Configuration.ascx", "SECC > Security", "34680C7A-84DF-413A-A474-3495C65372DA");
            RockMigrationHelper.AddBlock("C2B15044-B63C-43EB-BADC-9730890F4621", "", "34680C7A-84DF-413A-A474-3495C65372DA", "OAuth Configuration", "Main", "", "", 0, "3339F274-F61D-4CAA-AA8D-EC84E3290FC7");

            RockMigrationHelper.AddBlockTypeAttribute("34680C7A-84DF-413A-A474-3495C65372DA", "9C204CD0-1233-41C5-818A-C5DA439445AA", "OAuth Config Attribute Key", "OAuthConfigAttributeKey", "", "The OAuth Configuration Attribute\"s Key", 0, @"OAuthSettings", "3CE5DD1F-C69F-483D-B6BF-57FE0ED4C8AA");

            RockMigrationHelper.AddBlockTypeAttribute("34680C7A-84DF-413A-A474-3495C65372DA", "BB7AB90F-4DE9-4804-A852-F5593A35C8A0", "OAuth Site", "OAuthSite", "", "The OAuth Porta/Site", 0, @"1", "5ED9C998-F7D0-4057-93B6-F4B4FE0837DA");

            RockMigrationHelper.AddBlockAttributeValue("3339F274-F61D-4CAA-AA8D-EC84E3290FC7", "3CE5DD1F-C69F-483D-B6BF-57FE0ED4C8AA", @"OAuthSettings"); // OAuth Config Attribute Key

            // Get the site so we can get its ID
            Rock.Model.SiteService siteService = new Rock.Model.SiteService(new Rock.Data.RockContext());
            Rock.Model.Site site = siteService.Get("92A541DB-F88C-4EEA-B015-B67497C5B2A0".AsGuid());

            RockMigrationHelper.AddBlockAttributeValue("3339F274-F61D-4CAA-AA8D-EC84E3290FC7", "5ED9C998-F7D0-4057-93B6-F4B4FE0837DA", site.Id.ToString()); // OAuth Site

        }
        public override void Down()
        {
            RockMigrationHelper.DeleteAttribute("5ED9C998-F7D0-4057-93B6-F4B4FE0837DA");
            RockMigrationHelper.DeleteAttribute("3CE5DD1F-C69F-483D-B6BF-57FE0ED4C8AA");
            RockMigrationHelper.DeleteBlock("3339F274-F61D-4CAA-AA8D-EC84E3290FC7");
            RockMigrationHelper.DeletePage("C2B15044-B63C-43EB-BADC-9730890F4621"); //  Page: OAuth Configuration
        }

    }
}
