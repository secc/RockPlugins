<%@ Control Language="C#" AutoEventWireup="true" CodeFile="InteractionsDebug.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Cms.Interactions" %>

<asp:UpdatePanel ID="upContent" runat="server">
    <ContentTemplate>
        <div class="panel-body text-center">
            <asp:LinkButton ID="btnInteractionsModal" runat="server"  OnClick="btnInteractionsDebug_Click" CausesValidation="false" CssClass="btn btn-primary">
                Interactions Debug
            </asp:LinkButton>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>