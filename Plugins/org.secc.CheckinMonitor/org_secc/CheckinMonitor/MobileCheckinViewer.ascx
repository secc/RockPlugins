<%@ Control Language="C#" AutoEventWireup="true" CodeFile="MobileCheckinViewer.ascx.cs" Inherits="RockWeb.Plugins.org_secc.CheckinMonitor.MobileCheckinViewer" %>
<asp:UpdatePanel ID="upContent" runat="server">
    <ContentTemplate>

        <Rock:NotificationBox runat="server" ID="nbError" NotificationBoxType="Danger" Visible="false" />

        <asp:Panel runat="server" CssClass="text-center" ID="pnlNoRecords">
            <h2>There are no active mobile check-in reservations at this time.</h2>
        </asp:Panel>

        <Rock:ModalAlert runat="server" ID="mdAlert" />

        <div class="panel-group" id="accordion" role="tablist" aria-multiselectable="true">
            <div class="row">

                <asp:Repeater runat="server" ID="rMCR" OnItemCommand="rMCR_ItemCommand">
                    <ItemTemplate>
                        <div class="col-sm-6">
                            <div class="panel panel-block">
                                <div class="panel-heading" role="tab" id="heading<%# Eval("Caption") %>">
                                    <h4 class="panel-title pull-left btn-block">
                                        <a role="button" data-toggle="collapse" data-parent="#accordion" class="btn-block"
                                            href="#collapse<%# Eval("Record.Id") %>" aria-controls="collapse<%# Eval("Record.Id") %>">
                                            <%# Eval("Caption") %>
                                        </a>
                                    </h4>
                                    <div class="panel-labels pull-right">
                                        <span class="label label-primary">
                                            <asp:LinkButton Text="Complete Checkin" runat="server" ID="btnComplete" CommandName="Checkin" CommandArgument='<%# Eval("Record.AccessKey") %>' />
                                        </span>
                                    </div>
                                </div>
                                <div id="collapse<%# Eval("Record.Id") %>" class="panel-collapse collapse" role="tabpanel" aria-labelledby="heading<%# Eval("Record.Id") %>">
                                    <div class="panel-body">
                                        <%# Eval("SubCaption") %>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </ItemTemplate>
                </asp:Repeater>
            </div>
        </div>
        <div class="text-center">
            <asp:LinkButton Text="Refresh" runat="server" ID="btnRefresh" CssClass="btn btn-default" OnClick="btnRefresh_Click" />
        </div>
    </ContentTemplate>
</asp:UpdatePanel>
