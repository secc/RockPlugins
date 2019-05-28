<%@ Control Language="C#" AutoEventWireup="true" CodeFile="SignatureAudit.ascx.cs" Inherits="RockWeb.Blocks.Reporting.NextGen.SignatureAudit" %>
<asp:UpdatePanel ID="upReport" runat="server">
    <ContentTemplate>
        <Rock:ModalDialog runat="server" ID="mdSettings" SaveButtonText="Save" OnSaveClick="mdSettings_SaveClick" Title="Settings">
            <Content>
                <Rock:DatePicker runat="server" ID="dpEndDate" Label="Last Date Of Event" Required="true" />
                <Rock:GroupPicker runat="server" ID="gpGroup" Label="Group To Audit" Required="true" />
                <Rock:PanelWidget runat="server" Title="Minor Signature Documents">
                    <Rock:RockCheckBoxList runat="server" ID="cblMinorDocuments" Label="Required Documents" DataTextField="Name" DataValueField="Id" Required="true" />
                </Rock:PanelWidget>
                <Rock:PanelWidget runat="server" Title="Adult Signature Documents">
                    <Rock:RockCheckBoxList runat="server" ID="cblAdultDocuments" Label="Required Documents" DataTextField="Name" DataValueField="Id" Required="true" />
                </Rock:PanelWidget>
            </Content>
        </Rock:ModalDialog>
        <asp:Literal ID="ltSettings" runat="server" />
        <Rock:BootstrapButton runat="server" ID="btnSettings" CssClass="btn btn-primary" Text="Settings" OnClick="btnSettings_Click" />
        <Rock:Grid runat="server" ID="gData" AllowSorting="true" OnRowDataBound="gData_RowDataBound" OnRowSelected="gData_RowSelected"
            DataKeyNames="Id" PersonIdField="Id">
            <Columns>
                <Rock:SelectField />
                <Rock:RockBoundField DataField="Person.FullName" HeaderText="Person" SortExpression="Person.FullName" />
                <Rock:RockBoundField DataField="Person.Age" HeaderText="Age" SortExpression="Person.Age" />
                <Rock:BoolField DataField="HasValidDocument" HeaderText="Has Valid Document" SortExpression="HasValidDocument" />
                <Rock:RockBoundField DataField="WarningText" HeaderText="Warning" SortExpression="WarningText" />
            </Columns>
        </Rock:Grid>
    </ContentTemplate>
</asp:UpdatePanel>
