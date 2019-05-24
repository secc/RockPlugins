<%@ Control Language="C#" AutoEventWireup="true" CodeFile="TimedContentChannel.ascx.cs" Inherits="RockWeb.Plugins.org_secc.CMS.TimedContentChannel" %>
<asp:UpdatePanel runat="server">
    <ContentTemplate>
        <asp:Panel runat="server" ID="pnlOptionalHeader">
            <asp:Literal ID="ltOutput" runat="server" />
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
