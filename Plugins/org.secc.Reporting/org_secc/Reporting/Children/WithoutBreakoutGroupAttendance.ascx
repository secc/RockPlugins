<%@ Control Language="C#" AutoEventWireup="true" CodeFile="WithoutBreakoutGroupAttendance.ascx.cs" Inherits="RockWeb.Blocks.Reporting.Children.WithoutBreakoutGroupAttendance" %>

<asp:UpdatePanel ID="upReport" runat="server">
    <ContentTemplate>
        <div class="row">
            <div class="col-sm-4">
                <Rock:GradePicker runat="server" ID="gpGrade" Label="Grade" />
            </div>
            <div class="col-sm-4">
                <Rock:DateRangePicker runat="server" ID="drRange" Label="Attendance Range" />
            </div>
        </div>
        <asp:LinkButton ID="excel" runat="server" OnClick="Actions_ExcelExportClick" CssClass="btn btn-primary" Style="margin-top: 5px;">Generate Report</asp:LinkButton>
    </ContentTemplate>
    <Triggers>
        <asp:PostBackTrigger ControlID="excel" />
    </Triggers>
</asp:UpdatePanel>
