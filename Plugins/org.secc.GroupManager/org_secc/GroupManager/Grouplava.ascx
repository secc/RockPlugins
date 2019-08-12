<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GroupLava.ascx.cs" Inherits="RockWeb.Plugins.org_secc.GroupManager.GroupLava" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <asp:Literal ID="lOutput" runat="server"></asp:Literal>

        <asp:Literal ID="lDebug" Visible="false" runat="server"></asp:Literal>
       
    </ContentTemplate>
</asp:UpdatePanel>