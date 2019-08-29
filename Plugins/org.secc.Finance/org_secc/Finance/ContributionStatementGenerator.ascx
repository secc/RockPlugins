<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ContributionStatementGenerator.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Finance.ContributionStatementGenerator" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <asp:Panel ID="pnlGivingUnitList" runat="server" class="panel panel-block">
                <div class="panel-heading">
                    <Rock:BootstrapButton ID="btnGenerateTop" runat="server" OnClick="btnContinue_Click" CssClass="btn btn-primary btn-sm pull-right">Continue</Rock:BootstrapButton>
                    <h1 class="panel-title"><i class="fa fa-file-pdf"></i> Contribution Statement Generator</h1>
                </div>
                <div class="panel-body">
                    <div class="row">
                        <div class="col-xs-12">
                            Please use the filter features to narrow down the list or simply select the giving groups to generate statements.  Once the selection is complete, click the "Continue" button:
                        </div>
                    </div>
                    <br />
                    <div class="grid grid-panel">
                        <Rock:GridFilter runat="server" ID="gfFilter" OnApplyFilterClick="gfFilter_ApplyFilterClick" OnClearFilterClick="gfFilter_ClearFilterClick">
                            <Rock:RockTextBox ID="tbGivingId" runat="server" Label="Giving ID"></Rock:RockTextBox>
                            <Rock:RockTextBox ID="tbGivingGroup" runat="server" Label="Giving Group"></Rock:RockTextBox>
                            <Rock:DateRangePicker ID="drpDates" runat="server" Label="Last Gift Date Range" />
                        </Rock:GridFilter>
                        <Rock:Grid ID="gdGivingUnits" runat="server" EmptyDataText="No Giving Units Found" 
                            RowItemText="Giving Units" AllowSorting="true" ExportSource="ColumnOutput"
                            OnGridRebind="gdGivingUnits_GridRebind" EnableStickyHeaders="true" DataKeyNames="GivingId">
                            <Columns>
                                <Rock:SelectField></Rock:SelectField>
                                <Rock:RockBoundField DataField="GivingId" HeaderText="Giving ID" SortExpression="Name" />
                                <Rock:RockBoundField DataField="GivingGroupName" HeaderText="Giving Group" SortExpression="GivingGroupName" />
                                <Rock:RockBoundField DataField="LastGift" HeaderText="Last Gift" SortExpression="LastGift" />
                             </Columns>
                        </Rock:Grid>
                    </div>
                </div>
            </div>
        </asp:Panel>
        
        <asp:Panel ID="pnlSettings" runat="server" class="panel panel-block" Visible="false">
            <div class="panel-heading">
                <div class="label label-info pull-right"><asp:label runat="server" ID="lAlert" ></asp:label></div>
                <h1 class="panel-title"><i class="fa fa-file-pdf"></i> Contribution Statement Generator Settings</h1>
            </div>
            <div class="panel-body">
                <Rock:DateRangePicker runat="server" ID="drpStatementDate" Label="Statement Date Range" />
                <Rock:RockTextBox runat="server" ID="tbVersion" Label="Statement Version" Text="1" Width="330" />
                <Rock:DataViewItemPicker runat="server" ID="dvipReviewDataView" Label="Review Group Dataview" Help="This is just a test." />
                <Rock:BootstrapButton runat="server" ID="btnGenerate" CssClass="btn btn-primary pull-right" OnClick="btnGenerate_Click" Text="Generate Statements" />
                <Rock:BootstrapButton runat="server" ID="btnBack" CssClass="btn btn-default pull-left" OnClick="btnBack_Click" Text="Back"></Rock:BootstrapButton>
            </div>
        </asp:Panel>

        
        <asp:Panel ID="pnlProgressBar" runat="server" class="panel panel-block" Visible="false">
            <div class="panel-heading">
                <h1 class="panel-title"><i class="fa fa-file-pdf"></i> Contribution Statement Generator Progress</h1>
            </div>
            <div class="panel-body">
                <asp:Panel runat="server" CssClass="alert alert-success" ID="pnlProgressText" />
            </div>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
