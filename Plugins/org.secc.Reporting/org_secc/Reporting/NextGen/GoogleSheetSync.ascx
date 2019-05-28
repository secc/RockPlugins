<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GoogleSheetSync.ascx.cs" Inherits="RockWeb.Blocks.Reporting.NextGen.GoogleSheetSync" %>
<asp:UpdatePanel ID="upReport" runat="server">
    <ContentTemplate>
        <Rock:BootstrapButton runat="server" ID="btnRunSync" CssClass="btn btn-default" Text="Sync from Google Doc" OnClick="btnRunSync_Click"></Rock:BootstrapButton>
        <Rock:ModalDialog runat="server" ID="mdShowOutput">
            <Content>
                <h3>Google Sync Results</h3>
                <div class="panel panel-default">
                    <div class="panel-heading clearfix collapsed">
                        <h4><asp:Literal runat="server" ID="litOutputSummary"></asp:Literal></h4>
                    </div>
                    <div class="panel-body">
                        <asp:Literal runat="server" ID="litOutput"></asp:Literal>
                    </div>
                </div>
                <div class="panel panel-danger">
                    <div class="panel-heading clearfix collapsed">
                        <h4><asp:Literal runat="server" ID="litErrorsSummary"></asp:Literal></h4>
                    </div>
                    <div class="panel-body">
                        <asp:Literal runat="server" ID="litErrors"></asp:Literal>
                    </div>
                </div>
                <div class="panel panel-success">
                    <div class="panel-heading clearfix collapsed">
                        <h4><asp:Literal runat="server" ID="litSuccessSummary"></asp:Literal></h4>
                    </div>
                    <div class="panel-body">
                        <asp:Literal runat="server" ID="litSuccess"></asp:Literal>
                    </div>
                </div>
            </Content>
        </Rock:ModalDialog>
    </ContentTemplate>
    <Triggers>
        <asp:PostBackTrigger ControlID="btnRunSync" />
    </Triggers>
</asp:UpdatePanel>
