<%@ Control Language="C#" AutoEventWireup="true" CodeFile="WidgityTypeList.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Widgities.WidgityTypeList" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <Rock:ModalDialog runat="server" ID="mdImport" Title="Import Widgity Type" OnSaveClick="mdImport_SaveClick" SaveButtonText="Import">
            <Content>
                <Rock:FileUploader runat="server" ID="fImport" Label="File" />
                <Rock:EntityTypePicker runat="server" ID="ddlEntityType" Label="EntityType" />
            </Content>
        </Rock:ModalDialog>

        <asp:Panel ID="pnlView" runat="server" CssClass="panel panel-block">
            <div class="panel-heading">
                <h1 class="panel-title">
                    <i class="fa fa-scroll"></i>
                    Widgety Types
                </h1>
                <div class="panel-label">
                    <asp:LinkButton Text="<i class='fa fa-file-import'></i> Import" runat="server"
                        CssClass="btn btn-default btn-sm" ID="btnImport" OnClick="btnImport_Click" />
                </div>
            </div>
            <div class="panel-body">
                <Rock:NotificationBox runat="server" NotificationBoxType="Info" ID="nbImport" Visible="false" Dismissable="true" />
                <div class="grid grid-panel">
                    <Rock:Grid ID="gList" runat="server" DataKeyNames="Id" OnRowSelected="gList_RowSelected">
                        <Columns>
                            <Rock:RockBoundField DataField="Name" HeaderText="Name" />
                        </Columns>
                    </Rock:Grid>
                </div>
            </div>
        </asp:Panel>

    </ContentTemplate>

</asp:UpdatePanel>
