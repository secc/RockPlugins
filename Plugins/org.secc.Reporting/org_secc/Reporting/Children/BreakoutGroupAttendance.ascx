<%@ Control Language="C#" AutoEventWireup="true" CodeFile="BreakoutGroupAttendance.ascx.cs" Inherits="RockWeb.Blocks.Reporting.Children.BreakoutGroupAttendance" %>

<asp:UpdatePanel ID="upReport" runat="server">
    <ContentTemplate>
        <div class="hidden-print">
            <Rock:GridFilter runat="server" ID="fBreakoutGroups" OnApplyFilterClick="fBreakoutGroups_ApplyFilterClick">
                <Rock:RockCheckBoxList runat="server" ID="cblGroups" Label="Groups To Display" />
                <Rock:RockCheckBoxList runat="server" ID="cblSchedules" Label="Schedules To Display" />
                <Rock:DateRangePicker runat="server" ID="drRange" Label="Attendance Range"/>
            </Rock:GridFilter>
        </div>
        <Rock:Grid runat="server" ID="gBreakoutGroups" AllowPaging="false" OnRowDataBound="gBreakoutGroups_RowDataBound">
            <Columns>
                <Rock:SelectField />
                <Rock:RockBoundField DataField="Breakout" HeaderText="Breakout" />
                <Rock:RockBoundField DataField="Name" HeaderText="Name" />
                <Rock:RockBoundField DataField="Gender" HeaderText="Gender" />
                <Rock:RockBoundField DataField="Grade" HeaderText="Grade" />
                <Rock:RockLiteralField HeaderText="First Time" />
                <Rock:RockLiteralField HeaderText="Last Time" />
                <Rock:RockLiteralField HeaderText="Total" ItemStyle-BackColor="LightGray" />
            </Columns>
        </Rock:Grid>
    </ContentTemplate>
</asp:UpdatePanel>
