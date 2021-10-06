<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CoursePageList.ascx.cs" Inherits="RockWeb.Plugins.org.secc.Equip.CoursePageList" %>
<script>
    function EditPage(url) {
        Rock.controls.modal.show($(this), url);
        $modalPopupIFrame = Rock.controls.modal.getModalPopupIFrame();
    }
</script>
<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <div class="panel panel-block">

            <div class="panel-heading">
                <h1 class="panel-title pull-left">
                    <i class="fa fa-sticky-note"></i>
                    Pages
                </h1>

                <div class="panel-labels">
                    <Rock:BootstrapButton runat="server" ID="btnAddPage" Text="Add Page" CssClass="btn btn-default" OnClick="btnAddPage_Click" />
                </div>
            </div>

            <div class="panel-body">
                <div class="grid grid-panel">
                    <Rock:Grid ID="gList" runat="server" DataKeyNames="Id" OnRowSelected="gList_RowSelected">
                        <Columns>
                            <Rock:ReorderField />
                            <Rock:RockBoundField DataField="Name" HeaderText="Name" />
                            <Rock:RockBoundField DataField="Type" HeaderText="Page Type" />
                            <Rock:DeleteField ID="btnDelete" OnClick="btnDelete_Click" />
                        </Columns>
                    </Rock:Grid>
                </div>
            </div>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>
