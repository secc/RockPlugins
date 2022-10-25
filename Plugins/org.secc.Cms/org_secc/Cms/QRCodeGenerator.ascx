<%@ Control Language="C#" AutoEventWireup="true" CodeFile="QRCodeGenerator.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Cms.QRCodeGenerator" %>

<asp:UpdatePanel ID="upnlQRCodeGenerator" runat="server">
    <ContentTemplate>
        <Rock:NotificationBox ID="nbError" runat="server" NotificationBoxType="Danger" Visible="false" />
        <div>
            <Rock:UrlLinkBox class="col-md-4" runat="server" ID="ulbUrl" Required="true" RequiredErrorMessage="A URL is Required" Help="Creates a shortlink and then displays a QR code for that shortlink" Label="URL" />
            <Rock:BootstrapButton runat="server" ID="btnCreate" OnClick="btnCreate_Click" Text="Create" CssClass="btn btn-primary" />
        
            <div id="QrCodeDiv">
                <canvas id="qrCodeCanvas"></canvas>
            </div>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>
