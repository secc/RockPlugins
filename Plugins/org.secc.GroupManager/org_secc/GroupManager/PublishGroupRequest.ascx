<%@ Control Language="C#" AutoEventWireup="true" CodeFile="PublishGroupRequest.ascx.cs" Inherits="RockWeb.Plugins.GroupManager.PublishGroupRequest" %>
<asp:UpdatePanel runat="server">
    <ContentTemplate>
        <asp:Panel runat="server" ID="pnlEdit" Visible="true">
            <div class="panel panel-default">
                <div class="panel-heading">
                    <h3 class="panel-title">Publish:
                        <asp:Literal ID="ltGroupName" runat="server" />
                    </h3>
                </div>
                <div class="panel-body">
                    <div class="row">
                        <div class="col-md-2">
                            <Rock:ImageUploader runat="server" ID="iGroupImage" Label="Image" />
                        </div>
                        <div class="col-md-10">
                            <Rock:RockTextBox runat="server" ID="tbDescription" TextMode="MultiLine" Label="Description" Required="true" Height="135" />
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-4">
                            <Rock:DateRangePicker runat="server" ID="drPublishDates" Label="Publish Dates" Required="true" Help="Dates the group will be published online. Dates are inclusive" />
                        </div>
                        <div class="col-md-8">
                            <Rock:DefinedValuesPickerEnhanced runat="server" EnhanceForLongLists="true" ID="ddlAudience" Label="Audience" />
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-2">
                            <Rock:RockDropDownList runat="server" ID="ddlDayOfWeek" Label="Meeting Day Of Week" />
                        </div>
                        <div class="col-md-2">
                            <Rock:TimePicker runat="server" ID="tTimeOfDay" Label="Meeting Time of Day" />
                        </div>
                        <div class="col-md-2">
                            <Rock:DatePicker runat="server" ID="dpStartDate" Label="Starts On" />
                        </div>
                        <div class="col-md-6">
                            <Rock:RockTextBox runat="server" ID="tbLocationName" Label="Location Name" ValidateRequestMode="Disabled" />
                        </div>
                    </div>
                    <h3>Registration Information</h3>
                    <hr />
                    <div class="row">
                        <div class="col-md-4">
                            <Rock:RockDropDownList runat="server" Label="Registration Options" ID="ddlRegistration" Required="true"
                                AutoPostBack="true" OnSelectedIndexChanged="ddlRegistration_SelectedIndexChanged"
                                Help="This setting allows you to manage how people are able to register.
                                You can have an optional or required registration that we will automatically generate for you.
                                If you have a registration that exists in a different location, select Custom Registration.
                                You will be required to provide the website address for that registration.">
                                <asp:ListItem Text="No Registration" Value="0" />
                                <asp:ListItem Text="Automatically Generate an Optional Registration" Value="1" />
                                <asp:ListItem Text="Automatically Generate a Required Registration" Value="2" />
                                <asp:ListItem Text="Custom Registration - I HAVE A Custom Link" Value="3" />
                                <asp:ListItem Text="Custom Registration - I NEED A Custom Registration" Value="4" />
                            </Rock:RockDropDownList>
                        </div>
                        <div class="col-md-8 registrationInformation">
                            <Rock:UrlLinkBox runat="server" ID="tbRegistrationLink" Label="Custom Registration Website" Visible="false" />
                            <Rock:Toggle runat="server" ID="cbAllowSpouseRegistration" OnCssClass="btn-primary" OffCssClass="btn-warning"
                                OnText="Yes" OffText="No" Label="Allow spouse to register at the same time?" Visible="false" />
                        </div>
                         <div class="col-md-12">
                            <Rock:RockTextBox runat="server" ID="tbRegistrationDetails" TextMode="MultiLine" Height="200" Label="Registration Request Details" />
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-12">
                            <Rock:NotificationBox runat="server" ID="nbRegistrationError" NotificationBoxType="Warning" Title="Warning"
                                Text="Childcare registration requires an internal registration. ('Automatically Generate an Optional Registration' or 'Automatically Generate a Required Registration')" />
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-4">
                            <Rock:RockDropDownList runat="server" ID="ddlChildcareOptions" Label="Childcare Options"
                                AutoPostBack="true" OnSelectedIndexChanged="ddlChildcareRegistration_SelectedIndexChanged">
                                <asp:ListItem Text="Childcare is NOT available" Value="0" />
                                <asp:ListItem Text="Childcare is available - No Registration" Value="1" />
                                <asp:ListItem Text="Childcare is available - Registration Required" Value="2" />
                                <asp:ListItem Text="Childcare registration included in Custom Registration" Value="3" />
                            </Rock:RockDropDownList>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-4">
                            <Rock:RockDropDownList runat="server" ID="ddlChildcareNeedRegistration" Required="true"
                                OnSelectedIndexChanged="ddlChildcareNeedRegistration_SelectedIndexChanged" AutoPostBack="true"
                                Label="Do You Already Have A Childcare Registration?">
                                <asp:ListItem Text="" Value="" />
                                <asp:ListItem Text="I already have a childcare registration link" Value="1" />
                                <asp:ListItem Text="I need a childcare registration made for me" Value="2" />
                            </Rock:RockDropDownList>

                        </div>
                        <div class="col-md-8">
                            <Rock:UrlLinkBox runat="server" ID="tbChildcareRegistrationLink" Label="Childcare Registration Link" Help="The url for the childcare registration." />
                        </div>
                        <div class="col-md-12">
                            <Rock:RockTextBox runat="server" ID="tbChildcareRegistrationDetails" TextMode="MultiLine" Height="200" Label="Childcare Registration Request Details"
                                Help="Please let us know when you want your registration to start and end and any additional information you may have." />
                        </div>
                    </div>
                    <h3>Contact Information</h3>
                    <hr />
                    <Rock:PersonPicker runat="server" ID="pContactPerson" Label="Contact Person" Required="true" OnSelectPerson="pRequestor_SelectPerson" />
                    <Rock:EmailBox runat="server" ID="tbContactEmail" Label="Contact Email" Required="true" />
                    <Rock:PhoneNumberBox runat="server" ID="tbContactPhoneNumber" Label="Contact Phone Number" Required="true" />

                    <asp:Panel runat="server" ID="pnlConfirmation">
                        <h3>Confirmation Email</h3>
                        <hr />
                        <Rock:RockTextBox runat="server" ID="tbConfirmationFromName" Label="Confirmation From Name" Required="true" />
                        <Rock:EmailBox runat="server" ID="tbConfirmationFromEmail" Label="Confirmation From Email" Required="true" />
                        <Rock:RockTextBox runat="server" ID="tbConfirmationSubject" Label="Confirmation Email Subject" Required="true" />
                        <Rock:HtmlEditor runat="server" ID="ceConfirmationBody" Label="Confirmation Email Body" Height="400" Required="true" />
                    </asp:Panel>
                    <asp:Panel runat="server" ID="pnlAttributes" Visible="false">
                        <h3>Attributes</h3>
                        <hr />
                        <Rock:DynamicPlaceholder ID="phAttributeEdits" runat="server" />
                    </asp:Panel>
                    <hr />
                    <Rock:RockDropDownList runat="server" ID="ddlStatus" Label="Status" />
                    <Rock:BootstrapButton runat="server" ID="btnDraft" Text="Save Draft" CssClass="btn btn-default" OnClick="btnDraft_Click" />
                    <Rock:BootstrapButton runat="server" ID="btnSave" Text="Publish" CssClass="btn btn-primary" OnClick="btnSave_Click" />
                </div>
            </div>
        </asp:Panel>
        <asp:Panel runat="server" ID="pnlSelectGroup" Visible="false">
            <Rock:GroupPicker runat="server" ID="gpGroup" OnSelectItem="gpGroup_SelectItem" />
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
