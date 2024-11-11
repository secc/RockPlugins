<%@ Control Language="C#" AutoEventWireup="true" CodeFile="DecisionAnalytics.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Reporting.DecisionAnalytics" %>
<asp:UpdatePanel ID="upMain" runat="server">
    <ContentTemplate>
        <asp:Panel ID="pnlView" runat="server">
            <div class="panel panel-block panel-analytics">
                <div class="panel-heading panel-follow">
                    <h1 class="panel-title">
                        <i class="fa fa-list"></i> Decision Analytics
                    </h1>
                    <div class="panel-labels"></div>
                    <div class="rock-fullscreen-toggle js-fullscreen-trigger"></div>
                </div>
                <div class="panel-body">
                    <div class="row row-eq-height-md">
                        <div class="col-md-3 filter-options">
                            <Rock:DateRangePicker ID="drpDecisionDate" runat="server" Label="Decision Date" />
                            <Rock:PersonPicker ID="ppDecisions" runat="server" Label="Person" />
                            <Rock:RockCheckBoxList ID="lGender" runat="server" Label="Gender">
                                <asp:ListItem Text="Male" Value="1" Selected="True" />
                                <asp:ListItem Text="Female" Value="2" Selected="True" />
                            </Rock:RockCheckBoxList>
                            <Rock:NumberRangeEditor ID="nreAgeRange" runat="server" Label="Age Range" MinimumValue="0" MaximumValue="110" NumberType="Integer" />
                            <label class="control-label">Grade</label>
                            <div class="form-control-group">
                                <Rock:RockDropDownList ID="ddlLowerGrade" runat="server" CssClass="input-width-md" />
                                <asp:Label ID="lblGradeRangeTo" runat="server" Text="to" CssClass="to" />
                                <Rock:RockDropDownList ID="ddlUpperGrade" runat="server" CssClass="input-width-md" />
                            </div>
                            <Rock:DefinedValuesPicker ID="dvpConnectionStatus" runat="server" Label="Connection Status" />
                            <Rock:CampusPicker ID="pkFamilyCampus" runat="server" Label="Family Campus" IncludeInactive="false" />
                            <Rock:CampusPicker ID="pkDecisionCampus" runat="server" Label="Decision Campus" IncludeInactive="false" />
                            <Rock:RockDropDownList ID="ddlDecisionType" runat="server" Label="Decision Type" />
                            <Rock:RockDropDownList ID="ddlEventType" runat="server" Label="Event" />
                            <Rock:DefinedValuesPicker ID="dvpBaptismType" runat="server" Label="Baptism Type" />

                        </div>
                        <div class="col-md-9">
                            <div class="row analysis-types">
                                <div class="col-sm-8">
                                    <%-- Controls --%>
                                </div>
                                <div class="col-sm-4">
                                    <div class="actions text-right">
                                        <asp:LinkButton ID="btnApply" runat="server" CssClass="btn btn-primary" ToolTip="Update Results" OnClick="btnApply_Click">
                                            <i class="fa fa-refresh"></i> Update
                                        </asp:LinkButton> 
                                    </div>
                                </div>
                            </div>
                            <asp:Panel ID="pnlUpdateMessage" runat="server" Visible="true">
                                <Rock:NotificationBox ID="nbUpdateMessage" runat="server" NotificationBoxType="Default" CssClass="text-center padding-all-lg"
                                    Heading="Confirm Settings" Text="<p>Confirm your settings and select the 'Update' button to display your results.</p>" />
                            </asp:Panel>
                            <asp:Panel ID="pnlGridResults" runat="server" Visible="false">
                                <Rock:Grid ID="gResults" runat="server" AllowSorting="true" RowItemText="Person" ExportSource="ColumnOutput"
                                    ExportFilename="GivingAnalytics" OnRowSelected="gResults_RowSelected" OnRowDataBound="gResults_RowDataBound" DataKeyNames="Id">
                                    <Columns>
                                        <Rock:RockBoundField HeaderText="Id" DataField="Id" Visible="false" />
                                        <Rock:RockBoundField HeaderText="Name" DataField="FullName" SortExpression="FullNameReversed" />
                                        <Rock:RockBoundField HeaderText="Gender" DataField="GenderText" SortExpression="Gender" />
                                        <Rock:RockBoundField HeaderText="Age" DataField="Age" SortExpression="Age" />
                                        <Rock:RockBoundField HeaderText="Mobile Phone" DataField="Mobile Phone" />
                                        <Rock:RockBoundField HeaderText="Email" DataField="Email" />
                                        <Rock:RockTemplateField HeaderText="Address">
                                            <ItemTemplate>
                                                <asp:Literal ID="lAddress" runat="server" />
                                            </ItemTemplate>
                                        </Rock:RockTemplateField>
                                        <Rock:RockBoundField HeaderText="Connection Status" DataField="ConnectionStatus" />
                                        <Rock:RockBoundField HeaderText="Primary Campus" DataField="PrimaryCampus" />
                                        <Rock:RockBoundField HeaderText="Decision Campus" DataField="DecisionCampus" />
                                        <Rock:RockBoundField HeaderText="Decision Type" DataField="DecisionType" />
                                        <Rock:RockBoundField HeaderText="Decision Date" DataField="DecisionDate" />
                                        <Rock:RockBoundField HeaderText="Decision Status" DataField="DecisionStatus" />
                                        <Rock:RockBoundField HeaderText="Event" DataField="Event" />
                                        <Rock:RockBoundField HeaderText="Baptism Type" DataField="BaptismType" />
<%--                                        <Rock:RockTemplateField HeaderText="Statement of Faith">
                                            <ItemTemplate>
                                                 <asp:Literal ID="l"
                                            </ItemTemplate>
                                        </Rock:RockTemplateField>
                                        <Rock:RockBoundField HeaderText="Membership Class" DataField="MembershipClass" />
                                        <Rock:RockBoundField HeaderText="Baptism Date" DataField="BaptismDate" />
                                        <Rock:RockBoundField HeaderText="Membership Date" DataField="MembershipDate" />--%>
                                        
                                        
                                    </Columns>
                                </Rock:Grid>
                            </asp:Panel>
                        </div>
                    </div>
                </div>
            </div>
        </asp:Panel>
        <script>
            Sys.Application.add_load( function ()
            {
                Rock.controls.fullScreen.initialize();
            } );
        </script>
    </ContentTemplate>
</asp:UpdatePanel>
