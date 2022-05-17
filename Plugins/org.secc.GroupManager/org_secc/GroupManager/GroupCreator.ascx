<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GroupCreator.ascx.cs" Inherits="RockWeb.Plugins.org_secc.GroupManager.GroupCreator" %>
<asp:UpdatePanel runat="server">
    <ContentTemplate>
        <div class="panel panel-block">
            <div class="panel-heading">
                <h1 class="panel-title" data-toc-skip="1">
                    <i class="fas fa-group"></i>
                    Group Creator
                </h1>
            </div>
            <div class="panel-body">
                <div class="row">
                    <div class="col-md-6 col-xs-12">
                        <Rock:NotificationBox ID="nbResult" runat="server" CssClass="" NotificationBoxType="Success" Title="Success" Visible="false" />
                        <asp:Panel runat="server" ID="pnlDetails" Visible="true">
                                <Rock:GroupPicker runat="server" ID="gpGroup" OnSelectItem="gpGroup_SelectItem" AllowMultiSelect="true" EnableFullWidth="true"
                                    Label="Select Parent Group(s)" Help="Select the parent group(s) under which the new groups will be created" />
                                <Rock:RockDropDownList ID="ddlGroupType" runat="server" Label="Select Group Type" Help="Select the group type for the new groups"
                                        CssClass="margin-r-lg" />
                            <Rock:RockTextBox ID="tbGroupNames" runat="server" Label="Group Name(s)"
                                Help="Comma-separated list of group names"></Rock:RockTextBox>
                            <Rock:BootstrapButton runat="server" ID="BootstrapButton1" Text="Create Group(s)" CssClass="btn btn-primary" OnClick="btnSave_Click" />
                        </asp:Panel>
                    </div>
                </div>
            </div>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>
