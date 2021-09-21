<%@ Control Language="C#" AutoEventWireup="true" CodeFile="JiraTopicDetail.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Jira.JiraTopicDetail" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <asp:Panel ID="pnlView" runat="server" CssClass="panel panel-block">

            <div class="panel-heading">
                <h1 class="panel-title">
                    <i class="fa fa-star"></i>
                    Blank Detail Block
                </h1>

                <div class="panel-labels">
                    <Rock:HighlightLabel ID="hlblTest" runat="server" LabelType="Info" Text="Label" />
                </div>
            </div>
            <Rock:PanelDrawer ID="pdAuditDetails" runat="server"></Rock:PanelDrawer>
            <div class="panel-body">
                <Rock:RockTextBox runat="server" ID="tbName" Label="Name" Required="true" />
                <Rock:RockTextBox runat="server" ID="tbJQL" Label="JQL" Help="Jira Query to get the tickets. <span class='tip tip-lava'></span>" Required="true" />
                <Rock:BootstrapButton runat="server" ID="btnSave" Text="Save" CssClass="btn btn-default" OnClick="btnSave_Click" />
                <asp:LinkButton runat="server" ID="btnCancel" Text="Cancel" CssClass="btn btn-link" OnClick="btnCancel_Click" />
            </div>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
