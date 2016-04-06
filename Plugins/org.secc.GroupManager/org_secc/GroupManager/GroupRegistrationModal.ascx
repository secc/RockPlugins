<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GroupRegistrationModal.ascx.cs" Inherits="RockWeb.Plugins.org_secc.GroupManager.GroupRegistrationModal" %>

<Rock:RockUpdatePanel ID="container" runat="server">
    <ContentTemplate>
        <Rock:RockUpdatePanel ID="upMain" runat="server">
            <ContentTemplate>
                <asp:Panel runat="server" ID="pnlButton">
                    <asp:LinkButton runat="server" CssClass="btn btn-default" ValidationGroup="None" ID="btnLaunchModal" CausesValidation="false"
                        OnClick="btnLaunchModal_Click" Text="<i class='fa fa-user-plus'></i> Add Group Member"></asp:LinkButton>
                </asp:Panel>
            </ContentTemplate>
        </Rock:RockUpdatePanel>
        <Rock:RockUpdatePanel runat="server" ID="pnlModal">
            <ContentTemplate>
                <Rock:ModalDialog ID="mdDialog" runat="server" Title="Add Group Member">
                    <Content>
                        <Rock:RockTextBox ID="tbFirstName" runat="server" Label="First Name" ValidationGroup="AddMemeber" Required="True"></Rock:RockTextBox>
                        <Rock:RockTextBox ID="tbLastName" runat="server" Label="Last Name" ValidationGroup="AddMember" Required="True"></Rock:RockTextBox>

                        <Rock:DatePicker runat="server" ID="dpBirthday" Label="Birthday"></Rock:DatePicker>

                        <Rock:PhoneNumberBox ID="pnCell" runat="server" Label="Cell Phone"></Rock:PhoneNumberBox>

                        <Rock:EmailBox ID="tbEmail" runat="server" Label="Email"></Rock:EmailBox>

                        <div class="actions">
                            <Rock:BootstrapButton ID="btnRegister" OnClick="btnRegister_Click" runat="server" Text="Add"
                                CssClass="btn btn-primary" ValidationGroup="AddMember" CausesValidation="true" />
                        </div>
                    </Content>
                </Rock:ModalDialog>
            </ContentTemplate>
        </Rock:RockUpdatePanel>
    </ContentTemplate>
</Rock:RockUpdatePanel>
