<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ViewEvents.ascx.cs" Inherits="RockWeb.Plugins.org_secc.EMS.ViewEvents" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <asp:Panel ID="pnlList" CssClass="panel panel-block" runat="server">

            <div class="panel-heading">
                <h1 class="panel-title pull-left"><i class="fa fa-list-ul"></i> View Events</h1>
            </div>
            <div class="panel-body">

                <div class="grid grid-panel">
                    <Rock:GridFilter ID="gfSettings" runat="server">
                        <Rock:DatePicker ID="dpDate" runat="server" Label="Events For" />
                        <Rock:RockCheckBox ID="cbShowAll" runat="server" Label="Show All Events"/>
                    </Rock:GridFilter>
                                
                    <Rock:Grid ID="gScroll" Runat="Server" EmptyDataText="No Events Found" RowItemText="Event" AllowSorting="true" ExportSource="ColumnOutput">
                        <Columns>
                            <Rock:RockBoundField DataField="TimeEventStart" HeaderText="Time" SortExpression="TimeEventStart" DataFormatString ="{0:t}" />    		
                            <Rock:RockBoundField DataField="TimeEventStart" HeaderText="Date" SortExpression="TimeEventStart" DataFormatString ="{0:d}" />
                            <Rock:RockBoundField DataField="ActivityName" HeaderText="Activity Name" SortExpression="ActivityName" />
                            <Rock:RockBoundField DataField="LocationName" HeaderText="Location" SortExpression="LocationName" />
                        </Columns>
                    </Rock:Grid>
                </div>

            </div>

        </asp:Panel>

        <Rock:NotificationBox ID="nbMessage" runat="server" Title="Error" NotificationBoxType="Danger" Visible="false" />

    </ContentTemplate>
</asp:UpdatePanel>
