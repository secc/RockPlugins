<%@ Control Language="C#" AutoEventWireup="true" CodeFile="MonitorTests.ascx.cs" Inherits="RockWeb.Plugins.org_secc.SystemsMonitor.MonitorTests" %>
<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <Rock:ModalAlert runat="server" ID="maNotification" />

        <asp:Panel ID="pnlView" runat="server" CssClass="panel panel-block">

            <div class="panel-heading">
                <h1 class="panel-title">
                    <i class="fa fa-tv"></i>
                    Monitor Tests
                </h1>
            </div>
            <div class="panel-body">

                <div class="grid grid-panel">
                    <Rock:Grid ID="gList" runat="server" DataKeyNames="Id" OnRowSelected="gList_RowSelected">
                        <Columns>
                            <Rock:RockBoundField DataField="Name" HeaderText="Name" />
                            <Rock:LinkButtonField ID="btnRun" Text="<i class='fa fa-play'></i>"
                                ToolTip="Run" OnClick="btnRun_Click" CssClass="btn btn-default" />
                        </Columns>
                    </Rock:Grid>
                </div>

            </div>

        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>
