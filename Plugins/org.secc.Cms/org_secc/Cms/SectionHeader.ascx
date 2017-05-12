<%@ Control Language="C#" AutoEventWireup="true" CodeFile="SectionHeader.ascx.cs" Inherits="RockWeb.Plugins.org_secc.CMS.SectionHeader" %>
<asp:UpdatePanel runat="server">
    <ContentTemplate>
        <header>
            <h1><asp:Literal ID="lHeaderText" runat="server" /></h1>
        </header>
    </ContentTemplate>
</asp:UpdatePanel>
