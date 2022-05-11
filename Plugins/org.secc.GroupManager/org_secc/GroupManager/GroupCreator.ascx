<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GroupCreator.ascx.cs" Inherits="RockWeb.Plugins.org_secc.GroupManager.GroupCreator" %>
<asp:UpdatePanel runat="server">
    <ContentTemplate>
        <div class="panel panel-block">
            <Rock:NotificationBox ID="nbResult" runat="server" CssClass="" NotificationBoxType="Success" Title="Success" Visible="false" />
            <asp:Panel runat="server" ID="pnlSelectGroup" Visible="true">
                <Rock:GroupPicker runat="server" ID="gpGroup" OnSelectItem="gpGroup_SelectItem" AllowMultiSelect="true" EnableFullWidth="true" Label="Select Parent Group(s)" />
                <Rock:RockDropDownList ID="ddlGroupType" runat="server" Label="Select Group Type" />
                <Rock:ListItems ID="liGroupNames" runat="server" Label="Group Name(s)"></Rock:ListItems>
                <Rock:BootstrapButton runat="server" ID="BootstrapButton1" Text="Create Group(s)" CssClass="btn btn-primary" OnClick="btnSave_Click" />
            </asp:Panel>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>
