<%@ Control Language="C#" AutoEventWireup="true" CodeFile="JiraTopicList.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Jira.JiraTopicList" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <asp:Panel ID="pnlView" runat="server" CssClass="panel panel-block">

            <div class="panel-heading">
                <h1 class="panel-title">
                    Jira Topics
                </h1>
                <div class="pull-right">
                    <Rock:BootstrapButton runat="server" ID="btnRefresh" Text="Refesh Tickets"  OnClick="btnRefresh_Click" CssClass="btn btn-success btn-xs"/>
                </div>
            </div>
            <div class="panel-body">

                <div class="grid grid-panel">
                    <Rock:Grid ID="gList" runat="server" AllowSorting="false" OnRowSelected="gList_RowSelected" DataKeyNames="Id">
                        <Columns>
                            <Rock:ReorderField />
                            <Rock:RockBoundField DataField="Name" HeaderText="Name" />
                            <Rock:RockBoundField DataField="JQL" HeaderText="JQL" />
                            <Rock:DeleteField OnClick="Delete_Click" />
                        </Columns>
                    </Rock:Grid>
                </div>

            </div>

        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>
