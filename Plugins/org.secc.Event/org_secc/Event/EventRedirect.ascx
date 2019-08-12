<%@ Control Language="C#" AutoEventWireup="true" CodeFile="EventRedirect.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Event.EventRedirect" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <Rock:NotificationBox runat="server" ID="nbRedirect" />
    </ContentTemplate>
</asp:UpdatePanel>
