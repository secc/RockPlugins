using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rock.Plugin;

namespace org.secc.Migrations
{
    [MigrationNumber( 5, "1.2.0" )]
    class GroupManager_PageData : Migration
    {
        public override void Up()
        {
            RockMigrationHelper.AddSite( "Group Manager", "Site for group manager app.", "Stark", "F310E046-4434-4966-884A-96403C76CF50" );   //Site: Group Manager
            RockMigrationHelper.AddLayout( "F310E046-4434-4966-884A-96403C76CF50", "FullWidth", "Full Width", "", "E5998D06-BCFB-48EC-8143-64592DF8CC13" ); // Site:Group Manager
            // Page: Group Manager Home Page
            RockMigrationHelper.AddPage( "", "E5998D06-BCFB-48EC-8143-64592DF8CC13", "Group Manager Home Page", "", "20A6336A-C9F8-4F55-94BF-991B0D3B49D7", "" ); // Site:Group Manager
            // Page: Small Group App
            RockMigrationHelper.AddPage( "20A6336A-C9F8-4F55-94BF-991B0D3B49D7", "E5998D06-BCFB-48EC-8143-64592DF8CC13", "Small Group App", "", "37053063-76C5-44EB-BC0A-043F10744CEC", "" ); // Site:Group Manager
            // Page: This Week
            RockMigrationHelper.AddPage( "37053063-76C5-44EB-BC0A-043F10744CEC", "E5998D06-BCFB-48EC-8143-64592DF8CC13", "This Week", "", "3BAD929C-832C-4FF0-97C8-FB5E8698D9D8", "fa fa-lightbulb-o" ); // Site:Group Manager
            // Page: Roster
            RockMigrationHelper.AddPage( "37053063-76C5-44EB-BC0A-043F10744CEC", "E5998D06-BCFB-48EC-8143-64592DF8CC13", "Roster", "", "8A5BB80C-BC42-4038-8F91-A9E875D8DC47", "fa fa-users" ); // Site:Group Manager
            // Page: Attendance
            RockMigrationHelper.AddPage( "37053063-76C5-44EB-BC0A-043F10744CEC", "E5998D06-BCFB-48EC-8143-64592DF8CC13", "Attendance", "", "DC25DA8D-4247-4CC4-AC40-178195E9308E", "fa fa-check-square-o" ); // Site:Group Manager
            // Page: Resources
            RockMigrationHelper.AddPage( "37053063-76C5-44EB-BC0A-043F10744CEC", "E5998D06-BCFB-48EC-8143-64592DF8CC13", "Resources", "", "C0881189-7F82-4669-B4D9-8FCFD5E5DCE1", "fa fa-info-circle" ); // Site:Group Manager
            // Page: Get Help
            RockMigrationHelper.AddPage( "37053063-76C5-44EB-BC0A-043F10744CEC", "E5998D06-BCFB-48EC-8143-64592DF8CC13", "Get Help", "", "00F0E543-F524-4A31-B040-B8613F91F854", "fa fa-plus-square" ); // Site:Group Manager
            RockMigrationHelper.AddPageRoute( "20A6336A-C9F8-4F55-94BF-991B0D3B49D7", "groupmanager" );
            RockMigrationHelper.UpdateBlockType( "Group Roster", "Presents members of group in roster format.", "~/Plugins/org_secc/GroupManager/GroupRoster.ascx", "Groups", "18555BDC-87C1-4AD2-B22F-64936289A0D7" ); //Block Type: Group Roster
            RockMigrationHelper.UpdateBlockType( "Group Manager Attendance", "Lists the group members for a specific occurrence datetime and allows selecting if they attended or not.", "~/Plugins/org_secc/GroupManager/GroupManagerAttendance.ascx", "Groups", "AF0E5AE5-593C-4B18-B559-88BEF743D3E7" ); //Block Type: Group Manager Attendance
            RockMigrationHelper.UpdateBlockType( "Small Group Content", "Block to display dynamic small group content for a group.", "~/Plugins/org_secc/GroupManager/SmallGroupContent.ascx", "Groups", "51C366D6-EBD8-4202-BA5A-B845A7903ADC" ); //Block Type: Small Group Content
            RockMigrationHelper.UpdateBlockType( "Group Registration Modal", "Allows a person to register for a group.", "~/Plugins/org_secc/GroupManager/GroupRegistrationModal.ascx", "Groups", "2AD8831E-8926-43A4-8CA1-095A60E0B2A2" ); //Block Type: Group Registration Modal
            RockMigrationHelper.AddBlock( "20A6336A-C9F8-4F55-94BF-991B0D3B49D7", "", "1B172C33-8672-4C98-A995-8E123FF316BD", "Small Group Ministries", "Main", "", "", 0, "3FBE1AA2-247F-497F-AD5C-237C4EEFBFBC" ); //Block of Type: Group List Personalized Lava
            RockMigrationHelper.AddBlock( "20A6336A-C9F8-4F55-94BF-991B0D3B49D7", "", "1B172C33-8672-4C98-A995-8E123FF316BD", "Huddle Groups", "Main", "", "", 1, "4696B0F8-A461-44D2-AFEA-6DB317E13C61" ); //Block of Type: Group List Personalized Lava
            RockMigrationHelper.AddBlock( "20A6336A-C9F8-4F55-94BF-991B0D3B49D7", "", "1B172C33-8672-4C98-A995-8E123FF316BD", "Small Groups", "Main", "", "", 2, "A4212ABD-27BA-4028-B341-FBFCB98C6A4C" ); //Block of Type: Group List Personalized Lava
            RockMigrationHelper.AddBlock( "37053063-76C5-44EB-BC0A-043F10744CEC", "", "CACB9D1A-A820-4587-986A-D66A69EE9948", "Page Menu", "Main", "", "", 1, "E2EC1261-E0C7-4409-9D2E-531279AEC435" ); //Block of Type: Page Menu
            RockMigrationHelper.AddBlock( "37053063-76C5-44EB-BC0A-043F10744CEC", "", "218B057F-B214-4317-8E84-7A95CF88067E", "Group Detail Lava", "Main", "", "", 0, "4B89477A-5401-47F3-A56A-8ED535C65253" ); //Block of Type: Group Detail Lava
            RockMigrationHelper.AddBlock( "8A5BB80C-BC42-4038-8F91-A9E875D8DC47", "", "18555BDC-87C1-4AD2-B22F-64936289A0D7", "Group Roster", "Main", "", "", 0, "C70E6839-D338-4B10-8C5A-EB43E65E4BA3" ); //Block of Type: Group Roster
            RockMigrationHelper.AddBlock( "DC25DA8D-4247-4CC4-AC40-178195E9308E", "", "AF0E5AE5-593C-4B18-B559-88BEF743D3E7", "Group Manager Attendance", "Main", "", "", 0, "2731B0B8-376F-4350-9F83-E9487B92B693" ); //Block of Type: Group Manager Attendance
            RockMigrationHelper.AddBlock( "3BAD929C-832C-4FF0-97C8-FB5E8698D9D8", "", "51C366D6-EBD8-4202-BA5A-B845A7903ADC", "Small Group Content", "Main", "", "", 1, "D45B7887-C728-4658-9865-095398CB8C84" ); //Block of Type: Small Group Content
            RockMigrationHelper.AddBlock( "C0881189-7F82-4669-B4D9-8FCFD5E5DCE1", "", "51C366D6-EBD8-4202-BA5A-B845A7903ADC", "Small Group Content", "Main", "", "", 0, "663764A7-5D96-4C66-9907-63DC94D88787" ); //Block of Type: Small Group Content
            RockMigrationHelper.AddBlock( "3BAD929C-832C-4FF0-97C8-FB5E8698D9D8", "", "CACB9D1A-A820-4587-986A-D66A69EE9948", "Page Menu", "Navigation", "", "", 1, "7A183520-7216-4CB5-A7B8-0AD80E349A62" ); //Block of Type: Page Menu
            RockMigrationHelper.AddBlock( "3BAD929C-832C-4FF0-97C8-FB5E8698D9D8", "", "CACB9D1A-A820-4587-986A-D66A69EE9948", "Page Menu", "Header", "", "", 0, "1970E8C3-0415-4253-984E-F2EFE7C9D4FF" ); //Block of Type: Page Menu
            RockMigrationHelper.AddBlock( "8A5BB80C-BC42-4038-8F91-A9E875D8DC47", "", "CACB9D1A-A820-4587-986A-D66A69EE9948", "Page Menu", "Navigation", "", "", 1, "7E56CA79-433A-459A-9E7E-0EEE9433BD34" ); //Block of Type: Page Menu
            RockMigrationHelper.AddBlock( "DC25DA8D-4247-4CC4-AC40-178195E9308E", "", "CACB9D1A-A820-4587-986A-D66A69EE9948", "Page Menu", "Navigation", "", "", 0, "196CAEBC-DB97-4903-92BB-5FFEE466F8E7" ); //Block of Type: Page Menu
            RockMigrationHelper.AddBlock( "C0881189-7F82-4669-B4D9-8FCFD5E5DCE1", "", "CACB9D1A-A820-4587-986A-D66A69EE9948", "Page Menu", "Navigation", "", "", 0, "D69774E9-1867-4E34-B53E-5C8DD558BF4F" ); //Block of Type: Page Menu
            RockMigrationHelper.AddBlock( "00F0E543-F524-4A31-B040-B8613F91F854", "", "CACB9D1A-A820-4587-986A-D66A69EE9948", "Page Menu", "Navigation", "", "", 0, "813C6600-1B46-4FDB-965F-CDDA77E17949" ); //Block of Type: Page Menu
            RockMigrationHelper.AddBlock( "8A5BB80C-BC42-4038-8F91-A9E875D8DC47", "", "2AD8831E-8926-43A4-8CA1-095A60E0B2A2", "Group Registration Modal", "Header", "<div style=\"margin-top:5px\">", "</div>", 0, "81D1AFD6-0448-40D5-8949-DE6A5A93D6F2" ); //Block of Type: Group Registration Modal
            RockMigrationHelper.AddBlock( "DC25DA8D-4247-4CC4-AC40-178195E9308E", "", "2AD8831E-8926-43A4-8CA1-095A60E0B2A2", "Group Registration Modal", "Header", "", "", 0, "420DA9C4-27A4-46F0-951C-2935231E5E12" ); //Block of Type: Group Registration Modal
            RockMigrationHelper.AddBlockTypeAttribute( "AF0E5AE5-593C-4B18-B559-88BEF743D3E7", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Allow Add Date", "AllowAddDate", "", "Should block support adding new attendance dates outside of the group\"s configured schedule and group type\"s exclusion dates?", 0, @"True", "33877A18-B34B-4B7C-AD20-025265A1198D" );
            RockMigrationHelper.AddBlockTypeAttribute( "AF0E5AE5-593C-4B18-B559-88BEF743D3E7", "890BAA33-5EBB-4343-A8AA-42E0E6C7467A", "Attendance Roster Template", "AttendanceRosterTemplate", "", "", 0, @"", "9D85ACFF-5A23-4DDC-A710-28EC476BC9FD" );
            RockMigrationHelper.AddBlockTypeAttribute( "51C366D6-EBD8-4202-BA5A-B845A7903ADC", "9C204CD0-1233-41C5-818A-C5DA439445AA", "Meta Description Attribute", "MetaDescriptionAttribute", "", "Attribute to use for storing the description attribute.", 0, @"", "CFAF57F4-516B-4C5D-9601-9282264EBE80" );
            RockMigrationHelper.AddBlockTypeAttribute( "51C366D6-EBD8-4202-BA5A-B845A7903ADC", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Cache Duration", "CacheDuration", "", "Number of seconds to cache the content.", 0, @"3600", "4025AE25-7C98-42E2-BDC8-D620E1DE3E71" );
            RockMigrationHelper.AddBlockTypeAttribute( "51C366D6-EBD8-4202-BA5A-B845A7903ADC", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Enable Debug", "EnableDebug", "", "Enabling debug will display the fields of the first 5 items to help show you wants available for your liquid.", 0, @"False", "405E1E2E-6E81-4279-90AA-0407204EDC12" );
            RockMigrationHelper.AddBlockTypeAttribute( "51C366D6-EBD8-4202-BA5A-B845A7903ADC", "BD0D9B57-2A41-4490-89FF-F01DAB7D4904", "Status", "Status", "", "Include items with the following status.", 0, @"2", "6EA89B4A-ED9C-4278-9A23-68F7C0539433" );
            RockMigrationHelper.AddBlockTypeAttribute( "51C366D6-EBD8-4202-BA5A-B845A7903ADC", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Query Parameter Filtering", "QueryParameterFiltering", "", "Determines if block should evaluate the query string parameters for additional filter criteria.", 0, @"False", "7F175B06-4E2C-4CDC-B71D-7BA5E6A4AC6B" );
            RockMigrationHelper.AddBlockTypeAttribute( "51C366D6-EBD8-4202-BA5A-B845A7903ADC", "9C204CD0-1233-41C5-818A-C5DA439445AA", "Order", "Order", "", "The specifics of how items should be ordered. This value is set through configuration and should not be modified here.", 0, @"", "51F35B54-E661-4F98-9F19-7520491E40BF" );
            RockMigrationHelper.AddBlockTypeAttribute( "51C366D6-EBD8-4202-BA5A-B845A7903ADC", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Merge Content", "MergeContent", "", "Should the content data and attribute values be merged using the liquid template engine.", 0, @"False", "FB9A5408-544A-4CAC-9C31-33737CE63C16" );
            RockMigrationHelper.AddBlockTypeAttribute( "51C366D6-EBD8-4202-BA5A-B845A7903ADC", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Set Page Title", "SetPageTitle", "", "Determines if the block should set the page title with the channel name or content item.", 0, @"False", "62DED4DC-6790-438B-B964-2728094DCECE" );
            RockMigrationHelper.AddBlockTypeAttribute( "51C366D6-EBD8-4202-BA5A-B845A7903ADC", "9C204CD0-1233-41C5-818A-C5DA439445AA", "Meta Image Attribute", "MetaImageAttribute", "", "Attribute to use for storing the image attribute.", 0, @"", "A060D39E-9635-4592-BF53-9D110B56B570" );
            RockMigrationHelper.AddBlockTypeAttribute( "51C366D6-EBD8-4202-BA5A-B845A7903ADC", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Count", "Count", "", "The maximum number of items to display.", 0, @"5", "733695E3-C5EB-4F01-A033-A70EB58B4747" );
            RockMigrationHelper.AddBlockTypeAttribute( "51C366D6-EBD8-4202-BA5A-B845A7903ADC", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Detail Page", "DetailPage", "", "The page to navigate to for details.", 0, @"", "C58A85CC-FDC3-4067-BA7D-C8E2C5934E57" );
            RockMigrationHelper.AddBlockTypeAttribute( "51C366D6-EBD8-4202-BA5A-B845A7903ADC", "BC48720C-3610-4BCF-AE66-D255A17F1CDF", "ItemType", "ItemType", "", "Content item types to display on page", 0, @"", "182F3813-C8B3-46F1-9F51-84C6358C2F51" );
            RockMigrationHelper.AddBlockTypeAttribute( "51C366D6-EBD8-4202-BA5A-B845A7903ADC", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "ShowSidebar", "ShowSidebar", "", "Determines if sidebar index should be displayed.", 0, @"False", "02F380AC-384E-4471-87AB-E14AEDBA8A24" );
            RockMigrationHelper.AddBlockTypeAttribute( "51C366D6-EBD8-4202-BA5A-B845A7903ADC", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "ShowCalendar", "ShowCalendar", "", "Show a calendar to select a new date for the lesson?", 0, @"False", "85CCE819-53D6-49B4-8BF5-632B723A0CCE" );
            RockMigrationHelper.AddBlockTypeAttribute( "51C366D6-EBD8-4202-BA5A-B845A7903ADC", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "FilterByDate", "FilterByDate", "", "Filter to show only those content items which are in date.", 0, @"False", "A024050C-BFD4-4B91-ACCE-B4CF7F9ED26A" );
            RockMigrationHelper.AddBlockTypeAttribute( "51C366D6-EBD8-4202-BA5A-B845A7903ADC", "1D0D3794-C210-48A8-8C68-3FBEC08A6BA5", "ContentLava", "ContentLava", "", "Lava to display content with.", 0, @"<div class='content'> {% for item in Items %} <div class='row'> <h2> {{ item.Title }} </h2> <div class=''> {{item.Content}} </div> </div> {% endfor %} </div>", "E41B91F5-E4C2-44E0-BED6-437AF54EC06B" );
            RockMigrationHelper.AddBlockTypeAttribute( "18555BDC-87C1-4AD2-B22F-64936289A0D7", "1D0D3794-C210-48A8-8C68-3FBEC08A6BA5", "Roster Lava", "RosterLava", "", "Lava to appear in member roster pannels", 0, @"", "9FAB5644-4D5D-4992-B6FA-EA57C59A4F0A" );
            RockMigrationHelper.AddBlockTypeAttribute( "AF0E5AE5-593C-4B18-B559-88BEF743D3E7", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Auto Count", "AutoCount", "", "Auto count membership.", 0, @"False", "EF07BDC4-1E71-4A97-8804-AA8884A06C06" );
            RockMigrationHelper.AddBlockTypeAttribute( "AF0E5AE5-593C-4B18-B559-88BEF743D3E7", "9C204CD0-1233-41C5-818A-C5DA439445AA", "Count Label", "CountLabel", "", "Label for count field.", 0, @"Head Count:", "04DCC287-C62D-4ED6-9385-63AB85FE4B8B" );
            RockMigrationHelper.AddBlockTypeAttribute( "2AD8831E-8926-43A4-8CA1-095A60E0B2A2", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Large Button", "LargeButton", "", "Show large button with text?", 0, @"False", "B0CBD860-C135-48CC-82BD-B1462DC4B9BF" );
            RockMigrationHelper.AddBlockTypeAttribute( "2AD8831E-8926-43A4-8CA1-095A60E0B2A2", "9C204CD0-1233-41C5-818A-C5DA439445AA", "CSS Class", "CSSClass", "", "Optional css class to style button", 0, @"btn btn-default", "6DB1A25B-A2B3-4334-97CE-69B23C064424" );
            RockMigrationHelper.AddBlockTypeAttribute( "2AD8831E-8926-43A4-8CA1-095A60E0B2A2", "7525C4CB-EE6B-41D4-9B64-A08048D5A5C0", "Group Member Status", "GroupMemberStatus", "", "The group member status to use when adding person to group (default: \"Pending\".)", 1, @"2", "3D899681-933D-4663-BA62-9B3089308D12" );
            RockMigrationHelper.AddBlockTypeAttribute( "AF0E5AE5-593C-4B18-B559-88BEF743D3E7", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Allow Add Person", "AllowAddPerson", "", "Should block support adding new attendee ( Requires that person has rights to search for new person )?", 1, @"False", "70F85FB0-ED49-489D-9C77-47AE60B97199" );
            RockMigrationHelper.AddBlockTypeAttribute( "2AD8831E-8926-43A4-8CA1-095A60E0B2A2", "59D5A94C-94A0-4630-B80A-BB25697D74C7", "Connection Status", "ConnectionStatus", "", "The connection status to use for new individuals (default: \"Web Prospect\".)", 2, @"368DD475-242C-49C4-A42C-7278BE690CC2", "B03A08AC-7594-4431-BAE5-DA344D0CF491" );
            RockMigrationHelper.AddBlockTypeAttribute( "2AD8831E-8926-43A4-8CA1-095A60E0B2A2", "59D5A94C-94A0-4630-B80A-BB25697D74C7", "Record Status", "RecordStatus", "", "The record status to use for new individuals (default: \"Pending\".)", 3, @"283999EC-7346-42E3-B807-BCE9B2BABB49", "E2BDA459-3E06-42CB-8576-DAA6C67B27E6" );
            RockMigrationHelper.AddBlockAttributeValue( "3FBE1AA2-247F-497F-AD5C-237C4EEFBFBC", "13921BE2-C0D4-4FD6-841F-36022B56DB54", @"20a6336a-c9f8-4f55-94bf-991b0d3b49d7" ); // Detail Page
            RockMigrationHelper.AddBlockAttributeValue( "3FBE1AA2-247F-497F-AD5C-237C4EEFBFBC", "81D7C7A0-5469-419A-9A4D-149511DB7271", @"e41f0971-d0d7-43c4-918c-11b1a7af5e0d" ); // Include Group Types
            RockMigrationHelper.AddBlockAttributeValue( "3FBE1AA2-247F-497F-AD5C-237C4EEFBFBC", "04AA4BAE-9E55-45C2-ABAB-F85B1F81596E", @"" ); // Exclude Group Types
            RockMigrationHelper.AddBlockAttributeValue( "3FBE1AA2-247F-497F-AD5C-237C4EEFBFBC", "C7EC3847-7419-4364-98E8-09FE42A04A76", @"{% assign groupCount = Groups | Size %}
  {% if groupCount != 0 %}

<div class=""panel panel-default"">
  <div class=""panel-heading"">Small Group Ministries</div>
  <ul class=""list-group list-group-panel"">
    {% for group in Groups %}
    
        <li class=""list-group-item"">
          <a href=""{{ LinkedPages.DetailPage }}?GroupId={{group.Group.Id}}"" class=""js-group-item"" data-toggle=""tooltip"" data-placement=""top"" title=""{{ group.GroupType }}"">
            {{ group.Group.Name }}
          </a>
          <ul>
          {%for huddle in group.Group.Groups%}
            <li class=""list-group-item"">
          <a href=""{{ LinkedPages.DetailPage }}?GroupId={{huddle.Id}}"" class=""js-group-item"" data-toggle=""tooltip"" data-placement=""top"" title=""{{ huddle.GroupType.Name }}"">
            {{ huddle.Name }}
          </a>
          </li>
          {% endfor %}
          </ul>
          
        </li>
        
          
    {% endfor %}
  </ul>

</div>
  {% endif %}" ); // Lava Template
            RockMigrationHelper.AddBlockAttributeValue( "3FBE1AA2-247F-497F-AD5C-237C4EEFBFBC", "89B144BE-8ECD-42AC-97A6-C76C8E403422", @"False" ); // Enable Debug
            RockMigrationHelper.AddBlockAttributeValue( "4696B0F8-A461-44D2-AFEA-6DB317E13C61", "89B144BE-8ECD-42AC-97A6-C76C8E403422", @"False" ); // Enable Debug
            RockMigrationHelper.AddBlockAttributeValue( "4696B0F8-A461-44D2-AFEA-6DB317E13C61", "C7EC3847-7419-4364-98E8-09FE42A04A76", @"{% assign groupCount = Groups | Size %}
  {% if groupCount != 0 %}

<div class=""panel panel-default"">
  <div class=""panel-heading"">Small Group Huddles</div>
  <ul class=""list-group list-group-panel"">
    {% for group in Groups %}
    
        <li class=""list-group-item"">
          <a href=""{{ LinkedPages.DetailPage }}?GroupId={{group.Group.Id}}"" class=""js-group-item"" data-toggle=""tooltip"" data-placement=""top"" title=""{{ group.GroupType }}"">
            {{ group.Group.Name }}
          </a>
          <ul>
          {%for small in group.Group.Groups%}
            <li class=""list-group-item"">
          <a href=""{{ LinkedPages.DetailPage }}?GroupId={{small.Id}}"" class=""js-group-item"" data-toggle=""tooltip"" data-placement=""top"" title=""{{ small.GroupType.Name }}"">
            {{ small.Name }}
          </a>
          </li>
          {% endfor %}
          </ul>
          
        </li>
        
          
    {% endfor %}
  </ul>

</div>
  {% endif %}" ); // Lava Template
            RockMigrationHelper.AddBlockAttributeValue( "4696B0F8-A461-44D2-AFEA-6DB317E13C61", "81D7C7A0-5469-419A-9A4D-149511DB7271", @"c2f97f19-a873-4870-af41-d105a0a7bbf4" ); // Include Group Types
            RockMigrationHelper.AddBlockAttributeValue( "4696B0F8-A461-44D2-AFEA-6DB317E13C61", "04AA4BAE-9E55-45C2-ABAB-F85B1F81596E", @"" ); // Exclude Group Types
            RockMigrationHelper.AddBlockAttributeValue( "4696B0F8-A461-44D2-AFEA-6DB317E13C61", "13921BE2-C0D4-4FD6-841F-36022B56DB54", @"20a6336a-c9f8-4f55-94bf-991b0d3b49d7" ); // Detail Page
            RockMigrationHelper.AddBlockAttributeValue( "A4212ABD-27BA-4028-B341-FBFCB98C6A4C", "13921BE2-C0D4-4FD6-841F-36022B56DB54", @"37053063-76c5-44eb-bc0a-043f10744cec" ); // Detail Page
            RockMigrationHelper.AddBlockAttributeValue( "A4212ABD-27BA-4028-B341-FBFCB98C6A4C", "C7EC3847-7419-4364-98E8-09FE42A04A76", @"<div class=""panel panel-default"">
  <div class=""panel-heading"">NextGen Small Groups</div>

  <ul class=""list-group list-group-panel"">
    {% for group in Groups %}
    
      {% if group.IsLeader %}
        <li class=""list-group-item"">
          <a href=""{{ LinkedPages.DetailPage }}?GroupId={{group.Group.Id}}"" class=""js-group-item"" data-toggle=""tooltip"" data-placement=""top"" title=""{{ group.GroupType }}"">
            {{ group.Group.Name }}
          </a>
        </li>      
      {% endif %}
    {% endfor %}
  </ul>

</div>" ); // Lava Template
            RockMigrationHelper.AddBlockAttributeValue( "A4212ABD-27BA-4028-B341-FBFCB98C6A4C", "04AA4BAE-9E55-45C2-ABAB-F85B1F81596E", @"" ); // Exclude Group Types
            RockMigrationHelper.AddBlockAttributeValue( "A4212ABD-27BA-4028-B341-FBFCB98C6A4C", "89B144BE-8ECD-42AC-97A6-C76C8E403422", @"False" ); // Enable Debug
            RockMigrationHelper.AddBlockAttributeValue( "A4212ABD-27BA-4028-B341-FBFCB98C6A4C", "81D7C7A0-5469-419A-9A4D-149511DB7271", @"4b8cc732-cc87-45ce-93ba-f9caa46bb99e,fab75ec6-0402-456a-be34-252097de4f20" ); // Include Group Types
            RockMigrationHelper.AddBlockAttributeValue( "E2EC1261-E0C7-4409-9D2E-531279AEC435", "0A49DABE-42EE-40E5-9E06-0E6530944865", @"" ); // Include Page List
            RockMigrationHelper.AddBlockAttributeValue( "E2EC1261-E0C7-4409-9D2E-531279AEC435", "C80209A8-D9E0-4877-A8E3-1F7DBF64D4C2", @"False" ); // Is Secondary Block
            RockMigrationHelper.AddBlockAttributeValue( "E2EC1261-E0C7-4409-9D2E-531279AEC435", "E4CF237D-1D12-4C93-AFD7-78EB296C4B69", @"False" ); // Include Current QueryString
            RockMigrationHelper.AddBlockAttributeValue( "E2EC1261-E0C7-4409-9D2E-531279AEC435", "2EF904CD-976E-4489-8C18-9BA43885ACD9", @"False" ); // Enable Debug
            RockMigrationHelper.AddBlockAttributeValue( "E2EC1261-E0C7-4409-9D2E-531279AEC435", "7A2010F0-0C0C-4CC5-A29B-9CBAE4DE3A22", @"" ); // CSS File
            RockMigrationHelper.AddBlockAttributeValue( "E2EC1261-E0C7-4409-9D2E-531279AEC435", "EEE71DDE-C6BC-489B-BAA5-1753E322F183", @"False" ); // Include Current Parameters
            RockMigrationHelper.AddBlockAttributeValue( "E2EC1261-E0C7-4409-9D2E-531279AEC435", "1322186A-862A-4CF1-B349-28ECB67229BA", @"{% assign queryParms = 'Global' | Page:'QueryString'  %}
{% for item in queryParms %}
    {% assign kvItem = item | PropertyToKeyValue %}
        {% if kvItem.Key == ""GroupId""%}
            {%assign GroupId=kvItem.Value%}
        {%endif%}
{% endfor %}

{% if Page.Pages != empty %}
    {% assign first = true%}
    <div class=""row"">
	{% for childPage in Page.Pages %}
	    {% if first == true %}
		<div class=""col-xs-12"" style=""padding-bottom:20px;"">
		    {% assign first = false %}
		{% else %}
		<div class=""col-xs-12 col-sm-6"" style=""padding-bottom:20px;"">    
		{% endif %}
		<a class=""btn btn-default btn-lg btn-block"" href=""{{ childPage.Url }}?GroupId={{GroupId}}"">
		    <i class=""{{childPage.IconCssClass}} fa-5x""></i>
		    <br>
		    {{ childPage.Title }}
		</a>
		</div>
    {% endfor %}
    </div>
{% endif %}
" ); // Template
            RockMigrationHelper.AddBlockAttributeValue( "E2EC1261-E0C7-4409-9D2E-531279AEC435", "41F1C42E-2395-4063-BD4F-031DF8D5B231", @"37053063-76c5-44eb-bc0a-043f10744cec" ); // Root Page
            RockMigrationHelper.AddBlockAttributeValue( "E2EC1261-E0C7-4409-9D2E-531279AEC435", "6C952052-BC79-41BA-8B88-AB8EA3E99648", @"2" ); // Number of Levels
            RockMigrationHelper.AddBlockAttributeValue( "4B89477A-5401-47F3-A56A-8ED535C65253", "AEB81535-5E99-4AAB-9379-25C0C68111EC", @"	<h1>{{ Group.Name }}</h1>
" ); // Lava Template
            RockMigrationHelper.AddBlockAttributeValue( "4B89477A-5401-47F3-A56A-8ED535C65253", "7B080222-D905-4A4A-A1F7-8175D511F6E5", @"False" ); // Enable Location Edit
            RockMigrationHelper.AddBlockAttributeValue( "4B89477A-5401-47F3-A56A-8ED535C65253", "29BCAE3E-7C98-4765-8804-AE6F7D326A18", @"" ); // Edit Group Pre-HTML
            RockMigrationHelper.AddBlockAttributeValue( "4B89477A-5401-47F3-A56A-8ED535C65253", "B8273C24-A51C-4FCD-A29C-302133F7D236", @"" ); // Edit Group Post-HTML
            RockMigrationHelper.AddBlockAttributeValue( "4B89477A-5401-47F3-A56A-8ED535C65253", "0162D261-2E91-4855-8FE4-572F6F7BBDDB", @"" ); // Edit Group Member Pre-HTML
            RockMigrationHelper.AddBlockAttributeValue( "4B89477A-5401-47F3-A56A-8ED535C65253", "0DADE5FA-9FD2-4C95-B30C-4DE4A1FB66B5", @"" ); // Edit Group Member Post-HTML
            RockMigrationHelper.AddBlockAttributeValue( "4B89477A-5401-47F3-A56A-8ED535C65253", "E8BD8616-33CE-4017-B4BA-E04F0FDE231A", @"False" ); // Enable Debug
            RockMigrationHelper.AddBlockAttributeValue( "4B89477A-5401-47F3-A56A-8ED535C65253", "A66FF680-1814-4B9C-B4B2-1B24F0C39AE1", @"20a6336a-c9f8-4f55-94bf-991b0d3b49d7" ); // Roster Page
            RockMigrationHelper.AddBlockAttributeValue( "4B89477A-5401-47F3-A56A-8ED535C65253", "9ECD9F72-5CDE-406F-99D1-5278CD3D0683", @"20a6336a-c9f8-4f55-94bf-991b0d3b49d7" ); // Attendance Page
            RockMigrationHelper.AddBlockAttributeValue( "4B89477A-5401-47F3-A56A-8ED535C65253", "A79C6325-80B6-4F79-9495-8764BAE61382", @"20a6336a-c9f8-4f55-94bf-991b0d3b49d7" ); // Communication Page
            RockMigrationHelper.AddBlockAttributeValue( "4B89477A-5401-47F3-A56A-8ED535C65253", "703D766A-F1BC-46FC-8176-7B42E4E41C48", @"False" ); // Hide the 'Active' Group checkbox
            RockMigrationHelper.AddBlockAttributeValue( "4B89477A-5401-47F3-A56A-8ED535C65253", "F2F93D98-2F53-44AB-8B1E-284F0A306325", @"False" ); // Hide Inactive Group Member Status
            RockMigrationHelper.AddBlockAttributeValue( "4B89477A-5401-47F3-A56A-8ED535C65253", "E23ABB40-1D0D-43ED-A6D1-554E27D0F121", @"False" ); // Hide Group Description Edit
            RockMigrationHelper.AddBlockAttributeValue( "2731B0B8-376F-4350-9F83-E9487B92B693", "9D85ACFF-5A23-4DDC-A710-28EC476BC9FD", @"" ); // Attendance Roster Template
            RockMigrationHelper.AddBlockAttributeValue( "2731B0B8-376F-4350-9F83-E9487B92B693", "EF07BDC4-1E71-4A97-8804-AA8884A06C06", @"True" ); // Auto Count
            RockMigrationHelper.AddBlockAttributeValue( "2731B0B8-376F-4350-9F83-E9487B92B693", "04DCC287-C62D-4ED6-9385-63AB85FE4B8B", @"Head Count:" ); // Count Label
            RockMigrationHelper.AddBlockAttributeValue( "2731B0B8-376F-4350-9F83-E9487B92B693", "70F85FB0-ED49-489D-9C77-47AE60B97199", @"False" ); // Allow Add Person
            RockMigrationHelper.AddBlockAttributeValue( "2731B0B8-376F-4350-9F83-E9487B92B693", "33877A18-B34B-4B7C-AD20-025265A1198D", @"True" ); // Allow Add Date
            RockMigrationHelper.AddBlockAttributeValue( "D45B7887-C728-4658-9865-095398CB8C84", "733695E3-C5EB-4F01-A033-A70EB58B4747", @"5" ); // Count
            RockMigrationHelper.AddBlockAttributeValue( "D45B7887-C728-4658-9865-095398CB8C84", "182F3813-C8B3-46F1-9F51-84C6358C2F51", @"a6156e09-6f7a-4021-8826-d6140507db03" ); // ItemType
            RockMigrationHelper.AddBlockAttributeValue( "D45B7887-C728-4658-9865-095398CB8C84", "02F380AC-384E-4471-87AB-E14AEDBA8A24", @"False" ); // ShowSidebar
            RockMigrationHelper.AddBlockAttributeValue( "D45B7887-C728-4658-9865-095398CB8C84", "CFAF57F4-516B-4C5D-9601-9282264EBE80", @"" ); // Meta Description Attribute
            RockMigrationHelper.AddBlockAttributeValue( "D45B7887-C728-4658-9865-095398CB8C84", "4025AE25-7C98-42E2-BDC8-D620E1DE3E71", @"0" ); // Cache Duration
            RockMigrationHelper.AddBlockAttributeValue( "D45B7887-C728-4658-9865-095398CB8C84", "405E1E2E-6E81-4279-90AA-0407204EDC12", @"False" ); // Enable Debug
            RockMigrationHelper.AddBlockAttributeValue( "D45B7887-C728-4658-9865-095398CB8C84", "6EA89B4A-ED9C-4278-9A23-68F7C0539433", @"2" ); // Status
            RockMigrationHelper.AddBlockAttributeValue( "D45B7887-C728-4658-9865-095398CB8C84", "7F175B06-4E2C-4CDC-B71D-7BA5E6A4AC6B", @"True" ); // Query Parameter Filtering
            RockMigrationHelper.AddBlockAttributeValue( "D45B7887-C728-4658-9865-095398CB8C84", "51F35B54-E661-4F98-9F19-7520491E40BF", @"" ); // Order
            RockMigrationHelper.AddBlockAttributeValue( "D45B7887-C728-4658-9865-095398CB8C84", "FB9A5408-544A-4CAC-9C31-33737CE63C16", @"True" ); // Merge Content
            RockMigrationHelper.AddBlockAttributeValue( "D45B7887-C728-4658-9865-095398CB8C84", "62DED4DC-6790-438B-B964-2728094DCECE", @"False" ); // Set Page Title
            RockMigrationHelper.AddBlockAttributeValue( "D45B7887-C728-4658-9865-095398CB8C84", "A060D39E-9635-4592-BF53-9D110B56B570", @"" ); // Meta Image Attribute
            RockMigrationHelper.AddBlockAttributeValue( "663764A7-5D96-4C66-9907-63DC94D88787", "A060D39E-9635-4592-BF53-9D110B56B570", @"" ); // Meta Image Attribute
            RockMigrationHelper.AddBlockAttributeValue( "663764A7-5D96-4C66-9907-63DC94D88787", "733695E3-C5EB-4F01-A033-A70EB58B4747", @"5" ); // Count
            RockMigrationHelper.AddBlockAttributeValue( "663764A7-5D96-4C66-9907-63DC94D88787", "FB9A5408-544A-4CAC-9C31-33737CE63C16", @"True" ); // Merge Content
            RockMigrationHelper.AddBlockAttributeValue( "663764A7-5D96-4C66-9907-63DC94D88787", "62DED4DC-6790-438B-B964-2728094DCECE", @"False" ); // Set Page Title
            RockMigrationHelper.AddBlockAttributeValue( "663764A7-5D96-4C66-9907-63DC94D88787", "7F175B06-4E2C-4CDC-B71D-7BA5E6A4AC6B", @"True" ); // Query Parameter Filtering
            RockMigrationHelper.AddBlockAttributeValue( "663764A7-5D96-4C66-9907-63DC94D88787", "51F35B54-E661-4F98-9F19-7520491E40BF", @"" ); // Order
            RockMigrationHelper.AddBlockAttributeValue( "663764A7-5D96-4C66-9907-63DC94D88787", "405E1E2E-6E81-4279-90AA-0407204EDC12", @"False" ); // Enable Debug
            RockMigrationHelper.AddBlockAttributeValue( "663764A7-5D96-4C66-9907-63DC94D88787", "6EA89B4A-ED9C-4278-9A23-68F7C0539433", @"2" ); // Status
            RockMigrationHelper.AddBlockAttributeValue( "663764A7-5D96-4C66-9907-63DC94D88787", "CFAF57F4-516B-4C5D-9601-9282264EBE80", @"" ); // Meta Description Attribute
            RockMigrationHelper.AddBlockAttributeValue( "663764A7-5D96-4C66-9907-63DC94D88787", "4025AE25-7C98-42E2-BDC8-D620E1DE3E71", @"3600" ); // Cache Duration
            RockMigrationHelper.AddBlockAttributeValue( "663764A7-5D96-4C66-9907-63DC94D88787", "182F3813-C8B3-46F1-9F51-84C6358C2F51", @"37718f91-ce72-416a-a81d-69505ac900e5" ); // ItemType
            RockMigrationHelper.AddBlockAttributeValue( "663764A7-5D96-4C66-9907-63DC94D88787", "02F380AC-384E-4471-87AB-E14AEDBA8A24", @"True" ); // ShowSidebar
            RockMigrationHelper.AddBlockAttributeValue( "7A183520-7216-4CB5-A7B8-0AD80E349A62", "0A49DABE-42EE-40E5-9E06-0E6530944865", @"" ); // Include Page List
            RockMigrationHelper.AddBlockAttributeValue( "7A183520-7216-4CB5-A7B8-0AD80E349A62", "C80209A8-D9E0-4877-A8E3-1F7DBF64D4C2", @"False" ); // Is Secondary Block
            RockMigrationHelper.AddBlockAttributeValue( "7A183520-7216-4CB5-A7B8-0AD80E349A62", "2EF904CD-976E-4489-8C18-9BA43885ACD9", @"False" ); // Enable Debug
            RockMigrationHelper.AddBlockAttributeValue( "7A183520-7216-4CB5-A7B8-0AD80E349A62", "41F1C42E-2395-4063-BD4F-031DF8D5B231", @"37053063-76c5-44eb-bc0a-043f10744cec" ); // Root Page
            RockMigrationHelper.AddBlockAttributeValue( "7A183520-7216-4CB5-A7B8-0AD80E349A62", "E4CF237D-1D12-4C93-AFD7-78EB296C4B69", @"False" ); // Include Current QueryString
            RockMigrationHelper.AddBlockAttributeValue( "7A183520-7216-4CB5-A7B8-0AD80E349A62", "6C952052-BC79-41BA-8B88-AB8EA3E99648", @"3" ); // Number of Levels
            RockMigrationHelper.AddBlockAttributeValue( "7A183520-7216-4CB5-A7B8-0AD80E349A62", "EEE71DDE-C6BC-489B-BAA5-1753E322F183", @"False" ); // Include Current Parameters
            RockMigrationHelper.AddBlockAttributeValue( "7A183520-7216-4CB5-A7B8-0AD80E349A62", "1322186A-862A-4CF1-B349-28ECB67229BA", @"{% assign queryParms = 'Global' | Page:'QueryString'  %}
{% for item in queryParms %}
    {% assign kvItem = item | PropertyToKeyValue %}
        {% if kvItem.Key == ""GroupId""%}
            {%assign GroupId=kvItem.Value%}
        {%endif%}
{% endfor %}

{% if Page.DisplayChildPages == 'true' and Page.Pages != empty %}
    <ul class=""nav navbar-nav"">

		<li><a href=""{{ Page.Url }}?GroupId={{GroupId}}"">{{ Page.Title }}</a></li>

		{% for childPage in Page.Pages %}
            <li{% if childPage.Current == 'true' %} class=""active""{% endif%}>
				<a href=""{{ childPage.Url }}?GroupId={{GroupId}}"">{{ childPage.Title }}</a>
            </li>
        {% endfor %}
    </ul>
{% endif %}" ); // Template
            RockMigrationHelper.AddBlockAttributeValue( "7A183520-7216-4CB5-A7B8-0AD80E349A62", "7A2010F0-0C0C-4CC5-A29B-9CBAE4DE3A22", @"" ); // CSS File
            RockMigrationHelper.AddBlockAttributeValue( "1970E8C3-0415-4253-984E-F2EFE7C9D4FF", "7A2010F0-0C0C-4CC5-A29B-9CBAE4DE3A22", @"" ); // CSS File
            RockMigrationHelper.AddBlockAttributeValue( "1970E8C3-0415-4253-984E-F2EFE7C9D4FF", "1322186A-862A-4CF1-B349-28ECB67229BA", @"{% include '~~/Assets/Lava/PageNav.lava' %}" ); // Template
            RockMigrationHelper.AddBlockAttributeValue( "1970E8C3-0415-4253-984E-F2EFE7C9D4FF", "EEE71DDE-C6BC-489B-BAA5-1753E322F183", @"False" ); // Include Current Parameters
            RockMigrationHelper.AddBlockAttributeValue( "1970E8C3-0415-4253-984E-F2EFE7C9D4FF", "6C952052-BC79-41BA-8B88-AB8EA3E99648", @"3" ); // Number of Levels
            RockMigrationHelper.AddBlockAttributeValue( "1970E8C3-0415-4253-984E-F2EFE7C9D4FF", "E4CF237D-1D12-4C93-AFD7-78EB296C4B69", @"False" ); // Include Current QueryString
            RockMigrationHelper.AddBlockAttributeValue( "1970E8C3-0415-4253-984E-F2EFE7C9D4FF", "41F1C42E-2395-4063-BD4F-031DF8D5B231", @"20a6336a-c9f8-4f55-94bf-991b0d3b49d7" ); // Root Page
            RockMigrationHelper.AddBlockAttributeValue( "1970E8C3-0415-4253-984E-F2EFE7C9D4FF", "2EF904CD-976E-4489-8C18-9BA43885ACD9", @"False" ); // Enable Debug
            RockMigrationHelper.AddBlockAttributeValue( "1970E8C3-0415-4253-984E-F2EFE7C9D4FF", "C80209A8-D9E0-4877-A8E3-1F7DBF64D4C2", @"False" ); // Is Secondary Block
            RockMigrationHelper.AddBlockAttributeValue( "1970E8C3-0415-4253-984E-F2EFE7C9D4FF", "0A49DABE-42EE-40E5-9E06-0E6530944865", @"" ); // Include Page List
            RockMigrationHelper.AddBlockAttributeValue( "7E56CA79-433A-459A-9E7E-0EEE9433BD34", "0A49DABE-42EE-40E5-9E06-0E6530944865", @"" ); // Include Page List
            RockMigrationHelper.AddBlockAttributeValue( "7E56CA79-433A-459A-9E7E-0EEE9433BD34", "C80209A8-D9E0-4877-A8E3-1F7DBF64D4C2", @"False" ); // Is Secondary Block
            RockMigrationHelper.AddBlockAttributeValue( "7E56CA79-433A-459A-9E7E-0EEE9433BD34", "2EF904CD-976E-4489-8C18-9BA43885ACD9", @"False" ); // Enable Debug
            RockMigrationHelper.AddBlockAttributeValue( "7E56CA79-433A-459A-9E7E-0EEE9433BD34", "E4CF237D-1D12-4C93-AFD7-78EB296C4B69", @"False" ); // Include Current QueryString
            RockMigrationHelper.AddBlockAttributeValue( "7E56CA79-433A-459A-9E7E-0EEE9433BD34", "6C952052-BC79-41BA-8B88-AB8EA3E99648", @"3" ); // Number of Levels
            RockMigrationHelper.AddBlockAttributeValue( "7E56CA79-433A-459A-9E7E-0EEE9433BD34", "EEE71DDE-C6BC-489B-BAA5-1753E322F183", @"False" ); // Include Current Parameters
            RockMigrationHelper.AddBlockAttributeValue( "7E56CA79-433A-459A-9E7E-0EEE9433BD34", "41F1C42E-2395-4063-BD4F-031DF8D5B231", @"37053063-76c5-44eb-bc0a-043f10744cec" ); // Root Page
            RockMigrationHelper.AddBlockAttributeValue( "7E56CA79-433A-459A-9E7E-0EEE9433BD34", "1322186A-862A-4CF1-B349-28ECB67229BA", @"{% assign queryParms = 'Global' | Page:'QueryString'  %}
{% for item in queryParms %}
    {% assign kvItem = item | PropertyToKeyValue %}
        {% if kvItem.Key == ""GroupId""%}
            {%assign GroupId=kvItem.Value%}
        {%endif%}
{% endfor %}

{% if Page.DisplayChildPages == 'true' and Page.Pages != empty %}
    <ul class=""nav navbar-nav"">

		<li><a href=""{{ Page.Url }}?GroupId={{GroupId}}"">{{ Page.Title }}</a></li>

		{% for childPage in Page.Pages %}
            <li{% if childPage.Current == 'true' %} class=""active""{% endif%}>
				<a href=""{{ childPage.Url }}?GroupId={{GroupId}}"">{{ childPage.Title }}</a>
            </li>
        {% endfor %}
    </ul>
{% endif %}" ); // Template
            RockMigrationHelper.AddBlockAttributeValue( "7E56CA79-433A-459A-9E7E-0EEE9433BD34", "7A2010F0-0C0C-4CC5-A29B-9CBAE4DE3A22", @"" ); // CSS File
            RockMigrationHelper.AddBlockAttributeValue( "196CAEBC-DB97-4903-92BB-5FFEE466F8E7", "7A2010F0-0C0C-4CC5-A29B-9CBAE4DE3A22", @"" ); // CSS File
            RockMigrationHelper.AddBlockAttributeValue( "196CAEBC-DB97-4903-92BB-5FFEE466F8E7", "1322186A-862A-4CF1-B349-28ECB67229BA", @"{% assign queryParms = 'Global' | Page:'QueryString'  %}
{% for item in queryParms %}
    {% assign kvItem = item | PropertyToKeyValue %}
        {% if kvItem.Key == ""GroupId""%}
            {%assign GroupId=kvItem.Value%}
        {%endif%}
{% endfor %}

{% if Page.DisplayChildPages == 'true' and Page.Pages != empty %}
    <ul class=""nav navbar-nav"">

		<li><a href=""{{ Page.Url }}?GroupId={{GroupId}}"">{{ Page.Title }}</a></li>

		{% for childPage in Page.Pages %}
            <li{% if childPage.Current == 'true' %} class=""active""{% endif%}>
				<a href=""{{ childPage.Url }}?GroupId={{GroupId}}"">{{ childPage.Title }}</a>
            </li>
        {% endfor %}
    </ul>
{% endif %}" ); // Template
            RockMigrationHelper.AddBlockAttributeValue( "196CAEBC-DB97-4903-92BB-5FFEE466F8E7", "41F1C42E-2395-4063-BD4F-031DF8D5B231", @"37053063-76c5-44eb-bc0a-043f10744cec" ); // Root Page
            RockMigrationHelper.AddBlockAttributeValue( "196CAEBC-DB97-4903-92BB-5FFEE466F8E7", "EEE71DDE-C6BC-489B-BAA5-1753E322F183", @"False" ); // Include Current Parameters
            RockMigrationHelper.AddBlockAttributeValue( "196CAEBC-DB97-4903-92BB-5FFEE466F8E7", "6C952052-BC79-41BA-8B88-AB8EA3E99648", @"3" ); // Number of Levels
            RockMigrationHelper.AddBlockAttributeValue( "196CAEBC-DB97-4903-92BB-5FFEE466F8E7", "E4CF237D-1D12-4C93-AFD7-78EB296C4B69", @"False" ); // Include Current QueryString
            RockMigrationHelper.AddBlockAttributeValue( "196CAEBC-DB97-4903-92BB-5FFEE466F8E7", "2EF904CD-976E-4489-8C18-9BA43885ACD9", @"False" ); // Enable Debug
            RockMigrationHelper.AddBlockAttributeValue( "196CAEBC-DB97-4903-92BB-5FFEE466F8E7", "C80209A8-D9E0-4877-A8E3-1F7DBF64D4C2", @"False" ); // Is Secondary Block
            RockMigrationHelper.AddBlockAttributeValue( "196CAEBC-DB97-4903-92BB-5FFEE466F8E7", "0A49DABE-42EE-40E5-9E06-0E6530944865", @"" ); // Include Page List
            RockMigrationHelper.AddBlockAttributeValue( "D69774E9-1867-4E34-B53E-5C8DD558BF4F", "0A49DABE-42EE-40E5-9E06-0E6530944865", @"" ); // Include Page List
            RockMigrationHelper.AddBlockAttributeValue( "D69774E9-1867-4E34-B53E-5C8DD558BF4F", "2EF904CD-976E-4489-8C18-9BA43885ACD9", @"False" ); // Enable Debug
            RockMigrationHelper.AddBlockAttributeValue( "D69774E9-1867-4E34-B53E-5C8DD558BF4F", "E4CF237D-1D12-4C93-AFD7-78EB296C4B69", @"False" ); // Include Current QueryString
            RockMigrationHelper.AddBlockAttributeValue( "D69774E9-1867-4E34-B53E-5C8DD558BF4F", "C80209A8-D9E0-4877-A8E3-1F7DBF64D4C2", @"False" ); // Is Secondary Block
            RockMigrationHelper.AddBlockAttributeValue( "D69774E9-1867-4E34-B53E-5C8DD558BF4F", "6C952052-BC79-41BA-8B88-AB8EA3E99648", @"3" ); // Number of Levels
            RockMigrationHelper.AddBlockAttributeValue( "D69774E9-1867-4E34-B53E-5C8DD558BF4F", "EEE71DDE-C6BC-489B-BAA5-1753E322F183", @"False" ); // Include Current Parameters
            RockMigrationHelper.AddBlockAttributeValue( "D69774E9-1867-4E34-B53E-5C8DD558BF4F", "41F1C42E-2395-4063-BD4F-031DF8D5B231", @"37053063-76c5-44eb-bc0a-043f10744cec" ); // Root Page
            RockMigrationHelper.AddBlockAttributeValue( "D69774E9-1867-4E34-B53E-5C8DD558BF4F", "1322186A-862A-4CF1-B349-28ECB67229BA", @"{% assign queryParms = 'Global' | Page:'QueryString'  %}
{% for item in queryParms %}
    {% assign kvItem = item | PropertyToKeyValue %}
        {% if kvItem.Key == ""GroupId""%}
            {%assign GroupId=kvItem.Value%}
        {%endif%}
{% endfor %}

{% if Page.DisplayChildPages == 'true' and Page.Pages != empty %}
    <ul class=""nav navbar-nav"">

		<li><a href=""{{ Page.Url }}?GroupId={{GroupId}}"">{{ Page.Title }}</a></li>

		{% for childPage in Page.Pages %}
            <li{% if childPage.Current == 'true' %} class=""active""{% endif%}>
				<a href=""{{ childPage.Url }}?GroupId={{GroupId}}"">{{ childPage.Title }}</a>
            </li>
        {% endfor %}
    </ul>
{% endif %}" ); // Template
            RockMigrationHelper.AddBlockAttributeValue( "D69774E9-1867-4E34-B53E-5C8DD558BF4F", "7A2010F0-0C0C-4CC5-A29B-9CBAE4DE3A22", @"" ); // CSS File
            RockMigrationHelper.AddBlockAttributeValue( "813C6600-1B46-4FDB-965F-CDDA77E17949", "7A2010F0-0C0C-4CC5-A29B-9CBAE4DE3A22", @"" ); // CSS File
            RockMigrationHelper.AddBlockAttributeValue( "813C6600-1B46-4FDB-965F-CDDA77E17949", "EEE71DDE-C6BC-489B-BAA5-1753E322F183", @"False" ); // Include Current Parameters
            RockMigrationHelper.AddBlockAttributeValue( "813C6600-1B46-4FDB-965F-CDDA77E17949", "1322186A-862A-4CF1-B349-28ECB67229BA", @"{% assign queryParms = 'Global' | Page:'QueryString'  %}
{% for item in queryParms %}
    {% assign kvItem = item | PropertyToKeyValue %}
        {% if kvItem.Key == ""GroupId""%}
            {%assign GroupId=kvItem.Value%}
        {%endif%}
{% endfor %}

{% if Page.DisplayChildPages == 'true' and Page.Pages != empty %}
    <ul class=""nav navbar-nav"">

		<li><a href=""{{ Page.Url }}?GroupId={{GroupId}}"">{{ Page.Title }}</a></li>

		{% for childPage in Page.Pages %}
            <li{% if childPage.Current == 'true' %} class=""active""{% endif%}>
				<a href=""{{ childPage.Url }}?GroupId={{GroupId}}"">{{ childPage.Title }}</a>
            </li>
        {% endfor %}
    </ul>
{% endif %}" ); // Template
            RockMigrationHelper.AddBlockAttributeValue( "813C6600-1B46-4FDB-965F-CDDA77E17949", "41F1C42E-2395-4063-BD4F-031DF8D5B231", @"37053063-76c5-44eb-bc0a-043f10744cec" ); // Root Page
            RockMigrationHelper.AddBlockAttributeValue( "813C6600-1B46-4FDB-965F-CDDA77E17949", "6C952052-BC79-41BA-8B88-AB8EA3E99648", @"3" ); // Number of Levels
            RockMigrationHelper.AddBlockAttributeValue( "813C6600-1B46-4FDB-965F-CDDA77E17949", "E4CF237D-1D12-4C93-AFD7-78EB296C4B69", @"False" ); // Include Current QueryString
            RockMigrationHelper.AddBlockAttributeValue( "813C6600-1B46-4FDB-965F-CDDA77E17949", "2EF904CD-976E-4489-8C18-9BA43885ACD9", @"False" ); // Enable Debug
            RockMigrationHelper.AddBlockAttributeValue( "813C6600-1B46-4FDB-965F-CDDA77E17949", "C80209A8-D9E0-4877-A8E3-1F7DBF64D4C2", @"False" ); // Is Secondary Block
            RockMigrationHelper.AddBlockAttributeValue( "813C6600-1B46-4FDB-965F-CDDA77E17949", "0A49DABE-42EE-40E5-9E06-0E6530944865", @"" ); // Include Page List
            RockMigrationHelper.AddBlockAttributeValue( "81D1AFD6-0448-40D5-8949-DE6A5A93D6F2", "E2BDA459-3E06-42CB-8576-DAA6C67B27E6", @"283999ec-7346-42e3-b807-bce9b2babb49" ); // Record Status
            RockMigrationHelper.AddBlockAttributeValue( "81D1AFD6-0448-40D5-8949-DE6A5A93D6F2", "B03A08AC-7594-4431-BAE5-DA344D0CF491", @"368dd475-242c-49c4-a42c-7278be690cc2" ); // Connection Status
            RockMigrationHelper.AddBlockAttributeValue( "81D1AFD6-0448-40D5-8949-DE6A5A93D6F2", "3D899681-933D-4663-BA62-9B3089308D12", @"1" ); // Group Member Status
            RockMigrationHelper.AddBlockAttributeValue( "81D1AFD6-0448-40D5-8949-DE6A5A93D6F2", "B0CBD860-C135-48CC-82BD-B1462DC4B9BF", @"False" ); // Large Button
            RockMigrationHelper.AddBlockAttributeValue( "81D1AFD6-0448-40D5-8949-DE6A5A93D6F2", "6DB1A25B-A2B3-4334-97CE-69B23C064424", @"btn btn-default" ); // CSS Class
        }
        public override void Down()
        {
            RockMigrationHelper.DeleteAttribute( "210745A8-8537-41CA-BC4E-AB02E29EE0EA" );
            RockMigrationHelper.DeleteAttribute( "80D0F9DD-7780-46A8-97F4-8D8A97359BEC" );
            RockMigrationHelper.DeleteAttribute( "04DCC287-C62D-4ED6-9385-63AB85FE4B8B" );
            RockMigrationHelper.DeleteAttribute( "EF07BDC4-1E71-4A97-8804-AA8884A06C06" );
            RockMigrationHelper.DeleteAttribute( "6DB1A25B-A2B3-4334-97CE-69B23C064424" );
            RockMigrationHelper.DeleteAttribute( "B0CBD860-C135-48CC-82BD-B1462DC4B9BF" );
            RockMigrationHelper.DeleteAttribute( "3D899681-933D-4663-BA62-9B3089308D12" );
            RockMigrationHelper.DeleteAttribute( "B03A08AC-7594-4431-BAE5-DA344D0CF491" );
            RockMigrationHelper.DeleteAttribute( "E2BDA459-3E06-42CB-8576-DAA6C67B27E6" );
            RockMigrationHelper.DeleteAttribute( "33877A18-B34B-4B7C-AD20-025265A1198D" );
            RockMigrationHelper.DeleteAttribute( "70F85FB0-ED49-489D-9C77-47AE60B97199" );
            RockMigrationHelper.DeleteAttribute( "02F380AC-384E-4471-87AB-E14AEDBA8A24" );
            RockMigrationHelper.DeleteAttribute( "182F3813-C8B3-46F1-9F51-84C6358C2F51" );
            RockMigrationHelper.DeleteAttribute( "C58A85CC-FDC3-4067-BA7D-C8E2C5934E57" );
            RockMigrationHelper.DeleteAttribute( "733695E3-C5EB-4F01-A033-A70EB58B4747" );
            RockMigrationHelper.DeleteAttribute( "A060D39E-9635-4592-BF53-9D110B56B570" );
            RockMigrationHelper.DeleteAttribute( "62DED4DC-6790-438B-B964-2728094DCECE" );
            RockMigrationHelper.DeleteAttribute( "FB9A5408-544A-4CAC-9C31-33737CE63C16" );
            RockMigrationHelper.DeleteAttribute( "51F35B54-E661-4F98-9F19-7520491E40BF" );
            RockMigrationHelper.DeleteAttribute( "7F175B06-4E2C-4CDC-B71D-7BA5E6A4AC6B" );
            RockMigrationHelper.DeleteAttribute( "6EA89B4A-ED9C-4278-9A23-68F7C0539433" );
            RockMigrationHelper.DeleteAttribute( "405E1E2E-6E81-4279-90AA-0407204EDC12" );
            RockMigrationHelper.DeleteAttribute( "4025AE25-7C98-42E2-BDC8-D620E1DE3E71" );
            RockMigrationHelper.DeleteAttribute( "CFAF57F4-516B-4C5D-9601-9282264EBE80" );
            RockMigrationHelper.DeleteAttribute( "9D85ACFF-5A23-4DDC-A710-28EC476BC9FD" );
            RockMigrationHelper.DeleteAttribute( "E23ABB40-1D0D-43ED-A6D1-554E27D0F121" );
            RockMigrationHelper.DeleteAttribute( "F2F93D98-2F53-44AB-8B1E-284F0A306325" );
            RockMigrationHelper.DeleteAttribute( "703D766A-F1BC-46FC-8176-7B42E4E41C48" );
            RockMigrationHelper.DeleteAttribute( "0A49DABE-42EE-40E5-9E06-0E6530944865" );
            RockMigrationHelper.DeleteAttribute( "A79C6325-80B6-4F79-9495-8764BAE61382" );
            RockMigrationHelper.DeleteAttribute( "9ECD9F72-5CDE-406F-99D1-5278CD3D0683" );
            RockMigrationHelper.DeleteAttribute( "A66FF680-1814-4B9C-B4B2-1B24F0C39AE1" );
            RockMigrationHelper.DeleteAttribute( "6B23DC5D-3E1B-41CE-912E-7DE231B5B2F3" );
            RockMigrationHelper.DeleteAttribute( "C1AFBB38-7C45-4391-B332-ABB0F66F8F14" );
            RockMigrationHelper.DeleteAttribute( "E8BD8616-33CE-4017-B4BA-E04F0FDE231A" );
            RockMigrationHelper.DeleteAttribute( "0DADE5FA-9FD2-4C95-B30C-4DE4A1FB66B5" );
            RockMigrationHelper.DeleteAttribute( "0162D261-2E91-4855-8FE4-572F6F7BBDDB" );
            RockMigrationHelper.DeleteAttribute( "B8273C24-A51C-4FCD-A29C-302133F7D236" );
            RockMigrationHelper.DeleteAttribute( "29BCAE3E-7C98-4765-8804-AE6F7D326A18" );
            RockMigrationHelper.DeleteAttribute( "7B080222-D905-4A4A-A1F7-8175D511F6E5" );
            RockMigrationHelper.DeleteAttribute( "AEB81535-5E99-4AAB-9379-25C0C68111EC" );
            RockMigrationHelper.DeleteAttribute( "04AA4BAE-9E55-45C2-ABAB-F85B1F81596E" );
            RockMigrationHelper.DeleteAttribute( "81D7C7A0-5469-419A-9A4D-149511DB7271" );
            RockMigrationHelper.DeleteAttribute( "89B144BE-8ECD-42AC-97A6-C76C8E403422" );
            RockMigrationHelper.DeleteAttribute( "C7EC3847-7419-4364-98E8-09FE42A04A76" );
            RockMigrationHelper.DeleteAttribute( "13921BE2-C0D4-4FD6-841F-36022B56DB54" );
            RockMigrationHelper.DeleteAttribute( "C80209A8-D9E0-4877-A8E3-1F7DBF64D4C2" );
            RockMigrationHelper.DeleteAttribute( "2EF904CD-976E-4489-8C18-9BA43885ACD9" );
            RockMigrationHelper.DeleteAttribute( "E4CF237D-1D12-4C93-AFD7-78EB296C4B69" );
            RockMigrationHelper.DeleteAttribute( "6C952052-BC79-41BA-8B88-AB8EA3E99648" );
            RockMigrationHelper.DeleteAttribute( "41F1C42E-2395-4063-BD4F-031DF8D5B231" );
            RockMigrationHelper.DeleteAttribute( "1322186A-862A-4CF1-B349-28ECB67229BA" );
            RockMigrationHelper.DeleteAttribute( "EEE71DDE-C6BC-489B-BAA5-1753E322F183" );
            RockMigrationHelper.DeleteAttribute( "7A2010F0-0C0C-4CC5-A29B-9CBAE4DE3A22" );
            RockMigrationHelper.DeleteBlock( "420DA9C4-27A4-46F0-951C-2935231E5E12" );
            RockMigrationHelper.DeleteBlock( "81D1AFD6-0448-40D5-8949-DE6A5A93D6F2" );
            RockMigrationHelper.DeleteBlock( "813C6600-1B46-4FDB-965F-CDDA77E17949" );
            RockMigrationHelper.DeleteBlock( "D69774E9-1867-4E34-B53E-5C8DD558BF4F" );
            RockMigrationHelper.DeleteBlock( "196CAEBC-DB97-4903-92BB-5FFEE466F8E7" );
            RockMigrationHelper.DeleteBlock( "7E56CA79-433A-459A-9E7E-0EEE9433BD34" );
            RockMigrationHelper.DeleteBlock( "1970E8C3-0415-4253-984E-F2EFE7C9D4FF" );
            RockMigrationHelper.DeleteBlock( "7A183520-7216-4CB5-A7B8-0AD80E349A62" );
            RockMigrationHelper.DeleteBlock( "663764A7-5D96-4C66-9907-63DC94D88787" );
            RockMigrationHelper.DeleteBlock( "D45B7887-C728-4658-9865-095398CB8C84" );
            RockMigrationHelper.DeleteBlock( "2731B0B8-376F-4350-9F83-E9487B92B693" );
            RockMigrationHelper.DeleteBlock( "C70E6839-D338-4B10-8C5A-EB43E65E4BA3" );
            RockMigrationHelper.DeleteBlock( "4B89477A-5401-47F3-A56A-8ED535C65253" );
            RockMigrationHelper.DeleteBlock( "E2EC1261-E0C7-4409-9D2E-531279AEC435" );
            RockMigrationHelper.DeleteBlock( "A4212ABD-27BA-4028-B341-FBFCB98C6A4C" );
            RockMigrationHelper.DeleteBlock( "4696B0F8-A461-44D2-AFEA-6DB317E13C61" );
            RockMigrationHelper.DeleteBlock( "3FBE1AA2-247F-497F-AD5C-237C4EEFBFBC" );
            RockMigrationHelper.DeletePage( "20A6336A-C9F8-4F55-94BF-991B0D3B49D7" ); //  Page: Group Manager Home Page
            RockMigrationHelper.DeletePage( "37053063-76C5-44EB-BC0A-043F10744CEC" ); //  Page: Small Group App
            RockMigrationHelper.DeletePage( "3BAD929C-832C-4FF0-97C8-FB5E8698D9D8" ); //  Page: This Week
            RockMigrationHelper.DeletePage( "8A5BB80C-BC42-4038-8F91-A9E875D8DC47" ); //  Page: Roster
            RockMigrationHelper.DeletePage( "DC25DA8D-4247-4CC4-AC40-178195E9308E" ); //  Page: Attendance
            RockMigrationHelper.DeletePage( "C0881189-7F82-4669-B4D9-8FCFD5E5DCE1" ); //  Page: Resources
            RockMigrationHelper.DeletePage( "00F0E543-F524-4A31-B040-B8613F91F854" ); //  Page: Get Help
            RockMigrationHelper.DeleteLayout( "E5998D06-BCFB-48EC-8143-64592DF8CC13" ); //  Layout: Full Width, Site: Group Manager
            RockMigrationHelper.DeleteSite( "F310E046-4434-4966-884A-96403C76CF50" );
        }
    }
}

