<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GenerateApplePass.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Crm.GenerateApplePass" %>
<asp:UpdatePanel ID="upMain" runat="server">
    <ContentTemplate>
        <div class="row">
            <div class="col-xs-12">
                <asp:Panel ID="pnlValidation" runat="server" CssClass="alert alert-validation" Visible="false">
                    <h3>Please Verify Block Attributes</h3>
                    <ul>
                        <asp:Literal ID="lErrors" runat="server" />
                    </ul>
                </asp:Panel>
            </div>
        </div>

        <asp:HiddenField ID="hfPassInfo" runat="server" ClientIDMode="Static" />
        <asp:Button ID="lbGenerateApplePass" runat="server" Text="Generate" ClientIDMode="Static" style="display:none;visibility:hidden" OnClick="lbGeneratePass_Click" />
    </ContentTemplate>
</asp:UpdatePanel>
