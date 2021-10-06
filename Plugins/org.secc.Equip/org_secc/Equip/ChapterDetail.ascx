<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ChapterDetail.ascx.cs" Inherits="RockWeb.Plugins.org.secc.Equip.ChapterDetail" %>

<asp:UpdatePanel ID="upnlContent" runat="server" CssClass="panel panel-block">
    <ContentTemplate>
        <div class="panel panel-block">

            <div class="panel-heading">
                <h1 class="panel-title">
                    <asp:Literal ID="ltTitle" runat="server" />
                </h1>
            </div>

            <Rock:PanelDrawer ID="pdAuditDetails" runat="server"></Rock:PanelDrawer>
            <div class="panel-body">
                <asp:Panel runat="server" ID="pnlView">
                    <Rock:RockLiteral runat="server" ID="ltName" Label="Name" />
                    <Rock:RockLiteral runat="server" ID="ltDescription" Label="Description" />
                    <Rock:BootstrapButton runat="server" ID="btnEdit" Text="Edit" CssClass="btn btn-primary" OnClick="btnEdit_Click" />
                    <asp:LinkButton runat="server" ID="btnDelete" Text="Delete" CssClass="btn" OnClick="btnDelete_Click" />
                </asp:Panel>

                <asp:Panel ID="pnlEdit" runat="server">
                    <Rock:RockTextBox runat="server" Label="Chapter Name" ID="tbName" Required="true" />
                    <Rock:RockTextBox runat="server" Label="Description" ID="tbDescription" TextMode="MultiLine" Height="200" />
                    <Rock:BootstrapButton runat="server" ID="btnSave" Text="Save" CssClass="btn btn-primary" OnClick="btnSave_Click" />
                    <asp:LinkButton Text="Cancel" runat="server" ID="btnCancel" OnClick="btnCancel_Click" CssClass="btn" CausesValidation="false" />
                </asp:Panel>
            </div>
        </div>

    </ContentTemplate>
</asp:UpdatePanel>
