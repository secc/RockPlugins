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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rock.Plugin;

namespace org.secc.Migrations
{
    [MigrationNumber( 9, "1.2.0" )]
    class SuperyCheckin_PageData : Migration
    {
        public override void Up()
        {
            RockMigrationHelper.AddSite( "Super Checkin", "", "Stark", "7D5718BE-D285-4235-9246-BD9CFA57DB59" );   //Site: Super Checkin
            RockMigrationHelper.AddLayout( "7D5718BE-D285-4235-9246-BD9CFA57DB59", "FullWidth", "Full Width", "", "4BB26D3D-0B21-488A-8BC1-B76536C85693" ); // Site:Super Checkin
            // Page: Super Checkin Home Page
            RockMigrationHelper.AddPage( "", "4BB26D3D-0B21-488A-8BC1-B76536C85693", "Super Checkin Home Page", "", "EBAC52D5-15FE-495D-A517-D1581241D4B8", "" ); // Site:Super Checkin
            // Page: Admin
            RockMigrationHelper.AddPage( "EBAC52D5-15FE-495D-A517-D1581241D4B8", "4BB26D3D-0B21-488A-8BC1-B76536C85693", "Admin", "", "15E6506E-54EB-4C37-8384-C5C9378BF08E", "" ); // Site:Super Checkin
            // Page: Super Search
            RockMigrationHelper.AddPage( "EBAC52D5-15FE-495D-A517-D1581241D4B8", "4BB26D3D-0B21-488A-8BC1-B76536C85693", "Super Search", "", "4ADDA171-D5ED-4EE2-B4CB-ECE507BA283F", "" ); // Site:Super Checkin
            // Page: Super Checkin
            RockMigrationHelper.AddPage( "EBAC52D5-15FE-495D-A517-D1581241D4B8", "4BB26D3D-0B21-488A-8BC1-B76536C85693", "Super Checkin", "", "0616F4DD-5449-4D85-B573-FDD07A155453", "" ); // Site:Super Checkin
            // Page: OZ Screen
            RockMigrationHelper.AddPage( "EBAC52D5-15FE-495D-A517-D1581241D4B8", "4BB26D3D-0B21-488A-8BC1-B76536C85693", "OZ Screen", "", "4330FAF3-31F1-43A5-B1B0-6BB9742DE10E", "" ); // Site:Super Checkin
            RockMigrationHelper.AddPageRoute( "15E6506E-54EB-4C37-8384-C5C9378BF08E", "supercheckin" );
            RockMigrationHelper.UpdateBlockType( "Super Search", "Displays keypad for searching on phone numbers.", "~/Plugins/org_secc/CheckinMonitor/SuperSearch.ascx", "SECC > Check-in", "F7389CD6-861E-4254-83ED-5777399E06B2" ); //Block Type: Super Search
            RockMigrationHelper.UpdateBlockType( "Super Checkin", "Advanced tool for managing checkin.", "~/Plugins/org_secc/CheckinMonitor/SuperCheckin.ascx", "SECC > Check-in", "5A7058FF-3A26-44D2-B8F4-C1E550987E2D" ); //Block Type: Super Checkin
            RockMigrationHelper.UpdateBlockType( "Checkin Monitor", "Helps manage rooms and room ratios.", "~/Plugins/org_secc/CheckinMonitor/CheckinMonitor.ascx", "Check-in", "98BB67DA-385F-4DAE-85A1-7C8CE73F1B97" ); //Block Type: Checkin Monitor
            RockMigrationHelper.AddBlock( "15E6506E-54EB-4C37-8384-C5C9378BF08E", "", "3B5FBE9A-2904-4220-92F3-47DD16E805C0", "Administration", "Main", "", "", 0, "3A79D1F1-3B80-4DD0-AE8C-0A26F8BC90FE" ); //Block of Type: Administration
            RockMigrationHelper.AddBlock( "4ADDA171-D5ED-4EE2-B4CB-ECE507BA283F", "", "F7389CD6-861E-4254-83ED-5777399E06B2", "Super Search", "Main", "", "", 0, "E2C644E0-5920-4F0D-940F-12E062DE026D" ); //Block of Type: Super Search
            RockMigrationHelper.AddBlock( "0616F4DD-5449-4D85-B573-FDD07A155453", "", "5A7058FF-3A26-44D2-B8F4-C1E550987E2D", "Super Checkin", "Main", "", "", 0, "14EDC32D-D889-495D-B002-DAFBC0FE5724" ); //Block of Type: Super Checkin
            RockMigrationHelper.AddBlock( "4ADDA171-D5ED-4EE2-B4CB-ECE507BA283F", "", "19B61D65-37E3-459F-A44F-DEF0089118A3", "OZ Button", "Feature", "", "", 0, "25CD4B0B-28EA-4FAE-BE81-BAA1384939C3" ); //Block of Type: HTML Content
            RockMigrationHelper.AddBlock( "4330FAF3-31F1-43A5-B1B0-6BB9742DE10E", "", "98BB67DA-385F-4DAE-85A1-7C8CE73F1B97", "Checkin Monitor", "Main", "", "", 0, "4E9FC706-E577-489F-83CB-63E28EC120F2" ); //Block of Type: Checkin Monitor
            RockMigrationHelper.AddBlock( "4330FAF3-31F1-43A5-B1B0-6BB9742DE10E", "", "19B61D65-37E3-459F-A44F-DEF0089118A3", "yay", "Main", "", "", 1, "35AC1538-3609-48FD-9AC8-5625D09F40D7" ); //Block of Type: HTML Content
            RockMigrationHelper.AddBlockTypeAttribute( "F7389CD6-861E-4254-83ED-5777399E06B2", "46A03F59-55D3-4ACE-ADD5-B4642225DD20", "Workflow Type", "WorkflowType", "", "The workflow type to activate for check-in", 0, @"", "55296F6A-4F8E-4174-9739-CF2C28B2A7A7" );
            RockMigrationHelper.AddBlockTypeAttribute( "F7389CD6-861E-4254-83ED-5777399E06B2", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Add Family Option", "AddFamilyOption", "", "Should the option to add a new family be available after search?", 0, @"True", "54442531-9A29-4107-91E0-EA2DFA008770" );
            RockMigrationHelper.AddBlockTypeAttribute( "5A7058FF-3A26-44D2-B8F4-C1E550987E2D", "9C204CD0-1233-41C5-818A-C5DA439445AA", "Checkin Activity", "CheckinActivity", "", "Name of the activity to complete checkin", 0, @"", "411C375C-787D-4499-A7A8-AF56EDF39E1C" );
            RockMigrationHelper.AddBlockTypeAttribute( "5A7058FF-3A26-44D2-B8F4-C1E550987E2D", "9C204CD0-1233-41C5-818A-C5DA439445AA", "Reprint Activity", "ReprintActivity", "", "Name of the activity to reprint tag", 0, @"", "327A0DE8-C350-405D-BE75-466E64AFA4B2" );
            RockMigrationHelper.AddBlockTypeAttribute( "5A7058FF-3A26-44D2-B8F4-C1E550987E2D", "59D5A94C-94A0-4630-B80A-BB25697D74C7", "SMS Phone", "SMSPhone", "", "Phone number type to save as when SMS enabled", 0, @"", "C3E1DDBB-44BD-4902-A77F-13059458FE06" );
            RockMigrationHelper.AddBlockTypeAttribute( "5A7058FF-3A26-44D2-B8F4-C1E550987E2D", "59D5A94C-94A0-4630-B80A-BB25697D74C7", "Other Phone", "OtherPhone", "", "Phone number type to save as when SMS NOT enabled", 0, @"", "DFD8821B-F504-47FB-A744-2A070DEFCDDE" );
            RockMigrationHelper.AddBlockTypeAttribute( "5A7058FF-3A26-44D2-B8F4-C1E550987E2D", "46A03F59-55D3-4ACE-ADD5-B4642225DD20", "Workflow Type", "WorkflowType", "", "The workflow type to activate for check-in", 0, @"", "86E7A555-F17D-450E-8E02-724C80017DF6" );
            RockMigrationHelper.AddBlockTypeAttribute( "5A7058FF-3A26-44D2-B8F4-C1E550987E2D", "59D5A94C-94A0-4630-B80A-BB25697D74C7", "Connection Status", "ConnectionStatus", "", "Connection status for new people.", 0, @"", "9CD322BF-7A0E-4C3B-BCB1-992527E27F3F" );
            RockMigrationHelper.AddBlockTypeAttribute( "5A7058FF-3A26-44D2-B8F4-C1E550987E2D", "309460EF-0CC5-41C6-9161-B3837BA3D374", "Checkin Category", "CheckinCategory", "", "The Attribute Category to display checkin attributes from", 0, @"", "63174BBE-4CA8-4EC3-9D58-6E166308F757" );
            RockMigrationHelper.AddBlockTypeAttribute( "98BB67DA-385F-4DAE-85A1-7C8CE73F1B97", "46A03F59-55D3-4ACE-ADD5-B4642225DD20", "Workflow Type", "WorkflowType", "", "The workflow type to activate for check-in", 0, @"", "62E9BFC4-3672-4AFC-AE98-3EFB71B88502" );
            RockMigrationHelper.AddBlockTypeAttribute( "98BB67DA-385F-4DAE-85A1-7C8CE73F1B97", "9C204CD0-1233-41C5-818A-C5DA439445AA", "Workflow Activity", "WorkflowActivity", "", "The name of the workflow activity to run on selection.", 1, @"", "7534C5DD-EE98-4CD7-B391-A0B1D14B4490" );
            RockMigrationHelper.AddBlockTypeAttribute( "5A7058FF-3A26-44D2-B8F4-C1E550987E2D", "9C204CD0-1233-41C5-818A-C5DA439445AA", "Workflow Activity", "WorkflowActivity", "", "The name of the workflow activity to run on selection.", 1, @"", "E935C1FD-2331-4C64-8722-E8A0779EC7B8" );
            RockMigrationHelper.AddBlockTypeAttribute( "F7389CD6-861E-4254-83ED-5777399E06B2", "9C204CD0-1233-41C5-818A-C5DA439445AA", "Workflow Activity", "WorkflowActivity", "", "The name of the workflow activity to run on selection.", 1, @"", "34652569-8727-4DFC-BF66-74744C1BE8E3" );
            RockMigrationHelper.AddBlockTypeAttribute( "F7389CD6-861E-4254-83ED-5777399E06B2", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Home Page", "HomePage", "", "", 2, @"", "D77D09B6-3910-4085-9FBF-751BC845A821" );
            RockMigrationHelper.AddBlockTypeAttribute( "5A7058FF-3A26-44D2-B8F4-C1E550987E2D", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Home Page", "HomePage", "", "", 2, @"", "B41DF542-5AF0-4DE0-A7A6-5B42A47C17DA" );
            RockMigrationHelper.AddBlockTypeAttribute( "98BB67DA-385F-4DAE-85A1-7C8CE73F1B97", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Home Page", "HomePage", "", "", 2, @"", "BCC3EAA8-A29C-487F-96DF-6C04E444638B" );
            RockMigrationHelper.AddBlockTypeAttribute( "98BB67DA-385F-4DAE-85A1-7C8CE73F1B97", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Previous Page", "PreviousPage", "", "", 3, @"", "3262BDFD-2645-4FC5-8090-D7ACBCD9BE84" );
            RockMigrationHelper.AddBlockTypeAttribute( "F7389CD6-861E-4254-83ED-5777399E06B2", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Previous Page", "PreviousPage", "", "", 3, @"", "CC1B39CF-5E8A-449A-9F55-FB9BAD0BC92F" );
            RockMigrationHelper.AddBlockTypeAttribute( "5A7058FF-3A26-44D2-B8F4-C1E550987E2D", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Previous Page", "PreviousPage", "", "", 3, @"", "F42F50CD-0EEE-4530-9F7F-17DCFEC19F36" );
            RockMigrationHelper.AddBlockTypeAttribute( "98BB67DA-385F-4DAE-85A1-7C8CE73F1B97", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Next Page", "NextPage", "", "", 4, @"", "1E7EC12E-8AAB-411E-9D19-31FF059C847D" );
            RockMigrationHelper.AddBlockTypeAttribute( "5A7058FF-3A26-44D2-B8F4-C1E550987E2D", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Next Page", "NextPage", "", "", 4, @"", "1C2FB83B-FF46-40DC-BBD8-91F99B336C17" );
            RockMigrationHelper.AddBlockTypeAttribute( "F7389CD6-861E-4254-83ED-5777399E06B2", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Next Page", "NextPage", "", "", 4, @"", "7050A025-4B31-40C4-A53D-2F9AC83F46FE" );
            RockMigrationHelper.AddBlockAttributeValue( "3A79D1F1-3B80-4DD0-AE8C-0A26F8BC90FE", "D46F3099-5700-4CCD-8B6C-F1F306BA02B8", @"15e6506e-54eb-4c37-8384-c5c9378bf08e" ); // Home Page
            RockMigrationHelper.AddBlockAttributeValue( "3A79D1F1-3B80-4DD0-AE8C-0A26F8BC90FE", "36C334EF-E723-4065-9C39-BD5663582751", @"4adda171-d5ed-4ee2-b4cb-ece507ba283f" ); // Next Page
            RockMigrationHelper.AddBlockAttributeValue( "3A79D1F1-3B80-4DD0-AE8C-0A26F8BC90FE", "7675AE35-1A61-460E-8FF6-B2A5C473F319", @"15e6506e-54eb-4c37-8384-c5c9378bf08e" ); // Previous Page
            RockMigrationHelper.AddBlockAttributeValue( "3A79D1F1-3B80-4DD0-AE8C-0A26F8BC90FE", "992F693A-1019-468C-B7A7-B945A616BAF0", @"False" ); // Enable Location Sharing
            RockMigrationHelper.AddBlockAttributeValue( "3A79D1F1-3B80-4DD0-AE8C-0A26F8BC90FE", "C2A1F6A2-A801-4B52-8CFB-9B394CCA2D17", @"20" ); // Time to Cache Kiosk GeoLocation
            RockMigrationHelper.AddBlockAttributeValue( "3A79D1F1-3B80-4DD0-AE8C-0A26F8BC90FE", "EBBC10ED-18E4-4E7D-9467-E7C27F12A745", @"True" ); // Allow Manual Setup
            RockMigrationHelper.AddBlockAttributeValue( "3A79D1F1-3B80-4DD0-AE8C-0A26F8BC90FE", "50DF7B49-FAF4-45D5-919F-14E589B37666", @"" ); // Workflow Activity
            RockMigrationHelper.AddBlockAttributeValue( "3A79D1F1-3B80-4DD0-AE8C-0A26F8BC90FE", "0E252443-86E1-4068-8B32-9943E0974C94", @"True" ); // Enable Kiosk Match By Name
            RockMigrationHelper.AddBlockAttributeValue( "E2C644E0-5920-4F0D-940F-12E062DE026D", "D77D09B6-3910-4085-9FBF-751BC845A821", @"15e6506e-54eb-4c37-8384-c5c9378bf08e" ); // Home Page
            RockMigrationHelper.AddBlockAttributeValue( "E2C644E0-5920-4F0D-940F-12E062DE026D", "7050A025-4B31-40C4-A53D-2F9AC83F46FE", @"0616f4dd-5449-4d85-b573-fdd07a155453" ); // Next Page
            RockMigrationHelper.AddBlockAttributeValue( "E2C644E0-5920-4F0D-940F-12E062DE026D", "CC1B39CF-5E8A-449A-9F55-FB9BAD0BC92F", @"15e6506e-54eb-4c37-8384-c5c9378bf08e" ); // Previous Page
            RockMigrationHelper.AddBlockAttributeValue( "E2C644E0-5920-4F0D-940F-12E062DE026D", "55296F6A-4F8E-4174-9739-CF2C28B2A7A7", @"c1be87c3-64d0-4f63-928a-8e3ff4c876fb" ); // Workflow Type
            RockMigrationHelper.AddBlockAttributeValue( "E2C644E0-5920-4F0D-940F-12E062DE026D", "34652569-8727-4DFC-BF66-74744C1BE8E3", @"Family Search" ); // Workflow Activity
            RockMigrationHelper.AddBlockAttributeValue( "14EDC32D-D889-495D-B002-DAFBC0FE5724", "B41DF542-5AF0-4DE0-A7A6-5B42A47C17DA", @"15e6506e-54eb-4c37-8384-c5c9378bf08e" ); // Home Page
            RockMigrationHelper.AddBlockAttributeValue( "14EDC32D-D889-495D-B002-DAFBC0FE5724", "1C2FB83B-FF46-40DC-BBD8-91F99B336C17", @"4adda171-d5ed-4ee2-b4cb-ece507ba283f" ); // Next Page
            RockMigrationHelper.AddBlockAttributeValue( "14EDC32D-D889-495D-B002-DAFBC0FE5724", "F42F50CD-0EEE-4530-9F7F-17DCFEC19F36", @"4adda171-d5ed-4ee2-b4cb-ece507ba283f" ); // Previous Page
            RockMigrationHelper.AddBlockAttributeValue( "14EDC32D-D889-495D-B002-DAFBC0FE5724", "86E7A555-F17D-450E-8E02-724C80017DF6", @"c1be87c3-64d0-4f63-928a-8e3ff4c876fb" ); // Workflow Type
            RockMigrationHelper.AddBlockAttributeValue( "14EDC32D-D889-495D-B002-DAFBC0FE5724", "E935C1FD-2331-4C64-8722-E8A0779EC7B8", @"Load All" ); // Workflow Activity
            RockMigrationHelper.AddBlockAttributeValue( "14EDC32D-D889-495D-B002-DAFBC0FE5724", "9CD322BF-7A0E-4C3B-BCB1-992527E27F3F", @"ff94c500-a80b-4c40-85fe-b0ed11ef153c" ); // Connection Status
            RockMigrationHelper.AddBlockAttributeValue( "14EDC32D-D889-495D-B002-DAFBC0FE5724", "63174BBE-4CA8-4EC3-9D58-6E166308F757", @"752dc692-836e-4a3e-b670-4325cd7724bf" ); // Checkin Category
            RockMigrationHelper.AddBlockAttributeValue( "14EDC32D-D889-495D-B002-DAFBC0FE5724", "411C375C-787D-4499-A7A8-AF56EDF39E1C", @"Save Attendance" ); // Checkin Activity
            RockMigrationHelper.AddBlockAttributeValue( "14EDC32D-D889-495D-B002-DAFBC0FE5724", "C3E1DDBB-44BD-4902-A77F-13059458FE06", @"407e7e45-7b2e-4fcd-9605-ecb1339f2453" ); // SMS Phone
            RockMigrationHelper.AddBlockAttributeValue( "14EDC32D-D889-495D-B002-DAFBC0FE5724", "DFD8821B-F504-47FB-A744-2A070DEFCDDE", @"aa8732fb-2cea-4c76-8d6d-6aaa2c6a4303" ); // Other Phone
            RockMigrationHelper.AddBlockAttributeValue( "14EDC32D-D889-495D-B002-DAFBC0FE5724", "327A0DE8-C350-405D-BE75-466E64AFA4B2", @"Reprint Aggregate" ); // Reprint Activity
            RockMigrationHelper.AddBlockAttributeValue( "4E9FC706-E577-489F-83CB-63E28EC120F2", "7534C5DD-EE98-4CD7-B391-A0B1D14B4490", @"" ); // Workflow Activity
            RockMigrationHelper.AddBlockAttributeValue( "4E9FC706-E577-489F-83CB-63E28EC120F2", "BCC3EAA8-A29C-487F-96DF-6C04E444638B", @"15e6506e-54eb-4c37-8384-c5c9378bf08e" ); // Home Page
            RockMigrationHelper.AddBlockAttributeValue( "4E9FC706-E577-489F-83CB-63E28EC120F2", "3262BDFD-2645-4FC5-8090-D7ACBCD9BE84", @"4adda171-d5ed-4ee2-b4cb-ece507ba283f" ); // Previous Page
            RockMigrationHelper.AddBlockAttributeValue( "4E9FC706-E577-489F-83CB-63E28EC120F2", "1E7EC12E-8AAB-411E-9D19-31FF059C847D", @"4adda171-d5ed-4ee2-b4cb-ece507ba283f" ); // Next Page
            RockMigrationHelper.UpdateHtmlContentBlock( "25CD4B0B-28EA-4FAE-BE81-BAA1384939C3", @"<a href=/page/4692 class=""btn btn-default"">OZ Screen</a>", "8BBF3F71-4937-412F-A6FC-2842D293E2FF" ); //HTML Content
        }
        public override void Down()
        {
            RockMigrationHelper.DeleteAttribute( "54442531-9A29-4107-91E0-EA2DFA008770" );
            RockMigrationHelper.DeleteAttribute( "327A0DE8-C350-405D-BE75-466E64AFA4B2" );
            RockMigrationHelper.DeleteAttribute( "1E7EC12E-8AAB-411E-9D19-31FF059C847D" );
            RockMigrationHelper.DeleteAttribute( "3262BDFD-2645-4FC5-8090-D7ACBCD9BE84" );
            RockMigrationHelper.DeleteAttribute( "BCC3EAA8-A29C-487F-96DF-6C04E444638B" );
            RockMigrationHelper.DeleteAttribute( "7534C5DD-EE98-4CD7-B391-A0B1D14B4490" );
            RockMigrationHelper.DeleteAttribute( "62E9BFC4-3672-4AFC-AE98-3EFB71B88502" );
            RockMigrationHelper.DeleteAttribute( "DFD8821B-F504-47FB-A744-2A070DEFCDDE" );
            RockMigrationHelper.DeleteAttribute( "C3E1DDBB-44BD-4902-A77F-13059458FE06" );
            RockMigrationHelper.DeleteAttribute( "411C375C-787D-4499-A7A8-AF56EDF39E1C" );
            RockMigrationHelper.DeleteAttribute( "63174BBE-4CA8-4EC3-9D58-6E166308F757" );
            RockMigrationHelper.DeleteAttribute( "9CD322BF-7A0E-4C3B-BCB1-992527E27F3F" );
            RockMigrationHelper.DeleteAttribute( "E935C1FD-2331-4C64-8722-E8A0779EC7B8" );
            RockMigrationHelper.DeleteAttribute( "86E7A555-F17D-450E-8E02-724C80017DF6" );
            RockMigrationHelper.DeleteAttribute( "F42F50CD-0EEE-4530-9F7F-17DCFEC19F36" );
            RockMigrationHelper.DeleteAttribute( "1C2FB83B-FF46-40DC-BBD8-91F99B336C17" );
            RockMigrationHelper.DeleteAttribute( "B41DF542-5AF0-4DE0-A7A6-5B42A47C17DA" );
            RockMigrationHelper.DeleteAttribute( "34652569-8727-4DFC-BF66-74744C1BE8E3" );
            RockMigrationHelper.DeleteAttribute( "55296F6A-4F8E-4174-9739-CF2C28B2A7A7" );
            RockMigrationHelper.DeleteAttribute( "CC1B39CF-5E8A-449A-9F55-FB9BAD0BC92F" );
            RockMigrationHelper.DeleteAttribute( "7050A025-4B31-40C4-A53D-2F9AC83F46FE" );
            RockMigrationHelper.DeleteAttribute( "D77D09B6-3910-4085-9FBF-751BC845A821" );
            RockMigrationHelper.DeleteAttribute( "0E252443-86E1-4068-8B32-9943E0974C94" );
            RockMigrationHelper.DeleteAttribute( "48FF43A9-8E12-4768-80A9-88FBB81F11D8" );
            RockMigrationHelper.DeleteAttribute( "6783D47D-92F9-4F48-93C0-16111D675A0F" );
            RockMigrationHelper.DeleteAttribute( "3BDB8AED-32C5-4879-B1CB-8FC7C8336534" );
            RockMigrationHelper.DeleteAttribute( "9D3E4ED9-1BEF-4547-B6B0-CE29FE3835EE" );
            RockMigrationHelper.DeleteAttribute( "26F3AFC6-C05B-44A4-8593-AFE1D9969B0E" );
            RockMigrationHelper.DeleteAttribute( "0673E015-F8DD-4A52-B380-C758011331B2" );
            RockMigrationHelper.DeleteAttribute( "50DF7B49-FAF4-45D5-919F-14E589B37666" );
            RockMigrationHelper.DeleteAttribute( "EBBC10ED-18E4-4E7D-9467-E7C27F12A745" );
            RockMigrationHelper.DeleteAttribute( "C2A1F6A2-A801-4B52-8CFB-9B394CCA2D17" );
            RockMigrationHelper.DeleteAttribute( "992F693A-1019-468C-B7A7-B945A616BAF0" );
            RockMigrationHelper.DeleteAttribute( "7675AE35-1A61-460E-8FF6-B2A5C473F319" );
            RockMigrationHelper.DeleteAttribute( "36C334EF-E723-4065-9C39-BD5663582751" );
            RockMigrationHelper.DeleteAttribute( "D46F3099-5700-4CCD-8B6C-F1F306BA02B8" );
            RockMigrationHelper.DeleteAttribute( "162A2B82-A71F-4B29-970A-047266FE696D" );
            RockMigrationHelper.DeleteAttribute( "466993F7-D838-447A-97E7-8BBDA6A57289" );
            RockMigrationHelper.DeleteAttribute( "3FFC512D-A576-4289-B648-905FD7A64ABB" );
            RockMigrationHelper.DeleteAttribute( "7C1CE199-86CF-4EAE-8AB3-848416A72C58" );
            RockMigrationHelper.DeleteAttribute( "EC2B701B-4C1D-4F3F-9C77-A73C75D7FF7A" );
            RockMigrationHelper.DeleteAttribute( "4DFDB295-6D0F-40A1-BEF9-7B70C56F66C4" );
            RockMigrationHelper.DeleteBlock( "4E9FC706-E577-489F-83CB-63E28EC120F2" );
            RockMigrationHelper.DeleteBlock( "25CD4B0B-28EA-4FAE-BE81-BAA1384939C3" );
            RockMigrationHelper.DeleteBlock( "14EDC32D-D889-495D-B002-DAFBC0FE5724" );
            RockMigrationHelper.DeleteBlock( "E2C644E0-5920-4F0D-940F-12E062DE026D" );
            RockMigrationHelper.DeleteBlock( "3A79D1F1-3B80-4DD0-AE8C-0A26F8BC90FE" );
            RockMigrationHelper.DeletePage( "EBAC52D5-15FE-495D-A517-D1581241D4B8" ); //  Page: Super Checkin Home Page
            RockMigrationHelper.DeletePage( "15E6506E-54EB-4C37-8384-C5C9378BF08E" ); //  Page: Admin
            RockMigrationHelper.DeletePage( "4ADDA171-D5ED-4EE2-B4CB-ECE507BA283F" ); //  Page: Super Search
            RockMigrationHelper.DeletePage( "0616F4DD-5449-4D85-B573-FDD07A155453" ); //  Page: Super Checkin
            RockMigrationHelper.DeletePage( "4330FAF3-31F1-43A5-B1B0-6BB9742DE10E" ); //  Page: OZ Screen
            RockMigrationHelper.DeleteLayout( "4BB26D3D-0B21-488A-8BC1-B76536C85693" ); //  Layout: Full Width, Site: Super Checkin
            RockMigrationHelper.DeleteSite( "7D5718BE-D285-4235-9246-BD9CFA57DB59" );
        }
    }
}

