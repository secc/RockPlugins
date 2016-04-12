<%@ Control Language="C#" AutoEventWireup="true" CodeFile="PDFLavaExample.ascx.cs" Inherits="RockWeb.Plugins.org_secc.PDFExamples.PDFLavaExample" %>

<Rock:RockUpdatePanel ID="upMain" runat="server">
    <ContentTemplate>
        <h1>Write some lava!</h1>
        <Rock:CodeEditor ID="ceLava" EditorMode="Lava" runat="server"></Rock:CodeEditor>
        <h1>Mergefields!</h1>
        <Rock:KeyValueList ID="kvlMerge" runat="server" />
        <h1>Options!</h1>
        <Rock:RockCheckBox ID="cbCurrentPerson" runat="server" Label="Include Current Person"/>
        <Rock:RockCheckBox ID="cbGlobal" runat="server" Label="Include Global Attributes"/>
        <Rock:BootstrapButton ID="bntMerge" runat="server" Text="Merge" OnClick="bntMerge_Click" CssClass="btn btn-primary"></Rock:BootstrapButton>
    </ContentTemplate>
</Rock:RockUpdatePanel>