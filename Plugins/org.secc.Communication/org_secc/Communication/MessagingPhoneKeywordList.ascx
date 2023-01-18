<%@ Control Language="C#" AutoEventWireup="true" CodeFile="MessagingPhoneKeywordList.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Communication.MessagingPhoneKeywordList" %>
<asp:UpdatePanel ID="upKeywordList" runat="server">
    <ContentTemplate>
        <asp:Panel ID="pnlContnet" CssClass="panel panel-block" runat="server">
            <asp:HiddenField ID="hfPhoneNumberId" runat="server" />
            <div class="panel-heading">
                <h1 class="panel-title"><i class="fas fa-comments"></i><asp:Literal ID="lTitle" runat="server" Text="Keywords"></asp:Literal></h1>
            </div>
            <div class="panel-body">
                <Rock:NotificationBox ID="nbNotifications" runat="server" />
                <div class="grid grid-panel">
                    <Rock:Grid ID="gKeywords" runat="server" AllowPaging="true" AllowSorting="false">
                        <Columns>
                            <Rock:ReorderField />
                            <Rock:RockBoundField DataField="MessageToMatch" HeaderText="Keyword" />
                            <Rock:RockBoundField DataField="ResponseMessage" HeaderText="Response" />
                            <Rock:DateField DataField="StartDate" HeaderText="Start"  />
                            <Rock:DateField DataField="EndDate" HeaderText="End" />
                            <Rock:RockBoundField DataField="Status" HeaderText="Status" />
                        </Columns>
                    </Rock:Grid>

                </div>
                <Rock:ModalDialog ID="mdlEditKeyword" runat="server" SaveButtonText="Save">
                    <Content>
                        <asp:HiddenField ID="hfKeyword" runat="server" />
                        <Rock:RockTextBox ID="tbWord" runat="server" Label="Phrase to Match" Required="true"
                            ValidationGroup="valKeyword" RequiredErrorMessage="Phrase to Match is required" />
                        <Rock:RockTextBox ID="tbResponse" runat="server" Label="Response Message" TextMode="MultiLine"
                            MaxLength="160" ShowCountDown="true" Required="true" ValidationGroup="valKeyword" RequiredErrorMessage="Response Message is required." />
                        <Rock:DateTimePicker ID="dtpStartDate" runat="server" Label="Start Date" ValidationGroup="valKeyword" />
                        <Rock:DateTimePicker ID="dtpEndDate" runat="server" Label="End Date" ValidationGroup="valKeyword" />
                    </Content>
                </Rock:ModalDialog>
            </div>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>