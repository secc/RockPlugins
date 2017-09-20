<%@ Control Language="C#" AutoEventWireup="true" CodeFile="BreakoutGroupAttendance.ascx.cs" Inherits="RockWeb.Blocks.Reporting.Children.BreakoutGroupAttendance" %>

<asp:UpdatePanel ID="upReport" runat="server">
    <ContentTemplate>
        <Rock:NotificationBox runat="server" ID="nbMissingSchedules" NotificationBoxType="Warning" Visible="false"></Rock:NotificationBox>
        <div class="hidden-print">
            <Rock:GridFilter runat="server" ID="fBreakoutGroups" OnApplyFilterClick="fBreakoutGroups_ApplyFilterClick">
            <asp:Panel ID="pnlGroupsContainer" runat="server"> 
                        <a onclick="$('input[id*=\'cblGroups\'][type=checkbox]').prop('checked', false); return false;" href="#">Uncheck All</a>
        <a onclick="$('input[id*=\'cblGroups\'][type=checkbox]').prop('checked', true); return false;" href="#">Check All</a>
                <Rock:RockCheckBoxList runat="server" ID="cblGroups" Label="Groups To Display" />
                </asp:Panel>
                <Rock:RockCheckBoxList runat="server" ID="cblSchedules" Label="Schedules To Display" />
                <Rock:DateRangePicker runat="server" ID="drRange" Label="Attendance Range" />
            </Rock:GridFilter>
        </div>
        <Rock:Grid runat="server" ID="gBreakoutGroups" AllowPaging="false" DataKeyNames="Id" OnRowDataBound="gBreakoutGroups_RowDataBound" OnRowSelected="gBreakoutGroups_RowSelected">
            <Columns>
                <Rock:RockBoundField DataField="Breakout" HeaderText="Breakout" />
                <Rock:RockBoundField DataField="Name" HeaderText="Name" />
                <Rock:RockBoundField DataField="Gender" HeaderText="Gender" />
                <Rock:RockBoundField DataField="Birthdate" HeaderText="Birthdate" />
                <Rock:RockBoundField DataField="Grade" HeaderText="Grade" />
                <Rock:RockLiteralField HeaderText="First Time" />
                <Rock:RockLiteralField HeaderText="Last Time" />
                <Rock:RockLiteralField HeaderText="Total" ItemStyle-BackColor="LightGray" />
            </Columns>
        </Rock:Grid>
        <asp:LinkButton ID="excel" runat="server" OnClick="Actions_ExcelExportClick" CssClass="btn btn-default pull-right" Style="margin-top: 5px;"><i class="fa fa-table"></i></asp:LinkButton>
    </ContentTemplate>
    <Triggers>
        <asp:PostBackTrigger ControlID="excel" />
    </Triggers>
</asp:UpdatePanel>
