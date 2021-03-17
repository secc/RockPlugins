<%@ Control Language="C#" AutoEventWireup="true" CodeFile="RiseSamlRedirect.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Rise.RiseSamlRedirect" %>


<asp:UpdatePanel runat="server">
    <ContentTemplate>
        <asp:Panel runat="server" ID="pnlDebug" Visible="false">
            <asp:Literal runat="server" ID="ltDebug" />
            <asp:LinkButton Text="Post Data" runat="server" ID="btnPost" OnClick="btnPost_Click" />
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
