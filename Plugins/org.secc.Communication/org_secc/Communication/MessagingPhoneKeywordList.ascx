<%@ Control Language="C#" AutoEventWireup="true" CodeFile="MessagingPhoneKeywordList.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Communication.MessagingPhoneKeywordList" %>
<asp:UpdatePanel ID="upKeywordList" runat="server">
    <ContentTemplate>
        <asp:Panel ID="pnlContent" CssClass="panel panel-block" runat="server">
            <asp:HiddenField ID="hfPhoneNumberId" runat="server" />
            <div class="panel-heading">
                <h1 class="panel-title"><i class="fas fa-comments"></i><asp:Literal ID="lTitle" runat="server" Text="Keywords"></asp:Literal></h1>
            </div>
            <div class="panel-body">
                <Rock:NotificationBox ID="nbNotifications" runat="server" />
                <div class="grid grid-panel">
                    <Rock:Grid ID="gKeywords" runat="server" AllowPaging="true" AllowSorting="false" DataKeyNames="KeywordId">
                        <Columns>
                            <Rock:ReorderField />
                            <Rock:RockBoundField DataField="MessageToMatch" HeaderText="Keyword" />
                            <Rock:RockBoundField DataField="ResponseMessage" HeaderText="Response" />
                            <Rock:DateTimeField DataField="StartDate" HeaderText="Start"  />
                            <Rock:DateTimeField DataField="EndDate" HeaderText="End" />
                            <Rock:RockBoundField DataField="Status" HeaderText="Status" />
                            <Rock:DeleteField OnClick="gKeywords_DeleteClick" />
                        </Columns>
                    </Rock:Grid>

                </div>
                <Rock:ModalDialog ID="mdlEditKeyword" runat="server" SaveButtonText="Save"
                    SaveButtonCausesValidation="true" ValidationGroup="valKeyword">
                    <Content>
                        <asp:ValidationSummary ID="valSummaryKeyword" runat="server" CssClass="alert alert-validation" ValidationGroup="valKeyword" />
                        <Rock:NotificationBox ID="nbKeywordPanel" runat="server" Visible="false" NotificationBoxType="Validation" />
                        <asp:HiddenField ID="hfKeyword" runat="server" />
                        <Rock:RockTextBox ID="tbWord" runat="server" Label="Phrase to Match" Required="true"
                            ValidationGroup="valKeyword" RequiredErrorMessage="Phrase to Match is required" />
                        <Rock:RockTextBox ID="tbResponse" runat="server" Label="Response Message" TextMode="MultiLine"
                            MaxLength="160" ShowCountDown="true" Required="true" ValidationGroup="valKeyword" RequiredErrorMessage="Response Message is required." />
                        <div class="row">
                            <div class="col-md-6">
                                <Rock:DateTimePicker ID="dtpStartDate" runat="server" Label="Start Date" ValidationGroup="valKeyword" />
                            </div>
                            <div class="col-md-6">
                                <Rock:DateTimePicker ID="dtpEndDate" runat="server" Label="End Date" ValidationGroup="valKeyword" />
                            </div>
                        </div>
                        <Rock:RockCheckBox ID="cbActive" runat="server" Label="Active" />
                        <asp:Panel ID="pnlStatus" runat="server" CssClass="row">
                            <div class="col-md-6">
                                <Rock:RockLiteral ID="lCreatedBy" runat="server" Label="Created By" />
                            </div>
                            <div class="col-md-6">
                                <Rock:RockLiteral ID="lModifiedBy" runat="server" Label="Modified By" />
                            </div>
                        </asp:Panel>
                    </Content>
                </Rock:ModalDialog>
            </div>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>