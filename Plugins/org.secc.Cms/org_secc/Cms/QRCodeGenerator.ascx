<%@ Control Language="C#" AutoEventWireup="true" CodeFile="QRCodeGenerator.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Cms.QRCodeGenerator" %>

<asp:UpdatePanel runat="server">
    <ContentTemplate>
        <Rock:NotificationBox ID="nbError" runat="server" NotificationBoxType="Danger" Visible="false" />
        <div class="col-md-4">
            <Rock:UrlLinkBox class="col-md-4" runat="server" ID="ulbUrl" Required="true" RequiredErrorMessage="A URL is Required" Help="Creates a shortlink and then displays a QR code for that shortlink" Label="URL" />
            <Rock:BootstrapButton runat="server" ID="btnCreate" OnClick="btnCreate_Click" Text="Create" CssClass="btn btn-primary" />
        
        
            <div id="QrCodeDiv">
                <asp:Image runat="server" ID="QrCode" class="img-responsive" visible="false"/>
            </div>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>
