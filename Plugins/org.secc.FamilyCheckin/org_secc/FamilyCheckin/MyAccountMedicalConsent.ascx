<%@ Control Language="C#" AutoEventWireup="true" CodeFile="MyAccountMedicalConsent.ascx.cs"
    Inherits="Plugins_org_secc_FamilyCheckin_MyAccountMedicalConsent" %>

    <asp:UpdatePanel ID="upnlContent" runat="server">
        <ContentTemplate>
            <Rock:NotificationBox ID="nbNoMinors" runat="server" Text="There are no minors in your family."
                Visible="false" CssClass="warning" />
            <Rock:NotificationBox ID="nbAllMinorsHaveConsent" runat="server"
                Text="All minors in your family already have medical consent." Visible="false" CssClass="success" />

            <asp:Panel ID="pnlConsent" runat="server" Visible="false" CssClass="alert alert-danger g-margin-x-20--xs">
                <div class="">
                    <h3 class="g-font-family--primary g-margin-b-5--xs g-font-weight--500">Household Information</h3>
                    <p>Please verify your household information and help us update our ministry consent</p>
                    <div class="row">
                        <div class="col-sm-6">
                            <h4 ID="h4FamilyName" runat="server" class="g-font-weight--500 g-margin-b-5--xs"></h4>
                            <p ID="sCurrentPersonAddress" runat="server"></p>
                            <div class="row">
                                <div class="col-sm-6">
                                    <p>
                                        <span ID="sCurrentPersonFullName" runat="server"></span><br />
                                        <span ID="sCurrentPersonCellNumber" runat="server"></span>
                                    </p>
                                    <a ID="aUpdateCurrentPerson" class="btn btn-primary btn-xs" runat="server">Update Info</a>
                                </div>
                                <asp:Panel runat="server" ID="pnlOtherAdults" Visible="false">
                                </asp:Panel>                                
                            </div>
                        </div>
                        <div class="col-sm-6">
                            <h4 class="g-font-family--primary g-margin-b-5--xs g-font-weight--500">Minor Children</h4>
                            <asp:Panel runat="server" ID="pnlChildren"></asp:Panel>
                        </div>
                    </div>
                    <div class="g-margin-t-10--xs row">

                        <div class="col-sm-10">
                            <p>By clicking, I acknowledge that as a parent/guardian, I authorize first aid and/or medical treatment for my child, and I release Southeast Christian Church, its employees, and its volunteers from any and all responsibility, including negligence.</p>
                        </div>
                        <div class="col-sm-1">
                            <Rock:BootstrapButton runat="server" ID="btnConsent" Text="I consent"
                                CssClass="btn btn-primary" OnClick="btnConsent_Click" />
                        </div>

                    </div>
                </div>
            </asp:Panel>
            <asp:Panel ID="pnlSuccess" runat="server" Visible="false" CssClass="alert alert-success g-margin-x-20--xs">
                <p ID="pSuccess" runat="server">Thank you for your assistance. Medical consent has been recorded.</p>
            </asp:Panel>
        </ContentTemplate>
    </asp:UpdatePanel>