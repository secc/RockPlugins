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

                <asp:Panel ID="pnlDetails" runat="server">
                    <div class="row">
                        <div class="col-md-4">
                            <dl>
                                <dt>Machine Name</dt>
                                <dd><asp:Literal ID="lMachineName" runat="server" /></dd>
                            </dl>
                        </div>
                        <div class="col-md-4">
                            <dl>
                                <dt>Web Farm</dt>
                                <dd><asp:Literal ID="lWebFarmEnabled" runat="server" /></dd>
                            </dl>
                        </div>
                        <div class="col-md-4">
                            <dl>
                                <dt>Last Seen</dt>
                                <dd><asp:Literal ID="lLastSeen" runat="server" /></dd>
                            </dl>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <dl>
                                <dt>Message Bus</dt>
                                <dd><asp:Literal ID="lMessageBus" runat="server" /></dd>
                            </dl>
                        </div>
                        <div class="col-md-6">
                            <dl>
                                <dt>Cache Queue Rate/Min</dt>
                                <dd><asp:Literal ID="lCacheQueueRate" runat="server" /></dd>
                            </dl>
                        </div>
                    </div>
                </asp:Panel>
            </div>

        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
