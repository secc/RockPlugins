<%@ Control Language="C#" AutoEventWireup="true" CodeFile="BreakoutGroupHandout.ascx.cs" Inherits="RockWeb.Blocks.Reporting.Children.BreakoutGroupHandout" %>

<asp:UpdatePanel ID="upReport" runat="server">
    <ContentTemplate>
        <a onclick="$('input[id*=\'cblGroups\'][type=checkbox]').prop('checked', false); return false;" href="#">Uncheck All</a>
        <a onclick="$('input[id*=\'cblGroups\'][type=checkbox]').prop('checked', true); return false;" href="#">Check All</a>
        <Rock:RockCheckBoxList runat="server" ID="cblGroups" Label="Groups To Display" />
        <asp:LinkButton ID="excel" runat="server" OnClick="ExcelExportClick" CssClass="btn btn-primary" Style="margin-top: 5px;"><i class="fa fa-table"></i> Export</asp:LinkButton>
    </ContentTemplate>
    <Triggers>
        <asp:PostBackTrigger ControlID="excel" />
    </Triggers>
</asp:UpdatePanel>