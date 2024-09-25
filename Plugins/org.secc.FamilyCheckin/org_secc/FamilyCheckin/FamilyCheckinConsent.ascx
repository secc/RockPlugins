<%@ Control Language="C#" AutoEventWireup="true" CodeFile="FamilyCheckinConsent.ascx.cs" Inherits="RockWeb.Plugins.org_secc.FamilyCheckin.FamilyCheckinConsent" %>
<style>
    #divVerifyInfo
    {
        background-color: #545454;
        opacity:0.4;
    }
    #divVerifyInfo p
    {
        color: #fff;
        font-size: 1em;
    }
</style>
<asp:UpdatePanel ID="upMain" runat="server" >
    <ContentTemplate>
        <div class="checkin-header">
            <h1>Medical Consent</h1>
        </div>
        <div class="checkin-body">
            <div class="checkin-scroll-panel">
                <div class="scroller">
                    <asp:Panel ID="pnlVerifyInfo" runat="server" Visible="false">
                        <asp:Literal ID="lVerifyInfoHtml" runat="server" />
                    </asp:Panel>
                    <asp:Panel ID="pnlMedicalConsent" runat="server" Visible="false">
                        <asp:Literal ID="lMedConsentHTML" runat="server" />
                    </asp:Panel>
                </div>
            </div>
        </div>
        <div class="checkin-footer">
            <div class="checkin-actions">

                <asp:LinkButton ID="lbCancel" runat="server" CssClass="btn btn-default" Text="Cancel" />
            </div>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>