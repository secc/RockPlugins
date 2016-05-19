<%@ Control Language="C#" AutoEventWireup="true" CodeFile="SuperCheckin.ascx.cs" Inherits="RockWeb.Plugins.org_secc.CheckinMonitor.SuperCheckin" %>
<asp:UpdatePanel ID="upContent" runat="server">
    <ContentTemplate>
        <Rock:ModalAlert ID="maWarning" runat="server" />
        <asp:Panel ID="pnlNewFamily" Visible="false" runat="server">
            New Family
        </asp:Panel>
        <asp:Panel ID="pnlManageFamily" Visible="false" runat="server">
            Existing Family
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
