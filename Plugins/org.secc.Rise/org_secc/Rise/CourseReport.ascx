<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CourseReport.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Rise.CourseReport" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <asp:Panel ID="pnlView" runat="server" CssClass="panel panel-block">
            <div class="panel-heading">
                <h1 class="panel-title">
                    <i class="fa fa-chalkboard"></i>
                    <asp:Literal ID="ltName" runat="server" />
                </h1>
            </div>
            <div class="panel-body">
                <div class="row">
                    <div class="col-md-6">
                        <Rock:RockLiteral runat="server" ID="ltEnrolled" Label="Enrolled" />
                        <Rock:RockLiteral runat="server" ID="ltCompleted" Label="Completed" />
                        <Rock:RockLiteral runat="server" ID="ltIncomplete" Label="Incomplete" />
                    </div>
                    <div class="col-md-6">
                        <Rock:PieChart runat="server" ID="pcComplete" />
                    </div>
                </div>
            </div>
        </asp:Panel>
        <asp:Panel runat="server" ID="pnlUsers">
            <h3>Enrollment Report</h3>
            <Rock:GridFilter runat="server" ID="fUsers" OnClearFilterClick="fUsers_ClearFilterClick" OnApplyFilterClick="fUsers_ApplyFilterClick">
                <Rock:RockDropDownList runat="server" ID="ddlStatus" Label="Status">
                    <asp:ListItem Value="" Text="" />
                    <asp:ListItem Value="Complete" Text="Complete" />
                    <asp:ListItem Value="Incomplete" Text="Incomplete" />
                </Rock:RockDropDownList>
                <Rock:RockDropDownList runat="server" ID="ddlEnrolled" Label="Enrollment">
                    <asp:ListItem Value="" Text="" />
                    <asp:ListItem Value="true" Text="Enrolled" />
                    <asp:ListItem Value="false" Text="Not Enrolled" />
                </Rock:RockDropDownList>
            </Rock:GridFilter>

            <Rock:Grid runat="server" ID="gUsers" AllowSorting="true" DataKeyNames="PersonId" PersonIdField="PersonId">
                <Columns>
                    <Rock:SelectField />
                    <Rock:PersonField DataField="Person" HeaderText="Person" SortExpression="Person" />
                    <Rock:RockBoundField HeaderText="Status" DataField="Status" SortExpression="Status" />
                    <Rock:RockBoundField HeaderText="Score" DataField="Score" SortExpression="Score" />
                    <Rock:BoolField HeaderText="Enrolled" DataField="Enrolled" SortExpression="Enrolled" />
                </Columns>
            </Rock:Grid>
        </asp:Panel>


    </ContentTemplate>
</asp:UpdatePanel>
