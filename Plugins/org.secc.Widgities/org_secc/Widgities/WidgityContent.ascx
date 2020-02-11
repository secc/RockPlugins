<%@ Control Language="C#" AutoEventWireup="true" CodeFile="WidgityContent.ascx.cs" Inherits="RockWeb.Plugins.org.secc.Widgities.WidgityContent" %>


<asp:UpdatePanel ID="upnlContent" runat="server" CssClass="panel panel-block">
    <ContentTemplate>
        <asp:PlaceHolder runat="server" ID="phPlaceholder" />
    </ContentTemplate>
</asp:UpdatePanel> 