<%@ Control Language="C#" AutoEventWireup="true" CodeFile="DecisionAnalytics.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Reporting.DecisionAnalytics" %>
<asp:UpdatePanel ID="upMain" runat="server">
    <ContentTemplate>
        <asp:Panel ID="pnlView" runat="server">
            <div class="panel panel-block panel-analytics">
                <div class="panel-heading panel-follow">
                    <h1 class="panel-title">
                        <i class="fa fa-list"></i>Decision Analytics
                    </h1>
                    <div class="panel-labels"></div>
                    <div class="rock-fullscreen-toggle js-fullscreen-trigger"></div>
                </div>
                <div class="panel-body">
                    <div class="row row-eq-height-md">
                        <div class="col-md-3 filter-options">
                            <Rock:DateRangePicker ID="drpDecisionDate" runat="server" Label="Decision Date" />
                            <Rock:PersonPicker ID="ppDecisions" runat="server" Label="Person" />
                            <Rock:RockCheckBoxList ID="cblGender" runat="server" Label="Gender" RepeatDirection="Horizontal" RepeatColumns="2">
                                <asp:ListItem Text="Male" Value="M" />
                                <asp:ListItem Text="Female" Value="F" />
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
                            <Rock:DefinedValuesPicker ID="dvpBaptismType" runat="server" Label="Baptism Type" RepeatColumns="2" />
                            <div class="actions">
                                <span class="pull-right-md">
                                    <asp:LinkButton ID="lbClearFilters" runat="server" CssClass="btn btn-sm btn-default">Clear Filters</asp:LinkButton>
                                </span>
                            </div>
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
                                <Rock:Grid ID="gResults" runat="server" AllowSorting="true" RowItemText="Decision" ExportSource="ColumnOutput"
                                    ExportFilename="GivingAnalytics" OnRowSelected="gResults_RowSelected" OnRowDataBound="gResults_RowDataBound" DataKeyNames="RecordType, Id">
                                    <Columns>
                                        <Rock:RockBoundField HeaderText="Id" DataField="WorkflowId" Visible="false" />
                                        <Rock:RockBoundField HeaderText="Name" DataField="FullName" SortExpression="FullNameReversed" />
                                        <Rock:RockBoundField HeaderText="Gender" DataField="Gender" SortExpression="Gender" />
                                        <Rock:RockBoundField HeaderText="BirthDate" DataField="Birthdate" DataFormatString="{0:d}" />
                                        <Rock:RockBoundField HeaderText="Age" DataField="Age" SortExpression="Age" />
                                        <Rock:RockBoundField HeaderText="Grade" DataField="Grade" />
                                        <Rock:RockBoundField HeaderText="Mobile Phone" DataField="MobilePhoneGridValue" />
                                        <Rock:RockBoundField HeaderText="Email" DataField="EmailGridValue" />
                                        <Rock:RockBoundField HeaderText="Address" DataField="FullAddressGrid" />
                                        <Rock:RockBoundField HeaderText="Connection Status" DataField="ConnectionStatusValue" />
                                        <Rock:RockBoundField HeaderText="Primary Campus" DataField="FamilyCampusName" />
                                        <Rock:RockBoundField HeaderText="Decision Campus" DataField="DecisionCampusName" />
                                        <Rock:RockBoundField HeaderText="Decision Type" DataField="DecisionType" />
                                        <Rock:RockBoundField HeaderText="Decision Date" DataField="FormDate" DataFormatString="{0:d}" />
                                        <Rock:RockBoundField HeaderText="Event" DataField="EventName" />
                                        <Rock:RockBoundField HeaderText="Baptism Type" DataField="BaptismTypeValue" />
                                        <Rock:RockBoundField HeaderText="Baptism Date" DataField="BaptismDate" DataFormatString="{0:d}" />
                                        <Rock:RockBoundField HeaderText="SOF Date" DataField="StatementOfFaithSignedDate" DataFormatString="{0:d}" />
                                        <Rock:RockBoundField HeaderText="Membership Class" DataField="MembershipClassDate" DataFormatString="{0:d}" />
                                        <Rock:RockBoundField HeaderText="Membership" DataField="MembershipDate" DataFormatString="{0:d}" />


                                    </Columns>
                                </Rock:Grid>
                            </asp:Panel>
                        </div>
                    </div>
                </div>
            </div>
        </asp:Panel>
        <Rock:ModalDialog ID="mdPersonInfo" runat="server">
            <Content>
                <div class="row">
                    <div class="col-md-6">
                        <Rock:RockLiteral ID="lAddress" runat="server" Label="Home Address" />
                        <asp:Panel ID="pnlParentInfo" runat="server" Visible="false">
                            <Rock:RockLiteral ID="lParentName" runat="server" Label="Parent Name" />
                            <Rock:RockLiteral ID="lParentPhone" runat="server" Label="Parent Phone" />
                            <Rock:RockLiteral ID="lParentEmail" runat="server" Label="Parent Email" />
                        </asp:Panel>
                        <asp:Panel ID="pnlPersonInfo" runat="server" Visible="false">
                            <Rock:RockLiteral ID="lMobilePhone" runat="server" Label="Mobile Phone" />
                            <Rock:RockLiteral ID="lEmail" runat="server" Label="Email" />
                        </asp:Panel>
                    </div>
                    <div class="col-md-6">
                        <Rock:RockLiteral ID="lBaptism" runat="server" Label="Baptism" />
                        <Rock:RockLiteral ID="lStatementOfFaith" runat="server" Label="Statement of Faith" />
                        <Rock:RockLiteral ID="lMembershipDate" runat="server" Label="Membership Date" />
                        <Rock:RockLiteral ID="lMembershipClass" runat="server" Label="Membership Class" />
                    </div>
                </div>
            </Content>
        </Rock:ModalDialog>
        <script>
            Sys.Application.add_load( function ()
            {
                Rock.controls.fullScreen.initialize();
            } );
        </script>
    </ContentTemplate>
</asp:UpdatePanel>
