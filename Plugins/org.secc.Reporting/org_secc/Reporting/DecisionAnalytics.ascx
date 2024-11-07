<%@ Control Language="C#" AutoEventWireup="true" CodeFile="DecisionAnalytics.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Reporting.DecisionAnalytics" %>
<asp:UpdatePanel ID="upMain" runat="server">
    <ContentTemplate>
        <asp:Panel ID="pnlView" runat="server">
            <div class="panel panel-block panel-analytics">
                <div class="panel-heading panel-follow">
                    <h1 class="panel-title">
                        <i class="fa fa-list">Decision Analytics</i>
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
                            <%--content--%>
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
