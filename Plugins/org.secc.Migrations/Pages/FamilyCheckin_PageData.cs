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

namespace org.secc.Migrations
{
    [MigrationNumber( 3, "1.2.0" )]
    class FamilyCheckin_PageData : Migration
    {
        public override void Up()
        {
            RockMigrationHelper.AddSite( "Family Checkin", "Family check in for quick and painless checking in.", "SE Kids", "FB2A03C3-D5E9-4DBA-A56F-90311DD8BF13" );   //Site: Family Checkin
            RockMigrationHelper.AddLayout( "FB2A03C3-D5E9-4DBA-A56F-90311DD8BF13", "Checkin", "Checkin", "", "8A87FF45-C694-43B5-9494-0B2D6A71FFA1" ); // Site:Family Checkin
            // Page: Family Checkin Home Page
            RockMigrationHelper.AddPage( "", "8A87FF45-C694-43B5-9494-0B2D6A71FFA1", "Family Checkin Home Page", "", "4159825D-B2CE-4BC1-A5DB-917020059E89", "" ); // Site:Family Checkin
            // Page: Admin
            RockMigrationHelper.AddPage( "4159825D-B2CE-4BC1-A5DB-917020059E89", "8A87FF45-C694-43B5-9494-0B2D6A71FFA1", "Admin", "", "F2F82210-0149-4F67-AD28-B618F3158E95", "" ); // Site:Family Checkin
            // Page: QuickSearch
            RockMigrationHelper.AddPage( "4159825D-B2CE-4BC1-A5DB-917020059E89", "8A87FF45-C694-43B5-9494-0B2D6A71FFA1", "QuickSearch", "", "ABF839C7-8A7B-4F51-9D33-63AA841EAD38", "" ); // Site:Family Checkin
            // Page: QuickCheckin
            RockMigrationHelper.AddPage( "4159825D-B2CE-4BC1-A5DB-917020059E89", "8A87FF45-C694-43B5-9494-0B2D6A71FFA1", "QuickCheckin", "", "71959598-BE24-4A51-9DB6-CC34993B523D", "" ); // Site:Family Checkin
            RockMigrationHelper.AddOrUpdatePageRoute( "F2F82210-0149-4F67-AD28-B618F3158E95", "familycheckin" );
            RockMigrationHelper.UpdateBlockType( "QuickSearch", "QuickSearch block for helping parents find their family quickly.", "~/Plugins/org_secc/FamilyCheckin/QuickSearch.ascx", "Check-in", "315A175F-C682-4810-9F33-1BDB93904A4E" ); //Block Type: QuickSearch
            RockMigrationHelper.UpdateBlockType( "QuickCheckin", "QuickCheckin block for helping parents check in their family quickly.", "~/Plugins/org_secc/FamilyCheckin/QuickCheckin.ascx", "Check-in", "AF961217-4223-4F03-9658-11DAB4B24DCD" ); //Block Type: QuickCheckin
            RockMigrationHelper.AddBlock( "F2F82210-0149-4F67-AD28-B618F3158E95", "", "3B5FBE9A-2904-4220-92F3-47DD16E805C0", "Administration", "Main", "", "", 0, "1CFD1EC1-198C-4F4C-B87D-32DD19648B39" ); //Block of Type: Administration
            RockMigrationHelper.AddBlock( "ABF839C7-8A7B-4F51-9D33-63AA841EAD38", "", "315A175F-C682-4810-9F33-1BDB93904A4E", "Quick Search", "Main", "", "", 0, "3EEC4622-0BEB-433E-AF0C-01560468AFD5" ); //Block of Type: QuickSearch
            RockMigrationHelper.AddBlock( "71959598-BE24-4A51-9DB6-CC34993B523D", "", "AF961217-4223-4F03-9658-11DAB4B24DCD", "Quick Checkin", "Main", "", "", 0, "A5D4FDB1-D022-45B6-8001-7D69C3A2C33C" ); //Block of Type: QuickCheckin
            RockMigrationHelper.AddBlockTypeAttribute( "315A175F-C682-4810-9F33-1BDB93904A4E", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Minimum Phone Number Length", "MinimumPhoneNumberLength", "", "Minimum length for phone number searches (defaults to 4).", 0, @"4", "55638480-CFBF-4228-A534-66C64CCF7812" );
            RockMigrationHelper.AddBlockTypeAttribute( "315A175F-C682-4810-9F33-1BDB93904A4E", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Maximum Phone Number Length", "MaximumPhoneNumberLength", "", "Maximum length for phone number searches (defaults to 10).", 0, @"10", "599916FE-39E4-4103-907E-3EB888CDCEA3" );
            RockMigrationHelper.AddBlockTypeAttribute( "315A175F-C682-4810-9F33-1BDB93904A4E", "9C204CD0-1233-41C5-818A-C5DA439445AA", "Search Regex", "SearchRegex", "", "Regular Expression to run the search input through before sending it to the workflow. Useful for stripping off characters.", 0, @"", "F781126E-9ADB-47E5-B593-30C526333567" );
            RockMigrationHelper.AddBlockTypeAttribute( "315A175F-C682-4810-9F33-1BDB93904A4E", "46A03F59-55D3-4ACE-ADD5-B4642225DD20", "Workflow Type", "WorkflowType", "", "The workflow type to activate for check-in", 0, @"", "3D683472-885B-4FB1-8401-EAB7B0BC4FAF" );
            RockMigrationHelper.AddBlockTypeAttribute( "315A175F-C682-4810-9F33-1BDB93904A4E", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Refresh Interval", "RefreshInterval", "", "How often (seconds) should page automatically query server for new Check-in data", 0, @"10", "6E75589E-429A-4961-9C5D-278A6AE60F80" );
            RockMigrationHelper.AddBlockTypeAttribute( "AF961217-4223-4F03-9658-11DAB4B24DCD", "46A03F59-55D3-4ACE-ADD5-B4642225DD20", "Workflow Type", "WorkflowType", "", "The workflow type to activate for check-in", 0, @"", "107BFB22-2844-443E-B61D-ADD5BEA0C54F" );
            RockMigrationHelper.AddBlockTypeAttribute( "AF961217-4223-4F03-9658-11DAB4B24DCD", "9C204CD0-1233-41C5-818A-C5DA439445AA", "Workflow Activity", "WorkflowActivity", "", "The name of the workflow activity to run on selection.", 1, @"", "9ECBB4F3-9052-4E49-81EA-08DA3F93868F" );
            RockMigrationHelper.AddBlockTypeAttribute( "315A175F-C682-4810-9F33-1BDB93904A4E", "9C204CD0-1233-41C5-818A-C5DA439445AA", "Workflow Activity", "WorkflowActivity", "", "The name of the workflow activity to run on selection.", 1, @"", "C20253EB-1572-43A5-95B5-4B5FD104A6D2" );
            RockMigrationHelper.AddBlockTypeAttribute( "315A175F-C682-4810-9F33-1BDB93904A4E", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Home Page", "HomePage", "", "", 2, @"", "C96C2070-533E-41FB-995B-3EEA512EF63D" );
            RockMigrationHelper.AddBlockTypeAttribute( "AF961217-4223-4F03-9658-11DAB4B24DCD", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Home Page", "HomePage", "", "", 2, @"", "C0F78DCA-BE6D-4BAD-8985-35D0F7027248" );
            RockMigrationHelper.AddBlockTypeAttribute( "AF961217-4223-4F03-9658-11DAB4B24DCD", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Previous Page", "PreviousPage", "", "", 3, @"", "FC3AE63A-E7D1-476A-91BD-9E934B358963" );
            RockMigrationHelper.AddBlockTypeAttribute( "315A175F-C682-4810-9F33-1BDB93904A4E", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Previous Page", "PreviousPage", "", "", 3, @"", "9CA7B05F-FC51-4F32-8795-3A06FBC9E634" );
            RockMigrationHelper.AddBlockTypeAttribute( "315A175F-C682-4810-9F33-1BDB93904A4E", "59D5A94C-94A0-4630-B80A-BB25697D74C7", "Search Type", "SearchType", "", "The type of search to use for check-in (default is phone number).", 4, @"F3F66040-C50F-4D13-9652-780305FFFE23", "F34D5FC1-0E69-4C32-A6BB-4A35EE1AD744" );
            RockMigrationHelper.AddBlockTypeAttribute( "315A175F-C682-4810-9F33-1BDB93904A4E", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Next Page", "NextPage", "", "", 4, @"", "A210618E-FF31-412A-BD26-FFA3BCDDFCE9" );
            RockMigrationHelper.AddBlockTypeAttribute( "AF961217-4223-4F03-9658-11DAB4B24DCD", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Next Page", "NextPage", "", "", 4, @"", "C8266886-8A86-401C-9982-8F675132CFF0" );
            RockMigrationHelper.AddBlockTypeAttribute( "315A175F-C682-4810-9F33-1BDB93904A4E", "1D0D3794-C210-48A8-8C68-3FBEC08A6BA5", "Default Content", "DefaultContent", "", "Default content to display", 12, @"", "9127E939-B8FC-46FC-864A-0FE352A7E77C" );
            RockMigrationHelper.AddBlockAttributeValue( "1CFD1EC1-198C-4F4C-B87D-32DD19648B39", "D46F3099-5700-4CCD-8B6C-F1F306BA02B8", @"f2f82210-0149-4f67-ad28-b618f3158e95" ); // Home Page
            RockMigrationHelper.AddBlockAttributeValue( "1CFD1EC1-198C-4F4C-B87D-32DD19648B39", "36C334EF-E723-4065-9C39-BD5663582751", @"abf839c7-8a7b-4f51-9d33-63aa841ead38" ); // Next Page
            RockMigrationHelper.AddBlockAttributeValue( "1CFD1EC1-198C-4F4C-B87D-32DD19648B39", "7675AE35-1A61-460E-8FF6-B2A5C473F319", @"f2f82210-0149-4f67-ad28-b618f3158e95" ); // Previous Page
            RockMigrationHelper.AddBlockAttributeValue( "1CFD1EC1-198C-4F4C-B87D-32DD19648B39", "992F693A-1019-468C-B7A7-B945A616BAF0", @"False" ); // Enable Location Sharing
            RockMigrationHelper.AddBlockAttributeValue( "1CFD1EC1-198C-4F4C-B87D-32DD19648B39", "C2A1F6A2-A801-4B52-8CFB-9B394CCA2D17", @"20" ); // Time to Cache Kiosk GeoLocation
            RockMigrationHelper.AddBlockAttributeValue( "1CFD1EC1-198C-4F4C-B87D-32DD19648B39", "EBBC10ED-18E4-4E7D-9467-E7C27F12A745", @"True" ); // Allow Manual Setup
            RockMigrationHelper.AddBlockAttributeValue( "1CFD1EC1-198C-4F4C-B87D-32DD19648B39", "50DF7B49-FAF4-45D5-919F-14E589B37666", @"" ); // Workflow Activity
            RockMigrationHelper.AddBlockAttributeValue( "1CFD1EC1-198C-4F4C-B87D-32DD19648B39", "0E252443-86E1-4068-8B32-9943E0974C94", @"False" ); // Enable Kiosk Match By Name
            RockMigrationHelper.AddBlockAttributeValue( "3EEC4622-0BEB-433E-AF0C-01560468AFD5", "F34D5FC1-0E69-4C32-A6BB-4A35EE1AD744", @"f3f66040-c50f-4d13-9652-780305fffe23" ); // Search Type
            RockMigrationHelper.AddBlockAttributeValue( "3EEC4622-0BEB-433E-AF0C-01560468AFD5", "55638480-CFBF-4228-A534-66C64CCF7812", @"4" ); // Minimum Phone Number Length
            RockMigrationHelper.AddBlockAttributeValue( "3EEC4622-0BEB-433E-AF0C-01560468AFD5", "599916FE-39E4-4103-907E-3EB888CDCEA3", @"10" ); // Maximum Phone Number Length
            RockMigrationHelper.AddBlockAttributeValue( "3EEC4622-0BEB-433E-AF0C-01560468AFD5", "F781126E-9ADB-47E5-B593-30C526333567", @"" ); // Search Regex
            RockMigrationHelper.AddBlockAttributeValue( "3EEC4622-0BEB-433E-AF0C-01560468AFD5", "C96C2070-533E-41FB-995B-3EEA512EF63D", @"f2f82210-0149-4f67-ad28-b618f3158e95" ); // Home Page
            RockMigrationHelper.AddBlockAttributeValue( "3EEC4622-0BEB-433E-AF0C-01560468AFD5", "A210618E-FF31-412A-BD26-FFA3BCDDFCE9", @"71959598-be24-4a51-9db6-cc34993b523d" ); // Next Page
            RockMigrationHelper.AddBlockAttributeValue( "3EEC4622-0BEB-433E-AF0C-01560468AFD5", "9CA7B05F-FC51-4F32-8795-3A06FBC9E634", @"f2f82210-0149-4f67-ad28-b618f3158e95" ); // Previous Page
            RockMigrationHelper.AddBlockAttributeValue( "3EEC4622-0BEB-433E-AF0C-01560468AFD5", "3D683472-885B-4FB1-8401-EAB7B0BC4FAF", @"80973ff7-e2c4-4a18-9939-216e347bdc27" ); // Workflow Type
            RockMigrationHelper.AddBlockAttributeValue( "3EEC4622-0BEB-433E-AF0C-01560468AFD5", "C20253EB-1572-43A5-95B5-4B5FD104A6D2", @"Family Search" ); // Workflow Activity
            RockMigrationHelper.AddBlockAttributeValue( "3EEC4622-0BEB-433E-AF0C-01560468AFD5", "6E75589E-429A-4961-9C5D-278A6AE60F80", @"10" ); // Refresh Interval
            RockMigrationHelper.AddBlockAttributeValue( "3EEC4622-0BEB-433E-AF0C-01560468AFD5", "9127E939-B8FC-46FC-864A-0FE352A7E77C", @"<h1>Check In Here</h1>
Welcome to Southeast Christian Church. We value your children's safety,
so we require all children to check in at one of our kiosks. If you need help or have any question please see one of our helpful volunteers.
" ); // Default Content
            RockMigrationHelper.AddBlockAttributeValue( "A5D4FDB1-D022-45B6-8001-7D69C3A2C33C", "C0F78DCA-BE6D-4BAD-8985-35D0F7027248", @"abf839c7-8a7b-4f51-9d33-63aa841ead38" ); // Home Page
            RockMigrationHelper.AddBlockAttributeValue( "A5D4FDB1-D022-45B6-8001-7D69C3A2C33C", "C8266886-8A86-401C-9982-8F675132CFF0", @"abf839c7-8a7b-4f51-9d33-63aa841ead38" ); // Next Page
            RockMigrationHelper.AddBlockAttributeValue( "A5D4FDB1-D022-45B6-8001-7D69C3A2C33C", "FC3AE63A-E7D1-476A-91BD-9E934B358963", @"abf839c7-8a7b-4f51-9d33-63aa841ead38" ); // Previous Page
            RockMigrationHelper.AddBlockAttributeValue( "A5D4FDB1-D022-45B6-8001-7D69C3A2C33C", "107BFB22-2844-443E-B61D-ADD5BEA0C54F", @"80973ff7-e2c4-4a18-9939-216e347bdc27" ); // Workflow Type
            RockMigrationHelper.AddBlockAttributeValue( "A5D4FDB1-D022-45B6-8001-7D69C3A2C33C", "9ECBB4F3-9052-4E49-81EA-08DA3F93868F", @"Person Search" ); // Workflow Activity
        }
        public override void Down()
        {
            RockMigrationHelper.DeleteAttribute( "9BD6125E-8DBA-4EB2-8FFE-F449F9DF28AF" );
            RockMigrationHelper.DeleteAttribute( "9127E939-B8FC-46FC-864A-0FE352A7E77C" );
            RockMigrationHelper.DeleteAttribute( "6E75589E-429A-4961-9C5D-278A6AE60F80" );
            RockMigrationHelper.DeleteAttribute( "9ECBB4F3-9052-4E49-81EA-08DA3F93868F" );
            RockMigrationHelper.DeleteAttribute( "107BFB22-2844-443E-B61D-ADD5BEA0C54F" );
            RockMigrationHelper.DeleteAttribute( "FC3AE63A-E7D1-476A-91BD-9E934B358963" );
            RockMigrationHelper.DeleteAttribute( "C8266886-8A86-401C-9982-8F675132CFF0" );
            RockMigrationHelper.DeleteAttribute( "C0F78DCA-BE6D-4BAD-8985-35D0F7027248" );
            RockMigrationHelper.DeleteAttribute( "C20253EB-1572-43A5-95B5-4B5FD104A6D2" );
            RockMigrationHelper.DeleteAttribute( "3D683472-885B-4FB1-8401-EAB7B0BC4FAF" );
            RockMigrationHelper.DeleteAttribute( "9CA7B05F-FC51-4F32-8795-3A06FBC9E634" );
            RockMigrationHelper.DeleteAttribute( "A210618E-FF31-412A-BD26-FFA3BCDDFCE9" );
            RockMigrationHelper.DeleteAttribute( "C96C2070-533E-41FB-995B-3EEA512EF63D" );
            RockMigrationHelper.DeleteAttribute( "F781126E-9ADB-47E5-B593-30C526333567" );
            RockMigrationHelper.DeleteAttribute( "599916FE-39E4-4103-907E-3EB888CDCEA3" );
            RockMigrationHelper.DeleteAttribute( "55638480-CFBF-4228-A534-66C64CCF7812" );
            RockMigrationHelper.DeleteAttribute( "F34D5FC1-0E69-4C32-A6BB-4A35EE1AD744" );
            RockMigrationHelper.DeleteAttribute( "0E252443-86E1-4068-8B32-9943E0974C94" );
            RockMigrationHelper.DeleteAttribute( "50DF7B49-FAF4-45D5-919F-14E589B37666" );
            RockMigrationHelper.DeleteAttribute( "EBBC10ED-18E4-4E7D-9467-E7C27F12A745" );
            RockMigrationHelper.DeleteAttribute( "C2A1F6A2-A801-4B52-8CFB-9B394CCA2D17" );
            RockMigrationHelper.DeleteAttribute( "992F693A-1019-468C-B7A7-B945A616BAF0" );
            RockMigrationHelper.DeleteAttribute( "7675AE35-1A61-460E-8FF6-B2A5C473F319" );
            RockMigrationHelper.DeleteAttribute( "36C334EF-E723-4065-9C39-BD5663582751" );
            RockMigrationHelper.DeleteAttribute( "D46F3099-5700-4CCD-8B6C-F1F306BA02B8" );
            RockMigrationHelper.DeleteAttribute( "162A2B82-A71F-4B29-970A-047266FE696D" );
            RockMigrationHelper.DeleteBlock( "A5D4FDB1-D022-45B6-8001-7D69C3A2C33C" );
            RockMigrationHelper.DeleteBlock( "3EEC4622-0BEB-433E-AF0C-01560468AFD5" );
            RockMigrationHelper.DeleteBlock( "1CFD1EC1-198C-4F4C-B87D-32DD19648B39" );
            RockMigrationHelper.DeletePage( "4159825D-B2CE-4BC1-A5DB-917020059E89" ); //  Page: Family Checkin Home Page
            RockMigrationHelper.DeletePage( "F2F82210-0149-4F67-AD28-B618F3158E95" ); //  Page: Admin
            RockMigrationHelper.DeletePage( "ABF839C7-8A7B-4F51-9D33-63AA841EAD38" ); //  Page: QuickSearch
            RockMigrationHelper.DeletePage( "71959598-BE24-4A51-9DB6-CC34993B523D" ); //  Page: QuickCheckin
            RockMigrationHelper.DeleteLayout( "8A87FF45-C694-43B5-9494-0B2D6A71FFA1" ); //  Layout: Checkin, Site: Family Checkin
            RockMigrationHelper.DeleteSite( "FB2A03C3-D5E9-4DBA-A56F-90311DD8BF13" );
        }
    }
}

