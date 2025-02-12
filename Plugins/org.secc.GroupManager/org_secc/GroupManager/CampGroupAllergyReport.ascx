<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CampGroupAllergyReport.ascx.cs" Inherits="RockWeb.Plugins.org_secc.GroupManager.CampGroupAllergyReport" %>
<asp:UpdatePanel ID="upMain" runat="server" >
    <ContentTemplate>
        <Rock:NotificationBox ID="nbMain" runat="server" />
        <asp:Panel ID="pnlResults" runat="server" Visible="false">
            <asp:Literal ID="lResults" runat="server" />
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
