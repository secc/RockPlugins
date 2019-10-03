<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ChurchOnlineRedirect.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Security.ChurchOnlineRedirect" %>
<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <Rock:NotificationBox runat="server" ID="nbAdminRedirect" Visible="false" NotificationBoxType="Warning" ></Rock:NotificationBox>
    </ContentTemplate>
</asp:UpdatePanel>