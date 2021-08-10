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

    [MigrationNumber( 26, "1.10.2" )]
    public partial class MobileCheckinPages : Migration
    {
        public override void Up()
        {
            RockMigrationHelper.AddSite( "Mobile Check-in", "", "SECC2019_Child_Invert", "C61A40D2-E234-4E0C-A768-A13D712CD623" );   //Site: Mobile Check-in
            RockMigrationHelper.AddLayout( "C61A40D2-E234-4E0C-A768-A13D712CD623", "Checkin", "Checkin", "", "C9CB02B0-A217-4DC4-80C4-09F6DCCB6C5C" ); // Site:Mobile Check-in
            RockMigrationHelper.AddLayout( "C61A40D2-E234-4E0C-A768-A13D712CD623", "FullWidth", "Full Width", "", "89B28C7B-4EBC-4676-9F83-E6890F75B29F" ); // Site:Mobile Check-in
            // Page: Mobile Check-in | Welcome
            RockMigrationHelper.AddPage( "", "89B28C7B-4EBC-4676-9F83-E6890F75B29F", "Mobile Check-in | Welcome", "", "BB8DA185-885B-4D50-854D-9317A7736524", "" ); // Site:Mobile Check-in
            // Page: Select Options
            RockMigrationHelper.AddPage( "BB8DA185-885B-4D50-854D-9317A7736524", "C9CB02B0-A217-4DC4-80C4-09F6DCCB6C5C", "Select Options", "", "2177896E-B34D-42F9-B171-87D8501A7A34", "" ); // Site:Mobile Check-in
            RockMigrationHelper.AddPageRoute( "BB8DA185-885B-4D50-854D-9317A7736524", "mobilecheckin" );
            RockMigrationHelper.UpdateBlockType( "Include Admin CSS", "Loads the admin css when it's required.", "~/Plugins/org_secc/Sass/AdminCss.ascx", "SECC > CMS", "FB434022-10C8-4DC7-9782-ECB25A041517" ); //Block Type: Include Admin CSS
            RockMigrationHelper.UpdateBlockType( "Mobile Check-in Start", "Start page for the mobile check-in process.", "~/Plugins/org_secc/FamilyCheckin/MobileCheckinStart.ascx", "SECC > Check-in", "5E4CEAB7-3444-4510-A458-6A981B9903BC" ); //Block Type: Mobile Check-in Start
            RockMigrationHelper.AddBlock( "BB8DA185-885B-4D50-854D-9317A7736524", "", "5E4CEAB7-3444-4510-A458-6A981B9903BC", "Mobile Check-in Start", "Main", "<style> option {     background-color:#555; } </style>", "", 0, "E642D275-C639-42FA-ABA9-59CB4CBA793F" ); //Block of Type: Mobile Check-in Start
            RockMigrationHelper.AddBlock( "2177896E-B34D-42F9-B171-87D8501A7A34", "", "AF961217-4223-4F03-9658-11DAB4B24DCD", "QuickCheckin", "Main", "", "", 0, "2E06F1B1-8C2D-473A-A9DE-8D96F65FBFDB" ); //Block of Type: QuickCheckin
            RockMigrationHelper.AddBlock( "2177896E-B34D-42F9-B171-87D8501A7A34", "", "FB434022-10C8-4DC7-9782-ECB25A041517", "Include Admin CSS", "Main", "", "", 1, "032990F5-1519-4BCA-8796-2D7160EDF636" ); //Block of Type: Include Admin CSS
            RockMigrationHelper.AddBlock( "BB8DA185-885B-4D50-854D-9317A7736524", "", "FB434022-10C8-4DC7-9782-ECB25A041517", "Include Admin CSS", "Main", "", "", 1, "0E68D156-C065-4B72-B00E-0E2E6BA0BE35" ); //Block of Type: Include Admin CSS
            RockMigrationHelper.AddBlockTypeAttribute( "5E4CEAB7-3444-4510-A458-6A981B9903BC", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Debug Mode", "DebugMode", "", "Turn on debug mode, for debugging and load testing.", 0, @"False", "0CC4170A-BAF8-418C-BB1B-D37DC1AFA305" );
            RockMigrationHelper.AddBlockTypeAttribute( "5E4CEAB7-3444-4510-A458-6A981B9903BC", "46A03F59-55D3-4ACE-ADD5-B4642225DD20", "Workflow Type", "WorkflowType", "", "The workflow type to activate for check-in", 0, @"", "86292CB0-2505-4172-883A-ED7EED3E692E" );
            RockMigrationHelper.AddBlockTypeAttribute( "5E4CEAB7-3444-4510-A458-6A981B9903BC", "9C204CD0-1233-41C5-818A-C5DA439445AA", "Workflow Activity", "WorkflowActivity", "", "The name of the workflow activity to run on selection.", 1, @"", "9118A5F3-3517-4153-B648-76E71BEA963B" );
            RockMigrationHelper.AddBlockTypeAttribute( "5E4CEAB7-3444-4510-A458-6A981B9903BC", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Home Page", "HomePage", "", "", 2, @"", "E40E3C60-DA01-4287-8CD2-4278C7C63EE9" );
            RockMigrationHelper.AddBlockTypeAttribute( "5E4CEAB7-3444-4510-A458-6A981B9903BC", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Previous Page", "PreviousPage", "", "", 3, @"", "D823C02C-D38B-43E8-AEA3-BF300C12A484" );
            RockMigrationHelper.AddBlockTypeAttribute( "5E4CEAB7-3444-4510-A458-6A981B9903BC", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Next Page", "NextPage", "", "", 4, @"", "9E545883-BD73-4C1C-88F2-DBE250709760" );
            RockMigrationHelper.AddBlockTypeAttribute( "5E4CEAB7-3444-4510-A458-6A981B9903BC", "1D0D3794-C210-48A8-8C68-3FBEC08A6BA5", "Introduction Text", "IntroductionText", "", "Text which appears as above the select campus page.<span class=\"tip tip-html\"></span>", 8, @"", "2FD5AA2C-1830-4023-A0BB-548575895A9B" );
            RockMigrationHelper.AddBlockTypeAttribute( "5E4CEAB7-3444-4510-A458-6A981B9903BC", "1D0D3794-C210-48A8-8C68-3FBEC08A6BA5", "Code Instructions", "CodeInstructions", "", "Text which appears above the qr code. <span class=\"tip tip-html\"></span>", 9, @"", "940CB4D2-EE4F-4B7F-AC96-95B2402FD91A" );
            RockMigrationHelper.AddBlockTypeAttribute( "5E4CEAB7-3444-4510-A458-6A981B9903BC", "1D0D3794-C210-48A8-8C68-3FBEC08A6BA5", "Post Check-in Instructions", "PostCheckinInstructions", "", "Text which appears immediatly after the user checks in. <span class=\"tip tip-html\"></span>", 10, @"", "98953A80-473A-4FD7-A825-25146FFA57F7" );
            RockMigrationHelper.AddBlockTypeAttribute( "5E4CEAB7-3444-4510-A458-6A981B9903BC", "1D0D3794-C210-48A8-8C68-3FBEC08A6BA5", "Not Logged In Message", "NotLoggedInMessage", "", "Message to show if the user is not logged in. <span class=\"tip tip-html\"></span>", 11, @"", "C2D6AF28-0ED4-42C6-954B-2DAD73518566" );
            RockMigrationHelper.AddBlockAttributeValue( "E642D275-C639-42FA-ABA9-59CB4CBA793F", "2FD5AA2C-1830-4023-A0BB-548575895A9B", @"<h2>Mobile Check-in</h2> Select your campus to check-in." ); // Introduction Text
            RockMigrationHelper.AddBlockAttributeValue( "E642D275-C639-42FA-ABA9-59CB4CBA793F", "940CB4D2-EE4F-4B7F-AC96-95B2402FD91A", @"<center> <h2>All Set</h2> Go to your campus and scan this at the kiosk. </center>" ); // Code Instructions
            RockMigrationHelper.AddBlockAttributeValue( "E642D275-C639-42FA-ABA9-59CB4CBA793F", "C2D6AF28-0ED4-42C6-954B-2DAD73518566", @"<center>     <h2>Not Logged In</h2>     You must be logged in to use Mobile Check-in.     <br><br> </center>" ); // Not Logged In Message
            RockMigrationHelper.AddBlockAttributeValue( "E642D275-C639-42FA-ABA9-59CB4CBA793F", "0CC4170A-BAF8-418C-BB1B-D37DC1AFA305", @"True" ); // Debug Mode
            RockMigrationHelper.AddBlockAttributeValue( "E642D275-C639-42FA-ABA9-59CB4CBA793F", "98953A80-473A-4FD7-A825-25146FFA57F7", @"<h2>Welcome</h2> If you need any help please ask one of our welcome volunteers. <br><br>" ); // Post Check-in Instructions
            RockMigrationHelper.AddBlockAttributeValue( "E642D275-C639-42FA-ABA9-59CB4CBA793F", "86292CB0-2505-4172-883A-ED7EED3E692E", @"80973ff7-e2c4-4a18-9939-216e347bdc27" ); // Workflow Type
            RockMigrationHelper.AddBlockAttributeValue( "E642D275-C639-42FA-ABA9-59CB4CBA793F", "9118A5F3-3517-4153-B648-76E71BEA963B", @"Person Search" ); // Workflow Activity
            RockMigrationHelper.AddBlockAttributeValue( "E642D275-C639-42FA-ABA9-59CB4CBA793F", "E40E3C60-DA01-4287-8CD2-4278C7C63EE9", @"bb8da185-885b-4d50-854d-9317a7736524" ); // Home Page
            RockMigrationHelper.AddBlockAttributeValue( "E642D275-C639-42FA-ABA9-59CB4CBA793F", "9E545883-BD73-4C1C-88F2-DBE250709760", @"2177896e-b34d-42f9-b171-87d8501a7a34" ); // Next Page
            RockMigrationHelper.AddBlockAttributeValue( "2E06F1B1-8C2D-473A-A9DE-8D96F65FBFDB", "107BFB22-2844-443E-B61D-ADD5BEA0C54F", @"80973ff7-e2c4-4a18-9939-216e347bdc27" ); // Workflow Type
            RockMigrationHelper.AddBlockAttributeValue( "2E06F1B1-8C2D-473A-A9DE-8D96F65FBFDB", "C0F78DCA-BE6D-4BAD-8985-35D0F7027248", @"bb8da185-885b-4d50-854d-9317a7736524" ); // Home Page
            RockMigrationHelper.AddBlockAttributeValue( "2E06F1B1-8C2D-473A-A9DE-8D96F65FBFDB", "FC3AE63A-E7D1-476A-91BD-9E934B358963", @"bb8da185-885b-4d50-854d-9317a7736524" ); // Previous Page
            RockMigrationHelper.AddBlockAttributeValue( "2E06F1B1-8C2D-473A-A9DE-8D96F65FBFDB", "C8266886-8A86-401C-9982-8F675132CFF0", @"bb8da185-885b-4d50-854d-9317a7736524" ); // Next Page
            RockMigrationHelper.AddBlockAttributeValue( "2E06F1B1-8C2D-473A-A9DE-8D96F65FBFDB", "CBE2D31C-F06B-4393-80CA-E6E4235F77E4", @"Mobile Save" ); // Complete Checkin Activity Name
            RockMigrationHelper.AddBlockAttributeValue( "2E06F1B1-8C2D-473A-A9DE-8D96F65FBFDB", "1801E1BD-A50C-47B2-89BC-07D602D8EE9A", @"True" ); // Is Mobile Checkin
            RockMigrationHelper.AddBlockAttributeValue( "2E06F1B1-8C2D-473A-A9DE-8D96F65FBFDB", "5843B703-F788-47DA-8223-91A52840F9E6", @"<h2>We are sorry</h2><h3>There are no members of your family who are able to check-in at this kiosk right now.<br><small>Check-in may become available for your family members at a future time today.<br />If you need assistance or believe this is in error, please see a check-in volunteer at your campus for help.</small></h3>" ); // No Eligible Family Members
            RockMigrationHelper.AddBlockAttributeValue( "2E06F1B1-8C2D-473A-A9DE-8D96F65FBFDB", "6D71A241-555B-402B-9AF1-AD6A1ECBBAB6", @"<h2>Welcome.</h2><h3>We are saving your attendance...</h3>" ); // Completion HTML
        }

        public override void Down()
        {

        }
    }
}
