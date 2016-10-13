<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ActivitiesCheckin.ascx.cs" Inherits="RockWeb.Plugins.org_secc.SportsAndFitness.ActivitiesCheckin" %>

<asp:UpdatePanel ID="upContent" runat="server">
    <ContentTemplate>
        <Rock:ModalAlert ID="maError" runat="server" />
        <asp:Panel runat="server" ID="pnlMain">
            <div class="row">
                <asp:PlaceHolder runat="server" ID="phMembers"></asp:PlaceHolder>
            </div>
            <Rock:BootstrapButton ID="btnCancel" runat="server" Text="Cancel" CssClass="btn btn-lg btn-danger" OnClick="btnCancel_Click"></Rock:BootstrapButton>
            <Rock:BootstrapButton ID="btnCheckin" runat="server" Text="Check-In" CssClass="btn btn-lg btn-success" OnClick="btnCheckin_Click"></Rock:BootstrapButton>
        </asp:Panel>
        <asp:Panel runat="server" ID="pnlNoEligibleMembers" Visible="false">
            <Rock:NotificationBox ID="nbNoEligibleMembers" CssClass="noMembers" Text="There are no eligible people for check-in today." runat="server"
                 NotificationBoxType="Warning"></Rock:NotificationBox>
            <Rock:BootstrapButton ID="btnBack" runat="server" Text="Back" OnClick="btnBack_Click" CssClass="btn btn-lg btn-primary"></Rock:BootstrapButton>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
