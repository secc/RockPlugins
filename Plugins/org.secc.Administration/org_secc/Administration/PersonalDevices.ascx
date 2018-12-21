<%@ Control Language="C#" AutoEventWireup="true" CodeFile="PersonalDevices.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Administration.PersonalDevices" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <asp:Literal ID="lContent" runat="server"></asp:Literal>
        <asp:Literal ID="Literal1" runat="server"></asp:Literal>
        <asp:Literal ID="lDebug" runat="server" Visible="false"></asp:Literal>        
    </ContentTemplate>
</asp:UpdatePanel>
