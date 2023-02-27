<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CMPublicProfileRemovePerson.ascx.cs" Inherits="RockWeb.Plugins.org_secc.ChangeManager.CMPublicProfileRemovePerson" %>
<asp:UpdatePanel ID="upContent" runat="server">
    <ContentTemplate>
        <div class="panel panel-block">
            <div class="panel-heading">
                <h1 class="panel-text"><i class="fa fa-user"></i>&nbsp;My Account</h1>
            </div>
            <div class="panel-body">
                <Rock:NotificationBox ID="nbNotAuthorized" runat="server" Text="You are not auuthorized to edit this account." NotificationBoxType="Danger" Visible="false" />
                <asp:HiddenField ID="hfPersonGuid" runat="server" />

                <asp:Panel ID="pnlEdit" runat="server">
                    <div class="row">
                        <div class="col-md-3">
                            <asp:Image ID="imgProfile" runat="server" CssClass="img-responsive img-circle" style="margin: 0 auto; max-width: 180px;" />
                        </div>
                        <div class="col-md-9 g-padding-b-80--xs g-padding-t-80--lg">
                            <h3 class="g-font-weight--700 g-padding-b-20--xs"><asp:Literal ID="lName" runat="server" /></h3>
                            <asp:Panel ID="pnlBirthDate" runat="server" CssClass="container-fluid g-padding-x-0--xs">
                                <div class="col-xs-6 g-padding-x-0--xs">
                                    <p class="g-margin-y-0--xs g-font-weight--600 text-uppercase">Age</p>
                                </div>
                                <div class="col-xs-6 g-padding-x-0--xs">
                                    <asp:Literal ID="lAge" runat="server" />
                                </div>
                            </asp:Panel>
                            <div class="container-fluid g-padding-x-0--xs">
                                <div class="col-xs-6 g-padding-x-0--xs">
                                    <p class="g-margin-y-0--xs g-font-weight--600 text-uppercase">Family Role</p>
                                </div>
                                <div class="col-xs-6 g-padding-x-0--xs">
                                    <asp:Literal ID="lFamilyRole" runat="server" />
                                </div>
                            </div>
                            <div class="container-fluid g-padding-x-0--xs">
                                <div class="col-xs-6 g-padding-x-0--xs">
                                    <p class="g-margin-y-0--xs g-font-weight--600 text-uppercase">Gender</p>
                                </div>
                                <div class="col-xs-6 g-padding-x-0--xs">
                                    <asp:Literal ID="lGender" runat="server" />
                                </div>
                            </div>
                            <asp:Panel ID="pnlEmail" runat="server" CssClass="container-fluid g-padding-x-0--xs">
                                <div class="col-xs-6 g-padding-x-0--xs">
                                    <p class="g-margin-y-0--xs g-font-weight--600 text-uppercase">Email</p>
                                </div>
                                <div class="col-xs-6 g-padding-x-0--xs">
                                    <asp:Literal ID="lEmail" runat="server" />
                                </div>
                            </asp:Panel>
                            <asp:Panel ID="pnlPhoneNumber" runat="server" CssClass="container-fluid g-padding-x-0--xs">
                                <div class="col-xs-6 g-padding-x-0--xs">
                                    <p class="g-margin-y-0--xs g-font-weight--600 text-uppercase">Phone</p>
                                </div>
                                <div class="col-xs-6 g-padding-x-0--xs">
                                    <asp:Literal ID="lPhoneNumber" runat="server" />
                                </div>
                            </asp:Panel>
                           <div class="container-fluid g-padding-x-0--xs">
                               <div class="col-xs-12 g-padding-x-0--xs">
                                   <p class="g-margin-y-10--xs g-font-weight--600 text-uppercase">Additional Information</p>
                               </div>
                               <div class="col-xs-12 g-padding-x-0--xs">
                                   <span class="g-margin-y-0--xs">
                                       <Rock:RockTextBox ID="tbAdditionalInfo" runat="server" TextMode="MultiLine" Rows="4" />
                                   </span>
                               </div>
                           </div>
                            <div class="container-fluid g-padding-x-0--xs">
                                <div class="col-xs-12 g-padding-x-0--xs g-margin-y-10--xs">
                                    <asp:LinkButton ID="lbRemoveMember" runat="server" CssClass="btn btn-primary btn-xs" Text="Remove" />
                                
                                    <a href="/MyAccount" class="btn btn-default btn-xs">Cancel</a>
                                </div>
                            </div>
                        </div>
                    </div>
                </asp:Panel>

            </div>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>
