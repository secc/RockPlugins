<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CMPublicProfileEdit.ascx.cs" Inherits="RockWeb.Plugins.org_secc.ChangeManager.CMPublicProfileEdit" %>

<asp:UpdatePanel ID="upContent" runat="server">
    <ContentTemplate>

        <script>
            $(function () {
                $(".photo a").fluidbox();
            });
        </script>

        <div class="panel panel-block">
            <div class="panel-heading">
                <h1 class="panel-title"><i class="fa fa-user"></i>&nbsp;My Account</h1>
            </div>
            <div class="panel-body">
                <Rock:NotificationBox ID="nbNotAuthorized" runat="server" Text="You are not authorized to edit this account." NotificationBoxType="Danger" Visible="false" />


                <asp:HiddenField ID="hfPersonGuid" runat="server" />

                <asp:Panel ID="pnlEdit" runat="server">
                    <asp:ValidationSummary ID="valValidation" runat="server" HeaderText="Please correct the following:" CssClass="alert alert-validation" />
                    <div class="row">

                        <div class="col-md-3">
                            <Rock:ImageEditor ID="imgPhoto" runat="server" Label="Photo" BinaryFileTypeGuid="03BD8476-8A9F-4078-B628-5B538F967AFC" />
                        </div>

                        <div class="col-md-9">
                            <Rock:RockDropDownList ID="ddlTitle" runat="server" CssClass="input-width-md" Label="Title" />
                            <Rock:DataTextBox ID="tbFirstName" runat="server" SourceTypeName="Rock.Model.Person, Rock" PropertyName="FirstName" Required="true" />
                            <Rock:DataTextBox ID="tbNickName" runat="server" SourceTypeName="Rock.Model.Person, Rock" PropertyName="NickName" />
                            <Rock:DataTextBox ID="tbLastName" runat="server" SourceTypeName="Rock.Model.Person, Rock" PropertyName="LastName" Required="true" />
                            <Rock:RockDropDownList ID="ddlSuffix" CssClass="input-width-md" runat="server" Label="Suffix" />
                            <Rock:BirthdayPicker ID="bpBirthDay" runat="server" Label="Birthday" />
                            <Rock:RockRadioButtonList ID="rblRole" runat="server" DataTextField="Name" DataValueField="Id" RepeatDirection="Horizontal" Label="Family Role" AutoPostBack="true" OnSelectedIndexChanged="rblRole_SelectedIndexChanged" />
                            <div class="row">
                                <div class="col-md-6">
                                    <Rock:RockRadioButtonList ID="rblGender" runat="server" RepeatDirection="Horizontal" Label="Gender" Required="true">
                                        <asp:ListItem Text="Male" Value="Male" />
                                        <asp:ListItem Text="Female" Value="Female" />
                                        <asp:ListItem Text="Unknown" Value="Unknown" />
                                    </Rock:RockRadioButtonList>
                                </div>
                                <div class="col-md-6">
                                    <%-- This YearPicker is needed for the GradePicker to work --%>
                                    <div style="display: none;">
                                        <Rock:YearPicker ID="ypGraduation" runat="server" Label="Graduation Year" Help="High School Graduation Year." />
                                    </div>
                                    <Rock:GradePicker ID="ddlGradePicker" runat="server" Label="Grade" UseAbbreviation="true" UseGradeOffsetAsValue="true" Visible="false" />
                                </div>
                            </div>

                            <Rock:CampusPicker ID="cpCampus" runat="server" Label="Campus" />
                        </div>
                    </div>
                    <hr />

                    <h3>Contact Info</h3>
                    <div class="form-horizontal">
                        <div class="form-group">
                            <div class="controls col-md-10 col-md-offset-2">
                                <Rock:DataTextBox ID="tbEmail" PrependText="<i class='fa fa-envelope'></i>" runat="server" SourceTypeName="Rock.Model.Person, Rock" PropertyName="Email" Label="Email Address" />
                            </div>
                        </div>

                        <div class="form-group">
                            <div class="controls col-md-10 col-md-offset-2">
                                <Rock:RockRadioButtonList ID="rblEmailPreference" runat="server" RepeatDirection="Horizontal" Label="Email Preference">
                                    <asp:ListItem Text="Email Allowed" Value="EmailAllowed" />
                                    <asp:ListItem Text="No Mass Emails" Value="NoMassEmails" />
                                    <asp:ListItem Text="Do Not Email" Value="DoNotEmail" />
                                </Rock:RockRadioButtonList>

                                <Rock:RockRadioButtonList ID="rblCommunicationPreference" runat="server" RepeatDirection="Horizontal" Label="Communication Preference">
                                    <asp:ListItem Text="Email" Value="1" />
                                    <asp:ListItem Text="SMS" Value="2" />
                                </Rock:RockRadioButtonList>
                            </div>
                        </div>
                    </div>

                    <asp:Panel ID="pnlPhoneNumbers" runat="server">
                        <h3>Phone Numbers</h3>
                        <div class="form-horizontal">
                            <asp:Repeater ID="rContactInfo" runat="server">
                                <ItemTemplate>
                                    <div id="divPhoneNumberContainer" runat="server" class="form-group">
                                        <div class="control-label col-md-2"><%# Eval("NumberTypeValue.Value")  %></div>
                                        <div class="controls col-md-10">
                                            <div class="row">
                                                <div class="controls col-md-7 form-group">
                                                    <asp:HiddenField ID="hfPhoneType" runat="server" Value='<%# Eval("NumberTypeValueId")  %>' />
                                                    <Rock:PhoneNumberBox ID="pnbPhone" runat="server" CountryCode='<%# Eval("CountryCode")  %>' Number='<%# Eval("NumberFormatted")  %>' />
                                                </div>
                                                <div class="col-md-5">
                                                    <div class="row">
                                                        <div class="col-md-6">
                                                            <asp:CheckBox ID="cbSms" runat="server" Text="SMS" Checked='<%# (bool)Eval("IsMessagingEnabled") %>' CssClass="js-sms-number" />
                                                        </div>
                                                        <div class="col-md-6">
                                                            <asp:CheckBox ID="cbUnlisted" runat="server" Text="Unlisted" Checked='<%# (bool)Eval("IsUnlisted") %>' />
                                                        </div>
                                                    </div>
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                </ItemTemplate>
                            </asp:Repeater>

                        </div>
                    </asp:Panel>

                    <asp:Panel ID="pnlAddress" runat="server">
                        <fieldset>
                            <legend>
                                <asp:Literal ID="lAddressTitle" runat="server" /></legend>

                            <div class="clearfix">
                                <div class="pull-left margin-b-md">
                                    <asp:Literal ID="lPreviousAddress" runat="server" />
                                </div>
                            </div>

                            <asp:HiddenField ID="hfStreet1" runat="server" />
                            <asp:HiddenField ID="hfStreet2" runat="server" />
                            <asp:HiddenField ID="hfCity" runat="server" />
                            <asp:HiddenField ID="hfState" runat="server" />
                            <asp:HiddenField ID="hfPostalCode" runat="server" />
                            <asp:HiddenField ID="hfCountry" runat="server" />

                            <Rock:AddressControl ID="acAddress" runat="server" RequiredErrorMessage="Your Address is Required" />

                        </fieldset>
                    </asp:Panel>

                    <asp:Panel ID="Panel1" runat="server">
                        <fieldset>
                            <legend>
                                <asp:Literal ID="Literal1" runat="server" />Other Changes</legend>
                        </fieldset>
                        <Rock:RockTextBox runat="server" ID="tbComments" TextMode="MultiLine" Height="200" Label="Other Change Requests" />
                    </asp:Panel>
                    <Rock:NotificationBox runat="server" ID="nbTOS" NotificationBoxType="Validation" Text="Please check the box below before saving your changes." Visible="false" />
                    <Rock:RockCheckBox runat="server" ID="cbTOS" Visible="false" />
                    <div class="actions">
                        <asp:LinkButton ID="btnSave" runat="server" AccessKey="s" ToolTip="Alt+s" Text="Save" CssClass="btn btn-primary" OnClick="btnSave_Click" />
                        <asp:LinkButton ID="btnCancel" runat="server" AccessKey="c" ToolTip="Alt+c" Text="Cancel" CssClass="btn btn-link" CausesValidation="false" OnClick="btnCancel_Click" />
                    </div>

                </asp:Panel>
            </div>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>
