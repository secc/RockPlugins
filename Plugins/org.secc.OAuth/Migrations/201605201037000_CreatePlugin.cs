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
            // Add the global attribute
            RockMigrationHelper.AddGlobalAttribute(Rock.SystemGuid.FieldType.KEY_VALUE_LIST, null, null, "OAuth Settings", "Settings for the OAuth server plugin.", 0, "OAuthRequireSsl^True|OAuthAuthorizePath^/OAuth/Authorize|OAuthLoginPath^/OAuth/Login|OAuthLogoutPath^/OAuth/Logout|OAuthTokenPath^/OAuth/Logout|OAuthTokenPath^/OAuth/Token|OAuthTokenLifespan^60", "9A85BBE0-ECC2-4A81-990F-D9318AE36DA9");
            Sql("INSERT INTO [dbo].[AttributeCategory] (AttributeId, CategoryId) SELECT Id, 5 from [dbo].[attribute] WHERE GUID = '9A85BBE0-ECC2-4A81-990F-D9318AE36DA9';");

            // Now create the site.  Thanks Mark!
            RockMigrationHelper.AddSite("OAuth", "OAuth server portal.", "Stark", "92A541DB-F88C-4EEA-B015-B67497C5B2A0");   //Site: OAuth
            RockMigrationHelper.AddLayout("92A541DB-F88C-4EEA-B015-B67497C5B2A0", "FullWidth", "FullWidth", "", "7823E618-D769-461E-BF21-42E71A0FBAD5"); // Site:OAuth
            // Page: OAuth Portal
            RockMigrationHelper.AddPage("", "7823E618-D769-461E-BF21-42E71A0FBAD5", "OAuth Portal", "OAuth authentication portal", "49F0DBFB-F2D7-4494-8E39-F7DDE4206336", ""); // Site:OAuth
            // Page: OAuth Login
            RockMigrationHelper.AddPage("49F0DBFB-F2D7-4494-8E39-F7DDE4206336", "7823E618-D769-461E-BF21-42E71A0FBAD5", "OAuth Login", "OAuth Login Page", "E9D51B46-09B8-4086-B499-9C613B11FE3C", ""); // Site:OAuth
            // Page: OAuth Authorization
            RockMigrationHelper.AddPage("49F0DBFB-F2D7-4494-8E39-F7DDE4206336", "7823E618-D769-461E-BF21-42E71A0FBAD5", "OAuth Authorization", "OAuth Authorization", "DA7D63B0-73A8-4321-AB3F-AC041F67E85B", ""); // Site:OAuth
            // Page: OAuth Logout
            RockMigrationHelper.AddPage("49F0DBFB-F2D7-4494-8E39-F7DDE4206336", "7823E618-D769-461E-BF21-42E71A0FBAD5", "OAuth Logout", "", "82903D44-A105-4B40-BEB3-2D7F55536F4A", ""); // Site:OAuth
            RockMigrationHelper.AddPageRoute("E9D51B46-09B8-4086-B499-9C613B11FE3C", "OAuth/Login");
            RockMigrationHelper.AddPageRoute("DA7D63B0-73A8-4321-AB3F-AC041F67E85B", "OAuth/Authorize");
            RockMigrationHelper.AddPageRoute("DA7D63B0-73A8-4321-AB3F-AC041F67E85B", "OAuth/Token");
            RockMigrationHelper.AddPageRoute("DA7D63B0-73A8-4321-AB3F-AC041F67E85B", "OAuth/Logout");
            RockMigrationHelper.UpdateBlockType("HTML Content", "Adds an editable HTML fragment to the page.", "~/Blocks/Cms/HtmlContentDetail.ascx", "CMS", "19B61D65-37E3-459F-A44F-DEF0089118A3"); //Block Type: HTML Content
            RockMigrationHelper.UpdateBlockType("OAuth Authorize", "Check to make sure the user has authorized this OAuth request (or prompt for permissions).", "~/Plugins/org_secc/OAuth/Authorize.ascx", "SECC > Security", "ED369489-5844-4BE5-8ACE-18A75F9E805C"); //Block Type: OAuth Authorize
            RockMigrationHelper.UpdateBlockType("OAuth Login", "Prompts user for login credentials during the OAuth Login process.", "~/Plugins/org_secc/OAuth/Login.ascx", "SECC > Security", "79698895-30F1-46A9-85D4-B6C55681781F"); //Block Type: OAuth Login
            RockMigrationHelper.UpdateBlockType("OAuth Logout", "Logs a user out of Rock/OAuth.", "~/Plugins/org_secc/OAuth/Logout.ascx", "SECC > Security", "C7F3107B-3FAD-459E-9B93-6F71A42A478C"); //Block Type: OAuth Logout
            RockMigrationHelper.AddBlock("E9D51B46-09B8-4086-B499-9C613B11FE3C", "", "79698895-30F1-46A9-85D4-B6C55681781F", "OAuth Login", "Main", "", "", 0, "F4CAB049-7B40-49E5-BBA6-4F69BB8CDC17"); //Block of Type: OAuth Login
            RockMigrationHelper.AddBlock("DA7D63B0-73A8-4321-AB3F-AC041F67E85B", "", "ED369489-5844-4BE5-8ACE-18A75F9E805C", "OAuth Login", "Main", "", "", 0, "69946856-0950-468C-9853-A887AAB95201"); //Block of Type: OAuth Authorize
            RockMigrationHelper.AddBlock("82903D44-A105-4B40-BEB3-2D7F55536F4A", "", "C7F3107B-3FAD-459E-9B93-6F71A42A478C", "OAuth Logout", "Main", "", "", 0, "F618AE9E-71E2-4831-BB72-291E90773C0B"); //Block of Type: OAuth Logout
            RockMigrationHelper.AddBlock("82903D44-A105-4B40-BEB3-2D7F55536F4A", "", "19B61D65-37E3-459F-A44F-DEF0089118A3", "Logout Content", "Main", "", "", 1, "0D6ADD3D-11BB-48F3-9641-EF122B0C3879"); //Block of Type: HTML Content
            RockMigrationHelper.AddBlockTypeAttribute("19B61D65-37E3-459F-A44F-DEF0089118A3", "3549BAB6-FE1B-4333-AFC4-C5ACA01BB8EB", "Entity Type", "ContextEntityType", "", "The type of entity that will provide context for this block", 0, @"", "6783D47D-92F9-4F48-93C0-16111D675A0F");
            RockMigrationHelper.AddBlockTypeAttribute("19B61D65-37E3-459F-A44F-DEF0089118A3", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Use Code Editor", "UseCodeEditor", "", "Use the code editor instead of the WYSIWYG editor", 0, @"True", "0673E015-F8DD-4A52-B380-C758011331B2");
            RockMigrationHelper.AddBlockTypeAttribute("ED369489-5844-4BE5-8ACE-18A75F9E805C", "9C204CD0-1233-41C5-818A-C5DA439445AA", "OAuth Config Attribute Key", "OAuthConfigAttributeKey", "", "The OAuth Configuration Attribute\"s Key", 0, @"OAuthSettings", "ED073B5F-BFBA-4DA2-9EBB-5833D15A1A50");
            RockMigrationHelper.AddBlockTypeAttribute("79698895-30F1-46A9-85D4-B6C55681781F", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "New Account Page", "NewAccountPage", "", "Page to navigate to when user selects \"Create New Account\" (if blank will use \"NewAccountPage\" page route)", 0, @"", "1D6B6904-85F1-4270-90AC-B2B45A46283A");
            RockMigrationHelper.AddBlockTypeAttribute("C7F3107B-3FAD-459E-9B93-6F71A42A478C", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Enabled", "Enabled", "", "Enabled or disabled.  This is helpful for editing the page!", 0, @"True", "96247DCE-CE08-4E03-871F-9829BD652016");
            RockMigrationHelper.AddBlockTypeAttribute("79698895-30F1-46A9-85D4-B6C55681781F", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Help Page", "HelpPage", "", "Page to navigate to when user selects \"Help\" option (if blank will use \"ForgotUserName\" page route)", 1, @"", "6208B5DE-42FF-46E9-B295-E1AA1C18F358");
            RockMigrationHelper.AddBlockTypeAttribute("19B61D65-37E3-459F-A44F-DEF0089118A3", "9C204CD0-1233-41C5-818A-C5DA439445AA", "Document Root Folder", "DocumentRootFolder", "", "The folder to use as the root when browsing or uploading documents.", 1, @"~/Content", "3BDB8AED-32C5-4879-B1CB-8FC7C8336534");
            RockMigrationHelper.AddBlockTypeAttribute("19B61D65-37E3-459F-A44F-DEF0089118A3", "9C204CD0-1233-41C5-818A-C5DA439445AA", "Image Root Folder", "ImageRootFolder", "", "The folder to use as the root when browsing or uploading images.", 2, @"~/Content", "26F3AFC6-C05B-44A4-8593-AFE1D9969B0E");
            RockMigrationHelper.AddBlockTypeAttribute("79698895-30F1-46A9-85D4-B6C55681781F", "1D0D3794-C210-48A8-8C68-3FBEC08A6BA5", "Confirm Caption", "ConfirmCaption", "", "The text (HTML) to display when a user\"s account needs to be confirmed.", 2, @"
Thank-you for logging in, however, we need to confirm the email associated with this account belongs to you. We've sent you an email that contains a link for confirming.  Please click the link in your email to continue.
", "9E0AB758-7710-469C-B823-0FB88237622D");
            RockMigrationHelper.AddBlockTypeAttribute("79698895-30F1-46A9-85D4-B6C55681781F", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Confirmation Page", "ConfirmationPage", "", "Page for user to confirm their account (if blank will use \"ConfirmAccount\" page route)", 3, @"", "B3C062BC-80A1-48D3-B78E-97EA821CD469");
            RockMigrationHelper.AddBlockTypeAttribute("19B61D65-37E3-459F-A44F-DEF0089118A3", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "User Specific Folders", "UserSpecificFolders", "", "Should the root folders be specific to current user?", 3, @"False", "9D3E4ED9-1BEF-4547-B6B0-CE29FE3835EE");
            RockMigrationHelper.AddBlockTypeAttribute("19B61D65-37E3-459F-A44F-DEF0089118A3", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Cache Duration", "CacheDuration", "", "Number of seconds to cache the content.", 4, @"3600", "4DFDB295-6D0F-40A1-BEF9-7B70C56F66C4");
            RockMigrationHelper.AddBlockTypeAttribute("79698895-30F1-46A9-85D4-B6C55681781F", "08F3003B-F3E2-41EC-BDF1-A2B7AC2908CF", "Confirm Account Template", "ConfirmAccountTemplate", "", "Confirm Account Email Template", 4, @"17aaceef-15ca-4c30-9a3a-11e6cf7e6411", "31D61FE4-44E9-4534-AF73-C449E1D52B2E");
            RockMigrationHelper.AddBlockTypeAttribute("79698895-30F1-46A9-85D4-B6C55681781F", "1D0D3794-C210-48A8-8C68-3FBEC08A6BA5", "Locked Out Caption", "LockedOutCaption", "", "The text (HTML) to display when a user\"s account has been locked.", 5, @"
Sorry, your account has been locked.  Please contact our office at {{ 'Global' | Attribute:'OrganizationPhone' }} or email {{ 'Global' | Attribute:'OrganizationEmail' }} to resolve this.  Thank-you. 
", "319313C7-9174-4A67-A702-7F8A31F6C541");
            RockMigrationHelper.AddBlockTypeAttribute("19B61D65-37E3-459F-A44F-DEF0089118A3", "9C204CD0-1233-41C5-818A-C5DA439445AA", "Context Parameter", "ContextParameter", "", "Query string parameter to use for \"personalizing\" content based on unique values.", 5, @"", "3FFC512D-A576-4289-B648-905FD7A64ABB");
            RockMigrationHelper.AddBlockTypeAttribute("19B61D65-37E3-459F-A44F-DEF0089118A3", "9C204CD0-1233-41C5-818A-C5DA439445AA", "Context Name", "ContextName", "", "Name to use to further \"personalize\" content.  Blocks with the same name, and referenced with the same context parameter will share html values.", 6, @"", "466993F7-D838-447A-97E7-8BBDA6A57289");
            RockMigrationHelper.AddBlockTypeAttribute("79698895-30F1-46A9-85D4-B6C55681781F", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Hide New Account Option", "HideNewAccount", "", "Should \"New Account\" option be hidden?  For site\"s that require user to be in a role (Internal Rock Site for example), users shouldn\"t be able to create their own account.", 6, @"False", "184ADC67-2348-4AE1-ADE9-8A2AF9AC0460");
            RockMigrationHelper.AddBlockTypeAttribute("79698895-30F1-46A9-85D4-B6C55681781F", "9C204CD0-1233-41C5-818A-C5DA439445AA", "New Account Text", "NewAccountButtonText", "", "The text to show on the New Account button.", 7, @"Register", "C1F1C06E-CAE1-4442-80BB-CCE8651949FA");
            RockMigrationHelper.AddBlockTypeAttribute("19B61D65-37E3-459F-A44F-DEF0089118A3", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Enable Versioning", "SupportVersions", "", "If checked, previous versions of the content will be preserved. Versioning is required if you want to require approval.", 7, @"False", "7C1CE199-86CF-4EAE-8AB3-848416A72C58");
            RockMigrationHelper.AddBlockTypeAttribute("19B61D65-37E3-459F-A44F-DEF0089118A3", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Require Approval", "RequireApproval", "", "Require that content be approved?", 8, @"False", "EC2B701B-4C1D-4F3F-9C77-A73C75D7FF7A");
            RockMigrationHelper.AddBlockTypeAttribute("19B61D65-37E3-459F-A44F-DEF0089118A3", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Enable Debug", "EnableDebug", "", "Show lava merge fields.", 9, @"False", "48FF43A9-8E12-4768-80A9-88FBB81F11D8");
            RockMigrationHelper.AddBlockTypeAttribute("79698895-30F1-46A9-85D4-B6C55681781F", "1D0D3794-C210-48A8-8C68-3FBEC08A6BA5", "Prompt Message", "PromptMessage", "", "Optional text (HTML) to display above username and password fields.", 9, @"", "4FA058BF-A24D-460B-BC9A-E6FAB1C8A653");
            RockMigrationHelper.AddBlockAttributeValue("F618AE9E-71E2-4831-BB72-291E90773C0B", "96247DCE-CE08-4E03-871F-9829BD652016", @"True"); // Enabled
            RockMigrationHelper.UpdateHtmlContentBlock("0D6ADD3D-11BB-48F3-9641-EF122B0C3879", @"You have successfully logged out!", "CC8CEB43-A860-46B5-8160-9F0CF5641A6C"); //HTML Content

        }

        public override void Down()
        {
            RockMigrationHelper.DeleteAttribute("9A85BBE0-ECC2-4A81-990F-D9318AE36DA9");
            RockMigrationHelper.DeleteAttribute("96247DCE-CE08-4E03-871F-9829BD652016");
            RockMigrationHelper.DeleteAttribute("ED073B5F-BFBA-4DA2-9EBB-5833D15A1A50");
            RockMigrationHelper.DeleteAttribute("C1F1C06E-CAE1-4442-80BB-CCE8651949FA");
            RockMigrationHelper.DeleteAttribute("184ADC67-2348-4AE1-ADE9-8A2AF9AC0460");
            RockMigrationHelper.DeleteAttribute("319313C7-9174-4A67-A702-7F8A31F6C541");
            RockMigrationHelper.DeleteAttribute("B3C062BC-80A1-48D3-B78E-97EA821CD469");
            RockMigrationHelper.DeleteAttribute("9E0AB758-7710-469C-B823-0FB88237622D");
            RockMigrationHelper.DeleteAttribute("4FA058BF-A24D-460B-BC9A-E6FAB1C8A653");
            RockMigrationHelper.DeleteAttribute("1D6B6904-85F1-4270-90AC-B2B45A46283A");
            RockMigrationHelper.DeleteAttribute("6208B5DE-42FF-46E9-B295-E1AA1C18F358");
            RockMigrationHelper.DeleteAttribute("31D61FE4-44E9-4534-AF73-C449E1D52B2E");
            RockMigrationHelper.DeleteAttribute("48FF43A9-8E12-4768-80A9-88FBB81F11D8");
            RockMigrationHelper.DeleteAttribute("6783D47D-92F9-4F48-93C0-16111D675A0F");
            RockMigrationHelper.DeleteAttribute("3BDB8AED-32C5-4879-B1CB-8FC7C8336534");
            RockMigrationHelper.DeleteAttribute("9D3E4ED9-1BEF-4547-B6B0-CE29FE3835EE");
            RockMigrationHelper.DeleteAttribute("26F3AFC6-C05B-44A4-8593-AFE1D9969B0E");
            RockMigrationHelper.DeleteAttribute("0673E015-F8DD-4A52-B380-C758011331B2");
            RockMigrationHelper.DeleteAttribute("466993F7-D838-447A-97E7-8BBDA6A57289");
            RockMigrationHelper.DeleteAttribute("3FFC512D-A576-4289-B648-905FD7A64ABB");
            RockMigrationHelper.DeleteAttribute("7C1CE199-86CF-4EAE-8AB3-848416A72C58");
            RockMigrationHelper.DeleteAttribute("EC2B701B-4C1D-4F3F-9C77-A73C75D7FF7A");
            RockMigrationHelper.DeleteAttribute("4DFDB295-6D0F-40A1-BEF9-7B70C56F66C4");
            RockMigrationHelper.DeleteBlock("0D6ADD3D-11BB-48F3-9641-EF122B0C3879");
            RockMigrationHelper.DeleteBlock("F618AE9E-71E2-4831-BB72-291E90773C0B");
            RockMigrationHelper.DeleteBlock("69946856-0950-468C-9853-A887AAB95201");
            RockMigrationHelper.DeleteBlock("F4CAB049-7B40-49E5-BBA6-4F69BB8CDC17");
            RockMigrationHelper.DeletePage("49F0DBFB-F2D7-4494-8E39-F7DDE4206336"); //  Page: OAuth Portal
            RockMigrationHelper.DeletePage("E9D51B46-09B8-4086-B499-9C613B11FE3C"); //  Page: OAuth Login
            RockMigrationHelper.DeletePage("DA7D63B0-73A8-4321-AB3F-AC041F67E85B"); //  Page: OAuth Authorization
            RockMigrationHelper.DeletePage("82903D44-A105-4B40-BEB3-2D7F55536F4A"); //  Page: OAuth Logout
            RockMigrationHelper.DeleteSite("92A541DB-F88C-4EEA-B015-B67497C5B2A0");
        }
    }
}
