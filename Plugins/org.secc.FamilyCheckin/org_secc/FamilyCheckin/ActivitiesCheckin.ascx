<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ActivitiesCheckin.ascx.cs" Inherits="RockWeb.Plugins.org_secc.FamilyCheckin.ActivitiesCheckin" %>

<asp:UpdatePanel ID="upContent" runat="server">
    <ContentTemplate>
        <asp:Panel runat="server" ID="pnlMain">
            <div>
                <asp:PlaceHolder runat="server" ID="phMembers"></asp:PlaceHolder>
            </div>
            <Rock:BootstrapButton ID="btnCancel" runat="server" Text="Cancel" CssClass="btn btn-danger" OnClick="btnCancel_Click"></Rock:BootstrapButton>
            <Rock:BootstrapButton ID="btnCheckin" runat="server" Text="Check-In" CssClass="btn btn-success" OnClick="btnCheckin_Click"></Rock:BootstrapButton>
        </asp:Panel>
        <asp:Panel runat="server" ID="pnlNoEligibleMembers" Visible="false">
            <Rock:NotificationBox ID="nbNoEligibleMembers" Text="There are no eligible people for check-in today." runat="server"
                 NotificationBoxType="Warning"></Rock:NotificationBox>
            <Rock:BootstrapButton ID="btnBack" runat="server" Text="Back" OnClick="btnBack_Click" CssClass="btn btn-primary"></Rock:BootstrapButton>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
