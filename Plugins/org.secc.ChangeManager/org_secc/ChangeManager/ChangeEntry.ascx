﻿<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ChangeEntry.ascx.cs" Inherits="RockWeb.Plugins.org_secc.ChangeManager.ChangeEntry" %>
<script>
    isDirty = false;

    $(document).ready(function () {
        $(":input[type=email],:input[type=tel]")
            .change(function () {
                if (!isDirty) {
                    $('.btn-save').click(function () {
                        return Rock.dialogs.confirmPreventOnCancel(event, 'Updating a phone number and/or e-mail address can impact a users login ability and can allow a users account to be stolen.  Please ensure that you have adequately identified the person you are updating and that they have authorization to make this change.');
                    });
                }
                isDirty = true;
            })
    });
</script>

<asp:UpdatePanel runat="server" ID="upContent">
    <ContentTemplate>
        <Rock:ModalAlert runat="server" ID="maNotice" />
        <div class="panel panel-default">
            <div class="panel-heading">
                <h3 class="panel-title">Person Update</h3>
            </div>
            <asp:Panel runat="server" ID="pnlMain" Visible="false">
                <div class="panel-body">
                    <asp:HiddenField runat="server" ID="hfPersonId" />
                    <div class="row">
                        <div class="col-md-2">
                            <div class="col-md-12">
                                <Rock:ImageUploader ID="iuPhoto" runat="server" />
                                <br />
                                <Rock:RockDropDownList runat="server" ID="ddlFamilyRole" Label="Family Role">
                                    <asp:ListItem Text="Adult" Value="A" />
                                    <asp:ListItem Text="Child" Value="C" />
                                </Rock:RockDropDownList>
                            </div>
                        </div>
                        <div class="col-md-10">
                            <div class="col-md-6">
                                <Rock:DefinedValuePicker runat="server" ID="ddlTitle" Label="Title" SourceTypeName="Rock.Model.Person" PropertyName="Title" />
                                <Rock:DataTextBox runat="server" ID="tbFirstName" SourceTypeName="Rock.Model.Person" PropertyName="FirstName" />
                                <Rock:DataTextBox runat="server" ID="tbLastName" SourceTypeName="Rock.Model.Person" PropertyName="LastName" />
                            </div>
                            <div class="col-md-6">
                                <Rock:DataTextBox runat="server" ID="tbNickName" SourceTypeName="Rock.Model.Person" PropertyName="NickName" />
                                <Rock:DataTextBox runat="server" ID="tbMiddleName" SourceTypeName="Rock.Model.Person" PropertyName="MiddleName" />
                                <Rock:DefinedValuePicker runat="server" ID="ddlSuffix" Label="Suffix" SourceTypeName="Rock.Model.Person" PropertyName="Suffix" />
                            </div>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <h3>Communication Information</h3>
                            <div style="border-top: solid 1px black">
                                <br />
                                <asp:Repeater ID="rContactInfo" runat="server">
                                    <ItemTemplate>
                                        <div class="form-group phonegroup clearfix">
                                            <div class="control-label col-sm-1 phonegroup-label"><%# Rock.Web.Cache.DefinedValueCache.Get( (int)Eval("NumberTypeValueId")).Value  %></div>
                                            <div class="controls col-sm-11 phonegroup-number">
                                                <div class="form-row">
                                                    <div class="col-md-7">
                                                        <asp:HiddenField ID="hfPhoneType" runat="server" Value='<%# Eval("NumberTypeValueId")  %>' />
                                                        <Rock:PhoneNumberBox ID="pnbPhone" runat="server" CountryCode='<%# Eval("CountryCode") %>' AutoPostBack="true"
                                                            Number='<%# Eval("NumberFormatted")  %>' autocomplete="off" OnTextChanged="pnbPhone_TextChanged" />
                                                    </div>
                                                    <div class="col-md-5">
                                                        <Rock:RockCheckBox ID="cbSms" runat="server" Text="SMS" Checked='<%# (bool)Eval("IsMessagingEnabled") %>' ContainerCssClass="pull-left" CssClass="js-sms-number" />
                                                        <Rock:RockCheckBox ID="cbUnlisted" runat="server" Text="Unlisted" Checked='<%# (bool)Eval("IsUnlisted") %>' ContainerCssClass="pull-left" />
                                                    </div>

                                                </div>
                                            </div>
                                        </div>
                                    </ItemTemplate>
                                </asp:Repeater>
                                <div class="container">
                                    <div class="row">
                                        <div class="col-sm-6">
                                            <Rock:EmailBox ID="tbEmail" runat="server" SourceTypeName="Rock.Model.Person" PropertyName="Email" Label="Email" />
                                        </div>
                                        <div class="col-sm-12">
                                            <Rock:RockCheckBox ID="cbIsEmailActive" runat="server" Label="Email Status" Text="Is Active" />
                                        </div>
                                        <div class="col-sm-12">
                                            <Rock:RockRadioButtonList ID="rblEmailPreference" runat="server" RepeatDirection="Horizontal" Label="Email Preference">
                                                <asp:ListItem Text="Email Allowed" Value="EmailAllowed" />
                                                <asp:ListItem Text="No Mass Emails" Value="NoMassEmails" />
                                                <asp:ListItem Text="Do Not Email" Value="DoNotEmail" />
                                            </Rock:RockRadioButtonList>
                                        </div>
                                        <div class="col-sm-12">
                                            <Rock:RockRadioButtonList ID="rblCommunicationPreference" runat="server" RepeatDirection="Horizontal" Label="Communication Preference">
                                                <asp:ListItem Text="Email" Value="1" />
                                                <asp:ListItem Text="SMS" Value="2" />
                                            </Rock:RockRadioButtonList>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <h3>Demographic Information</h3>
                            <div style="border-top: solid 1px black; padding: 20px">
                                <div class="row">
                                    <div class="col-md-8">
                                        <Rock:BirthdayPicker runat="server" ID="bpBirthday" Label="Birth Date" />
                                    </div>
                                    <div class="col-md-4">
                                        <Rock:DataDropDownList runat="server" ID="ddlGender" SourceTypeName="Rock.Model.Person" PropertyName="Gender" />
                                    </div>
                                    <div class="col-md-8">
                                        <Rock:DefinedValuePicker runat="server" ID="ddlMaritalStatus" Label="Marital Status" SourceTypeName="Rock.Model.Person" PropertyName="MaritalStatus" />
                                    </div>
                                    <div class="col-md-4">
                                        <Rock:DatePicker runat="server" ID="dpAnniversaryDate" SourceTypeName="Rock.Model.Person" PropertyName="AnniversaryDate" />
                                    </div>
                                    <div class="col-md-8">
                                        <Rock:GradePicker ID="ddlGradePicker" runat="server" UseAbbreviation="true" UseGradeOffsetAsValue="true" />
                                    </div>
                                    <div class="col-md-4">
                                        <Rock:YearPicker ID="ypGraduation" runat="server" Label="Graduation Year" Help="High School Graduation Year." />
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                    <h3>Any changes to the below information will be applied to the entire family.</h3>
                    <div style="border-top: solid 1px black; padding: 20px">
                        <div class="row">
                            <div class="col-md-6">
                                <Rock:AddressControl runat="server" ID="acAddress" Label="Address" />
                            </div>
                            <div class="col-md-6">
                                <Rock:CampusPicker runat="server" ID="ddlCampus" Label="Campus" />
                                <Rock:PersonPicker runat="server" ID="pAddPerson" Label="Add a Person to this Family" />
                                <Rock:RockCheckBox runat="server" ID="cbRemovePerson" Text="Remove this person from other families?" />
                            </div>
                        </div>
                    </div>
                    <h3>Additional Comments or Requests</h3>
                    <div style="border-top: solid 1px black; padding: 20px">
                        <div class="row">
                            <div class="col-md-12">
                                <Rock:RockTextBox runat="server" ID="tbComments" Label="(Optional)" TextMode="MultiLine" Height="200" />
                            </div>
                        </div>
                    </div>
                    <br />
                    <br />
                    <asp:LinkButton runat="server" ID="btnSave" Text="Save" CssClass="btn btn-primary pull-right btn-save"
                        OnClick="btnSave_Click" OnClientClick=";" />
            </asp:Panel>

            <asp:Panel runat="server" ID="pnlSimple" Visible="false">
                <div class="panel-body">
                    <h3>
                        <asp:Literal runat="server" ID="ltPersonName" />
                    </h3>
                    <Rock:RockTextBox runat="server" ID="tbSimpleRequest" Label="What would you like us to change?" TextMode="MultiLine" Height="200" />
                    <asp:LinkButton runat="server" ID="lbSimpleSave" Text="Save" CssClass="btn btn-primary pull-right btn-save"
                        OnClick="lbSimpleSave_Click" OnClientClick=";" />
                </div>
            </asp:Panel>

            <asp:Panel runat="server" ID="pnlNoPerson" Visible="false" CssClass="row">
                <div class="col-md-10" style="margin-left: 10px">
                    <Rock:PersonPicker runat="server" ID="pPerson" Label="Person" OnSelectPerson="pPerson_SelectPerson" />
                </div>
            </asp:Panel>

            <asp:Panel runat="server" ID="pnlDone" CssClass="text-center" Visible="false">
                <h3>Success!</h3>
                Your changes have been submitted. Pending review, they will be applied.
                <br />
                <br />
                <Rock:BootstrapButton runat="server" Text="Done" ID="btnDone" OnClick="btnDone_Click" CssClass="btn btn-primary" />
                <br />
                <br />
            </asp:Panel>
        </div>

    </ContentTemplate>
</asp:UpdatePanel>
