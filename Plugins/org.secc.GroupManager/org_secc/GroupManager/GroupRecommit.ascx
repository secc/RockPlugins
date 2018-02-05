<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GroupRecommit.ascx.cs" Inherits="RockWeb.Plugins.org_secc.GroupManager.GroupRecommit" ViewStateMode="Enabled" EnableViewState="true" %>


<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <asp:Panel runat="server" ID="pnlInfo" Visible="false">
            <div class="panel-heading">
                <asp:Literal Text="Information" runat="server" ID="ltHeading" />
            </div>
            <div class="panel-body">
                <asp:Literal Text="" runat="server" ID="ltBody" />
            </div>
        </asp:Panel>

        <asp:Panel runat="server" ID="pnlMain" CssClass="clearfix">
            <h2>
                <asp:Literal Text="" runat="server" ID="ltTitle" />
            </h2>
            <Rock:NotificationBox runat="server" ID="nbValidation" NotificationBoxType="Danger" Visible="false">
                <b>Please correct the following:</b>
                <ul>
                    <li>An address is required.</li>
                </ul>
            </Rock:NotificationBox>
            <div class="panel panel-default">
                <div class="panel-heading">Information</div>
                <div class="panel-body">
                    <div class="form-group rock-text-box ">
					    <label class="control-label">Group Name<span style="font-weight:normal">
                            <a href="javascript:;" tabindex="0" role="button" data-toggle="popover" title="Group Name" data-trigger="focus" data-placement="right" data-content="Enter the name of your home group using your last name + &quot;Home&quot; such as &quot;Smith Home&quot;.">
                                <i class="fa fa-question-circle"></i>
                            </a></span>
					    </label>
                        <div class="control-wrapper">
                            <Rock:RockTextBox runat="server" ID="tbName" Enabled="false" ></Rock:RockTextBox>
                        </div>
				    </div>
                    
                    <Rock:RockTextBox runat="server" ID="tbDescription" Label="Description" Visible="false" TextMode="MultiLine"></Rock:RockTextBox>
                    
                    <div class="form-group rock-text-box ">
					    <label class="control-label">Schedule Type<span style="font-weight:normal">
                            <a href="javascript:;" tabindex="0" role="button" data-toggle="popover" title="Schedule Type" data-trigger="focus" data-placement="right" data-content="Nearly all of our neighborhood groups meet weekly.  Please select the weekly option then select the day of the week and the time you are planning to meet.">
                                <i class="fa fa-question-circle"></i>
                            </a></span>
					    </label>
                        <div class="control-wrapper">
                            <Rock:RockRadioButtonList runat="server" ID="rblSchedule" Label="" OnSelectedIndexChanged="rblSchedule_SelectedIndexChanged" AutoPostBack="true" RepeatDirection="Horizontal">
                            </Rock:RockRadioButtonList>
                        </div>
				    </div>
                    <Rock:DayOfWeekPicker ID="dowWeekly" runat="server" CssClass="input-width-md" Label="Day of the Week" Visible="false" />

                    <Rock:TimePicker ID="timeWeekly" runat="server" Label="Time of Day" Visible="false" />

                    <Rock:SchedulePicker ID="spSchedule" runat="server" AllowMultiSelect="false" Visible="false" Label="Named Schedule" />

                    <Rock:ScheduleBuilder ID="sbSchedule" runat="server" ShowDuration="false" ShowScheduleFriendlyTextAsToolTip="true" Visible="false" Label="Custom Schedule" />

                    <Rock:LocationPicker runat="server" ID="lopAddress" ValidationGroup="main" Required="true" Label="Address" AllowedPickerModes="Address" />
                </div>
            </div>
            <asp:Panel runat="server" ID="pnlFilters" class="panel panel-default">
                <div class="panel-heading">Filters
                        <a href="javascript:;" tabindex="0" role="button" data-toggle="popover" title="Filter Information" data-trigger="focus" data-placement="right" data-content="Filters are used for helping people find your home group.  It gives them an idea of what to expect when they come to your home.  <br /><br /><b>Max Members</b>: The max members filter option is used to restrict the number of people who will be allowed to signup for your home.  <br /><br /><b>Group Photo</b>: You can submit a photo of your house or your family if you would like.">
                            <i class="fa fa-question-circle"></i>
                        </a></span></div>
                <div class="panel-body">
                    <asp:PlaceHolder runat="server" ID="phAttributes" EnableViewState="false"></asp:PlaceHolder>
                </div>
            </asp:Panel>
            <asp:Panel runat="server" ID="pnlMembers" class="panel panel-default groupMembers" Visible="false">
                <div class="panel-heading">Members <i>(select any existing group members who will rejoin)</i>
                    <a href="javascript:;" tabindex="0" role="button" data-toggle="popover" title="Group Member Selection" data-trigger="focus" data-placement="right" data-content="Use this section to select which members who have previously attended your home group.  If you need to add additional members to your home, you can ask them to sign up using the neighborhood group finder tool or you can add them to your roster using the LWYA group manager website/app.  If you have additional questions or concerns, please email <a href=&quot;mailto:lwya@secc.org&quot;>lwya@secc.org</a>.">
                        <i class="fa fa-question-circle"></i>
                    </a>
                </div>
                <div class="panel-body">
                    <Rock:Grid runat="server" ID="gMembers" DataKeyNames="Id" ShowActionRow="false" DisplayType="Light">
                        <Columns>
                            <Rock:SelectField></Rock:SelectField>
                            <Rock:RockBoundField DataField="Person.FullName" HeaderText="Member"></Rock:RockBoundField>
                            <Rock:RockBoundField DataField="GroupRole.Name" HeaderText="Role"></Rock:RockBoundField>
                        </Columns>
                    </Rock:Grid>
                </div>
            </asp:Panel>

            <Rock:BootstrapButton runat="server" ID="btnSave" Text="Sign Up For Group" CausesValidation="true"
                ValidationGroup="main" CssClass="btn btn-primary pull-right" OnClick="btnSave_Click"></Rock:BootstrapButton>

        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>

<script>
    $(document).ready(
        function ()
        {
            $('.groupMembers').find(':checkbox').prop('checked', true);
            $('a[data-toggle="popover"]').popover({ html: true });
        })
</script>

