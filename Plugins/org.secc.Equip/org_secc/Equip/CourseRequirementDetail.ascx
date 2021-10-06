<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CourseRequirementDetail.ascx.cs" Inherits="RockWeb.Plugins.org.secc.Equip.CourseRequirementDetail" %>
<%@ Register Namespace="org.secc.Equip.Controls" Assembly="org.secc.Equip" TagPrefix="SECC" %>

<asp:UpdatePanel ID="upnlContent" runat="server" CssClass="panel panel-block">
    <ContentTemplate>


        <div class="panel panel-block">

            <div class="panel-heading">
                <h1 class="panel-title">
                    <asp:Literal runat="server" ID="ltName" Text="Course Requirement" />
                </h1>
            </div>

            <Rock:PanelDrawer ID="pdAuditDetails" runat="server"></Rock:PanelDrawer>
            <div class="panel-body">
                <asp:Panel runat="server">
                </asp:Panel>
                <asp:Panel ID="pnlDetails" runat="server">

                    <div class="row">
                        <div class="col-md-4">
                            <Rock:RockLiteral runat="server" ID="ltCourseName" Label="Course" />
                            <Rock:RockLiteral runat="server" ID="ltSource" />
                            <Rock:RockLiteral runat="server" ID="ltDaysValid" Label="Days Valid" />
                        </div>
                        <div class="col-md-4">
                            <Rock:PieChart runat="server" ID="chart" />
                        </div>
                        <div class="col-md-4">
                            <asp:Literal ID="lTotals" runat="server" />
                        </div>
                    </div>
                    <Rock:BootstrapButton runat="server" ID="btnEdit" Text="Edit" CssClass="btn btn-primary" OnClick="btnEdit_Click" />

                    <asp:Panel runat="server" ID="pnlParticipants">
                        <h2>Participants</h2>
                        <Rock:GridFilter runat="server" ID="fStatuses" OnApplyFilterClick="fStatuses_ApplyFilterClick">
                            <Rock:RockCheckBoxList runat="server" ID="cblState" RepeatDirection="Horizontal" Label="Status" />
                        </Rock:GridFilter>
                        <Rock:Grid runat="server" ID="gStatuses" DataKeyNames="Id" DisplayType="Full">
                            <Columns>
                                <Rock:SelectField />
                                <Rock:PersonField HeaderText="Person" DataField="PersonAlias.Person" />
                                <Rock:RockBoundField HeaderText="Status" DataField="State" />
                            </Columns>
                        </Rock:Grid>
                    </asp:Panel>


                </asp:Panel>

                <asp:Panel ID="pnlEdit" runat="server">
                    <SECC:CoursePicker runat="server" Required="true" Label="Course" ID="pCourse" />
                    <Rock:RockDropDownList runat="server" ID="ddlSelect" AutoPostBack="true"
                        Required="true" Label="Users are in" OnSelectedIndexChanged="ddlSelect_SelectedIndexChanged">
                        <asp:ListItem Text="A Dataview" Value="DATAVIEW" />
                        <asp:ListItem Text="A Group" Value="GROUP" />
                    </Rock:RockDropDownList>
                    <Rock:GroupPicker runat="server" ID="pGroup" Label="Group" Visible="false" />
                    <Rock:DataViewItemPicker runat="server" ID="pDataview" Label="DataView" Visible="false" />
                    <Rock:RockTextBox runat="server" ID="tbDaysValid" Label="Days Valid" Help="The number of days a completed course is valid before it needs to be retaken. Leave blank for unlimited time." />
                    <Rock:BootstrapButton runat="server" ID="btnSave" Text="Save" CssClass="btn btn-primary" OnClick="btnSave_Click" />
                    <asp:LinkButton Text="Cancel" runat="server" ID="btnCancel" OnClick="btnCancel_Click" CssClass="btn" CausesValidation="false" />
                    <asp:LinkButton runat="server" Text="Delete" ID="btnDelete" OnClick="btnDelete_Click" CssClass="btn btn-danger pull-right" CausesValidation="false" />
                </asp:Panel>
            </div>
        </div>

    </ContentTemplate>
</asp:UpdatePanel>
