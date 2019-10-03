<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CaptivePortalStaff.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Security.CaptivePortalStaff" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <asp:HiddenField ID="hfMacAddress" runat="server" />
        <asp:HiddenField ID="hfPersonAliasId" runat="server" />
        <Rock:NotificationBox ID="nbAlert" runat="server" NotificationBoxType="Warning" />
        <asp:ValidationSummary ID="valCaptivePortal" runat="server" HeaderText="Please correct the following:" CssClass="alert alert-validation" ValidationGroup="CaptivePortalStaff" />
        <asp:Panel ID="pnlDetails" CssClass="panel panel-block" runat="server">
  

        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>