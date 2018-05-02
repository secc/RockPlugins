<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ServiceMetricsEntry.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Reporting.ServiceMetricsEntry" %>



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
                <style>
                    .panel-settings-group .btn-group,  .panel-settings-group .btn {
                        width: 100%;
                    }
                </style>
                <div class="panel-settings-group row" style="margin-bottom:15px" >
                    <div class="col-md-4" style="padding:0px"><Rock:ButtonDropDownList ID="bddlCampus" runat="server" OnSelectionChanged="bddl_SelectionChanged" /></div>
                    <div class="col-md-4" style="padding:0px"><Rock:ButtonDropDownList ID="bddlWeekend" runat="server" OnSelectionChanged="bddl_SelectionChanged" /></div>
                    <div class="col-md-4" style="padding:0px"><Rock:ButtonDropDownList ID="bddlService" runat="server" OnSelectionChanged="bddl_SelectionChanged" /></div>
                </div>
                
                <asp:Repeater ID="rptrMetricCategory" runat="server" OnItemDataBound="rptrMetricCategory_ItemDataBound" DataSourceID="">
                    <ItemTemplate>
                        <asp:ValidationSummary ID="vsDetails" runat="server" HeaderText="Please Correct the Following" CssClass="alert alert-danger" />
                        <Rock:NotificationBox ID="nbMetricsSaved" runat="server" Text="Metric Values Have Been Updated" NotificationBoxType="Success" Visible="false" />
                        <h3><asp:Label ID="lMetricCategoryTitle" runat="server"></asp:Label></h3>
                        <div class="form-horizontal label-md" >
                            <asp:Repeater ID="rptrMetric" runat="server" OnItemDataBound="rptrMetric_ItemDataBound">
                                <ItemTemplate>
                                    <asp:HiddenField ID="hfMetricId" runat="server" Value='<%# Eval("Id") %>' />
                                    <Rock:NumberBox ID="nbMetricValue" runat="server" NumberType="Double" Label='<%# Eval( "Name") %>' Text='<%# Eval( "Value") %>' />
                                </ItemTemplate>
                            </asp:Repeater>
                            <div class="form-group">
                                <div class="control-wrapper" style="width:100%">
                                    <Rock:RockTextBox ID="tbNote" runat="server" Placeholder="Notes" TextMode="MultiLine" />
                                </div>
                            </div>
                        </div>
                        <div class="clearfix">
                            <asp:LinkButton ID="btnSave" runat="server" Text="Save" AccessKey="s" CssClass="btn btn-primary pull-right" OnClick="btnSave_Click" CommandArgument='<%# Eval("Guid") %>' />
                        </div>
                        <hr />
                    </ItemTemplate>
                </asp:Repeater>
            </asp:Panel>

        </div>

    </div>

</ContentTemplate>
</asp:UpdatePanel>
