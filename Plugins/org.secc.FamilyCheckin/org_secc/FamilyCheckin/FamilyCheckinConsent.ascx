<%@ Control Language="C#" AutoEventWireup="true" CodeFile="FamilyCheckinConsent.ascx.cs" Inherits="RockWeb.Plugins.org_secc.FamilyCheckin.FamilyCheckinConsent" %>
<style>
    #pnlCheckinPrompt
    {
        background-color: #fff;
        min-height:450px;
        margin:auto;
        padding:15px;
    }
    #pnlCheckinPrompt p
    {
        color: #000;
        /*font-size: 1em;*/
    }

    #pnlCheckinPrompt .actions {
        padding-top:15px;
        padding-bottom:15px;
    }

    #pnlCheckinPrompt .btn-danger-solid {
        background-color: #d4442e !important ;
    }
    #pnlCheckinPrompt .btn-success {
        background-color:#16c98d !important;

    }

    #pnlCheckinPrompt .btn-skip {
        background-color: #1D1F21 !important;
    }
    #pnlCheckinPrompt .btn {
        width:150px !important;
        color: #ffffff !important;
        font-weight:bolder !important;
    }
</style>
<asp:UpdatePanel ID="upMain" runat="server" >
    <ContentTemplate>
        <div class="checkin-header">
<%--            <h1>Medical Consent</h1>--%>
        </div>
        <div class="checkin-body">
            <div class="checkin-scroll-panel">
                <div class="scroller">
                    <asp:Panel ID="pnlCheckinPrompt" ClientIDMode="Static" runat="server" >
                        <asp:Literal ID="lHouseholdInfo" runat="server" />

                        <asp:Panel ID="pnlVerifyInfo" runat="server" CssClass="row" Visible="false">
                            <div class="col-sm-12">
                                <div class="actions">
                                    <Rock:BootstrapButton ID="btnInfoYes" runat="server" CssClass="btn btn-lg btn-success" Text="Yes"  />
                                    <span class="pull-right">
                                        <Rock:BootstrapButton ID="btnInfoNo" runat="server" CssClass="btn btn-lg btn-danger-solid" Text="No" />
                                    </span>
                                </div>
                            </div>
                        </asp:Panel>

                        <asp:Panel ID="pnlMedicalConsent" runat="server"  Visible="false" Style="padding-top:25px;" >
                            <asp:Literal ID="lConsentMessage" runat="server" />
                            <div class="row">
                                <div class="col-sm-12">
                                    <Rock:BootstrapButton ID="btnMedicalIConsentYes" runat="server" CssClass="btn btn-lg btn-success" Text="I Consent" />
                                    <span class="pull-right">
                                        <Rock:BootstrapButton ID="btnMedicalConsentSkip" runat="server" CssClass="btn btn-lg btn-skip" Text="Skip" />
                                    </span>
                                </div>
                            </div>
                        </asp:Panel>
                    </asp:Panel>
                </div>
            </div>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>