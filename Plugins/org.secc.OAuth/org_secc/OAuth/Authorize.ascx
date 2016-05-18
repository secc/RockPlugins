<%@ Control Language="C#" AutoEventWireup="true" CodeFile="Authorize.ascx.cs" Inherits="RockWeb.Plugins.org_secc.OAuth.Authorize" %>

    <asp:Panel ID="pnlAuthorize" runat="server">

        <fieldset>
            <legend>Authorize</legend>

            <div class="row">
                <div class="col-md-10 col-md-offset-1">
                    <h4>Authorization for <asp:Literal ID="lClientName" runat="server" /></h4>
                    <form method="POST">
                        <p><br />Hello <asp:Literal ID="lUsername" runat="server" />,</p>
                        <p><asp:Literal ID="lClientName2" runat="server" /> wants to access the following information on your behalf:</p>
                        <ul>
                            <asp:Repeater ID="rptScopes" runat="server">
                                <ItemTemplate>
                                    <li><%# Eval("Identifier") %> - <%# Eval("Description") %></li>
                                </ItemTemplate>
                            </asp:Repeater>
                        </ul>
                        <p>
                            <input type="submit" name="submit.Grant" class="btn-normal pull-right" value="Grant and Continue" />
                        </p>
                        <p style="margin-top: 150px">
                            If you this is not your account, please <asp:LinkButton ID="logout" runat="server">logout</asp:LinkButton> to sign in as different user.
                        </p>
                    </form>
                </div>
            </div>
        </fieldset>

        


    </asp:Panel>

    
    <asp:Panel ID="pnlLockedOut" runat="server" Visible="false">

        <div class="alert alert-danger">
            <asp:Literal ID="lLockedOutCaption" runat="server" />
        </div>

    </asp:Panel>

    <asp:Panel ID="pnlConfirmation" runat="server" Visible="false">

        <div class="alert alert-warning">
            <asp:Literal ID="lConfirmCaption" runat="server" />
        </div>

    </asp:Panel>
   

