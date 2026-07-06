<%@ Control Language="C#" AutoEventWireup="true" CodeFile="SmsCaptureInbox.ascx.cs" Inherits="RockWeb.Plugins.org_secc.SmsCapture.SmsCaptureInbox" %>
<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <Rock:ModalAlert ID="mdGridWarning" runat="server" />
        <asp:Panel ID="pnlList" runat="server" CssClass="panel panel-block">
            <div class="panel-heading">
                <h1 class="panel-title"><i class="fa fa-comment-dots"></i> SMS Capture Inbox</h1>
                <div class="panel-labels">
                    <asp:LinkButton ID="btnClearAll" runat="server" CssClass="btn btn-danger btn-xs" OnClick="btnClearAll_Click" OnClientClick="return confirm('Are you sure you want to delete ALL captured messages?');" CausesValidation="false"><i class="fa fa-trash"></i> Clear All</asp:LinkButton>
                </div>
            </div>
            <div class="panel-body">
                <div class="grid grid-panel">
                    <Rock:GridFilter ID="gfFilter" runat="server" OnApplyFilterClick="gfFilter_ApplyFilterClick" OnDisplayFilterValue="gfFilter_DisplayFilterValue" OnClearFilterClick="gfFilter_ClearFilterClick">
                        <Rock:DateRangePicker ID="drpDates" runat="server" Label="Date Range" />
                        <Rock:RockTextBox ID="tbToNumber" runat="server" Label="To Number Contains" />
                        <Rock:RockTextBox ID="tbBody" runat="server" Label="Body Contains" />
                    </Rock:GridFilter>
                    <Rock:Grid ID="gMessages" runat="server" AllowSorting="true" RowItemText="Message" DataKeyNames="Id" OnRowSelected="gMessages_RowSelected" OnGridRebind="gMessages_GridRebind" OnRowDataBound="gMessages_RowDataBound">
                        <Columns>
                            <Rock:DateTimeField DataField="CreatedDateTime" HeaderText="Captured" SortExpression="CreatedDateTime" />
                            <Rock:RockBoundField DataField="FromNumber" HeaderText="From" SortExpression="FromNumber" />
                            <Rock:RockBoundField DataField="ToNumber" HeaderText="To" SortExpression="ToNumber" />
                            <Rock:RockTemplateField HeaderText="Person">
                                <ItemTemplate><asp:Literal ID="lPerson" runat="server" /></ItemTemplate>
                            </Rock:RockTemplateField>
                            <Rock:RockBoundField DataField="BodyPreview" HeaderText="Message" />
                            <Rock:RockBoundField DataField="Source" HeaderText="Source" SortExpression="Source" />
                            <Rock:RockTemplateField HeaderText="Communication">
                                <ItemTemplate><asp:Literal ID="lCommunication" runat="server" /></ItemTemplate>
                            </Rock:RockTemplateField>
                            <Rock:DeleteField OnClick="gMessages_Delete" />
                        </Columns>
                    </Rock:Grid>
                </div>
            </div>
        </asp:Panel>
        <Rock:ModalDialog ID="mdDetail" runat="server" Title="Captured SMS">
            <Content>
                <div class="row">
                    <div class="col-md-6">
                        <Rock:RockLiteral ID="lDetailFrom" runat="server" Label="From" />
                        <Rock:RockLiteral ID="lDetailTo" runat="server" Label="To" />
                        <Rock:RockLiteral ID="lDetailPerson" runat="server" Label="Person" />
                    </div>
                    <div class="col-md-6">
                        <Rock:RockLiteral ID="lDetailCaptured" runat="server" Label="Captured" />
                        <Rock:RockLiteral ID="lDetailSource" runat="server" Label="Source" />
                        <Rock:RockLiteral ID="lDetailCommunication" runat="server" Label="Communication" />
                    </div>
                </div>
                <Rock:RockLiteral ID="lDetailBody" runat="server" Label="Message" />
                <Rock:RockLiteral ID="lDetailAttachments" runat="server" Label="Attachments" />
            </Content>
        </Rock:ModalDialog>
    </ContentTemplate>
</asp:UpdatePanel>
