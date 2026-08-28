<%@ Control Language="C#" AutoEventWireup="true" CodeFile="RequestEventPass.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Event.RequestEventPass" %>
<asp:UpdatePanel ID="upMain" runat="server">
    <ContentTemplate>
        <div class="panel panel-block">
            <div class="panel-heading">
                <h1 class="panel-title">
                    <asp:Literal ID="lTitle" runat="server" /></h1>
            </div>
            <div class="panel-body">
                <Rock:NotificationBox ID="nbNotLoggedIn" runat="server" NotificationBoxType="Danger" Visible="false">
                    <strong><i class="fa fa-exclamation-triangle"></i> Not Logged In</strong>
                    <p>Please Login to request an Event Pass.</p>
                </Rock:NotificationBox>
                <Rock:NotificationBox ID="nbMessages" runat="server" Visible="false" />
                <asp:Panel ID="pnlRequest" runat="server">

                    <fieldset>
                        <div class="form-row" style="padding-bottom: 15px;">
                            <div class="col-xs-12">
                                <label class="control-label">Name</label>
                                <span class="label-text">
                                    <asp:Literal ID="lName" runat="server" /></span>
                            </div>
                        </div>
                        <div class="form-row">
                            <div class="col-sm-6">
                                <Rock:EmailBox ID="tbEmail" runat="server" Label="Email" ReadOnly="true" />
                            </div>
                        </div>
                        <div class="form-row">
                            <div class="col-sm-6">
                                <Rock:PhoneNumberBox ID="tbPhone" runat="server" Label="Mobile Phone" ReadOnly="true" />
                                <div class="small" style="color:#595959;margin:4px 0 12px 0;line-height:1.5;">
                                    <%= System.Web.HttpUtility.HtmlEncode( Rock.Web.Cache.GlobalAttributesCache.Value( "OrganizationName" ) ) %>
                                    text messages. Message frequency varies.
                                    Message &amp; data rates may apply. Reply STOP to opt out, HELP for help.
                                    <a href="https://se.church/privacy-policy" target="_blank" rel="noopener noreferrer" title="Opens in a new tab">Privacy Policy</a>
                                    &middot;
                                    <a href="https://se.church/terms" target="_blank" rel="noopener noreferrer" title="Opens in a new tab">Mobile Terms</a>
                                </div>
                            </div>
                        </div>
                        <div class="form-row">
                            <div class="col-sm-6">
                                <Rock:RockRadioButtonList ID="cblDeliveryMethod" runat="server" Label="Delivery Method" RepeatDirection="Horizontal" Required="true"
                                    RequiredErrorMessage="Email or Text Message selection required." ValidationGroup="valRequestPass">
                                    <asp:ListItem Text="Email" Value="Email" />
                                    <asp:ListItem Text="Text Message" Value="SMS" />
                                </Rock:RockRadioButtonList>
                            </div>
                        </div>
                    </fieldset>
                    <div class="actions">
                        <asp:LinkButton ID="lbApplePass" runat="server" ValidationGroup="valRequestPass" OnClick="lbApplePass_Click">
                            <img src="/Content/Apps/EventPass/AddToAppleWallet.svg" alt="Add to Apple Wallet" style="width:200px;" />
                        </asp:LinkButton>
                    </div>

                </asp:Panel>
            </div>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>
