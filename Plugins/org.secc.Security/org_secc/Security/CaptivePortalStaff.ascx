<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CaptivePortalStaff.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Security.CaptivePortalStaff" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <asp:HiddenField ID="hfMacAddress" runat="server" />
        <Rock:NotificationBox ID="nbAlert" runat="server" NotificationBoxType="Warning" />
        <asp:ValidationSummary ID="valCaptivePortal" runat="server" HeaderText="Please correct the following:" CssClass="alert alert-validation" ValidationGroup="CaptivePortalStaff" />
        <asp:Panel ID="pnlDetails" CssClass="panel panel-block" runat="server">

            <div class="panel-heading">
                <h1 class="panel-title"><i class="fa fa-wifi"></i> Wifi Welcome</h1>
            </div>

            <asp:Literal ID="lResponse" runat="server" Visible="true" />

  

        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>