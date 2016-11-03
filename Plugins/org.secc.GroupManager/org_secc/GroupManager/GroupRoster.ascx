<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GroupRoster.ascx.cs" Inherits="RockWeb.Plugins.org_secc.GroupManager.GroupRoster" %>

<style>
    tfoot {
        display: none;
    }
</style>

<asp:UpdatePanel ID="upDevice" runat="server">
    <ContentTemplate>
        <asp:HiddenField ID="hfCommunication" runat="server" Value="" />
        <Rock:NotificationBox runat="server" ID="nbAlert" NotificationBoxType="Danger" Visible="false"></Rock:NotificationBox>

        <Rock:ModalAlert ID="maSent" runat="server"></Rock:ModalAlert>

        <Rock:ModalDialog ID="mdMember" runat="server">
            <Content>
                <asp:HiddenField ID="hfMemberId" runat="server" />
                <div class="container">
                    <div>
                        <Rock:HighlightLabel ID="lbStatus" runat="server"></Rock:HighlightLabel>
                        <Rock:HighlightLabel ID="lbRole" LabelType="Default" runat="server"></Rock:HighlightLabel>
                    </div>
                    <div class="row">
                        <Rock:RockLiteral ID="ltAddress" runat="server" Label="Address" CssClass="col-sm-6 col-xs-12"></Rock:RockLiteral>
                        <Rock:RockLiteral ID="ltPhone" runat="server" Label="Phone Number" CssClass="col-sm-6 col-xs-12"></Rock:RockLiteral>
                    </div>
                    <div class="row">
                        <Rock:RockLiteral ID="ltEmail" runat="server" Label="Email" CssClass="col-sm-6 col-xs-12"></Rock:RockLiteral>
                        <Rock:RockLiteral ID="ltDateAdded" runat="server" Label="Date Added" CssClass="col-sm-6 col-xs-12"></Rock:RockLiteral>
                    </div>
                    <div class="row">
                        <Rock:RockLiteral ID="ltDateInactive" runat="server" Label="Date Inactivated"
                            CssClass="col-sm-6 col-xs-12"></Rock:RockLiteral>
                        <div class="col-sm-6 col-xs-12">
                            <Rock:BootstrapButton ID="btnActivate" runat="server" Text="Activate Member"
                                OnClick="btnActivate_Click" CssClass="btn btn-primary"></Rock:BootstrapButton>
                            <Rock:BootstrapButton ID="btnDeactivate" runat="server" Text="Deactivate Member"
                                OnClick="btnDeactivate_Click" CssClass="btn btn-danger"></Rock:BootstrapButton>

                            <Rock:BootstrapButton ID="btnCloseModal" CssClass="btn btn-default"
                                OnClick="btnCloseModal_Click" runat="server" Text="Close"></Rock:BootstrapButton>
                        </div>
                    </div>
                </div>



            </Content>
        </Rock:ModalDialog>

        <asp:Panel ID="pnlMain" runat="server">
            <asp:Literal ID="ltTitle" runat="server" />

            <Rock:BootstrapButton runat="server" Text="Member List" CssClass="btn btn-primary hidden-print" ID="btnMembership" OnClick="btnMembership_Click" />
            <Rock:BootstrapButton runat="server" Text="Roster" ID="btnRoster" CssClass="btn btn-default hidden-print" OnClick="btnRoster_Click" />
            <asp:LinkButton runat="server" Text="Print Attendance" ID="btnPrint" CssClass="btn btn-success pull-right hidden-sm hidden-xs hidden-md hidden-print" OnClientClick="window.print()"></asp:LinkButton>
            <hr>

            <asp:Panel runat="server" ID="pnlMembership">
                <div class="hidden-print">
                    <Rock:GridFilter ID="rFilter" runat="server" OnApplyFilterClick="rFilter_ApplyFilterClick">
                        <Rock:RockTextBox ID="txtFirstName" runat="server" Label="First Name"></Rock:RockTextBox>
                        <Rock:RockTextBox ID="txtLastName" runat="server" Label="Last Name"></Rock:RockTextBox>
                        <br>
                        <Rock:RockCheckBoxList ID="cblRole" runat="server" Label="Member Role"></Rock:RockCheckBoxList>
                        <Rock:RockCheckBoxList ID="cblGender" runat="server" Label="Gender">
                        </Rock:RockCheckBoxList>
                        <Rock:RockCheckBoxList runat="server" ID="cblStatus" Label="Status">
                        </Rock:RockCheckBoxList>
                    </Rock:GridFilter>
                </div>

                <Rock:Grid ID="gMembers" runat="server" OnRowSelected="gMembers_RowSelected"
                    OnRowDataBound="gMembers_RowDataBound" HeaderStyle-CssClass="hidden-print">
                    <Columns>
                        <Rock:SelectField ItemStyle-CssClass="hidden-print grid-select-field select-all"></Rock:SelectField>
                        <Rock:RockBoundField DataField="Name" HeaderText="Name" ColumnPriority="AlwaysVisible" />
                        <Rock:SelectField ItemStyle-CssClass="hidden-xs visible-print grid-select-field" ControlStyle-CssClass="hidden-xs"></Rock:SelectField>
                        <Rock:SelectField ItemStyle-CssClass="hidden-xs visible-print grid-select-field" ControlStyle-CssClass="hidden-xs"></Rock:SelectField>
                        <Rock:SelectField ItemStyle-CssClass="hidden-xs visible-print grid-select-field" ControlStyle-CssClass="hidden-xs"></Rock:SelectField>
                        <Rock:SelectField ItemStyle-CssClass="hidden-xs visible-print grid-select-field" ControlStyle-CssClass="hidden-xs"></Rock:SelectField>
                        <Rock:SelectField ItemStyle-CssClass="hidden-xs visible-print grid-select-field" ControlStyle-CssClass="hidden-xs"></Rock:SelectField>
                        <Rock:RockBoundField DataField="DateAdded" HeaderText="Date Added" ColumnPriority="Desktop" />
                        <Rock:RockBoundField DataField="Address" HeaderText="Address" ColumnPriority="Desktop" />
                        <Rock:RockBoundField DataField="City" HeaderText="City" ColumnPriority="Desktop" />
                        <Rock:RockBoundField DataField="State" HeaderText="State" ColumnPriority="Desktop" />
                        <Rock:RockBoundField DataField="Role" HeaderText="Role" ColumnPriority="Tablet" />
                        <Rock:RockBoundField DataField="Status" HeaderText="Status" ColumnPriority="Desktop" />
                    </Columns>
                </Rock:Grid>

                <Rock:BootstrapButton runat="server" ID="btnEmail" CssClass="btn btn-default btn-lg pull-right hidden-print" OnClick="btnEmail_Click"><i class="fa fa-laptop"></i> Email</Rock:BootstrapButton>
                <Rock:BootstrapButton runat="server" ID="btnSMS" CssClass="btn btn-default btn-lg pull-right hidden-print" OnClick="btnSMS_Click"><i class="fa fa-mobile-phone"></i> Text</Rock:BootstrapButton>
            </asp:Panel>

            <asp:Panel runat="server" ID="pnlRoster" Visible="false" CssClass="wrapper">
                <div class="row">
                    <asp:Repeater runat="server" ID="rRoster" OnItemDataBound="rRoster_ItemDataBound">
                        <ItemTemplate>
                            <div class="col-sm-4">
                                <div class="panel panel-default" style="min-height: 200px;">
                                    <div class="panel-heading">
                                        <asp:Label runat="server" Style="font-weight: bold"
                                            Text='<%# Eval("Name") %>' />
                                        <Rock:HighlightLabel runat="server" Text='<%# Eval("Status") %>'
                                            CssClass='<%# StyleStatusLabel(Eval("Status"))%>' />
                                        <Rock:HighlightLabel runat="server" Text='<%# Eval("Role") %>'
                                            CssClass='<%# StyleLeaderLabel(Eval("IsLeader"))%>' />
                                    </div>
                                    <div class="panel-body">
                                        <div class="col-sm-4 col-xs-12 thumbnail">
                                            <img src='<%# Eval("PhotoUrl") %>' alt="Photo">
                                        </div>
                                        <div class="col-sm-8 col-xs-12">
                                            <asp:Label runat="server" ID="Label1"
                                                Text='<%# Eval("FormattedAddress") %>' />
                                            <br />
                                            <asp:Label runat="server" ID="Label5"
                                                Text='<%# Eval("Phone") %>' />
                                            <br>
                                            <Rock:BootstrapButton runat="server" ID="btnRosterEmail"></Rock:BootstrapButton>
                                            <Rock:RockLiteral ID="ltLava" runat="server" Text='<%# RosterLava(Eval("Person"))%>'></Rock:RockLiteral>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </ItemTemplate>
                    </asp:Repeater>
                </div>
            </asp:Panel>
        </asp:Panel>


        <!-- SMS Panel -->
        <asp:Panel ID="pnlSMS" runat="server" Visible="false">
            <h1>Text Message:</h1>
            <Rock:RockLiteral ID="ltSMSRecipients" runat="server"></Rock:RockLiteral>
            <Rock:Toggle ID="cbSMSSendToParents" runat="server" OnText="Parents" OffText="Members" Label="Send To:"
                OnCssClass="btn-primary" OffCssClass="btn-primary" Checked="false" OnCheckedChanged="cbSMSSendToParents_CheckedChanged" />
            <div id="charCount"></div>
            <Rock:RockTextBox ID="tbMessage" runat="server" TextMode="MultiLine" Rows="5"></Rock:RockTextBox>
            <Rock:BootstrapButton ID="btnSMSSend" runat="server" OnClick="btnSMSSend_Click" CssClass="btn btn-primary">Send</Rock:BootstrapButton>
            <Rock:BootstrapButton ID="btnSMSCancel" runat="server" OnClick="btnCancel_Click" CssClass="btn btn-default">Cancel</Rock:BootstrapButton>
        </asp:Panel>

        <!-- Email Panel -->
        <asp:Panel ID="pnlEmail" runat="server" Visible="false">
            <h1>Email</h1>
            <Rock:RockLiteral ID="ltEmailRecipients" runat="server"></Rock:RockLiteral>
            <Rock:Toggle ID="cbEmailSendToParents" runat="server" OnText="Parents" OffText="Members" Label="Send To:"
                OnCssClass="btn-primary" OffCssClass="btn-primary" Checked="false" OnCheckedChanged="cbEmailSendToParents_CheckedChanged" />
            <Rock:RockTextBox ID="tbSubject" runat="server" Label="Subject"></Rock:RockTextBox>
            <Rock:RockTextBox ID="tbBody" runat="server" TextMode="MultiLine" Rows="5" Label="Message"></Rock:RockTextBox>
            <Rock:BootstrapButton ID="btnEmailSend" runat="server" OnClick="btnEmailSend_Click" CssClass="btn btn-primary">Send</Rock:BootstrapButton>
            <Rock:BootstrapButton ID="btnEmailCancel" runat="server" OnClick="btnCancel_Click" CssClass="btn btn-default">Cancel</Rock:BootstrapButton>
        </asp:Panel>
    </ContentTemplate>

</asp:UpdatePanel>
