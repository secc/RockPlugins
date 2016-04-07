<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GroupRegistrationModal.ascx.cs" Inherits="RockWeb.Plugins.org_secc.GroupManager.GroupRegistrationModal" %>

<style type="text/css">
    .modal-footer {
        visibility: hidden;
    }
</style>

<Rock:RockUpdatePanel ID="upMain" runat="server">
    <ContentTemplate>
        <asp:LinkButton runat="server" CssClass="btn btn-default" ValidationGroup="None" ID="btnLaunchModal" CausesValidation="false"
            OnClick="btnLaunchModal_Click" Text="<i class='fa fa-user-plus'></i> Add Group Member"></asp:LinkButton>
        <Rock:ModalDialog ID="mdDialog" ValidationGroup="AddMember" CancelLinkVisible="false" runat="server" Title="Add Group Member">
            <Content>
                <asp:Panel runat="server" ID="pnlForm">
                    <Rock:NotificationBox runat="server" ID="nbInvalid" NotificationBoxType="Warning" Dismissable="true" Visible="true">
                        First and Last name is required and one of Birthday, Phone Number, or Email.
                    </Rock:NotificationBox>
                    <Rock:RockTextBox ID="tbFirstName" runat="server" Label="First Name" ValidationGroup="AddMemeber" Required="True"></Rock:RockTextBox>
                    <Rock:RockTextBox ID="tbLastName" runat="server" Label="Last Name" ValidationGroup="AddMember" Required="True"></Rock:RockTextBox>

                    <Rock:DatePicker runat="server" ID="dpBirthday" Label="Birthday"></Rock:DatePicker>

                    <Rock:PhoneNumberBox ID="pnCell" runat="server" Label="Cell Phone"></Rock:PhoneNumberBox>

                    <Rock:EmailBox ID="tbEmail" runat="server" Label="Email"></Rock:EmailBox>

                    <div>
                        <Rock:BootstrapButton ID="btnCancel" runat="server"
                            CssClass="btn btn-default pull-right btn-lg" OnClick="btnCancel_Click">Close</Rock:BootstrapButton>
                        <div class="pull-right">&nbsp;</div>
                        <Rock:BootstrapButton ID="btnRegister" OnClick="btnRegister_Click" runat="server" Text="Add"
                            CssClass="btn btn-primary pull-right btn-lg" ValidationGroup="AddMember" CausesValidation="true" />
                        <div class="pull-right">&nbsp;</div>

                    </div>
                </asp:Panel>
                <asp:Panel runat="server" ID="pnlResults" Visible="false">
                    <Rock:RockLiteral runat="server" ID="ltResults"></Rock:RockLiteral>
                    
                    <div>
                        <Rock:BootstrapButton ID="btnAddAnother" runat="server" CssClass="btn btn-primary"
                             OnClick="btnAddAnother_Click">Add Another Member</Rock:BootstrapButton>
                        <Rock:BootstrapButton ID="btnClose" runat="server" CssClass="btn btn-default m-l-1"
                             OnClick="btnClose_Click">Finished</Rock:BootstrapButton>
                    </div>
                </asp:Panel>
            </Content>
        </Rock:ModalDialog>
    </ContentTemplate>
</Rock:RockUpdatePanel>
