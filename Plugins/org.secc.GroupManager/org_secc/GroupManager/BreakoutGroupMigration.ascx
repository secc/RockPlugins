<%@ Control Language="C#" AutoEventWireup="true" CodeFile="BreakoutGroupMigration.ascx.cs" Inherits="RockWeb.Plugins.org_secc.GroupManager.BreakoutGroupMigration" ViewStateMode="Enabled" EnableViewState="true" %>


<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <Rock:BootstrapButton runat="server" id="btnStart" Text="Start" OnClick="btnStart_Click" CssClass="btn btn-danger"></Rock:BootstrapButton>
    </ContentTemplate>
</asp:UpdatePanel>
