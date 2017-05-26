<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GoogleSheetSync.ascx.cs" Inherits="RockWeb.Blocks.Reporting.NextGen.GoogleSheetSync" %>
<asp:UpdatePanel ID="upReport" runat="server">
    <ContentTemplate>
        <Rock:BootstrapButton runat="server" ID="btnRunSync" CssClass="btn btn-default" Text="Sync from Google Doc" OnClick="btnRunSync_Click"></Rock:BootstrapButton>
        <Rock:ModalDialog runat="server" ID="mdShowOutput">
            <Content>
                <asp:Literal runat="server" ID="litOutput"></asp:Literal>
            </Content>
        </Rock:ModalDialog>
    </ContentTemplate>
</asp:UpdatePanel>
