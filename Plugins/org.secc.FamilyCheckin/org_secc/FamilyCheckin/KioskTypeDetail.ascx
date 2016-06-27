<%@ Control Language="C#" AutoEventWireup="true" CodeFile="KioskTypeDetail.ascx.cs" Inherits="RockWeb.Plugins.org_secc.FamilyCheckin.KioskTypeDetail" %>

<script type="text/javascript">
    function clearActiveDialog()
    {
        $('#<%=hfAddLocationId.ClientID %>').val('');
    }
</script>

<asp:UpdatePanel ID="upnlDevice" runat="server">
    <ContentTemplate>
        <asp:Panel ID="pnlDetails" CssClass="panel panel-block" runat="server" Visible="false">

            <asp:HiddenField ID="hfDeviceId" runat="server" />

            <div class="panel-heading">
                <h1 class="panel-title"><i class="fa fa-desktop"></i>
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
                            <Rock:DataTextBox  CausesValidation="false" ID="tbDescription" runat="server" SourceTypeName="org.secc.FamilyCheckin.Model.KioskType, org.secc.FamilyCheckin" PropertyName="Description" TextMode="MultiLine" Rows="3" />
                        </div>
                    </div>

                    <Rock:DataDropDownList CausesValidation="false" runat="server" ID="ddlTemplates" Label="Checkin Template" OnSelectedIndexChanged="ddlTemplates_SelectedIndexChanged" AutoPostBack="true"  DataTextField="Name" DataValueField="Id" SourceTypeName="org.secc.FamilyCheckin.Model.KioskType, org.secc.FamilyCheckin" PropertyName="CheckinTemplateId"></Rock:DataDropDownList>
                    <div class="row">
                    <div class="col-md-6">
                        <Rock:RockCheckBoxList CausesValidation="false" ID="cblPrimaryGroupTypes" runat="server" Label="Check-in Area(s)" DataTextField="Name" DataValueField="Id" ></Rock:RockCheckBoxList>
                    </div>
                </div>
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


    </ContentTemplate>
</asp:UpdatePanel>
