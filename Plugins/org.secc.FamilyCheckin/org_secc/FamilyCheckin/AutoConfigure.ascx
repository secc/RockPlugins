<%@ Control Language="C#" AutoEventWireup="true" CodeFile="AutoConfigure.ascx.cs" Inherits="RockWeb.Plugins.org_secc.FamilyCheckin.AutoConfigure" %>
<asp:UpdatePanel ID="upContent" runat="server">
<ContentTemplate>
    <Rock:NotificationBox runat="server" NotificationBoxType="Danger">
        <h2>Sorry</h2>
        There was a problem with the configuration of this kiosk. Please contact an administrator to resolve.
    </Rock:NotificationBox>
</ContentTemplate>
</asp:UpdatePanel>
