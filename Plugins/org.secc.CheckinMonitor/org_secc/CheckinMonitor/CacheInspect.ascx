<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CacheInspect.ascx.cs" Inherits="RockWeb.Plugins.org_secc.CheckinMonitor.CacheInspect" %>


<asp:UpdatePanel ID="upDevice" runat="server">
    <ContentTemplate>
        <Rock:Grid runat="server" ID="gOccurrences">
            <Columns>
                <Rock:RockBoundField HeaderText="AccessKey" DataField="AccessKey" />
                <Rock:RockBoundField HeaderText="GroupId" DataField="GroupId" />
                <Rock:RockBoundField HeaderText="GroupName" DataField="GroupName" />
                <Rock:RockBoundField HeaderText="LocationId" DataField="LocationId" />
                <Rock:RockBoundField HeaderText="LocationName" DataField="LocationName" />
                <Rock:RockBoundField HeaderText="ScheduleId" DataField="ScheduleId" />
                <Rock:RockBoundField HeaderText="ScheduleName" DataField="ScheduleName" />
                <Rock:BoolField HeaderText="IsActive" DataField="IsActive" />
                <Rock:BoolField HeaderText="IsVolunteer" DataField="IsVolunteer" />
                <Rock:RockBoundField HeaderText="SoftRoomThreshold" DataField="SoftRoomThreshold" />
                <Rock:RockBoundField HeaderText="FirmRoomThreshold" DataField="FirmRoomThreshold" />
                <Rock:RockBoundField HeaderText="Attendances Count" DataField="Attendances.Count" />
            </Columns>
        </Rock:Grid>


        <Rock:Grid runat="server" ID="gAttendances">
            <Columns>
                <Rock:RockBoundField HeaderText="OccurrenceAccessKey" DataField="OccurrenceAccessKey" />
                <Rock:DateField HeaderText="Id" DataField="Id" />
                <Rock:DateField HeaderText="PersonId" DataField="PersonId" />
                <Rock:RockBoundField HeaderText="PersonName" DataField="PersonName" />
                <Rock:DateField HeaderText="GroupId" DataField="GroupId" />
                <Rock:DateField HeaderText="LocationId" DataField="LocationId" />
                <Rock:DateField HeaderText="ScheduleId" DataField="ScheduleId" />
                <Rock:RockBoundField HeaderText="Code" DataField="Code" />
                <Rock:DateField HeaderText="CreatedDateTime" DataField="CreatedDateTime" />
                <Rock:DateField HeaderText="StartDateTime" DataField="StartDateTime" />
                <Rock:DateField HeaderText="EndDateTime" DataField="EndDateTime" />
                <Rock:EnumField HeaderText="AttendanceState" DataField="AttendanceState" />
                <Rock:BoolField HeaderText="IsVolunteer" DataField="IsVolunteer" />
            </Columns>
        </Rock:Grid>
    </ContentTemplate>
</asp:UpdatePanel>
