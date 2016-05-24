namespace org.secc.OAuth.Migrations
{
    using Rock.Migrations;
    using Rock.Plugin;
    using System.Data.Entity.Migrations;
    [MigrationNumber(2, "1.4.5")]
    public partial class CreatePlugin : Rock.Plugin.Migration
    {
        public override void Up()
        {
            RockMigrationHelper.AddSite("OAuth", "OAuth server portal.", "Stark", "92A541DB-F88C-4EEA-B015-B67497C5B2A0");
            RockMigrationHelper.AddLayout("92A541DB-F88C-4EEA-B015-B67497C5B2A0", "FullWidth", "FullWidth", "", "7823E618-D769-461E-BF21-42E71A0FBAD5");
            RockMigrationHelper.AddPage("", "7823E618-D769-461E-BF21-42E71A0FBAD5", "OAuth Portal", "OAuth authentication portal", "49F0DBFB-F2D7-4494-8E39-F7DDE4206336");

            // Create the login page with a friendly route
            RockMigrationHelper.AddPage("49F0DBFB-F2D7-4494-8E39-F7DDE4206336", "7823E618-D769-461E-BF21-42E71A0FBAD5", "OAuth Login", "OAuth Login Page", "E9D51B46-09B8-4086-B499-9C613B11FE3C");
            RockMigrationHelper.AddPageRoute("E9D51B46-09B8-4086-B499-9C613B11FE3C", "OAuth/Login");

            // Add the block to the page
            RockMigrationHelper.UpdateBlockType("OAuth Login", "Prompts user for login credentials during the OAuth Login process.", "~/Plugins/org_secc/OAuth/Login.ascx", "SECC > Security", "79698895-30F1-46A9-85D4-B6C55681781F");
            RockMigrationHelper.AddBlock("E9D51B46-09B8-4086-B499-9C613B11FE3C", "", "79698895-30F1-46A9-85D4-B6C55681781F", "OAuth Login", "Main", "", "", 0, "F4CAB049-7B40-49E5-BBA6-4F69BB8CDC17");

            // Create the authorize page and route
            RockMigrationHelper.AddPage("49F0DBFB-F2D7-4494-8E39-F7DDE4206336", "7823E618-D769-461E-BF21-42E71A0FBAD5", "OAuth Authorization", "OAuth Authorization", "DA7D63B0-73A8-4321-AB3F-AC041F67E85B");
            RockMigrationHelper.AddPageRoute("DA7D63B0-73A8-4321-AB3F-AC041F67E85B", "OAuth/Authorize");

            // Add the block to the page
            RockMigrationHelper.UpdateBlockType("OAuth Authorize", "Check to make sure the user has authorized this OAuth request (or prompt for permissions).", "~/Plugins/org_secc/OAuth/Authorize.ascx", "SECC > Security", "ED369489-5844-4BE5-8ACE-18A75F9E805C");
            RockMigrationHelper.AddBlock("DA7D63B0-73A8-4321-AB3F-AC041F67E85B", "", "ED369489-5844-4BE5-8ACE-18A75F9E805C", "OAuth Login", "Main", "", "", 0, "69946856-0950-468C-9853-A887AAB95201");

            // Add the global attribute
            RockMigrationHelper.AddGlobalAttribute(Rock.SystemGuid.FieldType.KEY_VALUE_LIST, null, null, "OAuth Settings", "Settings for the OAuth server plugin.", 0, "OAuthRequireSsl^True|OAuthAuthorizePath^/OAuth/Authorize|OAuthLoginPath^/OAuth/Login|OAuthLogoutPath^/OAuth/Logout|OAuthTokenPath^/OAuth/Logout|OAuthTokenPath^/OAuth/Token|OAuthTokenLifespan^60", "9A85BBE0-ECC2-4A81-990F-D9318AE36DA9");
            Sql("INSERT INTO [dbo].[AttributeCategory] (AttributeId, CategoryId) SELECT Id, 5 from [dbo].[attribute] WHERE GUID = '9A85BBE0-ECC2-4A81-990F-D9318AE36DA9';");

        }

        public override void Down()
        {
        }
    }
}
