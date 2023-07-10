<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ManageCommunicationLists.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Communication.ManageCommunicationLists" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <asp:Panel runat="server" ID="pnlKeyword">
            <h2>Subscribe to               
                <asp:Literal runat="server" ID="ltGroupName" /></h2>
            <div>
                <small>
                    <asp:Literal runat="server" ID="ltType" />
                </small>
            </div>
            <asp:Literal runat="server" ID="ltDescription"/>
            <Rock:NotificationBox runat="server" ID="nbAlreadySubscribed" NotificationBoxType="Success" />
            <asp:Literal runat="server" ID="ltAttributesHeader"/>
            <asp:PlaceHolder runat="server" ID="phGroupAttributes" />
            <Rock:BootstrapButton runat="server" ID="btnSubscribe" Text="Subscribe" CssClass="btn btn-primary" OnClick="btnSubscribe_Click" />
            <hr />
        </asp:Panel>
        <Rock:NotificationBox runat="server" id="nbNotice" Visible="false" />
        <Rock:NotificationBox runat="server" ID="nbSuccess" NotificationBoxType="Success" />
        <Rock:DynamicPlaceholder runat="server" ID="phGroups" />

    </ContentTemplate>
</asp:UpdatePanel>
