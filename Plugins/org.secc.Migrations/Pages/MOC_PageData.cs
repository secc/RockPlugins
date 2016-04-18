using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rock.Plugin;

namespace org.secc.Migrations
{
    [MigrationNumber( 1, "1.2.0" )]
    class MOC_PageData : Migration
    {
        public override void Up()
        {
            // Page: Minister On Call Home Page
            RockMigrationHelper.AddPage( "", "5D0446D1-F0B7-4FF2-B1A3-CCA85A9C9A1C", "Minister On Call Home Page", "", "9384126F-BC6F-444F-A651-CE6EC91B6435", "" ); // Site:Minister On Call
            // Page: MOC / Before your week begins.
            RockMigrationHelper.AddPage( "9384126F-BC6F-444F-A651-CE6EC91B6435", "5D0446D1-F0B7-4FF2-B1A3-CCA85A9C9A1C", "MOC / Before your week begins.", "", "6B7F1BC8-5EF2-4215-8428-8730D6211F13", "" ); // Site:Minister On Call
            // Page: Pastoral Care and Contacts
            RockMigrationHelper.AddPage( "9384126F-BC6F-444F-A651-CE6EC91B6435", "5D0446D1-F0B7-4FF2-B1A3-CCA85A9C9A1C", "Pastoral Care and Contacts", "", "69EB92FC-474A-4729-96A7-A1DF51E30F06", "" ); // Site:Minister On Call
            // Page: No Help List
            RockMigrationHelper.AddPage( "9384126F-BC6F-444F-A651-CE6EC91B6435", "5D0446D1-F0B7-4FF2-B1A3-CCA85A9C9A1C", "No Help List", "", "F0942A07-9806-46C1-A9D8-91418D539EC3", "" ); // Site:Minister On Call
            // Page: Call Intake Form
            RockMigrationHelper.AddPage( "9384126F-BC6F-444F-A651-CE6EC91B6435", "5D0446D1-F0B7-4FF2-B1A3-CCA85A9C9A1C", "Call Intake Form", "", "32679C05-48C7-49DE-A3E3-3F83088DC8C7", "" ); // Site:Minister On Call
            // Page: Calendar
            RockMigrationHelper.AddPage( "9384126F-BC6F-444F-A651-CE6EC91B6435", "5D0446D1-F0B7-4FF2-B1A3-CCA85A9C9A1C", "Calendar", "", "0DAC0D0C-18C5-4A45-A53C-8885419CFD6F", "" ); // Site:Minister On Call
            RockMigrationHelper.AddPageRoute( "9384126F-BC6F-444F-A651-CE6EC91B6435", "moc" );
            RockMigrationHelper.AddPageRoute( "6B7F1BC8-5EF2-4215-8428-8730D6211F13", "moc/beforeyoubegin" );
            RockMigrationHelper.AddPageRoute( "69EB92FC-474A-4729-96A7-A1DF51E30F06", "moc/careandcontacts" );
            RockMigrationHelper.AddPageRoute( "32679C05-48C7-49DE-A3E3-3F83088DC8C7", "moc/callintakeform" );
            RockMigrationHelper.AddPageRoute( "F0942A07-9806-46C1-A9D8-91418D539EC3", "moc/nohelp" );
            RockMigrationHelper.AddPageRoute( "0DAC0D0C-18C5-4A45-A53C-8885419CFD6F", "moc/calendar" );
            RockMigrationHelper.UpdateBlockType( "HTML Content", "Adds an editable HTML fragment to the page.", "~/Blocks/Cms/HtmlContentDetail.ascx", "CMS", "19B61D65-37E3-459F-A44F-DEF0089118A3" );
            RockMigrationHelper.UpdateBlockType( "Dynamic Data", "Block to display dynamic report, html, xml, or transformed xml based on a SQL query or stored procedure.", "~/Blocks/Reporting/DynamicData.ascx", "Reporting", "E31E02E9-73F6-4B3E-98BA-E0E4F86CA126" );
            RockMigrationHelper.UpdateBlockType( "Workflow Entry", "Used to enter information for a workflow form entry action.", "~/Blocks/WorkFlow/WorkflowEntry.ascx", "WorkFlow", "A8BD05C8-6F89-4628-845B-059E686F089A" );
            RockMigrationHelper.UpdateBlockType( "Content Channel View", "Block to display dynamic content channel items.", "~/Blocks/Cms/ContentChannelView.ascx", "CMS", "143A2345-3E26-4ED0-A2FE-42AAF11B4C0F" );
            RockMigrationHelper.AddBlock( "9384126F-BC6F-444F-A651-CE6EC91B6435", "", "19B61D65-37E3-459F-A44F-DEF0089118A3", "Header Type Thing", "Header", "", "", 0, "367DCF5F-4DA2-4670-9125-D7D03B343744" );

            RockMigrationHelper.AddBlock( "9384126F-BC6F-444F-A651-CE6EC91B6435", "", "19B61D65-37E3-459F-A44F-DEF0089118A3", "HTML Content", "Main", "", "", 0, "F5343899-8EB4-4967-8889-43F2F53282F1" );

            RockMigrationHelper.AddBlock( "6B7F1BC8-5EF2-4215-8428-8730D6211F13", "", "143A2345-3E26-4ED0-A2FE-42AAF11B4C0F", "Content Channel View", "Main", "", "", 0, "B54B058B-E1A3-458C-83E9-31B8116607CC" );

            RockMigrationHelper.AddBlock( "6B7F1BC8-5EF2-4215-8428-8730D6211F13", "", "19B61D65-37E3-459F-A44F-DEF0089118A3", "Header", "Header", "", "", 0, "B38C4AD5-90FD-4D46-B384-56642FCCD10B" );

            RockMigrationHelper.AddBlock( "69EB92FC-474A-4729-96A7-A1DF51E30F06", "", "143A2345-3E26-4ED0-A2FE-42AAF11B4C0F", "Content Channel View", "Main", "", "", 0, "E54EF292-BBAD-4919-92FF-666A7816FE66" );

            RockMigrationHelper.AddBlock( "69EB92FC-474A-4729-96A7-A1DF51E30F06", "", "19B61D65-37E3-459F-A44F-DEF0089118A3", "Header", "Header", "", "", 0, "F7A85AE3-9D0E-4B8D-BB6E-D2E571F3E2B8" );

            RockMigrationHelper.AddBlock( "F0942A07-9806-46C1-A9D8-91418D539EC3", "", "143A2345-3E26-4ED0-A2FE-42AAF11B4C0F", "Content Channel View", "Main", "", "", 0, "8ABA07EE-AB9F-4261-BE43-98178A38E742" );

            RockMigrationHelper.AddBlock( "F0942A07-9806-46C1-A9D8-91418D539EC3", "", "19B61D65-37E3-459F-A44F-DEF0089118A3", "Header", "Header", "", "", 0, "BAA0FDD1-67FC-40A0-B0E5-F595BAC4EAF7" );

            RockMigrationHelper.AddBlock( "32679C05-48C7-49DE-A3E3-3F83088DC8C7", "", "A8BD05C8-6F89-4628-845B-059E686F089A", "Workflow Entry", "Main", "", "", 0, "7994383D-4B44-439B-8669-9658F56FF8BB" );

            RockMigrationHelper.AddBlock( "32679C05-48C7-49DE-A3E3-3F83088DC8C7", "", "19B61D65-37E3-459F-A44F-DEF0089118A3", "Header", "Header", "", "", 0, "326B80F0-9E27-4C00-BFA7-0E11B19F7C3F" );

            RockMigrationHelper.AddBlock( "0DAC0D0C-18C5-4A45-A53C-8885419CFD6F", "", "19B61D65-37E3-459F-A44F-DEF0089118A3", "Header", "Header", "", "", 0, "C62F26A0-B918-4241-A89B-0CC335E89046" );

            RockMigrationHelper.AddBlock( "0DAC0D0C-18C5-4A45-A53C-8885419CFD6F", "", "E31E02E9-73F6-4B3E-98BA-E0E4F86CA126", "Dynamic Data", "Main", "", "", 1, "A58CABFA-2C1B-4D76-A659-62F26C4A51A0" );

            RockMigrationHelper.AddBlockTypeAttribute( "E31E02E9-73F6-4B3E-98BA-E0E4F86CA126", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Update Page", "UpdatePage", "", "If True, provides fields for updating the parent page's Name and Description", 0, @"True", "230EDFE8-33CA-478D-8C9A-572323AF3466" );

            RockMigrationHelper.AddBlockTypeAttribute( "E31E02E9-73F6-4B3E-98BA-E0E4F86CA126", "9C204CD0-1233-41C5-818A-C5DA439445AA", "Query Params", "QueryParams", "", "Parameters to pass to query", 0, @"", "B0EC41B9-37C0-48FD-8E4E-37A8CA305012" );

            RockMigrationHelper.AddBlockTypeAttribute( "E31E02E9-73F6-4B3E-98BA-E0E4F86CA126", "9C204CD0-1233-41C5-818A-C5DA439445AA", "Columns", "Columns", "", "The columns to hide or show", 0, @"", "90B0E6AF-B2F4-4397-953B-737A40D4023B" );

            RockMigrationHelper.AddBlockTypeAttribute( "E31E02E9-73F6-4B3E-98BA-E0E4F86CA126", "1D0D3794-C210-48A8-8C68-3FBEC08A6BA5", "Query", "Query", "", "The query to execute", 0, @"", "71C8BA4E-8EF2-416B-BFE9-D1D88D9AA356" );

            RockMigrationHelper.AddBlockTypeAttribute( "E31E02E9-73F6-4B3E-98BA-E0E4F86CA126", "9C204CD0-1233-41C5-818A-C5DA439445AA", "Url Mask", "UrlMask", "", "The Url to redirect to when a row is clicked", 0, @"", "B9163A35-E09C-466D-8A2D-4ED81DF0114C" );

            RockMigrationHelper.AddBlockTypeAttribute( "E31E02E9-73F6-4B3E-98BA-E0E4F86CA126", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Show Columns", "ShowColumns", "", "Should the 'Columns' specified below be the only ones shown (vs. the only ones hidden)", 0, @"False", "202A82BF-7772-481C-8419-600012607972" );

            RockMigrationHelper.AddBlockTypeAttribute( "E31E02E9-73F6-4B3E-98BA-E0E4F86CA126", "9C204CD0-1233-41C5-818A-C5DA439445AA", "Merge Fields", "MergeFields", "", "Any fields to make available as merge fields for any new communications", 0, @"", "8EB882CE-5BB1-4844-9C28-10190903EECD" );

            RockMigrationHelper.AddBlockTypeAttribute( "E31E02E9-73F6-4B3E-98BA-E0E4F86CA126", "1D0D3794-C210-48A8-8C68-3FBEC08A6BA5", "Formatted Output", "FormattedOutput", "", "Optional formatting to apply to the returned results.  If left blank, a grid will be displayed. Example: {% for row in rows %} {{ row.FirstName }}<br/> {% endfor %}", 0, @"", "6A233402-446C-47E9-94A5-6A247C29BC21" );

            RockMigrationHelper.AddBlockTypeAttribute( "E31E02E9-73F6-4B3E-98BA-E0E4F86CA126", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Person Report", "PersonReport", "", "Is this report a list of people?", 0, @"False", "8104CE53-FDB3-4E9F-B8E7-FD9E06E7551C" );

            RockMigrationHelper.AddBlockTypeAttribute( "E31E02E9-73F6-4B3E-98BA-E0E4F86CA126", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Use Dynamic Filter Controls", "UseDynamicFilterControls", "", "Show filtering controls that are dynamically generated to match the columns of the dynamic data.", 0, @"False", "1F4F0763-4ED7-4C62-9347-6029158E78AD" );

            RockMigrationHelper.AddBlockTypeAttribute( "E31E02E9-73F6-4B3E-98BA-E0E4F86CA126", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Stored Procedure", "StoredProcedure", "", "Is the query a stored procedure?", 0, @"False", "3327A07D-3A4E-4057-824D-C3EA7A5BD428" );

            RockMigrationHelper.AddBlockTypeAttribute( "E31E02E9-73F6-4B3E-98BA-E0E4F86CA126", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Show Merge Template", "ShowMergeTemplate", "", "Show Export to Merge Template button in grid footer?", 0, @"True", "A0B54268-508E-4B87-9201-C2A2E52418B1" );

            RockMigrationHelper.AddBlockTypeAttribute( "E31E02E9-73F6-4B3E-98BA-E0E4F86CA126", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Show Communicate", "ShowCommunicate", "", "Show Communicate button in grid footer?", 0, @"True", "C61A93CC-6032-4A52-B7D0-40DA906496E2" );

            RockMigrationHelper.AddBlockTypeAttribute( "E31E02E9-73F6-4B3E-98BA-E0E4F86CA126", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Show Merge Person", "ShowMergePerson", "", "Show Merge Person button in grid footer?", 0, @"True", "94584595-ECA5-4BFB-84A6-13C2D005DC49" );

            RockMigrationHelper.AddBlockTypeAttribute( "E31E02E9-73F6-4B3E-98BA-E0E4F86CA126", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Show Excel Export", "ShowExcelExport", "", "Show Export to Excel button in grid footer?", 0, @"True", "6D7C08E3-549A-46C8-B33A-35BC16F2CEB5" );

            RockMigrationHelper.AddBlockTypeAttribute( "E31E02E9-73F6-4B3E-98BA-E0E4F86CA126", "1D0D3794-C210-48A8-8C68-3FBEC08A6BA5", "Page Title Lava", "PageTitleLava", "", "Optional Lava for setting the page title. If nothing is provided then the page's title will be used.", 0, @"", "363DDE65-CA86-4B84-A79C-9FFCE3E9B15F" );

            RockMigrationHelper.AddBlockTypeAttribute( "E31E02E9-73F6-4B3E-98BA-E0E4F86CA126", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Paneled Grid", "PaneledGrid", "", "Add the 'grid-panel' class to the grid to allow it to fit nicely in a block.", 0, @"False", "E2A60EB3-64BA-45DB-B425-11402B812C90" );

            RockMigrationHelper.AddBlockTypeAttribute( "E31E02E9-73F6-4B3E-98BA-E0E4F86CA126", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Show Bulk Update", "ShowBulkUpdate", "", "Show Bulk Update button in grid footer?", 0, @"True", "F1D18DB0-AE9C-469A-A553-BF391F6DDFD0" );

            RockMigrationHelper.AddBlockTypeAttribute( "E31E02E9-73F6-4B3E-98BA-E0E4F86CA126", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Timeout", "Timeout", "", "The amount of time in xxx to allow the query to run before timing out.", 0, @"30", "50204518-AB90-4A6C-8D72-2FA554C4841F" );

            RockMigrationHelper.AddBlockTypeAttribute( "A8BD05C8-6F89-4628-845B-059E686F089A", "46A03F59-55D3-4ACE-ADD5-B4642225DD20", "Workflow Type", "WorkflowType", "", "Type of workflow to start.", 0, @"", "2F1D98C4-A8EF-4680-9F64-11BFC28D5597" );

            RockMigrationHelper.AddBlockTypeAttribute( "143A2345-3E26-4ED0-A2FE-42AAF11B4C0F", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Filter Id", "FilterId", "", "The data filter that is used to filter items", 0, @"0", "618EFBDA-941D-4F60-9AA8-54955B7A03A2" );

            RockMigrationHelper.AddBlockTypeAttribute( "143A2345-3E26-4ED0-A2FE-42AAF11B4C0F", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Query Parameter Filtering", "QueryParameterFiltering", "", "Determines if block should evaluate the query string parameters for additional filter criteria.", 0, @"False", "AA9CD867-FA21-43C2-822B-CAC06E1F18B8" );

            RockMigrationHelper.AddBlockTypeAttribute( "143A2345-3E26-4ED0-A2FE-42AAF11B4C0F", "9C204CD0-1233-41C5-818A-C5DA439445AA", "Order", "Order", "", "The specifics of how items should be ordered. This value is set through configuration and should not be modified here.", 0, @"", "07ED420E-749C-4938-ADFD-1DDEA6B63014" );

            RockMigrationHelper.AddBlockTypeAttribute( "143A2345-3E26-4ED0-A2FE-42AAF11B4C0F", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Merge Content", "MergeContent", "", "Should the content data and attribute values be merged using the liquid template engine.", 0, @"False", "20BE4E0A-E84C-4AA1-9368-9732A834E1DE" );

            RockMigrationHelper.AddBlockTypeAttribute( "143A2345-3E26-4ED0-A2FE-42AAF11B4C0F", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Set Page Title", "SetPageTitle", "", "Determines if the block should set the page title with the channel name or content item.", 0, @"False", "97161D67-EF24-4F21-9E6A-74B696DD33DE" );

            RockMigrationHelper.AddBlockTypeAttribute( "143A2345-3E26-4ED0-A2FE-42AAF11B4C0F", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Rss Autodiscover", "RssAutodiscover", "", "Determines if a RSS autodiscover link should be added to the page head.", 0, @"False", "ABA69AFC-2C5E-4CF6-A156-EDA2752CCC86" );

            RockMigrationHelper.AddBlockTypeAttribute( "143A2345-3E26-4ED0-A2FE-42AAF11B4C0F", "9C204CD0-1233-41C5-818A-C5DA439445AA", "Meta Description Attribute", "MetaDescriptionAttribute", "", "Attribute to use for storing the description attribute.", 0, @"", "E01AE3A7-2607-4DA5-AC98-3A368C900B64" );

            RockMigrationHelper.AddBlockTypeAttribute( "143A2345-3E26-4ED0-A2FE-42AAF11B4C0F", "9C204CD0-1233-41C5-818A-C5DA439445AA", "Meta Image Attribute", "MetaImageAttribute", "", "Attribute to use for storing the image attribute.", 0, @"", "A3510474-86E5-4AD2-BD4C-3C89E85795F5" );

            RockMigrationHelper.AddBlockTypeAttribute( "143A2345-3E26-4ED0-A2FE-42AAF11B4C0F", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Count", "Count", "", "The maximum number of items to display.", 0, @"5", "25A501FC-E269-40B8-9904-E20FA7A1ADB6" );

            RockMigrationHelper.AddBlockTypeAttribute( "143A2345-3E26-4ED0-A2FE-42AAF11B4C0F", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Detail Page", "DetailPage", "", "The page to navigate to for details.", 0, @"", "2D7E6F55-B25E-4EA2-8F7E-F7E138E39E21" );

            RockMigrationHelper.AddBlockTypeAttribute( "143A2345-3E26-4ED0-A2FE-42AAF11B4C0F", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Enable Debug", "EnableDebug", "", "Enabling debug will display the fields of the first 5 items to help show you wants available for your liquid.", 0, @"False", "72F4232B-8D2A-4823-B9F1-ED68F182C1A4" );

            RockMigrationHelper.AddBlockTypeAttribute( "143A2345-3E26-4ED0-A2FE-42AAF11B4C0F", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Cache Duration", "CacheDuration", "", "Number of seconds to cache the content.", 0, @"3600", "773BEFDD-EEBA-486C-98E6-AFD0D4156E22" );

            RockMigrationHelper.AddBlockTypeAttribute( "143A2345-3E26-4ED0-A2FE-42AAF11B4C0F", "D835A0EC-C8DB-483A-A37C-E8FB6E956C3D", "Channel", "Channel", "", "The channel to display items from.", 0, @"", "34EACB0F-DBC4-4F18-85C9-0D3EDFDF46BE" );

            RockMigrationHelper.AddBlockTypeAttribute( "143A2345-3E26-4ED0-A2FE-42AAF11B4C0F", "BD0D9B57-2A41-4490-89FF-F01DAB7D4904", "Status", "Status", "", "Include items with the following status.", 0, @"2", "DA1DEE5D-BCEF-4AA4-A9D9-EFD4DD64462B" );

            RockMigrationHelper.AddBlockTypeAttribute( "143A2345-3E26-4ED0-A2FE-42AAF11B4C0F", "1D0D3794-C210-48A8-8C68-3FBEC08A6BA5", "Template", "Template", "", "The template to use when formatting the list of items.", 0, @"", "8026FEA1-35C1-41CF-9D09-E8B1DB6CBDA8" );

            RockMigrationHelper.AddBlockTypeAttribute( "19B61D65-37E3-459F-A44F-DEF0089118A3", "3549BAB6-FE1B-4333-AFC4-C5ACA01BB8EB", "Entity Type", "ContextEntityType", "", "The type of entity that will provide context for this block", 0, @"", "6783D47D-92F9-4F48-93C0-16111D675A0F" );

            RockMigrationHelper.AddBlockTypeAttribute( "19B61D65-37E3-459F-A44F-DEF0089118A3", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Use Code Editor", "UseCodeEditor", "", "Use the code editor instead of the WYSIWYG editor", 0, @"True", "0673E015-F8DD-4A52-B380-C758011331B2" );

            RockMigrationHelper.AddBlockTypeAttribute( "19B61D65-37E3-459F-A44F-DEF0089118A3", "9C204CD0-1233-41C5-818A-C5DA439445AA", "Document Root Folder", "DocumentRootFolder", "", "The folder to use as the root when browsing or uploading documents.", 1, @"~/Content", "3BDB8AED-32C5-4879-B1CB-8FC7C8336534" );

            RockMigrationHelper.AddBlockTypeAttribute( "19B61D65-37E3-459F-A44F-DEF0089118A3", "9C204CD0-1233-41C5-818A-C5DA439445AA", "Image Root Folder", "ImageRootFolder", "", "The folder to use as the root when browsing or uploading images.", 2, @"~/Content", "26F3AFC6-C05B-44A4-8593-AFE1D9969B0E" );

            RockMigrationHelper.AddBlockTypeAttribute( "19B61D65-37E3-459F-A44F-DEF0089118A3", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "User Specific Folders", "UserSpecificFolders", "", "Should the root folders be specific to current user?", 3, @"False", "9D3E4ED9-1BEF-4547-B6B0-CE29FE3835EE" );

            RockMigrationHelper.AddBlockTypeAttribute( "19B61D65-37E3-459F-A44F-DEF0089118A3", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Cache Duration", "CacheDuration", "", "Number of seconds to cache the content.", 4, @"3600", "4DFDB295-6D0F-40A1-BEF9-7B70C56F66C4" );

            RockMigrationHelper.AddBlockTypeAttribute( "19B61D65-37E3-459F-A44F-DEF0089118A3", "9C204CD0-1233-41C5-818A-C5DA439445AA", "Context Parameter", "ContextParameter", "", "Query string parameter to use for 'personalizing' content based on unique values.", 5, @"", "3FFC512D-A576-4289-B648-905FD7A64ABB" );

            RockMigrationHelper.AddBlockTypeAttribute( "19B61D65-37E3-459F-A44F-DEF0089118A3", "9C204CD0-1233-41C5-818A-C5DA439445AA", "Context Name", "ContextName", "", "Name to use to further 'personalize' content.  Blocks with the same name, and referenced with the same context parameter will share html values.", 6, @"", "466993F7-D838-447A-97E7-8BBDA6A57289" );

            RockMigrationHelper.AddBlockTypeAttribute( "19B61D65-37E3-459F-A44F-DEF0089118A3", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Enable Versioning", "SupportVersions", "", "If checked, previous versions of the content will be preserved. Versioning is required if you want to require approval.", 7, @"False", "7C1CE199-86CF-4EAE-8AB3-848416A72C58" );

            RockMigrationHelper.AddBlockTypeAttribute( "19B61D65-37E3-459F-A44F-DEF0089118A3", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Require Approval", "RequireApproval", "", "Require that content be approved?", 8, @"False", "EC2B701B-4C1D-4F3F-9C77-A73C75D7FF7A" );

            RockMigrationHelper.AddBlockTypeAttribute( "19B61D65-37E3-459F-A44F-DEF0089118A3", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Enable Debug", "EnableDebug", "", "Show lava merge fields.", 9, @"False", "48FF43A9-8E12-4768-80A9-88FBB81F11D8" );

            RockMigrationHelper.AddBlockAttributeValue( "367DCF5F-4DA2-4670-9125-D7D03B343744", "2D3317AE-00CE-47E9-92A2-D28DDE72DBB2", @"False" ); // Is Link

            RockMigrationHelper.AddBlockAttributeValue( "F5343899-8EB4-4967-8889-43F2F53282F1", "4DFDB295-6D0F-40A1-BEF9-7B70C56F66C4", @"3600" ); // Cache Duration

            RockMigrationHelper.AddBlockAttributeValue( "F5343899-8EB4-4967-8889-43F2F53282F1", "EC2B701B-4C1D-4F3F-9C77-A73C75D7FF7A", @"False" ); // Require Approval

            RockMigrationHelper.AddBlockAttributeValue( "F5343899-8EB4-4967-8889-43F2F53282F1", "7C1CE199-86CF-4EAE-8AB3-848416A72C58", @"False" ); // Enable Versioning

            RockMigrationHelper.AddBlockAttributeValue( "F5343899-8EB4-4967-8889-43F2F53282F1", "3FFC512D-A576-4289-B648-905FD7A64ABB", @"" ); // Context Parameter

            RockMigrationHelper.AddBlockAttributeValue( "F5343899-8EB4-4967-8889-43F2F53282F1", "466993F7-D838-447A-97E7-8BBDA6A57289", @"" ); // Context Name

            RockMigrationHelper.AddBlockAttributeValue( "F5343899-8EB4-4967-8889-43F2F53282F1", "0673E015-F8DD-4A52-B380-C758011331B2", @"True" ); // Use Code Editor

            RockMigrationHelper.AddBlockAttributeValue( "F5343899-8EB4-4967-8889-43F2F53282F1", "26F3AFC6-C05B-44A4-8593-AFE1D9969B0E", @"~/Content" ); // Image Root Folder

            RockMigrationHelper.AddBlockAttributeValue( "F5343899-8EB4-4967-8889-43F2F53282F1", "9D3E4ED9-1BEF-4547-B6B0-CE29FE3835EE", @"False" ); // User Specific Folders

            RockMigrationHelper.AddBlockAttributeValue( "F5343899-8EB4-4967-8889-43F2F53282F1", "3BDB8AED-32C5-4879-B1CB-8FC7C8336534", @"~/Content" ); // Document Root Folder

            RockMigrationHelper.AddBlockAttributeValue( "F5343899-8EB4-4967-8889-43F2F53282F1", "48FF43A9-8E12-4768-80A9-88FBB81F11D8", @"False" ); // Enable Debug

            RockMigrationHelper.AddBlockAttributeValue( "F5343899-8EB4-4967-8889-43F2F53282F1", "2D3317AE-00CE-47E9-92A2-D28DDE72DBB2", @"True" ); // Is Link

            RockMigrationHelper.AddBlockAttributeValue( "B54B058B-E1A3-458C-83E9-31B8116607CC", "25A501FC-E269-40B8-9904-E20FA7A1ADB6", @"0" ); // Count

            RockMigrationHelper.AddBlockAttributeValue( "B54B058B-E1A3-458C-83E9-31B8116607CC", "72F4232B-8D2A-4823-B9F1-ED68F182C1A4", @"False" ); // Enable Debug

            RockMigrationHelper.AddBlockAttributeValue( "B54B058B-E1A3-458C-83E9-31B8116607CC", "773BEFDD-EEBA-486C-98E6-AFD0D4156E22", @"3600" ); // Cache Duration

            RockMigrationHelper.AddBlockAttributeValue( "B54B058B-E1A3-458C-83E9-31B8116607CC", "34EACB0F-DBC4-4F18-85C9-0D3EDFDF46BE", @"da133c99-c1ee-47ea-b359-372208855b64" ); // Channel

            RockMigrationHelper.AddBlockAttributeValue( "B54B058B-E1A3-458C-83E9-31B8116607CC", "DA1DEE5D-BCEF-4AA4-A9D9-EFD4DD64462B", @"2" ); // Status

            RockMigrationHelper.AddBlockAttributeValue( "B54B058B-E1A3-458C-83E9-31B8116607CC", "8026FEA1-35C1-41CF-9D09-E8B1DB6CBDA8", @"{%assign isFirst = true%}

<ul class=""nav nav-tabs"">
    {% for item in Items %}
        {%if isFirst%}
            <li class=active><a data-toggle=""pill"" href=""#c{{ item.Id }}"" class=""tab-pane"">
                {{item.TabName}}
            </a></li>
            {%assign isFirst=false%}
        {%else%}
            <li><a data-toggle=""pill"" href=""#c{{ item.Id }}"" class=""tab-pane"">
                {{item.TabName}}
            </a></li>
        {%endif%}
    {% endfor %}
</ul>

{%assign isFirst=true%}
<div class=""tab-content"">
    {% for item in Items %}
        {%if isFirst%}
            <div id=""c{{ item.Id }}"" class=""tab-pane active"">
                <h3>{{item.Title}}</h3>
                {{item.Content}}
            </div>
            {%assign isFirst=false%}
        {%else%}
            <div id=""c{{ item.Id }}"" class=""tab-pane"">
                <h3>{{item.Title}}</h3>
                {{item.Content}}
            </div>
        {%endif%}
    {% endfor %}
</div>" ); // Template

            RockMigrationHelper.AddBlockAttributeValue( "B54B058B-E1A3-458C-83E9-31B8116607CC", "618EFBDA-941D-4F60-9AA8-54955B7A03A2", @"173" ); // Filter Id

            RockMigrationHelper.AddBlockAttributeValue( "B54B058B-E1A3-458C-83E9-31B8116607CC", "AA9CD867-FA21-43C2-822B-CAC06E1F18B8", @"False" ); // Query Parameter Filtering

            RockMigrationHelper.AddBlockAttributeValue( "B54B058B-E1A3-458C-83E9-31B8116607CC", "07ED420E-749C-4938-ADFD-1DDEA6B63014", @"Priority^0|" ); // Order

            RockMigrationHelper.AddBlockAttributeValue( "B54B058B-E1A3-458C-83E9-31B8116607CC", "20BE4E0A-E84C-4AA1-9368-9732A834E1DE", @"False" ); // Merge Content

            RockMigrationHelper.AddBlockAttributeValue( "B54B058B-E1A3-458C-83E9-31B8116607CC", "97161D67-EF24-4F21-9E6A-74B696DD33DE", @"False" ); // Set Page Title

            RockMigrationHelper.AddBlockAttributeValue( "B54B058B-E1A3-458C-83E9-31B8116607CC", "ABA69AFC-2C5E-4CF6-A156-EDA2752CCC86", @"False" ); // Rss Autodiscover

            RockMigrationHelper.AddBlockAttributeValue( "B54B058B-E1A3-458C-83E9-31B8116607CC", "E01AE3A7-2607-4DA5-AC98-3A368C900B64", @"" ); // Meta Description Attribute

            RockMigrationHelper.AddBlockAttributeValue( "B54B058B-E1A3-458C-83E9-31B8116607CC", "A3510474-86E5-4AD2-BD4C-3C89E85795F5", @"" ); // Meta Image Attribute

            RockMigrationHelper.AddBlockAttributeValue( "8ABA07EE-AB9F-4261-BE43-98178A38E742", "25A501FC-E269-40B8-9904-E20FA7A1ADB6", @"0" ); // Count

            RockMigrationHelper.AddBlockAttributeValue( "8ABA07EE-AB9F-4261-BE43-98178A38E742", "72F4232B-8D2A-4823-B9F1-ED68F182C1A4", @"False" ); // Enable Debug

            RockMigrationHelper.AddBlockAttributeValue( "8ABA07EE-AB9F-4261-BE43-98178A38E742", "773BEFDD-EEBA-486C-98E6-AFD0D4156E22", @"3600" ); // Cache Duration

            RockMigrationHelper.AddBlockAttributeValue( "8ABA07EE-AB9F-4261-BE43-98178A38E742", "34EACB0F-DBC4-4F18-85C9-0D3EDFDF46BE", @"da133c99-c1ee-47ea-b359-372208855b64" ); // Channel

            RockMigrationHelper.AddBlockAttributeValue( "8ABA07EE-AB9F-4261-BE43-98178A38E742", "DA1DEE5D-BCEF-4AA4-A9D9-EFD4DD64462B", @"2" ); // Status

            RockMigrationHelper.AddBlockAttributeValue( "8ABA07EE-AB9F-4261-BE43-98178A38E742", "8026FEA1-35C1-41CF-9D09-E8B1DB6CBDA8", @"{%assign isFirst = true%}

<ul class=""nav nav-tabs"">
    {% for item in Items %}
        {%if isFirst%}
            <li class=active><a data-toggle=""pill"" href=""#c{{ item.Id }}"" class=""tab-pane"">
                {{item.TabName}}
            </a></li>
            {%assign isFirst=false%}
        {%else%}
            <li><a data-toggle=""pill"" href=""#c{{ item.Id }}"" class=""tab-pane"">
                {{item.TabName}}
            </a></li>
        {%endif%}
    {% endfor %}
</ul>

{%assign isFirst=true%}
<div class=""tab-content"">
    {% for item in Items %}
        {%if isFirst%}
            <div id=""c{{ item.Id }}"" class=""tab-pane active"">
                <h3>{{item.Title}}</h3>
                {{item.Content}}
            </div>
            {%assign isFirst=false%}
        {%else%}
            <div id=""c{{ item.Id }}"" class=""tab-pane"">
                <h3>{{item.Title}}</h3>
                {{item.Content}}
            </div>
        {%endif%}
    {% endfor %}
</div>" ); // Template

            RockMigrationHelper.AddBlockAttributeValue( "8ABA07EE-AB9F-4261-BE43-98178A38E742", "618EFBDA-941D-4F60-9AA8-54955B7A03A2", @"169" ); // Filter Id

            RockMigrationHelper.AddBlockAttributeValue( "8ABA07EE-AB9F-4261-BE43-98178A38E742", "AA9CD867-FA21-43C2-822B-CAC06E1F18B8", @"False" ); // Query Parameter Filtering

            RockMigrationHelper.AddBlockAttributeValue( "8ABA07EE-AB9F-4261-BE43-98178A38E742", "07ED420E-749C-4938-ADFD-1DDEA6B63014", @"Priority^0|" ); // Order

            RockMigrationHelper.AddBlockAttributeValue( "8ABA07EE-AB9F-4261-BE43-98178A38E742", "20BE4E0A-E84C-4AA1-9368-9732A834E1DE", @"False" ); // Merge Content

            RockMigrationHelper.AddBlockAttributeValue( "8ABA07EE-AB9F-4261-BE43-98178A38E742", "97161D67-EF24-4F21-9E6A-74B696DD33DE", @"False" ); // Set Page Title

            RockMigrationHelper.AddBlockAttributeValue( "8ABA07EE-AB9F-4261-BE43-98178A38E742", "ABA69AFC-2C5E-4CF6-A156-EDA2752CCC86", @"False" ); // Rss Autodiscover

            RockMigrationHelper.AddBlockAttributeValue( "8ABA07EE-AB9F-4261-BE43-98178A38E742", "E01AE3A7-2607-4DA5-AC98-3A368C900B64", @"" ); // Meta Description Attribute

            RockMigrationHelper.AddBlockAttributeValue( "8ABA07EE-AB9F-4261-BE43-98178A38E742", "A3510474-86E5-4AD2-BD4C-3C89E85795F5", @"" ); // Meta Image Attribute

            RockMigrationHelper.AddBlockAttributeValue( "7994383D-4B44-439B-8669-9658F56FF8BB", "2F1D98C4-A8EF-4680-9F64-11BFC28D5597", @"0557dfb5-f64e-4225-889a-3b0d3a584ca4" ); // Workflow Type

            RockMigrationHelper.AddBlockAttributeValue( "E54EF292-BBAD-4919-92FF-666A7816FE66", "25A501FC-E269-40B8-9904-E20FA7A1ADB6", @"0" ); // Count

            RockMigrationHelper.AddBlockAttributeValue( "E54EF292-BBAD-4919-92FF-666A7816FE66", "72F4232B-8D2A-4823-B9F1-ED68F182C1A4", @"False" ); // Enable Debug

            RockMigrationHelper.AddBlockAttributeValue( "E54EF292-BBAD-4919-92FF-666A7816FE66", "773BEFDD-EEBA-486C-98E6-AFD0D4156E22", @"3600" ); // Cache Duration

            RockMigrationHelper.AddBlockAttributeValue( "E54EF292-BBAD-4919-92FF-666A7816FE66", "34EACB0F-DBC4-4F18-85C9-0D3EDFDF46BE", @"da133c99-c1ee-47ea-b359-372208855b64" ); // Channel

            RockMigrationHelper.AddBlockAttributeValue( "E54EF292-BBAD-4919-92FF-666A7816FE66", "DA1DEE5D-BCEF-4AA4-A9D9-EFD4DD64462B", @"2" ); // Status

            RockMigrationHelper.AddBlockAttributeValue( "E54EF292-BBAD-4919-92FF-666A7816FE66", "8026FEA1-35C1-41CF-9D09-E8B1DB6CBDA8", @"{%assign isFirst = true%}

<ul class=""nav nav-tabs"">
    {% for item in Items %}
        {%if isFirst%}
            <li class=active><a data-toggle=""pill"" href=""#c{{ item.Id }}"" class=""tab-pane"">
                {{item.TabName}}
            </a></li>
            {%assign isFirst=false%}
        {%else%}
            <li><a data-toggle=""pill"" href=""#c{{ item.Id }}"" class=""tab-pane"">
                {{item.TabName}}
            </a></li>
        {%endif%}
    {% endfor %}
</ul>

{%assign isFirst=true%}
<div class=""tab-content"">
    {% for item in Items %}
        {%if isFirst%}
            <div id=""c{{ item.Id }}"" class=""tab-pane active"">
                <h3>{{item.Title}}</h3>
                {{item.Content}}
            </div>
            {%assign isFirst=false%}
        {%else%}
            <div id=""c{{ item.Id }}"" class=""tab-pane"">
                <h3>{{item.Title}}</h3>
                {{item.Content}}
            </div>
        {%endif%}
    {% endfor %}
</div>" ); // Template

            RockMigrationHelper.AddBlockAttributeValue( "E54EF292-BBAD-4919-92FF-666A7816FE66", "618EFBDA-941D-4F60-9AA8-54955B7A03A2", @"171" ); // Filter Id

            RockMigrationHelper.AddBlockAttributeValue( "E54EF292-BBAD-4919-92FF-666A7816FE66", "AA9CD867-FA21-43C2-822B-CAC06E1F18B8", @"False" ); // Query Parameter Filtering

            RockMigrationHelper.AddBlockAttributeValue( "E54EF292-BBAD-4919-92FF-666A7816FE66", "07ED420E-749C-4938-ADFD-1DDEA6B63014", @"Priority^1|" ); // Order

            RockMigrationHelper.AddBlockAttributeValue( "E54EF292-BBAD-4919-92FF-666A7816FE66", "20BE4E0A-E84C-4AA1-9368-9732A834E1DE", @"False" ); // Merge Content

            RockMigrationHelper.AddBlockAttributeValue( "E54EF292-BBAD-4919-92FF-666A7816FE66", "97161D67-EF24-4F21-9E6A-74B696DD33DE", @"False" ); // Set Page Title

            RockMigrationHelper.AddBlockAttributeValue( "E54EF292-BBAD-4919-92FF-666A7816FE66", "ABA69AFC-2C5E-4CF6-A156-EDA2752CCC86", @"False" ); // Rss Autodiscover

            RockMigrationHelper.AddBlockAttributeValue( "E54EF292-BBAD-4919-92FF-666A7816FE66", "E01AE3A7-2607-4DA5-AC98-3A368C900B64", @"" ); // Meta Description Attribute

            RockMigrationHelper.AddBlockAttributeValue( "E54EF292-BBAD-4919-92FF-666A7816FE66", "A3510474-86E5-4AD2-BD4C-3C89E85795F5", @"" ); // Meta Image Attribute

            RockMigrationHelper.AddBlockAttributeValue( "E54EF292-BBAD-4919-92FF-666A7816FE66", "46859902-DF5F-497D-801E-AC5B79DD3D28", @"True" ); // Is Closed

            RockMigrationHelper.AddBlockAttributeValue( "B38C4AD5-90FD-4D46-B384-56642FCCD10B", "46859902-DF5F-497D-801E-AC5B79DD3D28", @"True" ); // Is Closed

            RockMigrationHelper.AddBlockAttributeValue( "A58CABFA-2C1B-4D76-A659-62F26C4A51A0", "230EDFE8-33CA-478D-8C9A-572323AF3466", @"True" ); // Update Page

            RockMigrationHelper.AddBlockAttributeValue( "A58CABFA-2C1B-4D76-A659-62F26C4A51A0", "B0EC41B9-37C0-48FD-8E4E-37A8CA305012", @"" ); // Query Params

            RockMigrationHelper.AddBlockAttributeValue( "A58CABFA-2C1B-4D76-A659-62F26C4A51A0", "90B0E6AF-B2F4-4397-953B-737A40D4023B", @"" ); // Columns

            RockMigrationHelper.AddBlockAttributeValue( "A58CABFA-2C1B-4D76-A659-62F26C4A51A0", "71C8BA4E-8EF2-416B-BFE9-D1D88D9AA356", @"SELECT gm.id, p.id, p.nickname, p.lastname, gm.GroupId, av.Value
from dbo.person as  p
join dbo.GroupMember gm on gm.PersonId = p.Id
join [dbo].[AttributeValue] av on gm.Id = av.EntityId and av.AttributeId=2419
where gm.GroupId = 276 and substring( av.Value, 29, 10) > GETDATE()
order by av.Value" ); // Query

            RockMigrationHelper.AddBlockAttributeValue( "A58CABFA-2C1B-4D76-A659-62F26C4A51A0", "B9163A35-E09C-466D-8A2D-4ED81DF0114C", @"" ); // Url Mask

            RockMigrationHelper.AddBlockAttributeValue( "A58CABFA-2C1B-4D76-A659-62F26C4A51A0", "202A82BF-7772-481C-8419-600012607972", @"False" ); // Show Columns

            RockMigrationHelper.AddBlockAttributeValue( "A58CABFA-2C1B-4D76-A659-62F26C4A51A0", "8EB882CE-5BB1-4844-9C28-10190903EECD", @"" ); // Merge Fields

            RockMigrationHelper.AddBlockAttributeValue( "A58CABFA-2C1B-4D76-A659-62F26C4A51A0", "6A233402-446C-47E9-94A5-6A247C29BC21", @"<div class=""container"">
    <div clas=row>
    {% for row in rows %}
        {% assign dates = row.Value | Split: "","" %}
        <div class=""col-xs-4"" style=""text-align:right"">{{row.nickname}} {{row.lastname}}</div>
        <div class=""col-xs-8"">{{dates[0] | Date:'M/d/yyyy'}} to {{dates[1] | Date:'M/d/yyyy'}}</div>
    {%endfor%}
    </div>
</div>" ); // Formatted Output

            RockMigrationHelper.AddBlockAttributeValue( "A58CABFA-2C1B-4D76-A659-62F26C4A51A0", "8104CE53-FDB3-4E9F-B8E7-FD9E06E7551C", @"False" ); // Person Report

            RockMigrationHelper.AddBlockAttributeValue( "A58CABFA-2C1B-4D76-A659-62F26C4A51A0", "3327A07D-3A4E-4057-824D-C3EA7A5BD428", @"False" ); // Stored Procedure

            RockMigrationHelper.AddBlockAttributeValue( "A58CABFA-2C1B-4D76-A659-62F26C4A51A0", "A0B54268-508E-4B87-9201-C2A2E52418B1", @"False" ); // Show Merge Template

            RockMigrationHelper.AddBlockAttributeValue( "A58CABFA-2C1B-4D76-A659-62F26C4A51A0", "C61A93CC-6032-4A52-B7D0-40DA906496E2", @"False" ); // Show Communicate

            RockMigrationHelper.AddBlockAttributeValue( "A58CABFA-2C1B-4D76-A659-62F26C4A51A0", "94584595-ECA5-4BFB-84A6-13C2D005DC49", @"False" ); // Show Merge Person

            RockMigrationHelper.AddBlockAttributeValue( "A58CABFA-2C1B-4D76-A659-62F26C4A51A0", "6D7C08E3-549A-46C8-B33A-35BC16F2CEB5", @"False" ); // Show Excel Export

            RockMigrationHelper.AddBlockAttributeValue( "A58CABFA-2C1B-4D76-A659-62F26C4A51A0", "363DDE65-CA86-4B84-A79C-9FFCE3E9B15F", @"" ); // Page Title Lava

            RockMigrationHelper.AddBlockAttributeValue( "A58CABFA-2C1B-4D76-A659-62F26C4A51A0", "E2A60EB3-64BA-45DB-B425-11402B812C90", @"False" ); // Paneled Grid

            RockMigrationHelper.AddBlockAttributeValue( "A58CABFA-2C1B-4D76-A659-62F26C4A51A0", "F1D18DB0-AE9C-469A-A553-BF391F6DDFD0", @"False" ); // Show Bulk Update

            RockMigrationHelper.AddBlockAttributeValue( "A58CABFA-2C1B-4D76-A659-62F26C4A51A0", "2CC8DA00-AA35-4DF7-AB65-07B726AC7F48", @"False" ); // Active

            RockMigrationHelper.UpdateHtmlContentBlock( "367DCF5F-4DA2-4670-9125-D7D03B343744", @"<a class=""navbar-brand"" href=""~/page/1"">{{ GlobalAttribute.OrganizationName }}</a>", "5A1C0280-A378-46C8-BB3D-126193A54D08" ); //HTML Content

            RockMigrationHelper.UpdateHtmlContentBlock( "F5343899-8EB4-4967-8889-43F2F53282F1", @"<div class=""container-fluid"">
    <div class=""row"">
        <div class=""col-md-4"">
            <a href=""/moc/beforeyoubegin"" class=""btn btn-block btn-default btn-lg"" style=""margin-bottom:1em""><i class=""fa fa-check-square-o fa-5x""></i><br>Before your week begins</a>
        </div>
        <div class=""col-md-4"">
            <a href=""/moc/careandcontacts"" class=""col-sm-4 btn btn-block btn-default btn-lg"" style=""margin-bottom:1em""><i class=""fa fa-book fa-5x""></i><br>Pastoral and Care Contacts</a>
        </div>
        <div class=""col-md-4"">
            <a href=""/moc/community"" class=""col-sm-4 btn btn-block btn-default btn-lg"" style=""margin-bottom:1em""><i class=""fa  fa-university  fa-5x""></i><br>Community Resources</a>
        </div>
        <div class=""col-md-4"">
            <a href=""/moc/nohelp"" class=""col-sm-4 btn btn-block btn-default btn-lg"" style=""margin-bottom:1em""><i class=""fa fa-exclamation-triangle fa-5x""></i><br>No-Help List</a>
        </div>
        <div class=""col-md-4"">
            <a href=""/moc/calendar"" class=""col-sm-4 btn btn-block btn-default btn-lg"" style=""margin-bottom:1em""><i class=""fa fa-calendar-o fa-5x""></i><br>Calendar</a>
        </div>
        <div class=""col-md-4"">
            <a href=""/moc/callintakeform"" class=""col-sm-4 btn btn-block btn-default btn-lg"" style=""margin-bottom:1em""><i class=""fa fa-phone fa-5x""></i><br>Call Intake Form</a>
        </div>
    </div>
</div>", "C63EA7FE-AD2B-40A3-B6BE-1213E40B9F25" ); //HTML Content

            RockMigrationHelper.UpdateHtmlContentBlock( "B38C4AD5-90FD-4D46-B384-56642FCCD10B", @"<a class=""navbar-brand"" href=""~/page/1"">{{ GlobalAttribute.OrganizationName }}</a>", "A4643D27-EB86-4E75-A8B5-5345CDD8A5E6" ); //HTML Content

            RockMigrationHelper.UpdateHtmlContentBlock( "F7A85AE3-9D0E-4B8D-BB6E-D2E571F3E2B8", @"<a class=""navbar-brand"" href=""~/page/1"">{{ GlobalAttribute.OrganizationName }}</a>", "4DAC253C-E62A-42F2-BFC6-E387B8C39252" ); //HTML Content

            RockMigrationHelper.UpdateHtmlContentBlock( "BAA0FDD1-67FC-40A0-B0E5-F595BAC4EAF7", @"<a class=""navbar-brand"" href=""~/page/1"">{{ GlobalAttribute.OrganizationName }}</a>", "1658D980-A637-43A1-BB6A-6D62B0E6507E" ); //HTML Content

            RockMigrationHelper.UpdateHtmlContentBlock( "326B80F0-9E27-4C00-BFA7-0E11B19F7C3F", @"<a class=""navbar-brand"" href=""~/page/1"">{{ GlobalAttribute.OrganizationName }}</a>", "FD132D82-6E00-48D1-A91A-0A582F17FDAF" ); //HTML Content

            RockMigrationHelper.UpdateHtmlContentBlock( "C62F26A0-B918-4241-A89B-0CC335E89046", @"<a class=""navbar-brand"" href=""~/page/1"">{{ GlobalAttribute.OrganizationName }}</a>", "0EC78C51-A9EC-40C2-9922-5BCD2F99DB8F" ); //HTML Content

        }
        public override void Down()
        {
            RockMigrationHelper.DeleteAttribute( "1F4F0763-4ED7-4C62-9347-6029158E78AD" );
            RockMigrationHelper.DeleteAttribute( "50204518-AB90-4A6C-8D72-2FA554C4841F" );
            RockMigrationHelper.DeleteAttribute( "F1D18DB0-AE9C-469A-A553-BF391F6DDFD0" );
            RockMigrationHelper.DeleteAttribute( "E2A60EB3-64BA-45DB-B425-11402B812C90" );
            RockMigrationHelper.DeleteAttribute( "363DDE65-CA86-4B84-A79C-9FFCE3E9B15F" );
            RockMigrationHelper.DeleteAttribute( "6D7C08E3-549A-46C8-B33A-35BC16F2CEB5" );
            RockMigrationHelper.DeleteAttribute( "94584595-ECA5-4BFB-84A6-13C2D005DC49" );
            RockMigrationHelper.DeleteAttribute( "C61A93CC-6032-4A52-B7D0-40DA906496E2" );
            RockMigrationHelper.DeleteAttribute( "A0B54268-508E-4B87-9201-C2A2E52418B1" );
            RockMigrationHelper.DeleteAttribute( "3327A07D-3A4E-4057-824D-C3EA7A5BD428" );
            RockMigrationHelper.DeleteAttribute( "48FF43A9-8E12-4768-80A9-88FBB81F11D8" );
            RockMigrationHelper.DeleteAttribute( "A3510474-86E5-4AD2-BD4C-3C89E85795F5" );
            RockMigrationHelper.DeleteAttribute( "E01AE3A7-2607-4DA5-AC98-3A368C900B64" );
            RockMigrationHelper.DeleteAttribute( "ABA69AFC-2C5E-4CF6-A156-EDA2752CCC86" );
            RockMigrationHelper.DeleteAttribute( "97161D67-EF24-4F21-9E6A-74B696DD33DE" );
            RockMigrationHelper.DeleteAttribute( "20BE4E0A-E84C-4AA1-9368-9732A834E1DE" );
            RockMigrationHelper.DeleteAttribute( "07ED420E-749C-4938-ADFD-1DDEA6B63014" );
            RockMigrationHelper.DeleteAttribute( "AA9CD867-FA21-43C2-822B-CAC06E1F18B8" );
            RockMigrationHelper.DeleteAttribute( "618EFBDA-941D-4F60-9AA8-54955B7A03A2" );
            RockMigrationHelper.DeleteAttribute( "8026FEA1-35C1-41CF-9D09-E8B1DB6CBDA8" );
            RockMigrationHelper.DeleteAttribute( "DA1DEE5D-BCEF-4AA4-A9D9-EFD4DD64462B" );
            RockMigrationHelper.DeleteAttribute( "34EACB0F-DBC4-4F18-85C9-0D3EDFDF46BE" );
            RockMigrationHelper.DeleteAttribute( "773BEFDD-EEBA-486C-98E6-AFD0D4156E22" );
            RockMigrationHelper.DeleteAttribute( "72F4232B-8D2A-4823-B9F1-ED68F182C1A4" );
            RockMigrationHelper.DeleteAttribute( "2D7E6F55-B25E-4EA2-8F7E-F7E138E39E21" );
            RockMigrationHelper.DeleteAttribute( "25A501FC-E269-40B8-9904-E20FA7A1ADB6" );
            RockMigrationHelper.DeleteAttribute( "8104CE53-FDB3-4E9F-B8E7-FD9E06E7551C" );
            RockMigrationHelper.DeleteAttribute( "6A233402-446C-47E9-94A5-6A247C29BC21" );
            RockMigrationHelper.DeleteAttribute( "8EB882CE-5BB1-4844-9C28-10190903EECD" );
            RockMigrationHelper.DeleteAttribute( "6783D47D-92F9-4F48-93C0-16111D675A0F" );
            RockMigrationHelper.DeleteAttribute( "2F1D98C4-A8EF-4680-9F64-11BFC28D5597" );
            RockMigrationHelper.DeleteAttribute( "3BDB8AED-32C5-4879-B1CB-8FC7C8336534" );
            RockMigrationHelper.DeleteAttribute( "9D3E4ED9-1BEF-4547-B6B0-CE29FE3835EE" );
            RockMigrationHelper.DeleteAttribute( "26F3AFC6-C05B-44A4-8593-AFE1D9969B0E" );
            RockMigrationHelper.DeleteAttribute( "0673E015-F8DD-4A52-B380-C758011331B2" );
            RockMigrationHelper.DeleteAttribute( "202A82BF-7772-481C-8419-600012607972" );
            RockMigrationHelper.DeleteAttribute( "B9163A35-E09C-466D-8A2D-4ED81DF0114C" );
            RockMigrationHelper.DeleteAttribute( "71C8BA4E-8EF2-416B-BFE9-D1D88D9AA356" );
            RockMigrationHelper.DeleteAttribute( "90B0E6AF-B2F4-4397-953B-737A40D4023B" );
            RockMigrationHelper.DeleteAttribute( "B0EC41B9-37C0-48FD-8E4E-37A8CA305012" );
            RockMigrationHelper.DeleteAttribute( "230EDFE8-33CA-478D-8C9A-572323AF3466" );
            RockMigrationHelper.DeleteAttribute( "466993F7-D838-447A-97E7-8BBDA6A57289" );
            RockMigrationHelper.DeleteAttribute( "3FFC512D-A576-4289-B648-905FD7A64ABB" );
            RockMigrationHelper.DeleteAttribute( "7C1CE199-86CF-4EAE-8AB3-848416A72C58" );
            RockMigrationHelper.DeleteAttribute( "EC2B701B-4C1D-4F3F-9C77-A73C75D7FF7A" );
            RockMigrationHelper.DeleteAttribute( "4DFDB295-6D0F-40A1-BEF9-7B70C56F66C4" );
            RockMigrationHelper.DeleteBlock( "A58CABFA-2C1B-4D76-A659-62F26C4A51A0" );
            RockMigrationHelper.DeleteBlock( "C62F26A0-B918-4241-A89B-0CC335E89046" );
            RockMigrationHelper.DeleteBlock( "326B80F0-9E27-4C00-BFA7-0E11B19F7C3F" );
            RockMigrationHelper.DeleteBlock( "BAA0FDD1-67FC-40A0-B0E5-F595BAC4EAF7" );
            RockMigrationHelper.DeleteBlock( "F7A85AE3-9D0E-4B8D-BB6E-D2E571F3E2B8" );
            RockMigrationHelper.DeleteBlock( "B38C4AD5-90FD-4D46-B384-56642FCCD10B" );
            RockMigrationHelper.DeleteBlock( "E54EF292-BBAD-4919-92FF-666A7816FE66" );
            RockMigrationHelper.DeleteBlock( "7994383D-4B44-439B-8669-9658F56FF8BB" );
            RockMigrationHelper.DeleteBlock( "8ABA07EE-AB9F-4261-BE43-98178A38E742" );
            RockMigrationHelper.DeleteBlock( "B54B058B-E1A3-458C-83E9-31B8116607CC" );
            RockMigrationHelper.DeleteBlock( "F5343899-8EB4-4967-8889-43F2F53282F1" );
            RockMigrationHelper.DeleteBlock( "367DCF5F-4DA2-4670-9125-D7D03B343744" );
            RockMigrationHelper.DeleteBlockType( "143A2345-3E26-4ED0-A2FE-42AAF11B4C0F" );
            RockMigrationHelper.DeleteBlockType( "A8BD05C8-6F89-4628-845B-059E686F089A" );
            RockMigrationHelper.DeleteBlockType( "E31E02E9-73F6-4B3E-98BA-E0E4F86CA126" );
            RockMigrationHelper.DeleteBlockType( "19B61D65-37E3-459F-A44F-DEF0089118A3" );
            RockMigrationHelper.DeletePage( "9384126F-BC6F-444F-A651-CE6EC91B6435" ); //  Page: Minister On Call Home Page
            RockMigrationHelper.DeletePage( "6B7F1BC8-5EF2-4215-8428-8730D6211F13" ); //  Page: MOC / Before your week begins.
            RockMigrationHelper.DeletePage( "69EB92FC-474A-4729-96A7-A1DF51E30F06" ); //  Page: Pastoral Care and Contacts
            RockMigrationHelper.DeletePage( "F0942A07-9806-46C1-A9D8-91418D539EC3" ); //  Page: No Help List
            RockMigrationHelper.DeletePage( "32679C05-48C7-49DE-A3E3-3F83088DC8C7" ); //  Page: Call Intake Form
            RockMigrationHelper.DeletePage( "0DAC0D0C-18C5-4A45-A53C-8885419CFD6F" ); //  Page: Calendar
        }
    }
}

