<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GroupFinderMap.ascx.cs" Inherits="RockWeb.Plugins.org_secc.GroupManager.GroupFinderMap" %>

<asp:UpdatePanel ID="upnlContent" runat="server" UpdateMode="Conditional">
    <ContentTemplate>
        <Rock:NotificationBox ID="nbNotice" runat="server" Visible="false" />

        <%-- View Panel --%>
        <asp:Panel ID="pnlView" CssClass="container" runat="server">

            <div class="row">
                <asp:Literal ID="lTitle" runat="server" />

                <asp:Panel ID="pnlMessage" runat="server" CssClass="col-md-6 col-xs-12">
                    <asp:Literal ID="lMessage" runat="server" />
                </asp:Panel>

                <asp:Panel ID="pnlSearch" runat="server" CssClass="col-md-6 col-xs-12">
                    <asp:ValidationSummary ID="valSummary" runat="server" HeaderText="There was an issue:" CssClass="alert alert-info" />

                    <Rock:AddressControl ID="acAddress" runat="server" Required="true" RequiredErrorMessage="Please enter an address to search near." />
                    <Rock:RockDropDownList ID="ddlRange" runat="server" Label="Range"></Rock:RockDropDownList>
                    <asp:PlaceHolder ID="phFilterControls" runat="server" />

                    <Rock:PanelWidget ID="wpConnectionStatus" runat="server" Title="Connection Status">
                        <Rock:RockCheckBoxList ID="cblConnectionStatus" DataValueField="Id" DataTextField="Value" runat="server"></Rock:RockCheckBoxList>
                    </Rock:PanelWidget>

                    <div class="actions">
                        <asp:LinkButton ID="btnSearch" runat="server" Text="Search" CssClass="btn btn-primary" OnClick="btnSearch_Click" />
                        <asp:LinkButton ID="btnClear" runat="server" Text="Clear" CssClass="btn btn-link" OnClick="btnClear_Click" />
                    </div>

                </asp:Panel>

                <asp:Panel ID="pnlResults" runat="server" Visible="false">

                    <asp:Literal ID="lMapStyling" runat="server" />

                    <Rock:RockDropDownList ID="ddlPageSize" runat="server" OnSelectedIndexChanged="ddlPageSize_SelectedIndexChanged" AutoPostBack="true" Label="Number of groups to show" />

                    <asp:Panel ID="pnlMap" runat="server" CssClass="margin-v-sm col-md-6">
                        <div id="map_wrapper">
                            <div id="map_canvas" class="mapping"></div>
                        </div>
                        <asp:Literal ID="lMapInfoDebug" runat="server" />
                    </asp:Panel>

                    <asp:Panel ID="pnlLavaOutput" runat="server" CssClass="margin-v-sm">
                        <asp:Literal ID="lLavaOverview" runat="server" />
                        <asp:Literal ID="lLavaOutputDebug" runat="server" />
                    </asp:Panel>

                    <asp:Panel ID="pnlGrid" runat="server" CssClass="margin-v-sm col-md-6">
                        <div class="grid">
                            <Rock:Grid ID="gGroups" runat="server" RowItemText="Group" AllowSorting="true" OnRowSelected="gGroups_RowSelected">
                                <Columns>
                                    <Rock:RockBoundField DataField="Name" HeaderText="Name" SortExpression="Name" />
                                    <Rock:RockBoundField DataField="Description" HeaderText="Description" SortExpression="Description" />
                                    <Rock:RockBoundField DataField="Schedule" HeaderText="Schedule" />
                                    <Rock:RockBoundField DataField="MemberCount" HeaderText="Members" DataFormatString="{0:N0}" ItemStyle-HorizontalAlign="Right" HeaderStyle-HorizontalAlign="Right" />
                                    <Rock:RockBoundField DataField="AverageAge" HeaderText="Average Age" DataFormatString="{0:N0}" ItemStyle-HorizontalAlign="Right" HeaderStyle-HorizontalAlign="Right" />
                                    <Rock:RockBoundField DataField="Distance" HeaderText="Distance" DataFormatString="{0:N2} M" ItemStyle-HorizontalAlign="Right" HeaderStyle-HorizontalAlign="Right" />
                                </Columns>
                            </Rock:Grid>
                        </div>
                    </asp:Panel>
                    <asp:Panel ID="pnlFamilyGrid" runat="server" CssClass="margin-v-sm col-md-6">
                        <div class="grid">
                            <Rock:Grid ID="gFamilies" DataKeyNames="Id" runat="server" RowItemText="Famly" AllowSorting="true">
                                <Columns>
                                    <Rock:RockBoundField DataField="Name" HeaderText="Name" SortExpression="Name" />
                                    <Rock:RockBoundField DataField="Members" HeaderText="Family Members" SortExpression="Members" />
                                    <Rock:RockBoundField DataField="Address" HeaderText="Address" SortExpression="Address" />
                                    <Rock:RockBoundField DataField="CellPhone" HeaderText="Cell Numbers" SortExpression="CellPhone" />
                                    <Rock:RockBoundField DataField="Email" HeaderText="Email Addresses" SortExpression="Email" />
                                    <Rock:LinkButtonField  CssClass="btn btn-default" Text="<i class='fa fa-user'></i>" OnClick="PersonSelected_Click" ExcelExportBehavior="NeverInclude"/>
                                </Columns>
                            </Rock:Grid>
                        </div>
                    </asp:Panel>
                    <Rock:BootstrapButton runat="server" ID="btnReset" OnClick="btnReset_Click" Text="Reset Search" CssClass="btn btn-default"></Rock:BootstrapButton>
                </asp:Panel>
            </div>
        </asp:Panel>

        <%-- Edit Panel --%>
        <asp:Panel ID="pnlEditModal" runat="server" Visible="false">
            <Rock:ModalDialog ID="mdEdit" runat="server" OnSaveClick="lbSave_Click" Title="Group Finder Configuration" ValidationGroup="GroupFinderSettings">
                <Content>

                    <asp:UpdatePanel ID="upnlEdit" runat="server">
                        <ContentTemplate>

                            <asp:ValidationSummary ID="valSettings" runat="server" HeaderText="Please Correct the Following" CssClass="alert alert-danger" ValidationGroup="GroupFinderSettings" />

                            <Rock:PanelWidget ID="wpFilter" runat="server" Title="Filter Settings" Expanded="true">
                                <div class="row">
                                    <div class="col-md-6">
                                        <Rock:GroupTypePicker ID="gtpGroupType" runat="server" Label="Group Type" Help="The type of groups to look for."
                                            AutoPostBack="true" OnSelectedIndexChanged="gtpGroupType_SelectedIndexChanged" ValidationGroup="GroupFinderSettings" />
                                        <Rock:GroupPicker ID="gpGroupParent" runat="server" Label="Groups Within" Help="Limit to the groups contained within this group." />
                                        <Rock:GroupTypePicker ID="gtpGeofenceGroupType" runat="server" Label="Geofence Group Type"
                                            Help="An optional group type that contains groups with geographic boundary (fence). If specified, user will be prompted for their address, and only groups that are located in the same geographic boundary ( as defined by one or more groups of this type ) will be displayed."
                                            ValidationGroup="GroupFinderSettings" />
                                        <Rock:NumberUpDown runat="server" ID="nudMaxResults" Label="Max Group Results"
                                            Help="Maximum number of results to display. 0 is no filter" Minimum="0" />
                                        <Rock:Toggle runat="server" ID="cbShowReset" Label="Show Reset" Help="Should a reset button be displayed after searching?"
                                            OnCssClass="btn-success" OffCssClass="btn-danger" />

                                    </div>
                                    <div class="col-md-6">
                                        <Rock:RockCheckBoxList ID="cblSchedule" runat="server" Label="Display Schedule Filters" RepeatDirection="Horizontal"
                                            Help="Flags indicating if Day of Week and/or Time of Day filters should be displayed to filter groups with 'Weekly' schedules." ValidationGroup="GroupFinderSettings">
                                            <asp:ListItem Text="Day of Week" Value="Day" />
                                            <asp:ListItem Text="Time of Day" Value="Time" />
                                        </Rock:RockCheckBoxList>
                                        <Rock:RockCheckBoxList ID="cblAttributes" runat="server" Label="Display Attribute Filters" RepeatDirection="Horizontal"
                                            Help="The group attributes that should be available for user to filter results by." ValidationGroup="GroupFinderSettings" />
                                        <Rock:Toggle runat="server" ID="cbPreFill" Label="Pre Fill Out Address" Help="Should a logged in user have their address pre-filled out?"
                                            OnCssClass="btn-success" OffCssClass="btn-danger" />
                                        <Rock:Toggle runat="server" ID="cbHideFull" Label="Hide Full Groups" Help="Hide groups that have reached their capacity?"
                                            OnCssClass="btn-success" OffCssClass="btn-danger" />
                                    </div>
                                </div>
                            </Rock:PanelWidget>

                            <Rock:PanelWidget ID="wpMap" runat="server" Title="Map">
                                <div class="row">
                                    <div class="col-md-6">
                                        <Rock:Toggle ID="cbShowMap" runat="server" Label="Map" Text="Yes" OnCssClass="btn-success" OffCssClass="btn-danger"
                                            Help="Should a map be displayed that shows the location of each group?" ValidationGroup="GroupFinderSettings" />
                                        <Rock:Toggle ID="cbShowFamilies" runat="server" Label="Show Families" Text="Yes" OnCssClass="btn-success" OffCssClass="btn-danger"
                                            Help="Should families be shown on the map?" ValidationGroup="GroupFinderSettings" />
                                        <Rock:RockDropDownList ID="ddlMapStyle" runat="server" Label="Map Style"
                                            Help="The map theme that should be used for styling the map." ValidationGroup="GroupFinderSettings" />
                                        <Rock:Toggle ID="cbLargeMap" runat="server" Label="Large Map" Help="Set map to full width"
                                            OnCssClass="btn-success" OffCssClass="btn-danger" />
                                        <Rock:NumberBox ID="nbMapHeight" runat="server" Label="Map Height"
                                            Help="The pixel height to use for the map." ValidationGroup="GroupFinderSettings" />
                                        <Rock:ValueList runat="server" ID="vlRanges" Label="List of Ranges"
                                            Help="A list of ranges which can be selected as a search distance" />
                                    </div>
                                    <div class="col-md-6">
                                        Optional Map Icons: (34x34)
                                        <Rock:RockTextBox runat="server" ID="tbSearchIcon" Label="Search Icon URL"
                                            Help="URL for the pin on the map showing the location of the search address."></Rock:RockTextBox>
                                        <Rock:RockTextBox runat="server" ID="tbGroupIcon" Label="Search Group URL"
                                            Help="URL for the pins on the map showing the location of groups."></Rock:RockTextBox>
                                        <Rock:RockTextBox runat="server" ID="tbFamilyIcon" Label="Search Family URL"
                                            Help="URL for the pins on the map showing the location of families."></Rock:RockTextBox>
                                        <Rock:RockCheckBox ID="cbShowFence" runat="server" Label="Show Fence(s)" Text="Yes"
                                            Help="If a Geofence group type was selected, should that group's boundary be displayed on the map?" ValidationGroup="GroupFinderSettings" />
                                        <Rock:ValueList ID="vlPolygonColors" runat="server" Label="Fence Polygon Colors"
                                            Help="The list of colors to use when displaying multiple fences ( there should normally be only one fence)." ValidationGroup="GroupFinderSettings" />
                                    </div>
                                </div>
                                <div class="row">
                                    <div class="col-md-12">
                                        <Rock:CodeEditor ID="ceMapInfo" runat="server" Label="Group Window Contents" EditorMode="Lava" EditorTheme="Rock" Height="300"
                                            Help="The Lava template to use for formatting the group information that is displayed when user clicks the group marker on the map."
                                            ValidationGroup="GroupFinderSettings" />
                                        <Rock:CodeEditor ID="ceFamilyInfo" runat="server" Label="Family Window Contents" EditorMode="Lava" EditorTheme="Rock" Height="300"
                                            Help="The Lava template to use for formatting the group information that is displayed when user clicks the family marker on the map."
                                            ValidationGroup="GroupFinderSettings" />
                                        <Rock:RockCheckBox ID="cbMapInfoDebug" runat="server" Text="Enable Debug" />
                                    </div>
                                </div>
                            </Rock:PanelWidget>

                            <Rock:PanelWidget ID="wpLavaOutput" runat="server" Title="Lava">
                                <Rock:CodeEditor ID="ceMessage" runat="server" Label="Lava Message" EditorMode="Lava" EditorTheme="Rock" Height="300"
                                    Help="Message to display beside the address field."
                                    ValidationGroup="GroupFinderSettings" />
                                <div class="row">
                                    <div class="col-md-6">
                                        <Rock:RockCheckBox ID="cbShowLavaOutput" runat="server" Label="Show Formatted Output" Text="Yes"
                                            Help="Should the matching groups be merged with a Lava template and displayed to the user as formatted output?" ValidationGroup="GroupFinderSettings" />
                                    </div>
                                    <div class="col-md-6">
                                    </div>
                                </div>
                                <div class="row">
                                    <div class="col-md-12">
                                        <Rock:CodeEditor ID="ceLavaOutput" runat="server" Label="Lava Template" EditorMode="Lava" EditorTheme="Rock" Height="300"
                                            Help="The Lava template to use for formatting the matching groups."
                                            ValidationGroup="GroupFinderSettings" />
                                        <Rock:CodeEditor ID="ceGrouplessMessage" runat="server" Label="No Groups Message" EditorMode="Lava" EditorTheme="Rock" Height="300"
                                            Help="Message to show when no groups are found."
                                            ValidationGroup="GroupFinderSettings" />
                                        <Rock:RockCheckBox ID="cbLavaOutputDebug" runat="server" Text="Enable Debug" />
                                    </div>
                                </div>
                            </Rock:PanelWidget>

                            <Rock:PanelWidget ID="wpGrid" runat="server" Title="Group Grid">
                                <div class="row">
                                    <div class="col-md-6">
                                        <Rock:RockCheckBox ID="cbShowGrid" runat="server" Label="Show Grid" Text="Yes"
                                            Help="Should a grid be displayed showing the matching groups?" ValidationGroup="GroupFinderSettings" />
                                        <Rock:RockCheckBox ID="cbShowSchedule" runat="server" Label="Show Schedule" Text="Yes"
                                            Help="Should the schedule for each group be displayed?" ValidationGroup="GroupFinderSettings" />
                                        <Rock:RockCheckBox ID="cbShowDescription" runat="server" Label="Show Description" Text="Yes"
                                            Help="Should the description for each group be displayed?" ValidationGroup="GroupFinderSettings" />
                                        <Rock:RockCheckBox ID="cbShowCount" runat="server" Label="Show Member Count" Text="Yes"
                                            Help="Should the number of members in each group be displayed in the result grid?" ValidationGroup="GroupFinderSettings" />
                                    </div>
                                    <div class="col-md-6">
                                        <Rock:RockCheckBox ID="cbShowAge" runat="server" Label="Show Average Age" Text="Yes"
                                            Help="Should the average group member age be displayed for each group in the result grid?" ValidationGroup="GroupFinderSettings" />
                                        <Rock:RockCheckBox ID="cbProximity" runat="server" Label="Show Distance" Text="Yes"
                                            Help="Should the distance to each group be displayed? Using this option will require the user to enter their address when searching for groups." ValidationGroup="GroupFinderSettings" />
                                        <Rock:RockCheckBox ID="cbSortByDistance" runat="server" Label="Sort by Distance" Text="Yes"
                                            Help="Should the results be sorted from closest to furthest distance?" ValidationGroup="GroupFinderSettings" />
                                        <Rock:RockTextBox ID="tbPageSizes" runat="server" Label="Page Sizes" Help="To limit the number of groups displayed and to show a dropdown of page sizes, enter a comma delimited list of page sizes. For example: 10,20 will present a drop down with 10,20,All as options with the default as 10" />
                                        <Rock:RockCheckBoxList ID="cblGridAttributes" runat="server" Label="Show Attribute Columns" RepeatDirection="Horizontal"
                                            Help="The group attribute values that should be displayed in the result grid." ValidationGroup="GroupFinderSettings" />
                                    </div>
                                </div>
                            </Rock:PanelWidget>
                            <Rock:PanelWidget ID="wpFamilyGrid" runat="server" Title="Family Grid">
                                <div class="row">
                                    <div class="col-md-6">
                                        <Rock:Toggle ID="cbFamilyGrid" runat="server" Label="Show Family Grid" Help="Show grid of family members."
                                             OffCssClass="btn-danger" OnCssClass="btn-success" /> 
                                    </div>
                                </div>
                            </Rock:PanelWidget>


                            <Rock:PanelWidget ID="wpLinkedPages" runat="server" Title="Linked Pages">
                                <div class="row">
                                    <div class="col-md-6">
                                        <Rock:PagePicker ID="ppGroupDetailPage" runat="server" Label="Group Detail Page" Required="false" />
                                    </div>
                                    <div class="col-md-6">
                                        <Rock:PagePicker ID="ppRegisterPage" runat="server" Label="Register Page" Required="false" />
                                    </div>
                                </div>
                            </Rock:PanelWidget>

                        </ContentTemplate>
                    </asp:UpdatePanel>
                </Content>
            </Rock:ModalDialog>

        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>
