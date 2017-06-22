<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ApptDetail.ascx.cs" Inherits="RockWeb.Plugins.org_secc.TotalMD.ApptDetail" %>
<%@ Import Namespace="System.Data" %>
<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <asp:Panel ID="pnlList" CssClass="panel panel-block" runat="server">

            <div class="panel-heading">
                <h1 class="panel-title pull-left"><i class="fa  fa-file-text-o"></i> Appointment Details</h1>
            </div>
            <div class="panel-body">

                <div class="grid grid-panel">
                    <Rock:GridFilter ID="gfSettings" runat="server">
                        <Rock:RockCheckBoxList ID="cblCounselors" runat="server" Label="Counselor" RepeatDirection="Horizontal" />
                        <Rock:DateRangePicker ID="drpDates" runat="server" Label="Appt Date Range" />
                    </Rock:GridFilter>
                </div>
                
                <div style="height:30px;"></div>
                
                <asp:Repeater ID="rptrDateGrouping" runat="server" OnItemDataBound="rptrDateGrouping_ItemDataBound">
                    <ItemTemplate>
                        <div>
                            <b><%# Container.DataItem.ToString().Replace(" 12:00:00 AM","") %></b>
                            <asp:Repeater ID="rptrApptDetails" runat="server">
                                <HeaderTemplate>
                                    <ul>
                                </HeaderTemplate>
                                <FooterTemplate>
                                    </ul>
                                </FooterTemplate>
                                <ItemTemplate>
                                    <li id="liApptDate" runat="server"><%# Format12Hour(((DataRow)Container.DataItem)[2]) %> - <%# ((DataRow)Container.DataItem)[6] %>
                                        <ul>
                                            <li id="liPatient" runat="server"><%# ((DataRow)Container.DataItem)[0] %></li>
                                            <li id="liStatus" runat="server">Status: <b><%# ((DataRow)Container.DataItem)[3] %></b></li>
                                            <li id="liWorkPhone" runat="server">Work: <%# ((DataRow)Container.DataItem)[4] %></li>
                                            <li id="liCellPhone" runat="server">Cell: <%# ((DataRow)Container.DataItem)[5] %></li>
                                        </ul>
                                    </li>
                                </ItemTemplate>
                            </asp:Repeater>
                        </div>                
                    </ItemTemplate>
                </asp:Repeater>

            </div>

        </asp:Panel>

        <Rock:NotificationBox ID="nbMessage" runat="server" Title="Error" NotificationBoxType="Danger" Visible="false" />

    </ContentTemplate>
</asp:UpdatePanel>
