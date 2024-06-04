<%@ Control Language="C#" AutoEventWireup="true" CodeFile="UpdatePersonImage.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Imaging.UpdatePersonImage" %>

<asp:UpdatePanel runat="server">
    <ContentTemplate>
        <Rock:RegistrationTemplatePicker runat="server" ID="rtpTemplatePicker" AllowMultiSelect="true" Label="Registration Template(s)" Help="Choose registration templates to sync photos" />
        <Rock:RockCheckBox runat="server" ID="cbOverwritePhotos" Label="Overwrite current photos on child profiles & update missing photos for adult profiles (exlcuding staff)" Help="Selecting 'No' will ensure that only children & adults with a missing profile photo are updated" />
        <Rock:BootstrapButton runat="server" ID="btnRun" OnClick="btnRun_Click" Text="Run" CssClass="btn btn-primary" />
    </ContentTemplate>
</asp:UpdatePanel>
