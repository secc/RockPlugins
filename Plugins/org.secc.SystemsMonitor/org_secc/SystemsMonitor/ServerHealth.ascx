<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ServerHealth.ascx.cs" Inherits="RockWeb.Plugins.org_secc.SystemsMonitor.ServerHealth" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <asp:Panel ID="pnlView" runat="server" CssClass="panel panel-block">

            <div class="panel-heading">
                <h1 class="panel-title">
                    <i class="fa fa-heartbeat"></i>
                    Server Health
                </h1>
                <div class="panel-labels">
                    <asp:Literal ID="lStatusBadge" runat="server" />
                </div>
            </div>

            <div class="panel-body">
                <Rock:NotificationBox ID="nbMessage" runat="server" />
            </div>

        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
