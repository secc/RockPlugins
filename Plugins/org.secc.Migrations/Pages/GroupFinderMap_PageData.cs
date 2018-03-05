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
    [MigrationNumber( 4, "1.2.0" )]
    public class GroupFinderMap_PageData : Migration
    {
        /// <summary>
        /// The commands to run to migrate plugin to the specific version
        /// </summary>
        public override void Up()
        {
            // Page: Group Finder Map
            RockMigrationHelper.AddPage( "7F2581A1-941E-4D51-8A9D-5BE9B881B003", "D65F783D-87A9-4CC9-8110-E83466A0EADB", "Group Finder Map", "", "779D8828-2101-4B8E-B690-E713656320DA", "" ); // Site:Rock RMS
            RockMigrationHelper.UpdateBlockType( "Group Finder Map", "Block for people to find a group that matches their search parameters.", "~/Plugins/org_secc/GroupManager/GroupFinderMap.ascx", "Groups", "2419DF52-CEE9-4281-B7FB-80C27AB63FE2" );
            RockMigrationHelper.AddBlock( "779D8828-2101-4B8E-B690-E713656320DA", "", "2419DF52-CEE9-4281-B7FB-80C27AB63FE2", "Group Finder Map", "Main", "", "", 1, "711FE071-B100-4E77-A251-BDE312A91BC3" );

            RockMigrationHelper.AddBlockTypeAttribute( "2419DF52-CEE9-4281-B7FB-80C27AB63FE2", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Group Detail Page", "GroupDetailPage", "", "The page to navigate to for group details.", 0, @"", "FD59D3A0-C4E1-4479-A632-01E17C03B71F" );

            RockMigrationHelper.AddBlockTypeAttribute( "2419DF52-CEE9-4281-B7FB-80C27AB63FE2", "99B090AA-4D7E-46D8-B393-BF945EA1BA8B", "Attribute Filters", "AttributeFilters", "", "", 0, @"", "5E41DA52-93EC-4CE2-B4BA-86D3BCA1FD58" );

            RockMigrationHelper.AddBlockTypeAttribute( "2419DF52-CEE9-4281-B7FB-80C27AB63FE2", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Show Map", "ShowMap", "", "", 0, @"False", "36369981-9DE3-47BE-B3CE-FFB17A19FEAD" );

            RockMigrationHelper.AddBlockTypeAttribute( "2419DF52-CEE9-4281-B7FB-80C27AB63FE2", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Map Height", "MapHeight", "", "", 0, @"600", "7EC6E326-4D90-4657-85F1-A93E4728F094" );

            RockMigrationHelper.AddBlockTypeAttribute( "2419DF52-CEE9-4281-B7FB-80C27AB63FE2", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Show Fence", "ShowFence", "", "", 0, @"False", "BC570D8B-69DD-4519-9EDD-80B247FE51F5" );

            RockMigrationHelper.AddBlockTypeAttribute( "2419DF52-CEE9-4281-B7FB-80C27AB63FE2", "7BDAE237-6E49-47AC-9961-A45AFB69E240", "Polygon Colors", "PolygonColors", "", "", 0, @"#f37833|#446f7a|#afd074|#649dac|#f8eba2|#92d0df|#eaf7fc", "660870D8-C57F-4C94-9B2F-DFC8C2C43D3B" );

            RockMigrationHelper.AddBlockTypeAttribute( "2419DF52-CEE9-4281-B7FB-80C27AB63FE2", "1D0D3794-C210-48A8-8C68-3FBEC08A6BA5", "Map Info", "MapInfo", "", "", 0, @"
<h4 class='margin-t-none'>{{ Group.Name }}</h4> 

<div class='margin-b-sm'>
{% for attribute in Group.AttributeValues %}
    <strong>{{ attribute.AttributeName }}:</strong> {{ attribute.ValueFormatted }} <br />
{% endfor %}
</div>

<div class='margin-v-sm'>
{% if Location.FormattedHtmlAddress && Location.FormattedHtmlAddress != '' %}
	{{ Location.FormattedHtmlAddress }}
{% endif %}
</div>

{% if LinkedPages.GroupDetailPage != '' %}
    <a class='btn btn-xs btn-action margin-r-sm' href='{{ LinkedPages.GroupDetailPage }}?GroupId={{ Group.Id }}'>View {{ Group.GroupType.GroupTerm }}</a>
{% endif %}

{% if LinkedPages.RegisterPage != '' %}
    {% if LinkedPages.RegisterPage contains '?' %}
        <a class='btn btn-xs btn-action' href='{{ LinkedPages.RegisterPage }}&GroupId={{ Group.Id }}'>Register</a>
    {% else %}
        <a class='btn btn-xs btn-action' href='{{ LinkedPages.RegisterPage }}?GroupId={{ Group.Id }}'>Register</a>
    {% endif %}
{% endif %}
", "792337A9-1070-4364-94B8-48B28C357942" );

            RockMigrationHelper.AddBlockTypeAttribute( "2419DF52-CEE9-4281-B7FB-80C27AB63FE2", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Map Info Debug", "MapInfoDebug", "", "", 0, @"False", "F7AF0726-209E-44C9-8F0F-B7C5E6B2C6D8" );

            RockMigrationHelper.AddBlockTypeAttribute( "2419DF52-CEE9-4281-B7FB-80C27AB63FE2", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Show Lava Output", "ShowLavaOutput", "", "", 0, @"False", "12EFD573-F247-4DDB-87A7-665E5B7C8EB1" );

            RockMigrationHelper.AddBlockTypeAttribute( "2419DF52-CEE9-4281-B7FB-80C27AB63FE2", "1D0D3794-C210-48A8-8C68-3FBEC08A6BA5", "Lava Output", "LavaOutput", "", "", 0, @"
", "85769AA7-7A31-49A8-ACB2-F8091AAFF9B4" );

            RockMigrationHelper.AddBlockTypeAttribute( "2419DF52-CEE9-4281-B7FB-80C27AB63FE2", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Lava Output Debug", "LavaOutputDebug", "", "", 0, @"False", "DAFD5957-6823-452F-9892-A0F2E45AD3C7" );

            RockMigrationHelper.AddBlockTypeAttribute( "2419DF52-CEE9-4281-B7FB-80C27AB63FE2", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Show Grid", "ShowGrid", "", "", 0, @"False", "83516C32-4E47-487D-9493-D6B2AD910B21" );

            RockMigrationHelper.AddBlockTypeAttribute( "2419DF52-CEE9-4281-B7FB-80C27AB63FE2", "59D5A94C-94A0-4630-B80A-BB25697D74C7", "Map Style", "MapStyle", "", "", 0, @"BFC46259-FB66-4427-BF05-2B030A582BEA", "88FA39E3-C51C-4855-B519-CA62D5F6AAED" );

            RockMigrationHelper.AddBlockTypeAttribute( "2419DF52-CEE9-4281-B7FB-80C27AB63FE2", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Show Proximity", "ShowProximity", "", "", 0, @"True", "224D26D5-1F3E-41A2-983E-16D9CEE3E43C" );

            RockMigrationHelper.AddBlockTypeAttribute( "2419DF52-CEE9-4281-B7FB-80C27AB63FE2", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Show Count", "ShowCount", "", "", 0, @"False", "6845219F-D9AE-4C40-8B9D-149BBE0D0D82" );

            RockMigrationHelper.AddBlockTypeAttribute( "2419DF52-CEE9-4281-B7FB-80C27AB63FE2", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Show Age", "ShowAge", "", "", 0, @"False", "9985034C-531F-42CB-BAF6-F09DECF36083" );

            RockMigrationHelper.AddBlockTypeAttribute( "2419DF52-CEE9-4281-B7FB-80C27AB63FE2", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Show Description", "ShowDescription", "", "", 0, @"True", "60AABEE2-E250-491B-B4F9-0A55AD9BBFF1" );

            RockMigrationHelper.AddBlockTypeAttribute( "2419DF52-CEE9-4281-B7FB-80C27AB63FE2", "99B090AA-4D7E-46D8-B393-BF945EA1BA8B", "Attribute Columns", "AttributeColumns", "", "", 0, @"", "37B8161D-F57E-4A4B-B467-D001A8C0257E" );

            RockMigrationHelper.AddBlockTypeAttribute( "2419DF52-CEE9-4281-B7FB-80C27AB63FE2", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Sort By Distance", "SortByDistance", "", "", 0, @"True", "16C94C93-D4F2-4272-94E3-1E672748BD22" );

            RockMigrationHelper.AddBlockTypeAttribute( "2419DF52-CEE9-4281-B7FB-80C27AB63FE2", "9C204CD0-1233-41C5-818A-C5DA439445AA", "Page Sizes", "PageSizes", "", "To show a dropdown of page sizes, enter a comma delimited list of page sizes. For example: 10,20 will present a drop down with 10,20,All as options with the default as 10", 0, @"", "1817FEF0-CBEA-44BB-B50B-812328970178" );

            RockMigrationHelper.AddBlockTypeAttribute( "2419DF52-CEE9-4281-B7FB-80C27AB63FE2", "BD53F9C9-EBA9-4D3F-82EA-DE5DD34A8108", "Register Page", "RegisterPage", "", "The page to navigate to when registering for a group.", 0, @"", "3B1C3803-3F0F-46EA-82F9-B3698671FCDD" );

            RockMigrationHelper.AddBlockTypeAttribute( "2419DF52-CEE9-4281-B7FB-80C27AB63FE2", "1EDAFDED-DFE6-4334-B019-6EECBA89E05A", "Show Schedule", "ShowSchedule", "", "", 0, @"False", "2F41DFD3-D89D-4450-80FB-080B1C0A6D64" );

            RockMigrationHelper.AddBlockTypeAttribute( "2419DF52-CEE9-4281-B7FB-80C27AB63FE2", "18E29E23-B43B-4CF7-AE41-C85672C09F50", "Group Type", "GroupType", "", "", 0, @"", "52754745-CB86-4AD6-8A6A-80A563727347" );

            RockMigrationHelper.AddBlockTypeAttribute( "2419DF52-CEE9-4281-B7FB-80C27AB63FE2", "18E29E23-B43B-4CF7-AE41-C85672C09F50", "Geofenced Group Type", "GeofencedGroupType", "", "", 0, @"", "28FA69E6-B6C8-4376-959D-BA2C1748FB0F" );

            RockMigrationHelper.AddBlockTypeAttribute( "2419DF52-CEE9-4281-B7FB-80C27AB63FE2", "9C204CD0-1233-41C5-818A-C5DA439445AA", "ScheduleFilters", "ScheduleFilters", "", "", 0, @"", "A363D3AE-611D-4B3C-83D4-6AF659A061B6" );

            RockMigrationHelper.AddBlockTypeAttribute( "2419DF52-CEE9-4281-B7FB-80C27AB63FE2", "7BDAE237-6E49-47AC-9961-A45AFB69E240", "Ranges", "Ranges", "", "", 0, @"1|5|10", "65DC767A-8FC9-4DAA-B19D-1204FCCE2BD2" );

            RockMigrationHelper.AddBlockTypeAttribute( "2419DF52-CEE9-4281-B7FB-80C27AB63FE2", "1D0D3794-C210-48A8-8C68-3FBEC08A6BA5", "Message", "Message", "", "", 0, @"
", "D5A20991-CD4F-43F2-9DC2-CD098E793764" );

            RockMigrationHelper.AddBlockTypeAttribute( "2419DF52-CEE9-4281-B7FB-80C27AB63FE2", "1D0D3794-C210-48A8-8C68-3FBEC08A6BA5", "Groupless Message", "GrouplessMessage", "", "", 0, @"
", "0BAF885C-B1F4-4A0A-AA78-C53D4BD5796E" );

            RockMigrationHelper.AddBlockTypeAttribute( "2419DF52-CEE9-4281-B7FB-80C27AB63FE2", "A75DFC58-7A1B-4799-BF31-451B2BBE38FF", "Max Results", "MaxResults", "", "Maximum number of results to display. 0 is no filter", 0, @"0", "DFE6AF9A-917B-4007-8FEE-19942C46AAC5" );

            RockMigrationHelper.AddBlockAttributeValue( "711FE071-B100-4E77-A251-BDE312A91BC3", "FD59D3A0-C4E1-4479-A632-01E17C03B71F", @"20f97a93-7949-4c2a-8a5e-c756fe8585ca" ); // Group Detail Page

            RockMigrationHelper.AddBlockAttributeValue( "711FE071-B100-4E77-A251-BDE312A91BC3", "5E41DA52-93EC-4CE2-B4BA-86D3BCA1FD58", @"" ); // Attribute Filters

            RockMigrationHelper.AddBlockAttributeValue( "711FE071-B100-4E77-A251-BDE312A91BC3", "36369981-9DE3-47BE-B3CE-FFB17A19FEAD", @"True" ); // Show Map

            RockMigrationHelper.AddBlockAttributeValue( "711FE071-B100-4E77-A251-BDE312A91BC3", "7EC6E326-4D90-4657-85F1-A93E4728F094", @"600" ); // Map Height

            RockMigrationHelper.AddBlockAttributeValue( "711FE071-B100-4E77-A251-BDE312A91BC3", "BC570D8B-69DD-4519-9EDD-80B247FE51F5", @"False" ); // Show Fence

            RockMigrationHelper.AddBlockAttributeValue( "711FE071-B100-4E77-A251-BDE312A91BC3", "660870D8-C57F-4C94-9B2F-DFC8C2C43D3B", @"#f37833|#446f7a|#afd074|#649dac|#f8eba2|#92d0df|#eaf7fc" ); // Polygon Colors

            RockMigrationHelper.AddBlockAttributeValue( "711FE071-B100-4E77-A251-BDE312A91BC3", "792337A9-1070-4364-94B8-48B28C357942", @"<h4 class='margin-t-none'>{{ Group.Name }}</h4> 

<div class='margin-b-sm'>
{% for attribute in Group.AttributeValues %}
    <strong>{{ attribute.AttributeName }}:</strong> {{ attribute.ValueFormatted }} <br />
{% endfor %}
</div>

<div class='margin-v-sm'>
{% if Location.FormattedHtmlAddress && Location.FormattedHtmlAddress != '' %}
	{{ Location.FormattedHtmlAddress }}
{% endif %}
</div>

{% if LinkedPages.GroupDetailPage != '' %}
    <a class='btn btn-xs btn-action margin-r-sm' href='{{ LinkedPages.GroupDetailPage }}?GroupId={{ Group.Id }}'>View {{ Group.GroupType.GroupTerm }}</a>
{% endif %}

{% if LinkedPages.RegisterPage != '' %}
    {% if LinkedPages.RegisterPage contains '?' %}
        <a class='btn btn-xs btn-action' href='{{ LinkedPages.RegisterPage }}&GroupId={{ Group.Id }}'>Register</a>
    {% else %}
        <a class='btn btn-xs btn-action' href='{{ LinkedPages.RegisterPage }}?GroupId={{ Group.Id }}'>Register</a>
    {% endif %}
{% endif %}
" ); // Map Info

            RockMigrationHelper.AddBlockAttributeValue( "711FE071-B100-4E77-A251-BDE312A91BC3", "F7AF0726-209E-44C9-8F0F-B7C5E6B2C6D8", @"False" ); // Map Info Debug

            RockMigrationHelper.AddBlockAttributeValue( "711FE071-B100-4E77-A251-BDE312A91BC3", "12EFD573-F247-4DDB-87A7-665E5B7C8EB1", @"True" ); // Show Lava Output

            RockMigrationHelper.AddBlockAttributeValue( "711FE071-B100-4E77-A251-BDE312A91BC3", "85769AA7-7A31-49A8-ACB2-F8091AAFF9B4", @"You can register by clicking on any home in the list below. View a home's location on the map by clicking on the ""View Map"" link.
<br><br>
{% for group in Groups %}
    {% if LinkedPages.RegisterPage != '' %}
        {% if LinkedPages.RegisterPage contains '?' %}
            <a class=""btn btn-default small pull-right"" href='{{ LinkedPages.RegisterPage }}&GroupId={{ group.Id }}'>Join This Home</a>
        {% else %}
            <a class=""btn btn-default small pull-right"" href='{{ LinkedPages.RegisterPage }}?GroupId={{ group.Id }}'>Join This Home</a>
        {% endif %}
{% endif %}
    
    <b>{{ group.Name }}</b>
        - {{group.GroupLocations[0].Location.City}}
<br><br>
    <i class=""fa fa-calendar""></i> {{group.Schedule.FriendlyScheduleText}}
    {%assign parents = group  | Attribute:'AllowEmailParents' %}
    {%if parents == 'Yes'%}
    <br>
    <i class=""fa fa-child""></i> Children Welcome
    {%endif%}
    <br>
    <a href=""javascript:centerOnMarkerByGroupId({{group.Id}})""><i class=""fa fa-map""></i> View on Map</a>
    <hr>
{% endfor %}" ); // Lava Output

            RockMigrationHelper.AddBlockAttributeValue( "711FE071-B100-4E77-A251-BDE312A91BC3", "DAFD5957-6823-452F-9892-A0F2E45AD3C7", @"False" ); // Lava Output Debug

            RockMigrationHelper.AddBlockAttributeValue( "711FE071-B100-4E77-A251-BDE312A91BC3", "83516C32-4E47-487D-9493-D6B2AD910B21", @"False" ); // Show Grid

            RockMigrationHelper.AddBlockAttributeValue( "711FE071-B100-4E77-A251-BDE312A91BC3", "88FA39E3-C51C-4855-B519-CA62D5F6AAED", @"219" ); // Map Style

            RockMigrationHelper.AddBlockAttributeValue( "711FE071-B100-4E77-A251-BDE312A91BC3", "224D26D5-1F3E-41A2-983E-16D9CEE3E43C", @"True" ); // Show Proximity

            RockMigrationHelper.AddBlockAttributeValue( "711FE071-B100-4E77-A251-BDE312A91BC3", "6845219F-D9AE-4C40-8B9D-149BBE0D0D82", @"False" ); // Show Count

            RockMigrationHelper.AddBlockAttributeValue( "711FE071-B100-4E77-A251-BDE312A91BC3", "9985034C-531F-42CB-BAF6-F09DECF36083", @"False" ); // Show Age

            RockMigrationHelper.AddBlockAttributeValue( "711FE071-B100-4E77-A251-BDE312A91BC3", "60AABEE2-E250-491B-B4F9-0A55AD9BBFF1", @"True" ); // Show Description

            RockMigrationHelper.AddBlockAttributeValue( "711FE071-B100-4E77-A251-BDE312A91BC3", "37B8161D-F57E-4A4B-B467-D001A8C0257E", @"" ); // Attribute Columns

            RockMigrationHelper.AddBlockAttributeValue( "711FE071-B100-4E77-A251-BDE312A91BC3", "16C94C93-D4F2-4272-94E3-1E672748BD22", @"True" ); // Sort By Distance

            RockMigrationHelper.AddBlockAttributeValue( "711FE071-B100-4E77-A251-BDE312A91BC3", "1817FEF0-CBEA-44BB-B50B-812328970178", @"" ); // Page Sizes

            RockMigrationHelper.AddBlockAttributeValue( "711FE071-B100-4E77-A251-BDE312A91BC3", "3B1C3803-3F0F-46EA-82F9-B3698671FCDD", @"cdf2c599-d341-42fd-b7dc-cd402ea96050" ); // Register Page

            RockMigrationHelper.AddBlockAttributeValue( "711FE071-B100-4E77-A251-BDE312A91BC3", "2F41DFD3-D89D-4450-80FB-080B1C0A6D64", @"False" ); // Show Schedule

            RockMigrationHelper.AddBlockAttributeValue( "711FE071-B100-4E77-A251-BDE312A91BC3", "52754745-CB86-4AD6-8A6A-80A563727347", @"4b8cc732-cc87-45ce-93ba-f9caa46bb99e" ); // Group Type

            RockMigrationHelper.AddBlockAttributeValue( "711FE071-B100-4E77-A251-BDE312A91BC3", "28FA69E6-B6C8-4376-959D-BA2C1748FB0F", @"" ); // Geofenced Group Type

            RockMigrationHelper.AddBlockAttributeValue( "711FE071-B100-4E77-A251-BDE312A91BC3", "A363D3AE-611D-4B3C-83D4-6AF659A061B6", @"" ); // ScheduleFilters

            RockMigrationHelper.AddBlockAttributeValue( "711FE071-B100-4E77-A251-BDE312A91BC3", "65DC767A-8FC9-4DAA-B19D-1204FCCE2BD2", @"1|5|10|100|" ); // Ranges

            RockMigrationHelper.AddBlockAttributeValue( "711FE071-B100-4E77-A251-BDE312A91BC3", "D5A20991-CD4F-43F2-9DC2-CD098E793764", @"<h3>Join A Home</h3>

We're excited for you to be part of our Love Where You Are home gatherings! To find a home near you, 
enter your address and choose the distance you'd like to travel. We encourage you to choose a home 
in your neighborhood! You can narrow your search further by choosing the day of week you'd like to meet,
and by choosing whether or not you need the home to be handicap accessible or have childcare available.
" ); // Message

            RockMigrationHelper.AddBlockAttributeValue( "711FE071-B100-4E77-A251-BDE312A91BC3", "0BAF885C-B1F4-4A0A-AA78-C53D4BD5796E", @"<h2>Oh No!</h2>
We didn't find any groups near you. Would you consider signing up to be a home host?" ); // Groupless Message

            RockMigrationHelper.AddBlockAttributeValue( "711FE071-B100-4E77-A251-BDE312A91BC3", "DFE6AF9A-917B-4007-8FEE-19942C46AAC5", @"2" ); // Max Results

        }

        public override void Down()
        {
            RockMigrationHelper.DeleteAttribute( "DFE6AF9A-917B-4007-8FEE-19942C46AAC5" );
            RockMigrationHelper.DeleteAttribute( "0BAF885C-B1F4-4A0A-AA78-C53D4BD5796E" );
            RockMigrationHelper.DeleteAttribute( "D5A20991-CD4F-43F2-9DC2-CD098E793764" );
            RockMigrationHelper.DeleteAttribute( "65DC767A-8FC9-4DAA-B19D-1204FCCE2BD2" );
            RockMigrationHelper.DeleteAttribute( "A363D3AE-611D-4B3C-83D4-6AF659A061B6" );
            RockMigrationHelper.DeleteAttribute( "28FA69E6-B6C8-4376-959D-BA2C1748FB0F" );
            RockMigrationHelper.DeleteAttribute( "52754745-CB86-4AD6-8A6A-80A563727347" );
            RockMigrationHelper.DeleteAttribute( "2F41DFD3-D89D-4450-80FB-080B1C0A6D64" );
            RockMigrationHelper.DeleteAttribute( "3B1C3803-3F0F-46EA-82F9-B3698671FCDD" );
            RockMigrationHelper.DeleteAttribute( "1817FEF0-CBEA-44BB-B50B-812328970178" );
            RockMigrationHelper.DeleteAttribute( "16C94C93-D4F2-4272-94E3-1E672748BD22" );
            RockMigrationHelper.DeleteAttribute( "37B8161D-F57E-4A4B-B467-D001A8C0257E" );
            RockMigrationHelper.DeleteAttribute( "60AABEE2-E250-491B-B4F9-0A55AD9BBFF1" );
            RockMigrationHelper.DeleteAttribute( "9985034C-531F-42CB-BAF6-F09DECF36083" );
            RockMigrationHelper.DeleteAttribute( "6845219F-D9AE-4C40-8B9D-149BBE0D0D82" );
            RockMigrationHelper.DeleteAttribute( "224D26D5-1F3E-41A2-983E-16D9CEE3E43C" );
            RockMigrationHelper.DeleteAttribute( "88FA39E3-C51C-4855-B519-CA62D5F6AAED" );
            RockMigrationHelper.DeleteAttribute( "83516C32-4E47-487D-9493-D6B2AD910B21" );
            RockMigrationHelper.DeleteAttribute( "DAFD5957-6823-452F-9892-A0F2E45AD3C7" );
            RockMigrationHelper.DeleteAttribute( "85769AA7-7A31-49A8-ACB2-F8091AAFF9B4" );
            RockMigrationHelper.DeleteAttribute( "12EFD573-F247-4DDB-87A7-665E5B7C8EB1" );
            RockMigrationHelper.DeleteAttribute( "F7AF0726-209E-44C9-8F0F-B7C5E6B2C6D8" );
            RockMigrationHelper.DeleteAttribute( "792337A9-1070-4364-94B8-48B28C357942" );
            RockMigrationHelper.DeleteAttribute( "660870D8-C57F-4C94-9B2F-DFC8C2C43D3B" );
            RockMigrationHelper.DeleteAttribute( "BC570D8B-69DD-4519-9EDD-80B247FE51F5" );
            RockMigrationHelper.DeleteAttribute( "7EC6E326-4D90-4657-85F1-A93E4728F094" );
            RockMigrationHelper.DeleteAttribute( "36369981-9DE3-47BE-B3CE-FFB17A19FEAD" );
            RockMigrationHelper.DeleteAttribute( "5E41DA52-93EC-4CE2-B4BA-86D3BCA1FD58" );
            RockMigrationHelper.DeleteAttribute( "FD59D3A0-C4E1-4479-A632-01E17C03B71F" );
            RockMigrationHelper.DeleteBlock( "711FE071-B100-4E77-A251-BDE312A91BC3" );
            RockMigrationHelper.DeleteBlockType( "2419DF52-CEE9-4281-B7FB-80C27AB63FE2" );
            RockMigrationHelper.DeletePage( "779D8828-2101-4B8E-B690-E713656320DA" ); //  Page: Group Finder Map
        }
    }
}
