<%@ Control Language="C#" AutoEventWireup="true" CodeFile="MetricsEntry.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Reporting.MetricsEntry" %>



<asp:UpdatePanel ID="upnlContent" runat="server">
<ContentTemplate>

    <div class="panel panel-block">

        <div class="panel-heading">
            <h1 class="panel-title"><i class="fa fa-signal"></i> Metric Entry</h1>
        </div>

        <div class="panel-body">

            <asp:Panel ID="pnlSelection" runat="server">

                <h3><asp:Literal ID="lSelection" runat="server"></asp:Literal></h3>

                <asp:Repeater ID="rptrSelection" runat="server" OnItemCommand="rptrSelection_ItemCommand" >
                    <ItemTemplate>
                        <asp:LinkButton ID="lbSelection" runat="server" CommandName='<%# Eval("CommandName") %>'  CommandArgument='<%# Eval("CommandArg") %>' Text='<%# Eval("OptionText") %>' CssClass="btn btn-default btn-block" />
                    </ItemTemplate>
                </asp:Repeater>       

            </asp:Panel>

            <asp:Panel ID="pnlMetrics" runat="server" Visible="false">

                <div class="btn-group btn-group-justified margin-b-lg panel-settings-group" >
                    <Rock:ButtonDropDownList ID="bddlCampus" runat="server" OnSelectionChanged="bddl_SelectionChanged" />
                    <Rock:ButtonDropDownList ID="bddlWeekend" runat="server" OnSelectionChanged="bddl_SelectionChanged" />
                </div>

                <asp:ValidationSummary ID="vsDetails" runat="server" HeaderText="Please Correct the Following" CssClass="alert alert-danger" />
                <Rock:NotificationBox ID="nbMetricsSaved" runat="server" Text="Metric Values Have Been Updated" NotificationBoxType="Success" Visible="false" />
                <asp:Repeater ID="rptrMetricCategory" runat="server">
                    <HeaderTemplate>
                        <h3>Weekly Metrics</h3>
                    </HeaderTemplate>
                    <ItemTemplate>
                        <div class="well well-sm">
                            <h4><%# Eval("CategoryName") %></h4>
                            <div class="row">
                                <asp:Repeater ID="rptrMetric" runat="server" OnItemDataBound="rptrMetric_ItemDataBound" DataSource='<%# Eval("Metrics") %>'>
                                    <ItemTemplate>
                                        <div class="col-sm-6">
                                            <div class="form-horizontal label-lg">
                                                <asp:HiddenField ID="hfMetricId" runat="server" Value='<%# Eval("Id") %>' />
                                                <Rock:NumberBox ID="nbMetricValue" runat="server" NumberType="Double" Label='<%# Eval( "Name") %>' Text='<%# Eval( "Value") %>' />
                                            </div>
                                        </div>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </div>
                        </div>
                    </ItemTemplate>
                </asp:Repeater>
                

                <asp:Repeater ID="rptrService" runat="server" OnItemDataBound="rptrService_ItemDataBound" Visible="false">
                    <HeaderTemplate>
                        <h3>Service Specific Metrics</h3>
                        <div class="row">
                    </HeaderTemplate>
                    <ItemTemplate>
                        <asp:HiddenField ID="hfScheduleId" runat="server" Value='<%# Eval("Id") %>' />
                        <div runat="server" class='<%# ((Container.Parent as Repeater).DataSource as IList).Count%2==0?"col-md-6":"col-md-4" %>'>
                            <Rock:PanelWidget Id="pnlwService" runat="server" Title='<%# Eval("Name") %>' Expanded="true" >
                                <div class="form-horizontal label-md" >
                                    <asp:Repeater ID="rptrServiceMetric" runat="server" OnItemDataBound="rptrMetric_ItemDataBound">
                                        <ItemTemplate>
                                            <asp:HiddenField ID="hfMetricId" runat="server" Value='<%# Eval("Id") %>' />
                                            <Rock:NumberBox ID="nbMetricValue" runat="server" NumberType="Double" Label='<%# Eval( "Name") %>' Text='<%# Eval( "Value") %>' />
                                        </ItemTemplate>
                                    </asp:Repeater>
                                </div>
                            </Rock:PanelWidget>
                        </div>
                    </ItemTemplate>
                    <FooterTemplate>
                        </div>
                    </FooterTemplate>
                </asp:Repeater>


                <Rock:RockTextBox ID="tbNote" runat="server" Label="Note" TextMode="MultiLine" Rows="4" />

                <div class="actions">
                    <asp:LinkButton ID="btnSave" runat="server" Text="Save" AccessKey="s" CssClass="btn btn-primary" OnClick="btnSave_Click" />
                </div>

            </asp:Panel>

        </div>

    </div>

</ContentTemplate>
</asp:UpdatePanel>
