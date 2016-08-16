<%@ Control Language="C#" AutoEventWireup="true" CodeFile="PinAdderModal.ascx.cs" Inherits="RockWeb.Plugins.org_secc.SportsAndFitness.PinAdderModal" %>

<asp:UpdatePanel ID="upContent" runat="server">
    <ContentTemplate>
        <Rock:BootstrapButton runat="server" ID="btnRequest" CssClass="btn btn-default"
             OnClick="btnRequest_Click" Text="<i class='fa fa-barcode'></i> Add PIN to Person">
        </Rock:BootstrapButton>
        <Rock:ModalAlert runat="server" ID="maInfo"></Rock:ModalAlert>
        <Rock:ModalDialog runat="server" ID="mdPin" SaveButtonText="Save" CancelLinkVisible="false" ValidationGroup="main"
             OnSaveClick="mdPin_SaveClick" Title="Add PIN or Barcode to Person">
            <Content>
                <asp:Panel runat="server" ID="pnlErrors" Visible="false" CssClass="alert alert-danger">
                    <asp:ValidationSummary runat="server" ValidationGroup="main" />
                    <asp:Literal Text="" ID="ltErrors" runat="server" />
                </asp:Panel>
                <div class="row">
                    <div class="col-sm-6">
                <Rock:PersonPicker runat="server" Required="true" Label="Person" ID="ppPerson" ValidationGroup="main" />
                    </div>
                    <div class="col-sm-6">
                        <Rock:RockTextBox runat="server" ID="tbPin" Required="true" Label="PIN or Barcode" ValidationGroup="main"></Rock:RockTextBox>
                    </div>
                </div>
            </Content>
        </Rock:ModalDialog>
    </ContentTemplate>
</asp:UpdatePanel>
