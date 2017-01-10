<%@ Control Language="C#" AutoEventWireup="true" CodeFile="NeighborhoodGroupMerge.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Administration.NeighborhoodGroupMerge" ViewStateMode="Enabled" EnableViewState="true" %>
<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <Rock:GroupPicker ID="gpSource" Label="Source Group" runat="server" />
        <Rock:GroupPicker ID="gpTarget" Label="Target Group" runat="server" />
        <Rock:BootstrapButton ID="btnMerge" Text="Merge Groups" CssClass="btn btn-primary" runat="server" OnClick="btnMerge_Click"></Rock:BootstrapButton>

    </ContentTemplate>
</asp:UpdatePanel>
