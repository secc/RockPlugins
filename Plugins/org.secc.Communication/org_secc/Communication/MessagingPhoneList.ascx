<%@ Control Language="C#" AutoEventWireup="true" CodeFile="MessagingPhoneList.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Communication.MessagingPhoneList"  %>
<asp:UpdatePanel ID="upPhoneList" runat="server">
    <ContentTemplate>
        <asp:Panel ID="pnlPhoneNumbers" runat="server">
            <div class="panel panel-block">
                <div class="panel-heading">
                    <h1 class="panel-title"><i class="fas fa-phone"></i><asp:Literal ID="lTitle" runat="server" Text="Phone Numbers" /></h1>
                </div>
                <div class="panel-body">
                    <div class="grid grid-panel">
                        <Rock:NotificationBox ID="nbNotifications" runat="server" />
                        <Rock:Grid ID="gPhoneNumbers" runat="server" EmptyDataText="No Phone Numbers found"
                                RowItemText="Phone Number" DataKeyNames="Id" OnRowSelected="gPhoneNumbers_RowSelected">
                            <Columns>
                                <Rock:RockBoundField DataField="Name" HeaderText="Name" SortExpression="Name" />
                                <Rock:RockBoundField DataField="NumberFormatted" HeaderText="Phone Number" />
                                <Rock:BadgeField DataField="KeywordCount" HeaderText="Total Keywords" InfoMin="1" />
                                <Rock:BadgeField DataField="ActiveKeywordCount" HeaderText="Active Keywords" InfoMin="1" />
                                <Rock:BoolField DataField="IsActive" HeaderText="Active" />
                                <Rock:LinkButtonField ColumnPriority="AlwaysVisible" HeaderStyle-CssClass="grid-columncommand"
                                    ItemStyle-HorizontalAlign="Center" ItemStyle-CssClass="grid-columncommand" CssClass="btn btn-default btn-sm"
                                    ToolTip="View Keywords" Text="<i class='fas fa-comments'></i>" OnClick="viewKeywords_Click" />
                                <Rock:DeleteField OnClick="gPhoneNumber_Delete"  />
                            </Columns>
                        </Rock:Grid>
                    </div
                    <Rock:ModalDialog ID="mdlAddTwilioNumber" runat="server" SaveButtonText="Add" >
                        <Content>
                            <asp:HiddenField ID="hPhonenumberId" runat="server" />
                            <Rock:RockDropDownList ID="ddlTwilioNumbers" runat="server" Label="Phone Number" Required="true" RequiredErrorMessage="Phone Number Required" />
                            <Rock:RockLiteral ID="lPhone" runat="server" Label="Phone Number" />
                            <Rock:RockTextBox ID="tbName" runat="server" Label="Name" Required="true" RequiredErrorMessage="Name is required" />
                            <Rock:RockCheckBox ID="cbActive" runat="server" Label="Is Active"  />
                        </Content>
                    </Rock:ModalDialog>
                </div>
            </div>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
