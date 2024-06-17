<%@ Control Language="C#" AutoEventWireup="true" CodeFile="UpdatePersonImage.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Imaging.UpdatePersonImage" %>

<asp:UpdatePanel runat="server">
    <ContentTemplate>
        <Rock:RegistrationInstancePicker runat="server" ID="btnLoad" OnClick="btnLoad_SQL" Label="Registration Instance" Help="Choose a registration instance to sync photos"/>
        <Rock:CodeEditor runat="server" EditorMode="Sql" ID="ceQuery"
            Help="Return colums of [PersonId] and [BinaryFileId]" Label="SQL Query" />
        <Rock:BootstrapButton runat="server" ID="btnRun" OnClick="btnRun_Click" Text="Run" CssClass="btn btn-primary" />
    </ContentTemplate>
</asp:UpdatePanel>
