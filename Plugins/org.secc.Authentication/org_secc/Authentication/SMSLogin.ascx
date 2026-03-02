<%@ Control Language="C#" AutoEventWireup="true" CodeFile="SMSLogin.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Authentication.SMSLogin" %>
<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <div class="col-md-12">

            <legend>Mobile Login</legend>
            <asp:Panel ID="pnlPhoneNumber" runat="server" DefaultButton="btnGenerate">
                <asp:ValidationSummary ID="valSummary" runat="server" ValidationGroup="PhoneNumber" HeaderText="Please Correct the Following" CssClass="alert alert-danger" />
                <p>
                    <asp:Label Text="" ID="lbPrompt" runat="server" />
                </p>

                <Rock:PhoneNumberBox ID="tbPhoneNumber" runat="server" Label="Mobile Phone Number"
                    Required="true" DisplayRequiredIndicator="false" ValidationGroup="PhoneNumber" />
                <asp:RegularExpressionValidator ID="validateEmail" runat="server" ErrorMessage="Please enter a valid phone number." ControlToValidate="tbPhoneNumber"
                    ValidationExpression="^(\+\d{1,2}\s)?\(?\d{3}\)?[\s.-]\d{3}[\s.-]\d{4}$" ValidationGroup="PhoneNumber" Display="None" />

                <Rock:BootstrapButton ID="btnGenerate" runat="server" Text="Generate Code" CssClass="btn btn-primary" OnClick="btnGenerate_Click" ValidationGroup="PhoneNumber" CausesValidation="true" />
            </asp:Panel>

            <asp:Panel runat="server" ID="pnlCode" Visible="false" DefaultButton="btnLogin">
                <p>We have sent you a code please enter it here to login.</p>
                <Rock:NotificationBox ID="nbError" Visible="false" NotificationBoxType="Danger" runat="server" />
                <Rock:RockTextBox runat="server" Label="Code" ID="tbCode" Required="true" DisplayRequiredIndicator="false" ValidationGroup="Code" />
                <Rock:BootstrapButton runat="server" ID="btnLogin" Text="Login" CssClass="btn btn-primary" OnClick="btnLogin_Click" ValidationGroup="Code" CausesValidation="true" />
            </asp:Panel>

            <asp:Panel runat="server" ID="pnlResolve" Visible="false">
                <p>
                    <asp:Label runat="server" ID="lbResolve" />
                </p>
                <Rock:BootstrapButton runat="server" ID="btnResolve" Visible="false" CssClass="btn btn-primary" Text="Update Mobile Phone Number" OnClick="btnResolve_Click" />
                <Rock:BootstrapButton runat="server" ID="btnCancel" CssClass="btn btn-default" Text="Cancel" OnClick="btnCancel_Click" />
            </asp:Panel>
        </div>

        <script type="text/javascript">
            function smsLoginSetup() {
                // Phone number panel: handle Enter key to submit
                var phonePanel = document.getElementById('<%= pnlPhoneNumber.ClientID %>');
                var generateBtn = document.getElementById('<%= btnGenerate.ClientID %>');
                if (phonePanel && generateBtn) {
                    phonePanel.addEventListener('keydown', function (e) {
                        if (e.key === 'Enter' || e.keyCode === 13) {
                            e.preventDefault();
                            generateBtn.click();
                        }
                    });
                }

                // Code panel: auto-submit when 6 digits are entered
                var codeInput = document.getElementById('<%= tbCode.ClientID %>');
                var loginBtn = document.getElementById('<%= btnLogin.ClientID %>');
                if (codeInput && loginBtn) {
                    codeInput.addEventListener('input', function () {
                        codeInput.value = codeInput.value.replace(/\D/g, '');
                        if (codeInput.value.length >= 6) {
                            setTimeout(function () {
                                loginBtn.click();
                            }, 50);
                        }
                    });
                }
            }
            // Run on initial load and after each async postback (UpdatePanel)
            smsLoginSetup();
            var prm = Sys.WebForms.PageRequestManager.getInstance();
            prm.add_endRequest(smsLoginSetup);
        </script>
    </ContentTemplate>
</asp:UpdatePanel>