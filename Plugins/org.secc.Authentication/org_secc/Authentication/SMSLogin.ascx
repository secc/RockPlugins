<%@ Control Language="C#" AutoEventWireup="true" CodeFile="SMSLogin.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Authentication.SMSLogin" %>
<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <div class="col-md-12">

            <legend>SMS Login</legend>
            <asp:Panel ID="pnlPhoneNumber" runat="server">
                <asp:ValidationSummary ID="valSummary" runat="server" ValidationGroup="PhoneNumber" HeaderText="Please Correct the Following" CssClass="alert alert-danger" />
                <p>
                    <asp:Label Text="" ID="lbPrompt" runat="server" />
                </p>

                <Rock:PhoneNumberBox ID="tbPhoneNumber" runat="server" Label="Mobile Phone Number"
                    Required="true" DisplayRequiredIndicator="false" ValidationGroup="PhoneNumber" />
                <asp:RegularExpressionValidator ID="validateEmail" runat="server" ErrorMessage="Please enter a valid phone number." ControlToValidate="tbPhoneNumber"
                    ValidationExpression="^(\+\d{1,2}\s)?\(?\d{3}\)?[\s.-]\d{3}[\s.-]\d{4}$" ValidationGroup="PhoneNumber" Display="None" />

                <asp:Button ID="btnGenerate" runat="server" Text="Generate Code" CssClass="btn btn-primary" OnClick="btnGenerate_Click" ValidationGroup="PhoneNumber" CausesValidation="true" />
            </asp:Panel>

            <asp:Panel runat="server" ID="pnlCode" Visible="false">
                <Rock:NotificationBox ID="nbError" Visible="false" NotificationBoxType="Danger" Text="Sorry, the code you entered did not match the code we generated." runat="server" />
                <p>We have sent you a code please enter it here to login.</p>
                <Rock:RockTextBox runat="server" Label="Code" ID="tbCode" Required="true" DisplayRequiredIndicator="false" ValidationGroup="Code" />
                <asp:Button runat="server" ID="btnLogin" Text="Login" CssClass="btn btn-primary" OnClick="btnLogin_Click" ValidationGroup="Code" CausesValidation="true" />
            </asp:Panel>

            <asp:Panel runat="server" ID="pnlError" Visible="false">
                <h1>There Was An Error</h1>
            </asp:Panel>

        </div>
    </ContentTemplate>
</asp:UpdatePanel>


