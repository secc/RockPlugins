<%@ Control Language="C#" AutoEventWireup="true" CodeFile="SmallGroupContent.ascx.cs" Inherits="RockWeb.Plugins.org_secc.GroupManager.SmallGroupContent" %>

<script type="text/javascript">
    function clearDialog()
    {
        $('#rock-config-cancel-trigger').click();
    }
</script>

<asp:UpdatePanel ID="upnlContent" runat="server" UpdateMode="Conditional">
    <ContentTemplate>
        <Rock:NotificationBox runat="server" ID="nbAlert" NotificationBoxType="Danger" Visible="false"></Rock:NotificationBox>
        <div class="row">

            <%-- Sidebar Panel --%>
            <asp:Panel ID="pnlSidebar" runat="server" Visible="false">
                <asp:PlaceHolder ID="phLinks" runat="server" />
            </asp:Panel>

            <%-- View Panel --%>
            <asp:Panel ID="pnlView" CssClass="col-md-12" runat="server">
                <Rock:NotificationBox ID="nbContentError" runat="server" Dismissable="true" Visible="false" />
                <div class="col-xs-12">
                    <asp:Panel ID="pnlCalendar" Visible="false" runat="server" class="pull-right">
                        <Rock:BootstrapButton runat="server" ID="btnCalendar" Text="GO"
                             CssClass="btn btn-warning pull-right" OnClick="btnCalendar_Click"></Rock:BootstrapButton>
                        <Rock:DatePicker runat="server" ID="dpCalendar" CssClass="pull-right"></Rock:DatePicker>
                    </asp:Panel>
                </div>
                <div></div>
                <asp:PlaceHolder ID="phContent" runat="server" />
                <asp:Literal ID="lDebug" runat="server" />
            </asp:Panel>
        </div>

        <%-- Edit Panel --%>
        <asp:Panel ID="pnlEditModal" runat="server" Visible="false">
            <Rock:ModalDialog ID="mdEdit" runat="server" OnSaveClick="lbSave_Click" Title="Channel Configuration" OnCancelScript="clearDialog();">
                <Content>

                    <asp:UpdatePanel ID="upnlEdit" runat="server">
                        <ContentTemplate>

                            <Rock:NotificationBox ID="nbError" runat="server" Heading="Error" Title="Query Error!" NotificationBoxType="Danger" Visible="false" />

                            <div class="row">
                                <div class="col-md-7">
                                    <Rock:RockCheckBoxList ID="cblStatus" runat="server" Label="Status" RepeatDirection="Horizontal"
                                        Help="Include items with the following status." />
                                </div>
                            </div>
                            <div class="row">
                                <div class="col-md-6">
                                    <Rock:ButtonDropDownList Title="Select Item Type" ID="ddlTypes" runat="server" Label="Item Type"
                                        Help="The type of content items to display on page." />

                                    <Rock:NumberBox ID="nbCacheDuration" runat="server" CssClass="input-width-sm" Label="Cache Duration"
                                        Help="The number of seconds to cache the content for (use '0' for no caching)." />
                                    <Rock:RockCheckBox ID="cbSetPageTitle" runat="server" Label="Set Page Title" Text="Yes"
                                        Help="When enabled will update the page title with the channel's name unless there is a item id in the query string then it will display the item's title." />
                                </div>
                                <div class="col-md-6">
                                    <Rock:RockCheckBox ID="cbDebug" runat="server" Label="Enable Debug" Text="Yes"
                                        Help="Enabling debug will display the fields of the first 5 items to help show you whats available for your template." />
                                    <Rock:RockCheckBox ID="cbMergeContent" runat="server" Label="Merge Content" Text="Yes"
                                        Help="Enabling will result in the content data and attribute values to be merged using the liquid template engine." />
                                    <Rock:PagePicker ID="ppDetailPage" runat="server" Label="Detail Page" />
                                </div>
                            </div>
                            <div class="row">
                                <div class="col-md-6">
                                    <Rock:RockCheckBox ID="cbQueryParamFiltering" runat="server" Label="Enable Query/Route Parameter Filtering" Text="Yes"
                                        Help="Enabling this option will allow results to be filtered further by any query string our route parameters that are included. This includes item properties or attributes." />
                                    <Rock:RockCheckBox ID="cbShowSidebar" runat="server" Label="Show Sidebar Index" Text="Yes"
                                        Help="Enabling this option will display an index in a sidebar to the left. Requires parameter filtering to work." />
                                    <Rock:RockCheckBox ID="cbShowCalendar" runat="server" Label="Show Calendar" Text="Yes"
                                        Help="Show a calendar to select a new date for the lesson?" />
                                    <Rock:RockCheckBox ID="cbFilterByDate" runat="server" Label="FilterByDate" Text="Yes"
                                        Help="Filter lessons to show only those in range." />
                                </div>
                                <div class="col-md-6">
                                    <Rock:KeyValueList ID="kvlOrder" runat="server" Label="Order Items By" KeyPrompt="Field" ValuePrompt="Direction"
                                        Help="The field value and direction that items should be ordered by." />
                                </div>

                            </div>
                            <Rock:CodeEditor ID="ceContentLava" EditorMode="Lava" runat="server" Label="Display Lava"></Rock:CodeEditor>

                        </ContentTemplate>
                    </asp:UpdatePanel>
                </Content>
            </Rock:ModalDialog>

        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>
