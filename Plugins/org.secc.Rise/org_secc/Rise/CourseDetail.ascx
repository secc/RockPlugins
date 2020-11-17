<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CourseDetail.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Rise.CourseDetail" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <Rock:ModalDialog runat="server" ID="mdEnrollGroup" Title="Enroll Group"
            OnSaveClick="mdEnrollGroup_SaveClick" SaveButtonText="Enroll">
            <Content>
                <Rock:GroupPicker runat="server" ID="pGroup" Label="Group" />
            </Content>
        </Rock:ModalDialog>

        <asp:Panel ID="pnlView" runat="server" CssClass="panel panel-block">
            <div class="panel-heading">
                <h1 class="panel-title">
                    <i class="fa fa-chalkboard"></i>
                    <asp:Literal ID="ltName" runat="server" />
                </h1>
            </div>
            <Rock:PanelDrawer ID="pdAuditDetails" runat="server" />
            <div class="panel-body">
                <Rock:RockLiteral runat="server" ID="ltUrl" Label="Url" />
                <asp:PlaceHolder runat="server" ID="phAttributes" />

                <Rock:BootstrapButton runat="server" ID="btnSave" CssClass="btn btn-primary" Text="Save" OnClick="btnSave_Click" />
                <asp:LinkButton Text="Cancel" runat="server" ID="btnCancel" OnClick="btnCancel_Click" CausesValidation="false" CssClass="btn btn-link" />
        </asp:Panel>
        </div>

        <h3>Enrolled Groups</h3>
        <Rock:Grid runat="server" ID="gGroups" DataKeyNames="Id">
            <Columns>
                <Rock:RockBoundField HeaderText="Group" DataField="Name" />
                <Rock:DeleteField ID="btnGroupDelete" OnClick="btnGroupDelete_Click" />
            </Columns>
        </Rock:Grid>


    </ContentTemplate>
</asp:UpdatePanel>
