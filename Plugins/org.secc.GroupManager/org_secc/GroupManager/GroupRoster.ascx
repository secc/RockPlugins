<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GroupRoster.ascx.cs" Inherits="RockWeb.Plugins.org_secc.GroupManager.GroupRoster" %>

<asp:UpdatePanel ID="upDevice" runat="server">
    <ContentTemplate>
        <asp:HiddenField ID="hfCommunication" runat="server" Value="" />
        <Rock:NotificationBox runat="server" ID="nbAlert" NotificationBoxType="Danger" Visible="false"></Rock:NotificationBox>

        <Rock:ModalAlert ID="maSent" runat="server"></Rock:ModalAlert>


        <asp:Panel ID="pnlMain" runat="server">
            <Rock:RockLiteral ID="ltTitle" runat="server" />

            <Rock:BootstrapButton runat="server" Text="Membership List" CssClass="btn btn-primary" ID="btnMembership" OnClick="btnMembership_Click" />

            <Rock:BootstrapButton runat="server" Text="Roster" ID="btnRoster" CssClass="btn btn-default" OnClick="btnRoster_Click" />
            <asp:Panel runat="server" ID="pnlMembership">

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


                <Rock:Grid ID="gMembers" runat="server">
                    <Columns>
                        <Rock:SelectField></Rock:SelectField>
                        <Rock:RockBoundField DataField="Name" HeaderText="Name" ColumnPriority="AlwaysVisible" />
                        <Rock:RockBoundField DataField="DateAdded" HeaderText="Date Added" ColumnPriority="Desktop" />
                        <Rock:RockBoundField DataField="Address" HeaderText="Address" ColumnPriority="Desktop" />
                        <Rock:RockBoundField DataField="City" HeaderText="City" ColumnPriority="Desktop" />
                        <Rock:RockBoundField DataField="State" HeaderText="State" ColumnPriority="Desktop" />
                        <Rock:RockBoundField DataField="Role" HeaderText="Role" ColumnPriority="Tablet" />
                    </Columns>
                </Rock:Grid>

                <Rock:BootstrapButton runat="server" ID="btnEmail" CssClass="btn btn-default btn-lg pull-right" OnClick="btnEmail_Click"><i class="fa fa-laptop"></i> Email</Rock:BootstrapButton>
                <Rock:BootstrapButton runat="server" ID="btnSMS" CssClass="btn btn-default btn-lg pull-right" OnClick="btnSMS_Click"><i class="fa fa-mobile-phone"></i> Text</Rock:BootstrapButton>
            </asp:Panel>

            <asp:Panel runat="server" ID="pnlRoster" Visible="false">
                <br />
                <br />
                <asp:Repeater runat="server" ID="rRoster" OnItemDataBound="rRoster_ItemDataBound">
                    <ItemTemplate>
                        <div class="col-xs-12 col-md-4">
                            <div class="panel panel-default" style="min-height: 200px;">
                                <div class="panel-heading">
                                    <asp:Label runat="server" Style="font-weight: bold" ID="Label2"
                                        Text='<%# Eval("Name") %>' />
                                </div>
                                <div class="panel-body">
                                    <div class="col-xs-4 thumbnail">
                                        <img src='<%# Eval("PhotoUrl") %>' />
                                    </div>
                                    <div class="col-xs-8">
                                        <asp:Label runat="server" ID="Label1"
                                            Text='<%# Eval("FormattedAddress") %>' /><br />
                                        <asp:Label runat="server" ID="Label3"
                                            Text='<%# Eval("City") %>' />,
                                    <asp:Label runat="server" ID="Label4"
                                        Text='<%# Eval("State") %>' />
                                        <br />
                                        <asp:Label runat="server" ID="Label5"
                                            Text='<%# Eval("Phone") %>' />
                                        <Rock:BootstrapButton runat="server" ID="btnTest"></Rock:BootstrapButton>
                                        <asp:PlaceHolder runat="server" ID="phTest"></asp:PlaceHolder>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </ItemTemplate>
                </asp:Repeater>
            </asp:Panel>
        </asp:Panel>


        <!-- SMS Panel -->
        <asp:Panel ID="pnlSMS" runat="server" Visible="false">
            <h1>Text Message:</h1>
            <Rock:Toggle ID="cbSMSSendToParents" runat="server" OnText="Parents" OffText="Members" Label="Send To:" OnCssClass="btn-primary" OffCssClass="btn-primary" Checked="false" />
            <div id="charCount"></div>
            <Rock:RockTextBox ID="tbMessage" runat="server" TextMode="MultiLine" Rows="5"></Rock:RockTextBox>
            <Rock:BootstrapButton ID="btnSMSSend" runat="server" OnClick="btnSMSSend_Click" CssClass="btn btn-primary">Send</Rock:BootstrapButton>
            <Rock:BootstrapButton ID="btnSMSCancel" runat="server" OnClick="btnCancel_Click" CssClass="btn btn-default">Cancel</Rock:BootstrapButton>
        </asp:Panel>

        <!-- Email Panel -->
        <asp:Panel ID="pnlEmail" runat="server" Visible="false">
            <h1>Email</h1>
            <Rock:Toggle ID="cbEmailSendToParents" runat="server" OnText="Parents" OffText="Members" Label="Send To:" OnCssClass="btn-primary" OffCssClass="btn-primary" Checked="false" />
            <Rock:RockTextBox ID="tbSubject" runat="server" Label="Subject"></Rock:RockTextBox>
            <Rock:RockTextBox ID="tbBody" runat="server" TextMode="MultiLine" Rows="5" Label="Message"></Rock:RockTextBox>
            <Rock:BootstrapButton ID="btnEmailSend" runat="server" OnClick="btnEmailSend_Click" CssClass="btn btn-primary">Send</Rock:BootstrapButton>
            <Rock:BootstrapButton ID="btnEmailCancel" runat="server" OnClick="btnCancel_Click" CssClass="btn btn-default">Cancel</Rock:BootstrapButton>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
