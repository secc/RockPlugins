<%@ Control Language="C#" AutoEventWireup="true" CodeFile="PDFFormExample.ascx.cs" Inherits="RockWeb.Plugins.org_secc.PDFExamples.PDFFormExample" %>

<Rock:RockUpdatePanel ID="upMain" runat="server">
    <ContentTemplate>
        <h1>Add New PDF File</h1>
        <Rock:FileUploader runat="server" Id="fsFile" OnFileUploaded="fsPDF_FileUploaded" />
        
        <h1>Select Merge Document</h1>
        <Rock:BinaryFilePicker ID="fpSelectedFile" runat="server"></Rock:BinaryFilePicker>
        <Rock:BootstrapButton ID="bntDelete" runat="server" OnClick="bntDelete_Click" Text="DELETE!" CssClass="btn btn-danger"></Rock:BootstrapButton>
        
        <h1>Mergefields!</h1>
        <Rock:KeyValueList ID="kvlMerge" runat="server" />
        <Rock:BootstrapButton ID="bntMerge" runat="server" Text="Merge" OnClick="bntMerge_Click" CssClass="btn btn-primary"></Rock:BootstrapButton>
    </ContentTemplate>
</Rock:RockUpdatePanel>