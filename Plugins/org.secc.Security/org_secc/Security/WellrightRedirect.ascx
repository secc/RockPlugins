<%@ Control Language="C#" AutoEventWireup="true" CodeFile="WellrightRedirect.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Security.WellrightRedirect" %>

<asp:UpdatePanel runat="server">
    <ContentTemplate>
        <asp:Literal runat="server" ID="ltDebug" />
        <asp:LinkButton Text="Post Data" runat="server" ID="btnPost" OnClick="btnPost_Click" />
    </ContentTemplate>
</asp:UpdatePanel>
