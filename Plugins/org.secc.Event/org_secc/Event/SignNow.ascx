<%@ Control Language="C#" AutoEventWireup="true" CodeFile="SignNow.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Event.SignNow" ViewStateMode="Enabled" EnableViewState="true" %>

<Rock:NotificationBox ID="nbDigitalSignature" runat="server" NotificationBoxType="Info"></Rock:NotificationBox>
<asp:HiddenField ID="hfRequiredDocumentLinkUrl" runat="server" />
<asp:HiddenField ID="hfRegistrantKey" runat="server" />

<Rock:BootstrapButton ID="btnRequiredDocument" runat="server" CssClass="btn btn-default pull-right" Visible="false" Text="Sign Document" OnClick="btnRequiredDocument_Click"></Rock:BootstrapButton>
 