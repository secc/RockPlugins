<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GroupRoster.ascx.cs" Inherits="RockWeb.Plugins.org_secc.GroupManager.GroupRoster" %>

<asp:UpdatePanel ID="upDevice" runat="server">
    <ContentTemplate>

        <Rock:ModalAlert ID="maSent" runat="server"></Rock:ModalAlert>

        <asp:Panel ID="pnlMain" runat="server">
        <Rock:RockLiteral ID="ltTitle" runat="server" />
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
                <asp:BoundField DataField="Name" HeaderText="Name" SortExpression="Name" />
                <asp:BoundField DataField="DateAdded" HeaderText="Date Added" SortExpression="Name" />
                <asp:BoundField DataField="Address" HeaderText="Address" SortExpression="Name" />
                <asp:BoundField DataField="City" HeaderText="City" SortExpression="Name" />
                <asp:BoundField DataField="State" HeaderText="State" SortExpression="Name" />
                <asp:BoundField DataField="Role" HeaderText="Role" SortExpression="Name" />
            </Columns>
        </Rock:Grid>

        <Rock:BootstrapButton runat="server" ID="btnEmail" CssClass="btn btn-default btn-lg pull-right" OnClick="btnEmail_Click"><i class="fa fa-laptop"></i> Email</Rock:BootstrapButton>
        <Rock:BootstrapButton runat="server" ID="btnSMS" CssClass="btn btn-default btn-lg pull-right" OnClick="btnSMS_Click"><i class="fa fa-mobile-phone"></i> Text</Rock:BootstrapButton>
        </asp:Panel>
        
        
        <!-- SMS Panel -->
        <asp:Panel ID="pnlSMS" runat="server" Visible="false">
            <h1>Text Message:</h1>
            <Rock:Toggle ID="cbSMSSendToParents" runat="server" OnText="Parents" OffText="Members" Label="Send To:" OnCssClass="btn-primary" OffCssClass="btn-primary"  Checked="false"/>
            <div id="charCount"></div>
            <Rock:RockTextBox ID="tbMessage" runat="server" TextMode="MultiLine" Rows="5"></Rock:RockTextBox>
            <Rock:BootstrapButton ID="btnSMSSend" runat="server" OnClick="btnSMSSend_Click" CssClass="btn btn-primary">Send</Rock:BootstrapButton>
            <Rock:BootstrapButton ID="btnSMSCancel" runat="server" OnClick="btnCancel_Click" CssClass="btn btn-default">Cancel</Rock:BootstrapButton>
        </asp:Panel>

        <!-- Email Panel -->
        <asp:Panel ID="pnlEmail" runat="server" Visible="false">
            <h1>Email</h1>
            <Rock:Toggle ID="cbEmailSendToParents" runat="server" OnText="Parents" OffText="Members" Label="Send To:" OnCssClass="btn-primary" OffCssClass="btn-primary" Checked="false"/>
            <Rock:RockTextBox ID="tbSubject" runat="server" Label="Subject"></Rock:RockTextBox>
            <Rock:RockTextBox ID="tbBody" runat="server" TextMode="MultiLine" Rows="5" Label="Message"></Rock:RockTextBox>
            <Rock:BootstrapButton ID="btnEmailSend" runat="server" OnClick="btnEmailSend_Click" CssClass="btn btn-primary">Send</Rock:BootstrapButton>
            <Rock:BootstrapButton ID="btnEmailCancel" runat="server" OnClick="btnCancel_Click" CssClass="btn btn-default">Cancel</Rock:BootstrapButton>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>