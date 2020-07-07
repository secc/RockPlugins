<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CacheInspect.ascx.cs" Inherits="RockWeb.Plugins.org_secc.CheckinMonitor.CacheInspect" %>


<asp:UpdatePanel ID="upDevice" runat="server">
    <ContentTemplate>
        <Rock:BootstrapButton runat="server" ID="btnOccurrences" CssClass="btn btn-default" Text="Occurrences" OnClick="btnOccurrences_Click" />
        <Rock:BootstrapButton runat="server" ID="btnAttendances" CssClass="btn btn-default" Text="Attendances" OnClick="btnAttendances_Click" />
        <Rock:BootstrapButton runat="server" ID="btnMobileRecords" CssClass="btn btn-default" Text="Mobile Checkin Records" OnClick="btnMobileRecords_Click" />
        <Rock:BootstrapButton runat="server" ID="btnKioskTypes" CssClass="btn btn-default" Text="Kiosk Types" OnClick="btnKioskTypes_Click" />
        <Rock:BootstrapButton runat="server" ID="btnVerify" CssClass="btn btn-danger" Text="Verify Cache" OnClick="btnVerify_Click" />

        <asp:Panel runat="server" ID="pnlOccurrences" Visible="false">
            <Rock:Grid runat="server" ID="gOccurrences" DataKeyNames="Id" OnRowSelected="gOccurrences_RowSelected">
                <Columns>
                    <Rock:RockBoundField HeaderText="AccessKey" DataField="Item.AccessKey" />
                    <Rock:RockBoundField HeaderText="GroupId" DataField="Item.GroupId" />
                    <Rock:RockBoundField HeaderText="GroupName" DataField="Item.GroupName" />
                    <Rock:RockBoundField HeaderText="LocationId" DataField="Item.LocationId" />
                    <Rock:RockBoundField HeaderText="LocationName" DataField="Item.LocationName" />
                    <Rock:RockBoundField HeaderText="ScheduleId" DataField="Item.ScheduleId" />
                    <Rock:RockBoundField HeaderText="ScheduleName" DataField="Item.ScheduleName" />
                    <Rock:BoolField HeaderText="IsActive" DataField="Item.IsActive" />
                    <Rock:BoolField HeaderText="IsFull" DataField="Item.IsFull" />
                    <Rock:BoolField HeaderText="IsVolunteer" DataField="Item.IsVolunteer" />
                    <Rock:RockBoundField HeaderText="SoftRoomThreshold" DataField="Item.SoftRoomThreshold" />
                    <Rock:RockBoundField HeaderText="FirmRoomThreshold" DataField="Item.FirmRoomThreshold" />
                    <Rock:RockBoundField HeaderText="Attendances Count" DataField="Item.Attendances.Count" />
                    <Rock:RockBoundField HeaderText="Size" DataField="Size" />
                </Columns>
            </Rock:Grid>
        </asp:Panel>

        <asp:Panel runat="server" ID="pnlAttendances" Visible="false">
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
                    <Rock:DateTimeField HeaderText="CreatedDateTime" DataField="CreatedDateTime" />
                    <Rock:DateTimeField HeaderText="StartDateTime" DataField="StartDateTime" />
                    <Rock:DateTimeField HeaderText="EndDateTime" DataField="EndDateTime" />
                    <Rock:EnumField HeaderText="AttendanceState" DataField="AttendanceState" />
                    <Rock:BoolField HeaderText="IsVolunteer" DataField="IsVolunteer" />
                </Columns>
            </Rock:Grid>
            <Rock:RockLiteral runat="server" ID="ltAttendance" Label="Keys" />
        </asp:Panel>

        <asp:Panel runat="server" ID="pnlMobileRecords" Visible="false">
            <Rock:Grid runat="server" ID="gMobileRecords" DataKeyNames="Id" OnRowSelected="gMobileRecords_RowSelected">
                <Columns>
                    <Rock:RockBoundField HeaderText="Id" DataField="Id" />
                    <Rock:RockBoundField HeaderText="AccessKey" DataField="AccessKey" />
                    <Rock:RockBoundField HeaderText="UserName" DataField="UserName" />
                    <Rock:RockBoundField HeaderText="FamilyGroupId" DataField="FamilyGroupId" />
                    <Rock:DateTimeField HeaderText="CreatedDateTime" DataField="CreatedDateTime" />
                    <Rock:DateTimeField HeaderText="ReservedUntilDateTime" DataField="ReservedUntilDateTime" />
                    <Rock:DateTimeField HeaderText="ExpirationDateTime" DataField="ExpirationDateTime" />
                    <Rock:EnumField HeaderText="Status" DataField="Status" />
                    <Rock:BoolField HeaderText="IsDirty" DataField="IsDirty" />
                    <Rock:RockBoundField HeaderText="CampusId" DataField="CampusId" />
                    <Rock:RockBoundField HeaderText="Attendance Count" DataField="AttendanceIds.Count" />

                </Columns>
            </Rock:Grid>
        </asp:Panel>

        <asp:Panel runat="server" ID="pnlKioskTypes" Visible="false">
            <Rock:Grid runat="server" ID="gKioskTypes" DataKeyNames="Id" OnRowSelected="gKioskTypes_RowSelected">
                <Columns>
                    <Rock:RockBoundField HeaderText="Id" DataField="Item.Id" />
                    <Rock:RockBoundField HeaderText="Name" DataField="Item.Name" />
                    <Rock:RockBoundField HeaderText="Campus" DataField="Item.Campus.Name" />
                    <Rock:BoolField HeaderText="Is Mobile" DataField="Item.IsMobile" />
                    <Rock:RockBoundField HeaderText="Locations" DataField="Item.Locations.Count" />
                    <Rock:RockBoundField HeaderText="Kiosk Schedules" DataField="Item.Schedules.Count" />
                    <Rock:RockBoundField HeaderText="Check-in Schedules" DataField="Item.CheckInSchedules.Count" />
                    <Rock:RockBoundField HeaderText="Size" DataField="Size" />
                </Columns>
            </Rock:Grid>
        </asp:Panel>

        <asp:Panel runat="server" ID="pnlVerify" Visible="false">
            <h2>Cache Inaccuracies:
            </h2>
            <asp:Literal runat="server" ID="ltVerify" />
        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>
