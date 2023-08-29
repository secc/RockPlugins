<%@ Control Language="C#" AutoEventWireup="true" CodeFile="AttendanceQuickEntry.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Reporting.AttendanceQuickEntry" %>
<asp:UpdatePanel ID="upMain" runat="server">


    <ContentTemplate>
    <style>
        div[id$="pnlEntry"] .row {
            padding-bottom:15px;
        }
    </style>
        <Rock:NotificationBox ID="nbMessage" runat="server" Visible="false" />
        <asp:Panel ID="pnlEntry" runat="server" CssClass="panel panel-block" Visible="false">
            
            <div class="panel-heading">
                <h1 class="panel-title"><i class="fas fa-poll-people"></i> <asp:Literal ID="lPanelTitle" runat="server" /></h1>
            </div>
            <div class="panel-body">
                <asp:ValidationSummary ID="valSummary" runat="server" ValidationGroup="valAttendanceEntry" CssClass="alert alert-validation" />
                <div class="row">
                    <div class="col-sm-4">
                        <span class="control-label">Campus</span>
                    </div>
                    <div class="col-sm-4">
                        <asp:Literal ID="lCampus" runat="server"></asp:Literal>
                    </div>
                </div>
                <div  class="row">
                    <div class="col-sm-4">
                        <span class="control-label">
                            Day
                        </span>
                    </div>
                    <div class="col-sm-4">
                        <asp:Literal ID="lDate" runat="server"></asp:Literal>
                    </div>
                </div>
                <div class="row">
                    <div class="col-sm-4">
                        <span class="control-label">
                            Service
                        </span>
                    </div>
                    <div class="col-sm-4">
                        <Rock:RockDropDownList ID="ddlSchedule" runat="server" Required="true" RequiredErrorMessage="Schedule is Required" ValidationGroup="valAttendanceEntry" />
                    </div>
                </div>
                <div class="row">
                    <div class="col-sm-4">
                        <span class="control-label">
                            Attendance
                        </span>
                    </div>
                    <div class="col-sm-4">
                        <Rock:RockTextBox ID="tbAttendance" runat="server" TextMode="Number" Required="true" CssClass="" RequiredErrorMessage="Attendance is required." ValidationGroup="valAttendanceEntry" />
                    </div>
                </div>
                <div class="actions">
                    <span class="pull-right">
                        <asp:LinkButton ID="lbSubmit" runat="server" CssClass="btn btn-primary" Text="Submit" ValidationGroup="valAttendanceEntry" CausesValidation="true" />
                        <asp:LinkButton ID="lbReset" runat="server" CssClass="btn btn-cancel" Text="Reset" CausesValidation="false"/>
                    </div>
                </div>
            </div>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>