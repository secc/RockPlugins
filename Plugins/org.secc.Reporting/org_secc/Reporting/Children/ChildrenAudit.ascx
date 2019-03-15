<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ChildrenAudit.ascx.cs" Inherits="RockWeb.Blocks.Reporting.Children.ChildrenAudit" %>

<asp:UpdatePanel ID="upReport" runat="server">
    <ContentTemplate>
        <Rock:ModalDialog runat="server" ID="mdModal">
            <Content>
                <Rock:Grid runat="server" ID="gHistory" DisplayType="Light" ShowHeader="false">
                    <Columns>
                        <Rock:HtmlField DataField="Summary"  DisplayMode="Rendered" NullDisplayText="Empty record" />
                    </Columns>
                </Rock:Grid>
            </Content>
        </Rock:ModalDialog>
        <div class="row well">
            <Rock:NotificationBox runat="server" ID="nbNotification" NotificationBoxType="Warning"></Rock:NotificationBox>
            <Rock:DatePicker runat="server" ID="dpDate" Label="Date" Required="true" />
            <Rock:LocationPicker runat="server" ID="lpLocation" Label="Location"
                CurrentPickerMode="Named" AllowedPickerModes="Named" Required="true" />
            <Rock:Toggle runat="server" ID="tgChildLocations" Label="Include Child Locations" OnText="Yes" OffText="No" ActiveButtonCssClass="btn-primary" />
            <Rock:SchedulePicker runat="server" ID="spSchedule" Label="Schedule (Optional)" InitialItemParentIds="246" AllowMultiSelect="false" />
            <Rock:BootstrapButton runat="server" ID="btnGo" Text="Go" CssClass="btn btn-primary" OnClick="btnGo_Click" />
            <Rock:Grid runat="server" ID="gReport" AllowPaging="true" DataKeyNames="PersonId" OnRowSelected="gReport_RowSelected">
                <Columns>
                    <Rock:RockBoundField DataField="PersonName" HeaderText="Person Name" />
                    <Rock:RockBoundField DataField="Age" HeaderText="Age" />
                    <Rock:BoolField DataField="DidAttend" HeaderText="Attendance Counts" />
                    <Rock:BoolField DataField="IsVolunteer" HeaderText="Is Volunteer" />
                    <Rock:RockBoundField DataField="Location" HeaderText="Location" />
                    <Rock:RockBoundField DataField="Schedule" HeaderText="Schedule" />
                    <Rock:RockBoundField DataField="Group" HeaderText="Group" />
                    <Rock:RockBoundField DataField="CheckInTime" HeaderText="Check-In Time" />
                    <Rock:RockBoundField DataField="EntryTime" HeaderText="Entry Time" />
                    <Rock:RockBoundField DataField="ExitTime" HeaderText="Exit Time" />
                    <Rock:RockBoundField DataField="Device" HeaderText="Device" />
                    <Rock:RockBoundField DataField="SubLocation" HeaderText="Sub-Location" />
                </Columns>
            </Rock:Grid>
    </ContentTemplate>
</asp:UpdatePanel>
