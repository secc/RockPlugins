﻿<%@ Control Language="C#" AutoEventWireup="true" CodeFile="KioskTypeDetail.ascx.cs" Inherits="RockWeb.Plugins.org_secc.FamilyCheckin.KioskTypeDetail" %>

<script type="text/javascript">
    function clearActiveDialog() {
        $('#<%=hfAddLocationId.ClientID %>').val('');
    }
</script>

<asp:UpdatePanel ID="upnlDevice" runat="server">
    <ContentTemplate>
        <asp:Panel ID="pnlDetails" CssClass="panel panel-block" runat="server">

            <asp:HiddenField ID="hfKioskTypeId" runat="server" />

            <div class="panel-heading">
                <h1 class="panel-title"><i class="fa fa-hand-pointer-o"></i>
                    <asp:Literal ID="lActionTitle" runat="server" /></h1>
            </div>
            <div class="panel-body">

                <asp:ValidationSummary ID="ValidationSummary1" runat="server" HeaderText="Please Correct the Following" CssClass="alert alert-danger" />

                <fieldset>

                    <Rock:NotificationBox ID="nbEditModeMessage" runat="server" NotificationBoxType="Info" />
                    <Rock:NotificationBox ID="nbDuplicateDevice" runat="server" NotificationBoxType="Warning" Title="Sorry" Visible="false" />

                    <div class="row">
                        <div class="col-md-6">
                            <Rock:DataTextBox ID="tbName" runat="server" SourceTypeName="org.secc.FamilyCheckin.Model.KioskType, org.secc.FamilyCheckin" PropertyName="Name" Required="true" />
                        </div>
                        <div class="col-md-6">
                        </div>
                    </div>

                    <div class="row">
                        <div class="col-md-12">
                            <Rock:DataTextBox CausesValidation="false" ID="tbDescription" runat="server" SourceTypeName="org.secc.FamilyCheckin.Model.KioskType, org.secc.FamilyCheckin" PropertyName="Description" TextMode="MultiLine" Rows="3" />
                        </div>
                    </div>

                    <Rock:DataDropDownList CausesValidation="false" runat="server" ID="ddlTemplates" Label="Checkin Template" OnSelectedIndexChanged="ddlTemplates_SelectedIndexChanged" AutoPostBack="true" DataTextField="Name" DataValueField="Id" SourceTypeName="org.secc.FamilyCheckin.Model.KioskType, org.secc.FamilyCheckin" PropertyName="CheckinTemplateId"></Rock:DataDropDownList>
                    <div class="row">
                        <div class="col-md-6">
                            <Rock:RockCheckBoxList CausesValidation="false" ID="cblPrimaryGroupTypes" runat="server" Label="Check-in Area(s)" DataTextField="Name" DataValueField="Id"></Rock:RockCheckBoxList>
                        </div>
                    </div>

                    <div class="row">
                        <div class="col-md-12">
                            <Rock:CampusPicker runat="server" ID="ddlCampus" Label="Campus"
                                Help="Sets the campus that this kiosktype belongs." />
                        </div>
                    </div>

                    <div class="row">
                        <div class="col-md-3">
                         <Rock:RockCheckBox runat="server" ID="cbIsMobile" Label="Is Mobile" Help="Check if this kiosk type should be available from the mobile app" />
                        </div>
                        <div class="col-md-3">
                            <Rock:RockTextBox runat="server" ID="tbMinutesValid"
                                Label="Minutes Reserved" Help="The number of minutes a check-in is guaranteed after mobile checkin." />
                        </div>
                        <div class="col-md-3">
                            <Rock:RockTextBox runat="server" ID="tbGraceMinutes"
                                Label="Expire After" Help="The number of minutes a check-in is available after the reserved time expires." />
                        </div>
                    </div>

                    <Rock:DataDropDownList runat="server" CausesValidation="false" ID="ddlTheme" Label="Theme" SourceTypeName="org.secc.FamilyCheckin.Model.KioskType, org.secc.FamilyCheckin" PropertyName="Description" />

                    <h3>Locations</h3>
                    <Rock:Grid ID="gLocations" runat="server" DisplayType="Light" RowItemText="Location" ShowConfirmDeleteDialog="false">
                        <Columns>
                            <Rock:RockBoundField DataField="LocationPath" HeaderText="Name" />
                            <Rock:DeleteField OnClick="gLocations_Delete" />
                        </Columns>
                    </Rock:Grid>



                    <h3>Schedules</h3>
                    <Rock:Grid ID="gSchedules" runat="server" DisplayType="Light" RowItemText="Schedule" ShowConfirmDeleteDialog="false">
                        <Columns>
                            <Rock:RockBoundField DataField="Name" HeaderText="Name" />
                            <Rock:DeleteField OnClick="gSchedules_Delete" />
                        </Columns>
                    </Rock:Grid>

                    <Rock:HtmlEditor runat="server" Label="HTML Message" Height="300" ID="tbMessage" SourceTypeName="org.secc.FamilyCheckin.Model.KioskType, org.secc.FamilyCheckin" PropertyName="Message"></Rock:HtmlEditor>
                    <h3>Medical Consent</h3>
                    <div class="row">
                        <div class="col-md-6">
                            <Rock:RockCheckBox ID="cbRequireMedicalConsent" runat="server" Label="Require Medical Consent" Help="Check if this kiosk type requires a family to consent to medical assistance for minors." />
                        </div>
                        <div class="col-md-6">
                            <Rock:NumberBox ID="tbMedicalConsentMaxSkips" runat="server" Label="Maximum Skips" CssClass="input-width-md" Visible="false" />
                        </div>
                    </div>
                </fieldset>



                <div class="actions">
                    <asp:LinkButton ID="btnSave" runat="server" AccessKey="s" Text="Save" CssClass="btn btn-primary" OnClick="btnSave_Click" />
                    <asp:LinkButton ID="btnCancel" runat="server" AccessKey="c" Text="Cancel" CssClass="btn btn-link" CausesValidation="false" OnClick="btnCancel_Click" />
                </div>

            </div>

        </asp:Panel>

        <Rock:ModalDialog ID="mdLocationPicker" runat="server" SaveButtonText="Save" OnSaveClick="btnAddLocation_Click" Title="Select Check-in Location" OnCancelScript="clearActiveDialog();" ValidationGroup="Location">
            <Content ID="mdLocationPickerContent">
                <asp:HiddenField ID="hfAddLocationId" runat="server" />
                <Rock:LocationItemPicker ID="locationPicker" runat="server" Label="Check-in Location" ValidationGroup="Location" />
            </Content>
        </Rock:ModalDialog>

        <Rock:ModalDialog ID="mdSchedulepicker" runat="server" SaveButtonText="Save" OnSaveClick="mdSchedulepicker_SaveClick" Title="Select Check-in Location" OnCancelScript="clearActiveDialog();" ValidationGroup="Location">
            <Content ID="mdsSchedulePickerContent">
                <asp:HiddenField ID="HiddenField1" runat="server" />
                <Rock:SchedulePicker runat="server" ID="schedulePicker" Label="Kiosk Schedule" />
            </Content>
        </Rock:ModalDialog>
    </ContentTemplate>
</asp:UpdatePanel>
