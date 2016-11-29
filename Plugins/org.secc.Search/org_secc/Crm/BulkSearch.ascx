<%@ Control Language="C#" AutoEventWireup="true" CodeFile="BulkSearch.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Search.BulkSearch" %>


<asp:UpdatePanel ID="upnlDevice" runat="server">
    <ContentTemplate>
        <Rock:RockTextBox runat="server" ID="tbNumbers" TextMode="MultiLine" Rows="10"></Rock:RockTextBox>
        <Rock:BootstrapButton runat="server" ID="btnNumbers" Text="Load"  CssClass="btn btn-primary" OnClick="btnNumbers_Click"></Rock:BootstrapButton>
        <Rock:Grid runat="server" ID="gGrid">
            <Columns>
                <Rock:SelectField></Rock:SelectField>
               
                <Rock:RockBoundField DataField="FullName" HeaderText="Name"></Rock:RockBoundField>
            </Columns>
        </Rock:Grid>
    </ContentTemplate>
</asp:UpdatePanel>
