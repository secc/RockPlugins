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
                <asp:Panel id="pnlKeywordGrid" runat="server" Visible="true" CssClass="grid grid-panel">
                    <Rock:GridFilter ID="gfKeywords" runat="server">
                        <Rock:RockTextBox ID="tbKeywordSearch" runat="server" Label="Keyword" />
                        <Rock:RockCheckBoxList ID="cblStatus" runat="server" Label="Status" RepeatDirection="Horizontal" RepeatColumns="3">
                            <asp:ListItem Value="Active" Text="Active" />
                            <asp:ListItem Value="Inactive" Text="Inactive" />
                            <asp:ListItem Value="PendingApproval" Text="Pending Approval" />
                        </Rock:RockCheckBoxList>

                    </Rock:GridFilter>
                    <Rock:Grid ID="gKeywords" runat="server" AllowPaging="true" AllowSorting="false" DataKeyNames="KeywordId">
                        <Columns>
                            <Rock:ReorderField />
                            <Rock:RockBoundField DataField="Name" HeaderText="Name" />
                            <Rock:RockBoundField DataField="Description" HeaderText="Description" />
                            <Rock:RockBoundField DataField="ResponseMessage" HeaderText="Response" />
                            <Rock:DateField DataField="StartDate" HeaderStyle-HorizontalAlign="Left" ItemStyle-HorizontalAlign="Left" HeaderText="Start"  />
                            <Rock:Datefield DataField="EndDate" HeaderStyle-HorizontalAlign="Left" ItemStyle-HorizontalAlign="Left" HeaderText="End" />
                            <Rock:BadgeField DataField="PhraseCount" HeaderText="Phrases" InfoMin="1" />
                            <Rock:RockLiteralField  HeaderText="Status" HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center" >
                                <ItemTemplate>
                                    <asp:Literal ID="lStatus" runat="server" />
                                </ItemTemplate>
                            </Rock:RockLiteralField>
                            <Rock:DeleteField OnClick="gKeywords_DeleteClick" />
                        </Columns>
                    </Rock:Grid>

                </asp:Panel>
                <asp:Panel ID="pnlKeywordEdit" runat="server" Visible="false">
                    <asp:ValidationSummary iv="vsKeywordEdit" runat="server" ValidationGroup="keyword-edit" CssClass="alert alert-validation" HeaderText="Please correct the following:" />
                    <asp:HiddenField ID="hfKeywordId" runat="server" />
                    <Rock:RockTextBox ID="tbName" runat="server" Label="Name" CssClass="input-width-xxl" Required="true" RequiredErrorMessage="Name is required" ValidationGroup="keyword-edit" />
                    <Rock:RockTextBox ID="tbDescription" runat="server" Label="Description"  TextMode="MultiLine" MaxLength="250" ShowCountDown="true" ValidationGroup="keyword-edit"/>
                    <div class="row">
                        <div class="col-sm-6">
                            <Rock:DatePicker ID="dpStart" runat="server" Label="Start Date" />
                        </div>
                        <div class="col-sm-6">
                            <Rock:DatePicker ID="dpEnd" runat="server" Label="End Date" />
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-sm-6">
                            <Rock:PersonPicker ID="ppContact" runat="server" Label="Contact" />
                        </div>
                        <div class="col-sm-6">
                            <Rock:Switch ID="switchActive" Label="Active" runat="server" />
                        </div>

                    </div>
                    

                    <div class="well">
                        <div class="row">
                            <div class="col-md-4 col-sm-6">
                                <Rock:ListItems ID="listPhrasesToMatch" runat="server" Label="Phrases to Match" Required="true"  RequiredErrorMessage="At least one phrase to match is required." ValidationGroup="keyword-edit" />
                            </div>
                            <div class="col-md-8 col-sm-6">
                                <Rock:RockTextBox ID="tbResponseMessage" runat="server" Label="Response Message" TextMode="MultiLine" MaxLength="160" Required="true" RequiredErrorMessage="Response Message is required." ValidationGroup="keyword-edit" />
                            </div>
                        </div>
                    </div>
                    <div class="actions">
                        <asp:LinkButton ID="lbKeywordSave" runat="server" CssClass="btn btn-primary" ValidationGroup="keyword-edit" CausesValidation="true">Save</asp:LinkButton>
                        <asp:LinkButton ID="lbKeywordCancel" runat="server" CssClass="btn btn-default" CausesValidation="false">Cancel</asp:LinkButton>
                    </div>
                </asp:Panel>
            </div>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>