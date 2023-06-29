<%@ Control Language="C#" AutoEventWireup="true" CodeFile="SearchResults.ascx.cs" Inherits="RockWeb.Plugins.org_secc.SportsAndFitness.ControlCenter.SearchResults" %>
<asp:UpdatePanel ID="upMain" runat="server">
    <ContentTemplate>
        <Rock:NotificationBox ID="nbAlert" runat="server" NotificationBoxType="Info" Visible="false" />
        <asp:Panel ID="pnlDebug" runat="server" Visible="true">
            <Rock:RockLiteral ID="lSearchTerm" runat="server" Label="Search Term" />
            <Rock:RockLiteral ID="lSearchByPhone" runat="server" Label="Search By Phone" />
            <Rock:RockLiteral ID="lSearchByPIN" runat="server" Label="Search By PIN" />
            <Rock:RockLiteral ID="lResultCount" runat="server" Label="Result Count" />
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
