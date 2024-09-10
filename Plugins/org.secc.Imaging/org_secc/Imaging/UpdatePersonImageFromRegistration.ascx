<%@ Control Language="C#" AutoEventWireup="true" CodeFile="UpdatePersonImageFromRegistration.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Imaging.UpdatePersonImage" %>

<asp:UpdatePanel runat="server">
    <ContentTemplate>
        <Rock:RegistrationTemplatePicker runat="server" ID="rtpTemplatePicker" AllowMultiSelect="true" Label="Registration Template(s)" Help="Choose registration templates to sync photos" />
        <Rock:RockCheckBox runat="server" ID="cbOverwritePhotos" Label="Overwrite photos?" Help="If checked, children and non-staff adult profiles will be updated even if they have an existing photo" />
        <Rock:BootstrapButton runat="server" ID="btnRun" OnClick="btnRun_Click" Text="Run" CssClass="btn btn-primary" />
    </ContentTemplate>
</asp:UpdatePanel>
