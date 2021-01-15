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
    using org.secc.DevLib.Extensions.Migration;
    using org.secc.FamilyCheckin.Utilities;

    [MigrationNumber( 25, "1.10.2" )]
    public partial class SuperCheckinPages : Migration
    {
        public override void Up()
        {
            RockMigrationHelper.AddSite( "Super Checkin", "", "SE Kids", "7D5718BE-D285-4235-9246-BD9CFA57DB59" );   //Site: Super Checkin
            RockMigrationHelper.AddLayout( "7D5718BE-D285-4235-9246-BD9CFA57DB59", "Checkin", "Checkin", "", "9E8E6D3B-1977-4D48-8CC0-0442717EEFFE" ); // Site:Super Checkin
            RockMigrationHelper.AddLayout( "7D5718BE-D285-4235-9246-BD9CFA57DB59", "FullWidth", "Full Width", "", "4BB26D3D-0B21-488A-8BC1-B76536C85693" ); // Site:Super Checkin
            // Page: Super Checkin Home Page
            RockMigrationHelper.AddPage("","4BB26D3D-0B21-488A-8BC1-B76536C85693","Super Checkin Home Page","","EBAC52D5-15FE-495D-A517-D1581241D4B8",""); // Site:Super Checkin
            // Page: Admin            
            RockMigrationHelper.AddPage("EBAC52D5-15FE-495D-A517-D1581241D4B8","4BB26D3D-0B21-488A-8BC1-B76536C85693","Admin","","15E6506E-54EB-4C37-8384-C5C9378BF08E",""); // Site:Super Checkin
            // Page: Super Search  
            RockMigrationHelper.AddPage("EBAC52D5-15FE-495D-A517-D1581241D4B8","4BB26D3D-0B21-488A-8BC1-B76536C85693","Super Search","","4ADDA171-D5ED-4EE2-B4CB-ECE507BA283F",""); // Site:Super Checkin
            // Page: Super Checkin         
            RockMigrationHelper.AddPage("EBAC52D5-15FE-495D-A517-D1581241D4B8","4BB26D3D-0B21-488A-8BC1-B76536C85693","Super Checkin","","0616F4DD-5449-4D85-B573-FDD07A155453",""); // Site:Super Checkin
            // Page: OZ Screen           
            RockMigrationHelper.AddPage("EBAC52D5-15FE-495D-A517-D1581241D4B8","4BB26D3D-0B21-488A-8BC1-B76536C85693","OZ Screen","","4330FAF3-31F1-43A5-B1B0-6BB9742DE10E",""); // Site:Super Checkin
            // Page: Quick Search    
            RockMigrationHelper.AddPage("EBAC52D5-15FE-495D-A517-D1581241D4B8","9E8E6D3B-1977-4D48-8CC0-0442717EEFFE","Quick Search","","749C2FD3-6C6F-4CD6-B531-AB08AE7B1370",""); // Site:Super Checkin
            // Page: Quick Checkin  
            RockMigrationHelper.AddPage("EBAC52D5-15FE-495D-A517-D1581241D4B8","9E8E6D3B-1977-4D48-8CC0-0442717EEFFE","Quick Checkin","","129259FB-BA3D-4C5A-9457-5D69ABB967C8",""); // Site:Super Checkin
            // Page: Child ID Signs  
            RockMigrationHelper.AddPage("EBAC52D5-15FE-495D-A517-D1581241D4B8","4BB26D3D-0B21-488A-8BC1-B76536C85693","Child ID Signs","","836A9BE7-3C63-4799-8D7D-D7508436D20F",""); // Site:Super Checkin
            // Page: Check-In Data Error Workflow      
            RockMigrationHelper.AddPage("0616F4DD-5449-4D85-B573-FDD07A155453","4BB26D3D-0B21-488A-8BC1-B76536C85693","Check-In Data Error Workflow","","7B6A4CF4-1D8C-48D9-8731-9DD5D20880FC",""); // Site:Super Checkin
            // Page: Active Mobile Check-ins         
            RockMigrationHelper.AddPage("EBAC52D5-15FE-495D-A517-D1581241D4B8","4BB26D3D-0B21-488A-8BC1-B76536C85693","Active Mobile Check-ins","","2BDCBBE7-6FBD-4149-881D-F800DE50952F",""); // Site:Super Checkin
            RockMigrationHelper.AddPageRoute( "15E6506E-54EB-4C37-8384-C5C9378BF08E", "supercheckin" );
            RockMigrationHelper.UpdateBlockType( "QuickSearch", "QuickSearch block for helping parents find their family quickly.", "~/Plugins/org_secc/FamilyCheckin/QuickSearch.ascx", "SECC > Check-in", "315A175F-C682-4810-9F33-1BDB93904A4E" ); //Block Type: QuickSearch
            RockMigrationHelper.UpdateBlockType( "QuickCheckin", "QuickCheckin block for helping parents check in their family quickly.", "~/Plugins/org_secc/FamilyCheckin/QuickCheckin.ascx", "SECC > Check-in", "AF961217-4223-4F03-9658-11DAB4B24DCD" ); //Block Type: QuickCheckin
            RockMigrationHelper.UpdateBlockType( "Super Search", "Displays keypad for searching on phone numbers.", "~/Plugins/org_secc/CheckinMonitor/SuperSearch.ascx", "SECC > Check-in", "F7389CD6-861E-4254-83ED-5777399E06B2" ); //Block Type: Super Search
            RockMigrationHelper.UpdateBlockType( "Super Checkin", "Advanced tool for managing checkin.", "~/Plugins/org_secc/CheckinMonitor/SuperCheckin.ascx", "SECC > Check-in", "5A7058FF-3A26-44D2-B8F4-C1E550987E2D" ); //Block Type: Super Checkin
            RockMigrationHelper.UpdateBlockType( "Check-In Monitor", "Helps manage rooms and room ratios.", "~/Plugins/org_secc/CheckinMonitor/CheckinMonitor.ascx", "SECC > Check-in", "98BB67DA-385F-4DAE-85A1-7C8CE73F1B97" ); //Block Type: Check-In Monitor
            RockMigrationHelper.UpdateBlockType( "AutoConfigure", "Checkin auto configure block", "~/Plugins/org_secc/FamilyCheckin/AutoConfigure.ascx", "SECC > Check-in", "B4F4DD36-4912-4369-8C35-1500719A20C2" ); //Block Type: AutoConfigure
            RockMigrationHelper.UpdateBlockType( "Sign Code Manager", "Manages all of the codes sent out.", "~/Plugins/org_secc/Microframe/SignCodeManager.ascx", "SECC > Microframe", "84A28A57-5F62-4AAB-B219-1227E4EDB820" ); //Block Type: Sign Code Manager
            RockMigrationHelper.UpdateBlockType( "Mobile Check-in Viewer", "Displays active mobile check-in records for printing.", "~/Plugins/org_secc/CheckinMonitor/MobileCheckinViewer.ascx", "SECC > Check-in", "32AE7BF6-E51E-4A0D-80D0-18AA003D9070" ); //Block Type: Mobile Check-in Viewer
            RockMigrationHelper.AddBlock( "0616F4DD-5449-4D85-B573-FDD07A155453", "", "5A7058FF-3A26-44D2-B8F4-C1E550987E2D", "Super Checkin", "Main", "", "", 0, "14EDC32D-D889-495D-B002-DAFBC0FE5724" ); //Block of Type: Super Checkin
            RockMigrationHelper.AddBlock( "15E6506E-54EB-4C37-8384-C5C9378BF08E", "", "B4F4DD36-4912-4369-8C35-1500719A20C2", "AutoConfigure", "Main", "", "", 0, "CDF0B107-0941-4BF0-A71C-478B85E60011" ); //Block of Type: AutoConfigure
            RockMigrationHelper.AddBlock( "4330FAF3-31F1-43A5-B1B0-6BB9742DE10E", "", "19B61D65-37E3-459F-A44F-DEF0089118A3", "Title", "Header", "", "", 0, "2208EA2C-9749-4703-805C-A71BE945ED0D" ); //Block of Type: HTML Content
            RockMigrationHelper.AddBlock( "4ADDA171-D5ED-4EE2-B4CB-ECE507BA283F", "", "19B61D65-37E3-459F-A44F-DEF0089118A3", "Title", "Header", "", "", 0, "FE9ECB26-A03D-420A-A7E2-F75BC28FE6BB" ); //Block of Type: HTML Content
            RockMigrationHelper.AddBlock( "0616F4DD-5449-4D85-B573-FDD07A155453", "", "19B61D65-37E3-459F-A44F-DEF0089118A3", "Title", "Header", "", "", 0, "9D2D500B-8615-42D2-834B-9314C27C1184" ); //Block of Type: HTML Content
            RockMigrationHelper.AddBlock( "749C2FD3-6C6F-4CD6-B531-AB08AE7B1370", "", "315A175F-C682-4810-9F33-1BDB93904A4E", "QuickSearch", "Main", "", "", 0, "AFE608FC-3F1A-44C4-8859-DD32C0D26FD9" ); //Block of Type: QuickSearch
            RockMigrationHelper.AddBlock( "749C2FD3-6C6F-4CD6-B531-AB08AE7B1370", "", "19B61D65-37E3-459F-A44F-DEF0089118A3", "Super Checkin", "Main", "", "", 1, "A46F3E0C-629A-4461-80D4-09AEA4AD2312" ); //Block of Type: HTML Content
            RockMigrationHelper.AddBlock( "129259FB-BA3D-4C5A-9457-5D69ABB967C8", "", "AF961217-4223-4F03-9658-11DAB4B24DCD", "QuickCheckin", "Main", "", "", 0, "6F258208-F48F-4E01-BA5F-9AAEA0F75C50" ); //Block of Type: QuickCheckin
            RockMigrationHelper.AddBlock( "129259FB-BA3D-4C5A-9457-5D69ABB967C8", "", "19B61D65-37E3-459F-A44F-DEF0089118A3", "Back Button", "Main", "", "", 1, "756F7E0B-EC09-4B28-AE1C-00B54D5F0A6F" ); //Block of Type: HTML Content
            RockMigrationHelper.AddBlock( "836A9BE7-3C63-4799-8D7D-D7508436D20F", "", "84A28A57-5F62-4AAB-B219-1227E4EDB820", "Sign Code Manager", "Main", "<style>     .panel-heading {         display:block !important;     } </style>", "", 0, "0B11FF85-D960-4852-8ADC-EB7044D60DB8" ); //Block of Type: Sign Code Manager
            RockMigrationHelper.AddBlock( "836A9BE7-3C63-4799-8D7D-D7508436D20F", "", "19B61D65-37E3-459F-A44F-DEF0089118A3", "Title", "Header", "", "", 0, "60E73364-3259-4D3F-BC76-F9BF39C1C0C6" ); //Block of Type: HTML Content
            RockMigrationHelper.AddBlock( "4ADDA171-D5ED-4EE2-B4CB-ECE507BA283F", "", "F7389CD6-861E-4254-83ED-5777399E06B2", "Super Search", "Main", "", "", 0, "CEAF0B42-22CD-45AD-B492-9FD0556E3AAA" ); //Block of Type: Super Search
            RockMigrationHelper.AddBlock( "4330FAF3-31F1-43A5-B1B0-6BB9742DE10E", "", "98BB67DA-385F-4DAE-85A1-7C8CE73F1B97", "Check-In Monitor", "Main", "", "", 0, "A724C1F0-A542-43A4-8E9A-94D6A2CBE2E6" ); //Block of Type: Check-In Monitor
            RockMigrationHelper.AddBlock( "7B6A4CF4-1D8C-48D9-8731-9DD5D20880FC", "", "A8BD05C8-6F89-4628-845B-059E686F089A", "Workflow Entry", "Main", "", "", 0, "685EB217-AA5E-44E0-BFD4-DF9959776654" ); //Block of Type: Workflow Entry
            RockMigrationHelper.AddBlock( "2BDCBBE7-6FBD-4149-881D-F800DE50952F", "", "32AE7BF6-E51E-4A0D-80D0-18AA003D9070", "Mobile Check-in Viewer", "Main", "<style> .panel.panel-block > .panel-heading{     display:block !important }  .panel-block {     border: solid 1px #6aa3d5 !important; }  </style>", "", 0, "1ADFAA6E-242D-4F5F-86B1-FB39F64898AF" ); //Block of Type: Mobile Check-in Viewer
            RockMigrationHelper.AddBlockTypeAttribute( "B4F4DD36-4912-4369-8C35-1500719A20C2", "46A03F59-55D3-4ACE-ADD5-B4642225DD20", "Workflow Type", "WorkflowType", "", "The workflow type to activate for check-in", 0, @"", "FAA580CE-4B4A-481F-9BF1-47C334C60E82" );
            RockMigrationHelper.AddBlockTypeAttribute( "84A28A57-5F62-4AAB-B219-1227E4EDB820", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Max Length", "MaxLength", "", "The maximum length a code can be.", 0, @"4", "B3DE032B-F50C-432A-84C8-FEDF008F970E" );
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
            RockMigrationHelper.AddBlockAttributeValue( "14EDC32D-D889-495D-B002-DAFBC0FE5724", "411C375C-787D-4499-A7A8-AF56EDF39E1C", @"Save Attendance" ); // Checkin Activity
            RockMigrationHelper.AddBlockAttributeValue( "14EDC32D-D889-495D-B002-DAFBC0FE5724", "327A0DE8-C350-405D-BE75-466E64AFA4B2", @"Reprint Aggregate" ); // Reprint Activity
            RockMigrationHelper.AddBlockAttributeValue( "14EDC32D-D889-495D-B002-DAFBC0FE5724", "C3E1DDBB-44BD-4902-A77F-13059458FE06", @"407e7e45-7b2e-4fcd-9605-ecb1339f2453" ); // SMS Phone
            RockMigrationHelper.AddBlockAttributeValue( "14EDC32D-D889-495D-B002-DAFBC0FE5724", "DFD8821B-F504-47FB-A744-2A070DEFCDDE", @"aa8732fb-2cea-4c76-8d6d-6aaa2c6a4303" ); // Other Phone
            RockMigrationHelper.AddBlockAttributeValue( "14EDC32D-D889-495D-B002-DAFBC0FE5724", "86E7A555-F17D-450E-8E02-724C80017DF6", @"4f5803ac-c801-42df-86b7-b9bd85d02e07" ); // Workflow Type
            RockMigrationHelper.AddBlockAttributeValue( "14EDC32D-D889-495D-B002-DAFBC0FE5724", "9CD322BF-7A0E-4C3B-BCB1-992527E27F3F", @"77345b58-18eb-49cf-9469-17608c0cd3d8" ); // Connection Status
            RockMigrationHelper.AddBlockAttributeValue( "14EDC32D-D889-495D-B002-DAFBC0FE5724", "63174BBE-4CA8-4EC3-9D58-6E166308F757", @"752dc692-836e-4a3e-b670-4325cd7724bf" ); // Checkin Category
            RockMigrationHelper.AddBlockAttributeValue( "14EDC32D-D889-495D-B002-DAFBC0FE5724", "E935C1FD-2331-4C64-8722-E8A0779EC7B8", @"Load All" ); // Workflow Activity
            RockMigrationHelper.AddBlockAttributeValue( "14EDC32D-D889-495D-B002-DAFBC0FE5724", "B41DF542-5AF0-4DE0-A7A6-5B42A47C17DA", @"15e6506e-54eb-4c37-8384-c5c9378bf08e" ); // Home Page
            RockMigrationHelper.AddBlockAttributeValue( "14EDC32D-D889-495D-B002-DAFBC0FE5724", "F42F50CD-0EEE-4530-9F7F-17DCFEC19F36", @"4adda171-d5ed-4ee2-b4cb-ece507ba283f" ); // Previous Page
            RockMigrationHelper.AddBlockAttributeValue( "14EDC32D-D889-495D-B002-DAFBC0FE5724", "1C2FB83B-FF46-40DC-BBD8-91F99B336C17", @"4adda171-d5ed-4ee2-b4cb-ece507ba283f" ); // Next Page
            RockMigrationHelper.AddBlockAttributeValue( "14EDC32D-D889-495D-B002-DAFBC0FE5724", "F8B064BD-9D83-44B5-842C-112D8D65B119", @"aed692a5-4bb0-40fa-8c62-7948fab894c5" ); // Approved People
            RockMigrationHelper.AddBlockAttributeValue( "14EDC32D-D889-495D-B002-DAFBC0FE5724", "B873F9F0-6BD2-4B9C-B252-E1BF5387B7DC", @"True" ); // Allow Reprint
            RockMigrationHelper.AddBlockAttributeValue( "14EDC32D-D889-495D-B002-DAFBC0FE5724", "9E9F56D9-83E6-4B7E-9CEF-9987C5512F1D", @"Reprint ChildTag" ); // Child Reprint Activity
            RockMigrationHelper.AddBlockAttributeValue( "14EDC32D-D889-495D-B002-DAFBC0FE5724", "AE54EA5E-3B10-42F0-BA90-E4E521BE8768", @"False" ); // Allow NonApproved Adults
            RockMigrationHelper.AddBlockAttributeValue( "14EDC32D-D889-495D-B002-DAFBC0FE5724", "80BFF84D-F812-41BF-872A-7B62DF6EC350", @"71351937-218a-4ddc-a3dc-70cc6f28f3be" ); // Security Role Dataview
            RockMigrationHelper.AddBlockAttributeValue( "14EDC32D-D889-495D-B002-DAFBC0FE5724", "529F1E87-E8D8-4B0F-B3A1-AE0E09951E15", @"/page/802?PersonId={0}" ); // Data Error URL
            RockMigrationHelper.AddBlockAttributeValue( "14EDC32D-D889-495D-B002-DAFBC0FE5724", "07DB6C31-7E68-48A9-AAA0-2378F5E2B3EF", @"b77e27a2-a2a8-4a1e-8dd2-fdced77a7490" ); // Reprint Tag Security Group
            RockMigrationHelper.AddBlockAttributeValue( "CDF0B107-0941-4BF0-A71C-478B85E60011", "49D2F9D4-5D76-4105-88F4-1D5E6AF05B83", @"" ); // Workflow Activity
            RockMigrationHelper.AddBlockAttributeValue( "CDF0B107-0941-4BF0-A71C-478B85E60011", "A5A2D49C-0197-47F3-A73D-F922F7273E84", @"4adda171-d5ed-4ee2-b4cb-ece507ba283f" ); // Next Page
            RockMigrationHelper.AddBlockAttributeValue( "CDF0B107-0941-4BF0-A71C-478B85E60011", "929FAAF5-334D-4169-82ED-867F6A6B9201", @"" ); // Home Page
            RockMigrationHelper.AddBlockAttributeValue( "AFE608FC-3F1A-44C4-8859-DD32C0D26FD9", "41C20593-D875-4D28-9FAF-35B135F0F13A", @"<h1>Welcome!</h1> Your check-in name tags should print shortly. <br><br> Please see a check-in volunteer if you need assistance." ); // Completing Checkin Record
            RockMigrationHelper.AddBlockAttributeValue( "AFE608FC-3F1A-44C4-8859-DD32C0D26FD9", "C3077ED6-21E9-4625-8D94-A17C43D24274", @"<h1>No Reservation Found</h1> We're sorry, we could not find a check-in record that matched your code. <br><br> Please see a check-in volunteer if you need assistance." ); // No Mobile Checkin Record
            RockMigrationHelper.AddBlockAttributeValue( "AFE608FC-3F1A-44C4-8859-DD32C0D26FD9", "57170C5B-B2DD-4D0D-9920-0A549350CC91", @"<h1>Reservation Expired</h1> We're sorry, this check-in reservation has expired. <br><br> Please see a check-in volunteer if you need assistance." ); // Expired Checkin Record
            RockMigrationHelper.AddBlockAttributeValue( "AFE608FC-3F1A-44C4-8859-DD32C0D26FD9", "57E428E9-8B5F-4EAC-9DC7-519CB52BA8D2", @"<h1>Check-in Already Completed</h1> We're sorry, it seems we have already completed your check-in reservation. <br><br> Please see a check-in volunteer if you need assistance." ); // Already Completed Checkin Record
            RockMigrationHelper.AddBlockAttributeValue( "AFE608FC-3F1A-44C4-8859-DD32C0D26FD9", "0E71017F-F502-401F-8BE4-641D4C408EC5", @"0515ddc0-ae19-4b3e-8907-24be1f41e37d" ); // Aggregated Label
            RockMigrationHelper.AddBlockAttributeValue( "AFE608FC-3F1A-44C4-8859-DD32C0D26FD9", "9127E939-B8FC-46FC-864A-0FE352A7E77C", @"<h1>Check In Here</h1> Welcome to Southeast Christian Church. We value your children's safety and require all children to check in at one of our kiosks. If you need help or have any question please see one of our helpful volunteers. " ); // Default Content
            RockMigrationHelper.AddBlockAttributeValue( "AFE608FC-3F1A-44C4-8859-DD32C0D26FD9", "9CA7B05F-FC51-4F32-8795-3A06FBC9E634", @"4adda171-d5ed-4ee2-b4cb-ece507ba283f" ); // Previous Page
            RockMigrationHelper.AddBlockAttributeValue( "AFE608FC-3F1A-44C4-8859-DD32C0D26FD9", "F34D5FC1-0E69-4C32-A6BB-4A35EE1AD744", @"f3f66040-c50f-4d13-9652-780305fffe23" ); // Search Type
            RockMigrationHelper.AddBlockAttributeValue( "AFE608FC-3F1A-44C4-8859-DD32C0D26FD9", "A210618E-FF31-412A-BD26-FFA3BCDDFCE9", @"129259fb-ba3d-4c5a-9457-5d69abb967c8" ); // Next Page
            RockMigrationHelper.AddBlockAttributeValue( "AFE608FC-3F1A-44C4-8859-DD32C0D26FD9", "55638480-CFBF-4228-A534-66C64CCF7812", @"4" ); // Minimum Phone Number Length
            RockMigrationHelper.AddBlockAttributeValue( "AFE608FC-3F1A-44C4-8859-DD32C0D26FD9", "599916FE-39E4-4103-907E-3EB888CDCEA3", @"10" ); // Maximum Phone Number Length
            RockMigrationHelper.AddBlockAttributeValue( "AFE608FC-3F1A-44C4-8859-DD32C0D26FD9", "F781126E-9ADB-47E5-B593-30C526333567", @"" ); // Search Regex
            RockMigrationHelper.AddBlockAttributeValue( "AFE608FC-3F1A-44C4-8859-DD32C0D26FD9", "3D683472-885B-4FB1-8401-EAB7B0BC4FAF", @"80973ff7-e2c4-4a18-9939-216e347bdc27" ); // Workflow Type
            RockMigrationHelper.AddBlockAttributeValue( "AFE608FC-3F1A-44C4-8859-DD32C0D26FD9", "6E75589E-429A-4961-9C5D-278A6AE60F80", @"10" ); // Refresh Interval
            RockMigrationHelper.AddBlockAttributeValue( "AFE608FC-3F1A-44C4-8859-DD32C0D26FD9", "C20253EB-1572-43A5-95B5-4B5FD104A6D2", @"Family Search" ); // Workflow Activity
            RockMigrationHelper.AddBlockAttributeValue( "AFE608FC-3F1A-44C4-8859-DD32C0D26FD9", "C96C2070-533E-41FB-995B-3EEA512EF63D", @"15e6506e-54eb-4c37-8384-c5c9378bf08e" ); // Home Page
            RockMigrationHelper.AddBlockAttributeValue( "6F258208-F48F-4E01-BA5F-9AAEA0F75C50", "C0F78DCA-BE6D-4BAD-8985-35D0F7027248", @"15e6506e-54eb-4c37-8384-c5c9378bf08e" ); // Home Page
            RockMigrationHelper.AddBlockAttributeValue( "6F258208-F48F-4E01-BA5F-9AAEA0F75C50", "FC3AE63A-E7D1-476A-91BD-9E934B358963", @"749c2fd3-6c6f-4cd6-b531-ab08ae7b1370" ); // Previous Page
            RockMigrationHelper.AddBlockAttributeValue( "6F258208-F48F-4E01-BA5F-9AAEA0F75C50", "107BFB22-2844-443E-B61D-ADD5BEA0C54F", @"80973ff7-e2c4-4a18-9939-216e347bdc27" ); // Workflow Type
            RockMigrationHelper.AddBlockAttributeValue( "6F258208-F48F-4E01-BA5F-9AAEA0F75C50", "9ECBB4F3-9052-4E49-81EA-08DA3F93868F", @"Person Search" ); // Workflow Activity
            RockMigrationHelper.AddBlockAttributeValue( "6F258208-F48F-4E01-BA5F-9AAEA0F75C50", "C8266886-8A86-401C-9982-8F675132CFF0", @"749c2fd3-6c6f-4cd6-b531-ab08ae7b1370" ); // Next Page
            RockMigrationHelper.AddBlockAttributeValue( "CEAF0B42-22CD-45AD-B492-9FD0556E3AAA", "55296F6A-4F8E-4174-9739-CF2C28B2A7A7", @"4f5803ac-c801-42df-86b7-b9bd85d02e07" ); // Workflow Type
            RockMigrationHelper.AddBlockAttributeValue( "CEAF0B42-22CD-45AD-B492-9FD0556E3AAA", "54442531-9A29-4107-91E0-EA2DFA008770", @"True" ); // Add Family Option
            RockMigrationHelper.AddBlockAttributeValue( "CEAF0B42-22CD-45AD-B492-9FD0556E3AAA", "34652569-8727-4DFC-BF66-74744C1BE8E3", @"Family Search" ); // Workflow Activity
            RockMigrationHelper.AddBlockAttributeValue( "CEAF0B42-22CD-45AD-B492-9FD0556E3AAA", "D77D09B6-3910-4085-9FBF-751BC845A821", @"15e6506e-54eb-4c37-8384-c5c9378bf08e" ); // Home Page
            RockMigrationHelper.AddBlockAttributeValue( "CEAF0B42-22CD-45AD-B492-9FD0556E3AAA", "7050A025-4B31-40C4-A53D-2F9AC83F46FE", @"0616f4dd-5449-4d85-b573-fdd07a155453" ); // Next Page
            RockMigrationHelper.AddBlockAttributeValue( "CEAF0B42-22CD-45AD-B492-9FD0556E3AAA", "CC1B39CF-5E8A-449A-9F55-FB9BAD0BC92F", @"15e6506e-54eb-4c37-8384-c5c9378bf08e" ); // Previous Page
            RockMigrationHelper.AddBlockAttributeValue( "A724C1F0-A542-43A4-8E9A-94D6A2CBE2E6", "BCC3EAA8-A29C-487F-96DF-6C04E444638B", @"15e6506e-54eb-4c37-8384-c5c9378bf08e" ); // Home Page
            RockMigrationHelper.AddBlockAttributeValue( "A724C1F0-A542-43A4-8E9A-94D6A2CBE2E6", "3262BDFD-2645-4FC5-8090-D7ACBCD9BE84", @"4adda171-d5ed-4ee2-b4cb-ece507ba283f" ); // Previous Page
            RockMigrationHelper.AddBlockAttributeValue( "A724C1F0-A542-43A4-8E9A-94D6A2CBE2E6", "1E7EC12E-8AAB-411E-9D19-31FF059C847D", @"4adda171-d5ed-4ee2-b4cb-ece507ba283f" ); // Next Page
            RockMigrationHelper.AddBlockAttributeValue( "A724C1F0-A542-43A4-8E9A-94D6A2CBE2E6", "62E9BFC4-3672-4AFC-AE98-3EFB71B88502", @"" ); // Workflow Type
            RockMigrationHelper.AddBlockAttributeValue( "A724C1F0-A542-43A4-8E9A-94D6A2CBE2E6", "7534C5DD-EE98-4CD7-B391-A0B1D14B4490", @"" ); // Workflow Activity
            RockMigrationHelper.AddBlockAttributeValue( "A724C1F0-A542-43A4-8E9A-94D6A2CBE2E6", "68706676-CE3B-4E8A-86B1-F10B98B224E7", @"aed692a5-4bb0-40fa-8c62-7948fab894c5" ); // Approved People
            RockMigrationHelper.AddBlockAttributeValue( "A724C1F0-A542-43A4-8E9A-94D6A2CBE2E6", "C3C4B8A5-017E-4DDF-8D06-141F0C135685", @"58688000-30be-4d06-a1cc-feb6759a4fe6" ); // Location Label
            RockMigrationHelper.AddBlockAttributeValue( "A724C1F0-A542-43A4-8E9A-94D6A2CBE2E6", "BD66A994-539E-451D-B3CC-1476E0530727", @"RoomRatio" ); // Room Ratio Attribute Key
            RockMigrationHelper.AddBlockAttributeValue( "685EB217-AA5E-44E0-BFD4-DF9959776654", "2F1D98C4-A8EF-4680-9F64-11BFC28D5597", @"221bf486-a82c-40a7-85b7-bb44da45582f" ); // Workflow Type
            RockMigrationHelper.AddBlockAttributeValue( "1ADFAA6E-242D-4F5F-86B1-FB39F64898AF", "7EA8F79D-648D-4F9B-A80A-8EEF74F5DD37", @"0515ddc0-ae19-4b3e-8907-24be1f41e37d" ); // Aggregated Label
            RockMigrationHelper.UpdateHtmlContentBlock( "2208EA2C-9749-4703-805C-A71BE945ED0D", @"<span style=""color:white;font-size:2.5em;"">The OZ Screen</span>", "9749FCA1-8884-4C34-B281-D6C6FA6D7B99" ); //HTML Content
            RockMigrationHelper.UpdateHtmlContentBlock( "FE9ECB26-A03D-420A-A7E2-F75BC28FE6BB", @"<span style=""color:white;font-size:2.5em;"">Super Check-In</span>", "F229DEB5-B327-4B0F-BC6A-9271847E4C7A" ); //HTML Content
            RockMigrationHelper.UpdateHtmlContentBlock( "9D2D500B-8615-42D2-834B-9314C27C1184", @"<span style=""color:white; font-size:2.5em;"">Super Check-In</span>", "F6D2AE62-7067-467E-924D-110FF898A9F6" ); //HTML Content
            RockMigrationHelper.UpdateHtmlContentBlock( "A46F3E0C-629A-4461-80D4-09AEA4AD2312", @"<a href=""/supercheckin"" class=""btn btn-warning btn-lg"" style=""z-index: 999; position:fixed; top:2px; right: 2px;""><b>Back To Super Checkin</b></a>", "DBAE55A0-5764-4265-9314-3FFA2629940D" ); //HTML Content
            RockMigrationHelper.UpdateHtmlContentBlock( "756F7E0B-EC09-4B28-AE1C-00B54D5F0A6F", @"<a href=""/supercheckin"" class=""btn btn-warning btn-lg"" style=""z-index: 999; position:fixed; top:2px; right: 2px;""><b>Back To Super Checkin</b></a>", "B44C1837-957A-4E8A-933E-F5461D58EE67" ); //HTML Content
            RockMigrationHelper.UpdateHtmlContentBlock( "60E73364-3259-4D3F-BC76-F9BF39C1C0C6", @"<span style=""color:white;font-size:2.5em"">Super Check-In</span>", "53FD1914-D372-4A63-813A-C1DD44196A57" ); //HTML Content
        }

        public override void Down()
        {

        }
    }
}
