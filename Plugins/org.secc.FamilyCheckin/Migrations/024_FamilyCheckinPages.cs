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

    [MigrationNumber( 24, "1.10.2" )]
    public partial class FamilyCheckinPages : Migration
    {
        public override void Up()
        {
            RockMigrationHelper.AddSite( "Family Checkin", "Family check in for quick and painless checking in.", "SEKids2020", "FB2A03C3-D5E9-4DBA-A56F-90311DD8BF13" );   //Site: Family Checkin
            RockMigrationHelper.AddLayout( "FB2A03C3-D5E9-4DBA-A56F-90311DD8BF13", "Checkin", "Checkin", "", "8A87FF45-C694-43B5-9494-0B2D6A71FFA1" ); // Site:Family Checkin
            RockMigrationHelper.AddLayout( "FB2A03C3-D5E9-4DBA-A56F-90311DD8BF13", "FullWidth", "Full Width", "", "F497DC5C-877E-4D7C-A984-D58B636F63B5" ); // Site:Family Checkin
            // Page: Family Checkin Home Page
            RockMigrationHelper.AddPage( "", "F497DC5C-877E-4D7C-A984-D58B636F63B5", "Family Checkin Home Page", "", "4159825D-B2CE-4BC1-A5DB-917020059E89", "" ); // Site:Family Checkin
            // Page: Admin
            RockMigrationHelper.AddPage( "4159825D-B2CE-4BC1-A5DB-917020059E89", "8A87FF45-C694-43B5-9494-0B2D6A71FFA1", "Admin", "", "F2F82210-0149-4F67-AD28-B618F3158E95", "" ); // Site:Family Checkin
            // Page: QuickSearch
            RockMigrationHelper.AddPage( "4159825D-B2CE-4BC1-A5DB-917020059E89", "8A87FF45-C694-43B5-9494-0B2D6A71FFA1", "QuickSearch", "", "ABF839C7-8A7B-4F51-9D33-63AA841EAD38", "" ); // Site:Family Checkin
            // Page: QuickCheckin  
            RockMigrationHelper.AddPage( "4159825D-B2CE-4BC1-A5DB-917020059E89", "8A87FF45-C694-43B5-9494-0B2D6A71FFA1", "QuickCheckin", "", "71959598-BE24-4A51-9DB6-CC34993B523D", "" ); // Site:Family Checkin
            // Page: Super Search Lite 
            RockMigrationHelper.AddPage( "4159825D-B2CE-4BC1-A5DB-917020059E89", "F497DC5C-877E-4D7C-A984-D58B636F63B5", "Super Search Lite", "", "A9E89AD5-AEB1-4F3F-8DF4-525EEB077561", "" ); // Site:Family Checkin
            // Page: Super Checkin Lite 
            RockMigrationHelper.AddPage( "4159825D-B2CE-4BC1-A5DB-917020059E89", "F497DC5C-877E-4D7C-A984-D58B636F63B5", "Super Checkin Lite", "", "C539F927-06A8-40F7-A262-4166D168E8D6", "" ); // Site:Family Checkin
            // Page: Mobile Check-ins 
            RockMigrationHelper.AddPage( "4159825D-B2CE-4BC1-A5DB-917020059E89", "F497DC5C-877E-4D7C-A984-D58B636F63B5", "Mobile Check-ins", "", "47C8C26F-353D-45A8-8F16-7C09FA606AD3", "" ); // Site:Family Checkin
            // Page: Scanner Config   
            RockMigrationHelper.AddPage( "4159825D-B2CE-4BC1-A5DB-917020059E89", "F497DC5C-877E-4D7C-A984-D58B636F63B5", "Scanner Config", "", "908A0960-DF24-42DD-941E-64F8DCEE7EFC", "" ); // Site:Family Checkin
            RockMigrationHelper.AddOrUpdatePageRoute( "F2F82210-0149-4F67-AD28-B618F3158E95", "familycheckin" );
            RockMigrationHelper.AddOrUpdatePageRoute( "A9E89AD5-AEB1-4F3F-8DF4-525EEB077561", "supercheckinlite" );
            RockMigrationHelper.UpdateBlockType( "QuickSearch", "QuickSearch block for helping parents find their family quickly.", "~/Plugins/org_secc/FamilyCheckin/QuickSearch.ascx", "SECC > Check-in", "315A175F-C682-4810-9F33-1BDB93904A4E" ); //Block Type: QuickSearch
            RockMigrationHelper.UpdateBlockType( "QuickCheckin", "QuickCheckin block for helping parents check in their family quickly.", "~/Plugins/org_secc/FamilyCheckin/QuickCheckin.ascx", "SECC > Check-in", "AF961217-4223-4F03-9658-11DAB4B24DCD" ); //Block Type: QuickCheckin
            RockMigrationHelper.UpdateBlockType( "Super Search", "Displays keypad for searching on phone numbers.", "~/Plugins/org_secc/CheckinMonitor/SuperSearch.ascx", "SECC > Check-in", "F7389CD6-861E-4254-83ED-5777399E06B2" ); //Block Type: Super Search
            RockMigrationHelper.UpdateBlockType( "Super Checkin", "Advanced tool for managing checkin.", "~/Plugins/org_secc/CheckinMonitor/SuperCheckin.ascx", "SECC > Check-in", "5A7058FF-3A26-44D2-B8F4-C1E550987E2D" ); //Block Type: Super Checkin
            RockMigrationHelper.UpdateBlockType( "AutoConfigure", "Checkin auto configure block", "~/Plugins/org_secc/FamilyCheckin/AutoConfigure.ascx", "SECC > Check-in", "B4F4DD36-4912-4369-8C35-1500719A20C2" ); //Block Type: AutoConfigure
            RockMigrationHelper.UpdateBlockType( "Mobile Check-in Viewer", "Displays active mobile check-in records for printing.", "~/Plugins/org_secc/CheckinMonitor/MobileCheckinViewer.ascx", "SECC > Check-in", "32AE7BF6-E51E-4A0D-80D0-18AA003D9070" ); //Block Type: Mobile Check-in Viewer
            RockMigrationHelper.AddBlock( "ABF839C7-8A7B-4F51-9D33-63AA841EAD38", "", "315A175F-C682-4810-9F33-1BDB93904A4E", "Quick Search", "Main", "", "", 0, "3EEC4622-0BEB-433E-AF0C-01560468AFD5" ); //Block of Type: QuickSearch
            RockMigrationHelper.AddBlock( "71959598-BE24-4A51-9DB6-CC34993B523D", "", "AF961217-4223-4F03-9658-11DAB4B24DCD", "Quick Checkin", "Main", "", "", 0, "A5D4FDB1-D022-45B6-8001-7D69C3A2C33C" ); //Block of Type: QuickCheckin
            RockMigrationHelper.AddBlock( "F2F82210-0149-4F67-AD28-B618F3158E95", "", "B4F4DD36-4912-4369-8C35-1500719A20C2", "AutoConfigure", "Main", "", "", 0, "C5C4CA07-4A04-486D-B756-9E6973767960" ); //Block of Type: AutoConfigure
            RockMigrationHelper.AddBlock( "ABF839C7-8A7B-4F51-9D33-63AA841EAD38", "", "19B61D65-37E3-459F-A44F-DEF0089118A3", "SuperCheckinButton", "Main", "", "", 1, "43F1D63D-3C76-43C1-9C41-C2A57CDD83E1" ); //Block of Type: HTML Content
            RockMigrationHelper.AddBlock( "A9E89AD5-AEB1-4F3F-8DF4-525EEB077561", "", "F7389CD6-861E-4254-83ED-5777399E06B2", "Super Search", "Main", "", "", 0, "0935F201-0CAA-4889-BAF4-B809F336CB89" ); //Block of Type: Super Search
            RockMigrationHelper.AddBlock( "A9E89AD5-AEB1-4F3F-8DF4-525EEB077561", "", "19B61D65-37E3-459F-A44F-DEF0089118A3", "Back Button", "Main", "", "", 1, "63D6A421-DF6B-4706-B68E-0B925A801A3C" ); //Block of Type: HTML Content
            RockMigrationHelper.AddBlock( "C539F927-06A8-40F7-A262-4166D168E8D6", "", "5A7058FF-3A26-44D2-B8F4-C1E550987E2D", "Super Checkin", "Main", "", "", 0, "07E4D46A-C669-4B12-9D97-E90A27A4680E" ); //Block of Type: Super Checkin
            RockMigrationHelper.AddBlock( "A9E89AD5-AEB1-4F3F-8DF4-525EEB077561", "", "19B61D65-37E3-459F-A44F-DEF0089118A3", "Title", "Header", "", "", 0, "36915BD3-8226-47B0-98E9-C49020BA4253" ); //Block of Type: HTML Content
            RockMigrationHelper.AddBlock( "C539F927-06A8-40F7-A262-4166D168E8D6", "", "19B61D65-37E3-459F-A44F-DEF0089118A3", "Title", "Header", "", "", 0, "299919D7-D29C-47C8-982A-66D80E525D25" ); //Block of Type: HTML Content
            RockMigrationHelper.AddBlock( "47C8C26F-353D-45A8-8F16-7C09FA606AD3", "", "32AE7BF6-E51E-4A0D-80D0-18AA003D9070", "Mobile Check-in Viewer", "Main", "", "", 0, "8DC12C9B-A5BB-46B5-A209-8490506F3B4B" ); //Block of Type: Mobile Check-in Viewer
            RockMigrationHelper.AddBlock( "908A0960-DF24-42DD-941E-64F8DCEE7EFC", "", "19B61D65-37E3-459F-A44F-DEF0089118A3", "Scanner Config", "Main", "", "", 0, "55A40DB9-9AB2-46EF-936C-62FC21CA9862" ); //Block of Type: HTML Content
            RockMigrationHelper.AddBlockTypeAttribute( "B4F4DD36-4912-4369-8C35-1500719A20C2", "46A03F59-55D3-4ACE-ADD5-B4642225DD20", "Workflow Type", "WorkflowType", "", "The workflow type to activate for check-in", 0, @"", "FAA580CE-4B4A-481F-9BF1-47C334C60E82" );
            RockMigrationHelper.AddBlockTypeAttribute( "32AE7BF6-E51E-4A0D-80D0-18AA003D9070", "46A03F59-55D3-4ACE-ADD5-B4642225DD20", "Workflow Type", "WorkflowType", "", "The workflow type to activate for check-in", 0, @"", "B3DC753F-3568-45E5-A92F-D0ED10F0ADA8" );
            RockMigrationHelper.AddBlockTypeAttribute( "B4F4DD36-4912-4369-8C35-1500719A20C2", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Manual", "Manual", "", "Allow for manual configuration", 0, @"False", "C0FAA4A5-697A-49C4-8EC0-3BBD39804B49" );
            RockMigrationHelper.AddBlockTypeAttribute( "32AE7BF6-E51E-4A0D-80D0-18AA003D9070", "C403E219-A56B-439E-9D50-9302DFE760CF", "Aggregated Label", "AggregatedLabel", "", "Binary file that is the parent pickup label", 0, @"", "7EA8F79D-648D-4F9B-A80A-8EEF74F5DD37" );
            RockMigrationHelper.AddBlockTypeAttribute( "B4F4DD36-4912-4369-8C35-1500719A20C2", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Legacy Mode", "LegacyMode", "", "Allow for the using of the hostname to set the kiosk.", 0, @"True", "A69CF3B8-7C03-45EE-A16C-ADFD854AD4EC" );
            RockMigrationHelper.AddBlockTypeAttribute( "32AE7BF6-E51E-4A0D-80D0-18AA003D9070", "9C204CD0-1233-41C5-818A-C5DA439445AA", "Workflow Activity", "WorkflowActivity", "", "The name of the workflow activity to run on selection.", 1, @"", "E4490B24-85C5-451E-8D2C-1FC53489F369" );
            RockMigrationHelper.AddBlockTypeAttribute( "B4F4DD36-4912-4369-8C35-1500719A20C2", "9C204CD0-1233-41C5-818A-C5DA439445AA", "Workflow Activity", "WorkflowActivity", "", "The name of the workflow activity to run on selection.", 1, @"", "49D2F9D4-5D76-4105-88F4-1D5E6AF05B83" );
            RockMigrationHelper.AddBlockTypeAttribute( "B4F4DD36-4912-4369-8C35-1500719A20C2", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Home Page", "HomePage", "", "", 2, @"", "929FAAF5-334D-4169-82ED-867F6A6B9201" );
            RockMigrationHelper.AddBlockTypeAttribute( "32AE7BF6-E51E-4A0D-80D0-18AA003D9070", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Home Page", "HomePage", "", "", 2, @"", "A6B1D69B-FD0E-4DDE-B166-EBE7F514E8A1" );
            RockMigrationHelper.AddBlockTypeAttribute( "32AE7BF6-E51E-4A0D-80D0-18AA003D9070", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Previous Page", "PreviousPage", "", "", 3, @"", "6416A775-6770-4A10-97E5-7021CA6BEB5F" );
            RockMigrationHelper.AddBlockTypeAttribute( "B4F4DD36-4912-4369-8C35-1500719A20C2", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Previous Page", "PreviousPage", "", "", 3, @"", "A07E1566-83B6-47EF-A625-BE14BB9D8996" );
            RockMigrationHelper.AddBlockTypeAttribute( "32AE7BF6-E51E-4A0D-80D0-18AA003D9070", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Next Page", "NextPage", "", "", 4, @"", "18D5F9C7-E9B6-4FB8-9C98-E3E092DB664A" );
            RockMigrationHelper.AddBlockTypeAttribute( "B4F4DD36-4912-4369-8C35-1500719A20C2", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Next Page", "NextPage", "", "", 4, @"", "A5A2D49C-0197-47F3-A73D-F922F7273E84" );
            RockMigrationHelper.AddBlockAttributeValue( "3EEC4622-0BEB-433E-AF0C-01560468AFD5", "55638480-CFBF-4228-A534-66C64CCF7812", @"4" ); // Minimum Phone Number Length
            RockMigrationHelper.AddBlockAttributeValue( "3EEC4622-0BEB-433E-AF0C-01560468AFD5", "599916FE-39E4-4103-907E-3EB888CDCEA3", @"10" ); // Maximum Phone Number Length
            RockMigrationHelper.AddBlockAttributeValue( "3EEC4622-0BEB-433E-AF0C-01560468AFD5", "F781126E-9ADB-47E5-B593-30C526333567", @"" ); // Search Regex
            RockMigrationHelper.AddBlockAttributeValue( "3EEC4622-0BEB-433E-AF0C-01560468AFD5", "3D683472-885B-4FB1-8401-EAB7B0BC4FAF", @"80973ff7-e2c4-4a18-9939-216e347bdc27" ); // Workflow Type
            RockMigrationHelper.AddBlockAttributeValue( "3EEC4622-0BEB-433E-AF0C-01560468AFD5", "6E75589E-429A-4961-9C5D-278A6AE60F80", @"10" ); // Refresh Interval
            RockMigrationHelper.AddBlockAttributeValue( "3EEC4622-0BEB-433E-AF0C-01560468AFD5", "C20253EB-1572-43A5-95B5-4B5FD104A6D2", @"Family Search" ); // Workflow Activity
            RockMigrationHelper.AddBlockAttributeValue( "3EEC4622-0BEB-433E-AF0C-01560468AFD5", "C96C2070-533E-41FB-995B-3EEA512EF63D", @"f2f82210-0149-4f67-ad28-b618f3158e95" ); // Home Page
            RockMigrationHelper.AddBlockAttributeValue( "3EEC4622-0BEB-433E-AF0C-01560468AFD5", "9CA7B05F-FC51-4F32-8795-3A06FBC9E634", @"f2f82210-0149-4f67-ad28-b618f3158e95" ); // Previous Page
            RockMigrationHelper.AddBlockAttributeValue( "3EEC4622-0BEB-433E-AF0C-01560468AFD5", "F34D5FC1-0E69-4C32-A6BB-4A35EE1AD744", @"f3f66040-c50f-4d13-9652-780305fffe23" ); // Search Type
            RockMigrationHelper.AddBlockAttributeValue( "3EEC4622-0BEB-433E-AF0C-01560468AFD5", "A210618E-FF31-412A-BD26-FFA3BCDDFCE9", @"71959598-be24-4a51-9db6-cc34993b523d" ); // Next Page
            RockMigrationHelper.AddBlockAttributeValue( "3EEC4622-0BEB-433E-AF0C-01560468AFD5", "41C20593-D875-4D28-9FAF-35B135F0F13A", @"<h1>Welcome!</h1> Your check-in name tags should print shortly. <br><br> Please see a check-in volunteer if you need assistance." ); // Completing Checkin Record
            RockMigrationHelper.AddBlockAttributeValue( "3EEC4622-0BEB-433E-AF0C-01560468AFD5", "C3077ED6-21E9-4625-8D94-A17C43D24274", @"<h1>No Reservation Found</h1> We're sorry, we could not find a check-in record that matched your code. <br><br> Please see a check-in volunteer if you need assistance." ); // No Mobile Checkin Record
            RockMigrationHelper.AddBlockAttributeValue( "3EEC4622-0BEB-433E-AF0C-01560468AFD5", "57170C5B-B2DD-4D0D-9920-0A549350CC91", @"<h1>Reservation Expired</h1> We're sorry, this check-in reservation has expired. <br><br> Please see a check-in volunteer if you need assistance." ); // Expired Checkin Record
            RockMigrationHelper.AddBlockAttributeValue( "3EEC4622-0BEB-433E-AF0C-01560468AFD5", "57E428E9-8B5F-4EAC-9DC7-519CB52BA8D2", @"<h1>Check-in Already Completed</h1> We're sorry, it seems we have already completed your check-in reservation. <br><br> Please see a check-in volunteer if you need assistance." ); // Already Completed Checkin Record
            RockMigrationHelper.AddBlockAttributeValue( "3EEC4622-0BEB-433E-AF0C-01560468AFD5", "0E71017F-F502-401F-8BE4-641D4C408EC5", @"0515ddc0-ae19-4b3e-8907-24be1f41e37d" ); // Aggregated Label
            RockMigrationHelper.AddBlockAttributeValue( "3EEC4622-0BEB-433E-AF0C-01560468AFD5", "9127E939-B8FC-46FC-864A-0FE352A7E77C", @"<h1>Check In Here</h1> Welcome to Southeast Christian Church. We value your children's safety and require all children to check in at one of our kiosks. If you need help or have any question please see one of our helpful volunteers. " ); // Default Content
            RockMigrationHelper.AddBlockAttributeValue( "A5D4FDB1-D022-45B6-8001-7D69C3A2C33C", "C8266886-8A86-401C-9982-8F675132CFF0", @"abf839c7-8a7b-4f51-9d33-63aa841ead38" ); // Next Page
            RockMigrationHelper.AddBlockAttributeValue( "A5D4FDB1-D022-45B6-8001-7D69C3A2C33C", "C0F78DCA-BE6D-4BAD-8985-35D0F7027248", @"f2f82210-0149-4f67-ad28-b618f3158e95" ); // Home Page
            RockMigrationHelper.AddBlockAttributeValue( "A5D4FDB1-D022-45B6-8001-7D69C3A2C33C", "FC3AE63A-E7D1-476A-91BD-9E934B358963", @"abf839c7-8a7b-4f51-9d33-63aa841ead38" ); // Previous Page
            RockMigrationHelper.AddBlockAttributeValue( "A5D4FDB1-D022-45B6-8001-7D69C3A2C33C", "107BFB22-2844-443E-B61D-ADD5BEA0C54F", @"80973ff7-e2c4-4a18-9939-216e347bdc27" ); // Workflow Type
            RockMigrationHelper.AddBlockAttributeValue( "A5D4FDB1-D022-45B6-8001-7D69C3A2C33C", "9ECBB4F3-9052-4E49-81EA-08DA3F93868F", @"Person Search" ); // Workflow Activity
            RockMigrationHelper.AddBlockAttributeValue( "C5C4CA07-4A04-486D-B756-9E6973767960", "49D2F9D4-5D76-4105-88F4-1D5E6AF05B83", @"" ); // Workflow Activity
            RockMigrationHelper.AddBlockAttributeValue( "C5C4CA07-4A04-486D-B756-9E6973767960", "A5A2D49C-0197-47F3-A73D-F922F7273E84", @"abf839c7-8a7b-4f51-9d33-63aa841ead38" ); // Next Page
            RockMigrationHelper.AddBlockAttributeValue( "0935F201-0CAA-4889-BAF4-B809F336CB89", "34652569-8727-4DFC-BF66-74744C1BE8E3", @"Family Search" ); // Workflow Activity
            RockMigrationHelper.AddBlockAttributeValue( "0935F201-0CAA-4889-BAF4-B809F336CB89", "D77D09B6-3910-4085-9FBF-751BC845A821", @"f2f82210-0149-4f67-ad28-b618f3158e95" ); // Home Page
            RockMigrationHelper.AddBlockAttributeValue( "0935F201-0CAA-4889-BAF4-B809F336CB89", "55296F6A-4F8E-4174-9739-CF2C28B2A7A7", @"4f5803ac-c801-42df-86b7-b9bd85d02e07" ); // Workflow Type
            RockMigrationHelper.AddBlockAttributeValue( "0935F201-0CAA-4889-BAF4-B809F336CB89", "54442531-9A29-4107-91E0-EA2DFA008770", @"True" ); // Add Family Option
            RockMigrationHelper.AddBlockAttributeValue( "0935F201-0CAA-4889-BAF4-B809F336CB89", "CC1B39CF-5E8A-449A-9F55-FB9BAD0BC92F", @"abf839c7-8a7b-4f51-9d33-63aa841ead38" ); // Previous Page
            RockMigrationHelper.AddBlockAttributeValue( "0935F201-0CAA-4889-BAF4-B809F336CB89", "7050A025-4B31-40C4-A53D-2F9AC83F46FE", @"c539f927-06a8-40f7-a262-4166d168e8d6" ); // Next Page
            RockMigrationHelper.AddBlockAttributeValue( "07E4D46A-C669-4B12-9D97-E90A27A4680E", "F42F50CD-0EEE-4530-9F7F-17DCFEC19F36", @"a9e89ad5-aeb1-4f3f-8df4-525eeb077561" ); // Previous Page
            RockMigrationHelper.AddBlockAttributeValue( "07E4D46A-C669-4B12-9D97-E90A27A4680E", "1C2FB83B-FF46-40DC-BBD8-91F99B336C17", @"a9e89ad5-aeb1-4f3f-8df4-525eeb077561" ); // Next Page
            RockMigrationHelper.AddBlockAttributeValue( "07E4D46A-C669-4B12-9D97-E90A27A4680E", "411C375C-787D-4499-A7A8-AF56EDF39E1C", @"Save Attendance" ); // Checkin Activity
            RockMigrationHelper.AddBlockAttributeValue( "07E4D46A-C669-4B12-9D97-E90A27A4680E", "327A0DE8-C350-405D-BE75-466E64AFA4B2", @"Reprint Aggregate" ); // Reprint Activity
            RockMigrationHelper.AddBlockAttributeValue( "07E4D46A-C669-4B12-9D97-E90A27A4680E", "C3E1DDBB-44BD-4902-A77F-13059458FE06", @"407e7e45-7b2e-4fcd-9605-ecb1339f2453" ); // SMS Phone
            RockMigrationHelper.AddBlockAttributeValue( "07E4D46A-C669-4B12-9D97-E90A27A4680E", "DFD8821B-F504-47FB-A744-2A070DEFCDDE", @"aa8732fb-2cea-4c76-8d6d-6aaa2c6a4303" ); // Other Phone
            RockMigrationHelper.AddBlockAttributeValue( "07E4D46A-C669-4B12-9D97-E90A27A4680E", "86E7A555-F17D-450E-8E02-724C80017DF6", @"4f5803ac-c801-42df-86b7-b9bd85d02e07" ); // Workflow Type
            RockMigrationHelper.AddBlockAttributeValue( "07E4D46A-C669-4B12-9D97-E90A27A4680E", "9CD322BF-7A0E-4C3B-BCB1-992527E27F3F", @"77345b58-18eb-49cf-9469-17608c0cd3d8" ); // Connection Status
            RockMigrationHelper.AddBlockAttributeValue( "07E4D46A-C669-4B12-9D97-E90A27A4680E", "63174BBE-4CA8-4EC3-9D58-6E166308F757", @"b89576e0-c887-44b7-9ac8-b4078c450bbb" ); // Checkin Category
            RockMigrationHelper.AddBlockAttributeValue( "07E4D46A-C669-4B12-9D97-E90A27A4680E", "E935C1FD-2331-4C64-8722-E8A0779EC7B8", @"Load All" ); // Workflow Activity
            RockMigrationHelper.AddBlockAttributeValue( "07E4D46A-C669-4B12-9D97-E90A27A4680E", "B41DF542-5AF0-4DE0-A7A6-5B42A47C17DA", @"f2f82210-0149-4f67-ad28-b618f3158e95" ); // Home Page
            RockMigrationHelper.AddBlockAttributeValue( "07E4D46A-C669-4B12-9D97-E90A27A4680E", "F8B064BD-9D83-44B5-842C-112D8D65B119", @"aed692a5-4bb0-40fa-8c62-7948fab894c5" ); // Approved People
            RockMigrationHelper.AddBlockAttributeValue( "07E4D46A-C669-4B12-9D97-E90A27A4680E", "B873F9F0-6BD2-4B9C-B252-E1BF5387B7DC", @"True" ); // Allow Reprint
            RockMigrationHelper.AddBlockAttributeValue( "07E4D46A-C669-4B12-9D97-E90A27A4680E", "9E9F56D9-83E6-4B7E-9CEF-9987C5512F1D", @"Reprint ChildTag" ); // Child Reprint Activity
            RockMigrationHelper.AddBlockAttributeValue( "07E4D46A-C669-4B12-9D97-E90A27A4680E", "AE54EA5E-3B10-42F0-BA90-E4E521BE8768", @"False" ); // Allow NonApproved Adults
            RockMigrationHelper.AddBlockAttributeValue( "07E4D46A-C669-4B12-9D97-E90A27A4680E", "80BFF84D-F812-41BF-872A-7B62DF6EC350", @"71351937-218a-4ddc-a3dc-70cc6f28f3be" ); // Security Role Dataview
            RockMigrationHelper.AddBlockAttributeValue( "07E4D46A-C669-4B12-9D97-E90A27A4680E", "529F1E87-E8D8-4B0F-B3A1-AE0E09951E15", @"/page/802?PersonId={0}" ); // Data Error URL
            RockMigrationHelper.AddBlockAttributeValue( "07E4D46A-C669-4B12-9D97-E90A27A4680E", "07DB6C31-7E68-48A9-AAA0-2378F5E2B3EF", @"b77e27a2-a2a8-4a1e-8dd2-fdced77a7490" ); // Reprint Tag Security Group
            RockMigrationHelper.AddBlockAttributeValue( "8DC12C9B-A5BB-46B5-A209-8490506F3B4B", "7EA8F79D-648D-4F9B-A80A-8EEF74F5DD37", @"0515ddc0-ae19-4b3e-8907-24be1f41e37d" ); // Aggregated Label
            RockMigrationHelper.UpdateHtmlContentBlock( "43F1D63D-3C76-43C1-9C41-C2A57CDD83E1", @"<div style=""height:100px; width:100px; position:fixed; bottom:0px; right:0px; opacity:0;""> <a href=""/supercheckinlite"" style=""display:block;height:100%; width:100%"">Checkin Lite</a> </div>", "AEEC1BD4-F7A6-43E9-BEFE-50EB8281B5C6" ); //HTML Content
            RockMigrationHelper.UpdateHtmlContentBlock( "63D6A421-DF6B-4706-B68E-0B925A801A3C", @"<a href=""/familycheckin?logout=true"" class=""btn btn-warning"">Return to Checkin</a>", "87613AC4-708A-48AC-B337-529BDB74942F" ); //HTML Content
            RockMigrationHelper.UpdateHtmlContentBlock( "36915BD3-8226-47B0-98E9-C49020BA4253", @"<span style=""color:white; font-size: 2.5em"">Attended Check-In</span>", "02C2944B-E8DC-4D9C-93BD-35CAB8FD8553" ); //HTML Content
            RockMigrationHelper.UpdateHtmlContentBlock( "299919D7-D29C-47C8-982A-66D80E525D25", @"<span style=""color:white; font-size: 2.5em"">Attended Check-In</span>", "D8E609A1-4BBB-4351-8068-F5831DE4DB02" ); //HTML Content
            RockMigrationHelper.UpdateHtmlContentBlock( "55A40DB9-9AB2-46EF-936C-62FC21CA9862", @"<script>     var keybuffer = '';     var lastbufferedkey = 0;       Sys.Application.add_load(function () {          var captureSpecialKey = function (e) {             $phoneNumber = $(""input[id$='tbPhone']"");             e = e || event;             if (e.keyCode == 13) {                 if (keybuffer.length > 0 && keybuffer === 'MCRqrcodetestcode') {                     document.getElementById(""qrcode"").src = ""/Content/InternalSite/checkmark-flat.png"";                 }                 e.preventDefault();              }         }                  var captureKey = function (e) {             $phoneNumber = $(""input[id$='tbPhone']"");             e = e || event;             if (!$phoneNumber.is("":focus"")) {                 //Get the character                 var char = String.fromCharCode(e.keyCode || e.charCode);                  var date = new Date();                  //If it's been half a second since the last key press reset the keyboard buffer                 if (date.getTime() - lastbufferedkey > 500) {                     keybuffer = '';                 }                  if (keybuffer.length > 0) {                     keybuffer += char;                 }                 else if (char === ""M"") { // The first charater will be an ""M"" if it is a mobile checkin record access key                     keybuffer = ""M"";                 }                 else if ([""0"", ""1"", ""2"", ""3"", ""4"", ""5"", ""6"", ""7"", ""8"", ""9""].indexOf(char) > -1) {                     $phoneNumber = $(""input[id$='tbPhone']"");                     $phoneNumber.val($phoneNumber.val() + char);                 }                  lastbufferedkey = date.getTime();             }         }          document.body.onkeydown = captureSpecialKey;         document.body.onkeypress = captureKey;     });  </script>   <center> <h1>Configure Scanner</h1> <br> <br> <h2>Step 1: Scan Config Code</h2> <p><img src=""/Content/InternalSite/DS9308_Config.png""></p> <br> <br> <br> <br> <h2>Step 2: Scan Test Code</h2> <p><img src=""/Content/InternalSite/QrCodeTest.png"" id=""qrcode""></p> </center>", "77A79D4E-6AC0-41BB-8B32-B24D2AA0EF35" ); //HTML Content
        }

        public override void Down()
        {

        }
    }
}
