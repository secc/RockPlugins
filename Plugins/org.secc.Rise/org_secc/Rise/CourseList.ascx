<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CourseList.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Rise.CourseList" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <Rock:ModalAlert runat="server" ID="maSync" />
        <asp:Panel ID="pnlView" runat="server" CssClass="panel panel-block">

            <div class="panel-heading">
                <h1 class="panel-title">
                    <i class="fa fa-chalkboard"></i>
                    Rise Courses
                </h1>
                <div class="panel-labels">
                    <asp:LinkButton Text="<i class='fa fa-exchange'></i>" runat="server" ID="btnSync"
                        CssClass="label label-info" ToolTip="Sync Courses" OnClick="btnSync_Click" />
                </div>
            </div>
            <div class="panel-body">

                <div class="grid grid-panel">
                    <Rock:Grid ID="gList" runat="server" AllowSorting="true" OnRowSelected="gList_RowSelected" DataKeyNames="Id" OnRowDataBound="gList_RowDataBound">
                        <Columns>
                            <Rock:RockBoundField DataField="Name" HeaderText="Course" SortExpression="Name" />
                            <Rock:BoolField DataField="AvailableToAll" HeaderText="Available To All" />
                            <Rock:RockLiteralField ID="lCategories" HeaderText="Categories" />
                        </Columns>
                    </Rock:Grid>
                </div>

            </div>

        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>
