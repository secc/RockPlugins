<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GroupRecommit.ascx.cs" Inherits="RockWeb.Plugins.org_secc.GroupManager.GroupRecommit" ViewStateMode="Enabled" EnableViewState="true" %>


<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <Rock:NotificationBox runat="server" ID="nbWarning" Visible="false"
            NotificationBoxType="Warning">

        </Rock:NotificationBox>
        <div class="panel panel-default">
            <div class="panel-heading">Information</div>
            <div class="panel-body">
                <Rock:RockTextBox runat="server" ID="tbName" Required="true" ValidationGroup="main"
                    Label="Group Name" Help="The name of your group."></Rock:RockTextBox>

                <Rock:RockRadioButtonList runat="server" ID="rblSchedule" Label="Schedule Type" OnSelectedIndexChanged="rblSchedule_SelectedIndexChanged" AutoPostBack="true" RepeatDirection="Horizontal">
                </Rock:RockRadioButtonList>
                <Rock:DayOfWeekPicker ID="dowWeekly" runat="server" CssClass="input-width-md" Label="Day of the Week" Visible="false" />

                <Rock:TimePicker ID="timeWeekly" runat="server" Label="Time of Day" visible="false"/>

                <Rock:SchedulePicker ID="spSchedule" runat="server" AllowMultiSelect="false" Visible="false" Label="Named Schedule" />

                <Rock:ScheduleBuilder ID="sbSchedule" runat="server" ShowDuration="false" ShowScheduleFriendlyTextAsToolTip="true" Visible="false" Label="Custom Schedule" />

                <Rock:LocationPicker Required="true" ValidationGroup="main" runat="server" ID="lopAddress" Label="Address" AllowedPickerModes="Address" />
            </div>
        </div>
        <div class="panel panel-default">
            <div class="panel-heading">Filters</div>
            <div class="panel-body">
                <asp:PlaceHolder runat="server" ID="phAttributes" EnableViewState="false"></asp:PlaceHolder>
            </div>
        </div>

        <asp:Panel runat="server" ID="pnlMembers" class="panel panel-default" Visible="false">
            <div class="panel-heading">Members</div>
            <div class="panel-body">
                <Rock:Grid runat="server" ID="gMembers" ShowActionRow="false" DisplayType="Light">
                    <Columns>
                        <Rock:SelectField></Rock:SelectField>
                        <Rock:RockBoundField DataField="Person.FullName" HeaderText="Member"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="GroupRole.Name" HeaderText="Role"></Rock:RockBoundField>
                    </Columns>
                </Rock:Grid>
            </div>
        </asp:Panel>

        <Rock:BootstrapButton runat="server" ID="btnSave" Text="Sign Up For Group" CausesValidation="true"
            ValidationGroup="main" CssClass="btn btn-primary" OnClick="btnSave_Click"></Rock:BootstrapButton>


    </ContentTemplate>
</asp:UpdatePanel>
