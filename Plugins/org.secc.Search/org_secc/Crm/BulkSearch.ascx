<%@ Control Language="C#" AutoEventWireup="true" CodeFile="BulkSearch.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Search.BulkSearch" %>


<asp:UpdatePanel ID="upnlDevice" runat="server">
    <ContentTemplate>
        <Rock:RockTextBox runat="server" ID="tbNumbers" TextMode="MultiLine" Rows="10"></Rock:RockTextBox>
        <Rock:BootstrapButton runat="server" ID="btnNumbers" Text="Load"  CssClass="btn btn-primary" OnClick="btnNumbers_Click"></Rock:BootstrapButton>
        <Rock:Grid runat="server" ID="gGrid">
            <Columns>
                <Rock:SelectField></Rock:SelectField>
                <Rock:PersonField DataField="PrimaryAlias.Person" HeaderText="Name"></Rock:PersonField>
                <Rock:RockBoundField DataField="Email" HeaderText="Email"></Rock:RockBoundField>
                <Rock:PhoneNumbersField DataField="PhoneNumbers" HeaderText="Phone Numbers"></Rock:PhoneNumbersField>
            </Columns>
        </Rock:Grid>
    </ContentTemplate>
</asp:UpdatePanel>
