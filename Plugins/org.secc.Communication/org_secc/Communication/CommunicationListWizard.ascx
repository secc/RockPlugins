<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CommunicationListWizard.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Communication.CommunicationListWizard" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <asp:Panel ID="pnlGroups" runat="server" CssClass="panel panel-block">

            <div class="panel-heading">
                <h1 class="panel-title">
                    <i class="fa fa-star"></i>
                    Communication List Wizard
                </h1>
            </div>
            <div class="panel-body">
                <Rock:NotificationBox runat="server" ID="nbError" NotificationBoxType="Validation" />
                <asp:Panel ID="pnlGrid" runat="server">
                    <div class="grid grid-panel">
                        <Rock:Grid ID="gList" runat="server" DataKeyNames="Id" OnRowSelected="gList_RowSelected">
                            <Columns>
                                <Rock:RockBoundField DataField="Name" HeaderText="Name" />
                                <Rock:RockBoundField DataField="Description" HeaderText="Description" />
                                <Rock:RockBoundField DataField="Members" HeaderText="Members" />
                                <Rock:LinkButtonField HeaderText="Analytics" ID="btnAnalytics" Text="<i class='fa fa-line-chart'></i>"
                                    OnClick="btnAnalytics_Click" CssClass="btn btn-default btn-sm" />
                            </Columns>
                        </Rock:Grid>
                    </div>
                </asp:Panel>

                <asp:Panel runat="server" ID="pnlFilter" Visible="false">
                    <asp:PlaceHolder ID="phFilter" runat="server" />
                    <Rock:DataViewItemPicker runat="server" ID="dvDataView" Label="DataView" Help="Optional dataview to further subselect." />
                    <Rock:BootstrapButton runat="server" ID="btnSelect" Text="Select"
                        CssClass="btn btn-primary" OnClick="btnSelect_Click" />
                </asp:Panel>
            </div>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
