<%@ Control Language="C#" AutoEventWireup="true" CodeFile="Believe.ascx.cs" Inherits="RockWeb.Blocks.Reporting.NextGen.Believe" %>

<asp:UpdatePanel ID="upReport" runat="server">
    <ContentTemplate>
        <script type="text/javascript">
            if (window != window.top) {
                if (typeof window.parent.Rock.controls.modal.close === 'function') {
                    $('iframe', window.parent.document).hide().before("<div class=\"alert alert-success\">Group Member data was successfully saved.</div>");
                    window.parent.Rock.controls.modal.close('PAGE_UPDATED');
                };
            }
        </script>
        <asp:Panel runat="server" ID="pnlInfo" Visible="false">
            <div class="panel-heading">
                <asp:Literal Text="Information" runat="server" ID="ltHeading" />
            </div>
            <div class="panel-body">
                <asp:Literal Text="" runat="server" ID="ltBody" />
            </div>
        </asp:Panel>
        <asp:Panel runat="server" ID="pnlMain" Visible="true">
            <div class="panel panel-block">
                <div class="panel-heading">
                    <asp:HyperLink runat="server" ID="hlGroup" CssClass="pull-right btn btn-default" ToolTip="View Group"><i class="fa fa-group"></i></asp:HyperLink>
                    <asp:HyperLink runat="server" ID="hlStudentLeaderGroup" Visible="false" CssClass="pull-right btn btn-default" ToolTip="View Student Leader Group"><i class="fa fa-graduation-cap"></i></asp:HyperLink>

                    <h1 class="panel-title"><i class="se se-msm"></i>Believe Manager</h1>
                </div>
                <Rock:GridFilter runat="server" ID="gfReport" OnApplyFilterClick="gfReport_ApplyFilterClick" OnClearFilterClick="gfReport_ClearFilterClick">
                    <Rock:RockTextBox Label="Person Name" ID="txtPersonName" runat="server" />
                    <Rock:RockDropDownList Label="POA Received:" ID="ddlPOA" runat="server" Visible="false" />
                    <Rock:NumberRangeEditor Label="Balance Owed" ID="nreBalanceOwed" runat="server" />
                    <Rock:CampusPicker Label="Campus" ID="cmpCampus" Visible="false" runat="server"></Rock:CampusPicker>
                </Rock:GridFilter>
                <Rock:Grid ID="gReport" runat="server" AllowSorting="true" EmptyDataText="No Results" PersonIdField="RegisteredBy.Id" DataKeyNames="Id, RegistrationId" OnRowSelected="gReport_RowSelected" ExportSource="ColumnOutput">
                    <Columns>
                        <Rock:SelectField></Rock:SelectField>
                        <Rock:PersonField DataField="RegisteredBy.Model" HeaderText="Registered By" SortExpression="RegisteredBy" />
                        <Rock:PersonField DataField="Registrant.Model" HeaderText="Person" SortExpression="Registrant.Model.LastName" />
                        <Rock:RockBoundField DataField="Age" HeaderText="Age" SortExpression="Age"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="DOB" HeaderText="DOB (Person)" SortExpression="DOB"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="RegistrantData.BirthDate" HeaderText="DOB (Registration)" SortExpression="RegistrantData.BirthDate"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="LegalRelease" HeaderText="Legal Release" SortExpression="LegalRelease"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="RegistrantData.BalanceOwed" HeaderText="Balance Owed" DataFormatString="{0:C}" SortExpression="RegistrantData.BalanceOwed"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="Address" HeaderText="Address" SortExpression="Address"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="RegistrantData.ConfirmationEmail" HeaderText="Email" SortExpression="RegistrantData.ConfirmationEmail"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="Gender" HeaderText="Gender" SortExpression=""></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="GraduationYear" HeaderText="Grad&nbsp;Year (Person)" SortExpression="GraduationYear"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="RegistrantData.Grade" HeaderText="Grade (Registration)" SortExpression="RegistrantData.Grade"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="HomePhone" HeaderText="Home Phone" SortExpression="HomePhone"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="CellPhone" HeaderText="Cell Phone" SortExpression="CellPhone"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="RegistrantData.ParentName" HeaderText="Parent/Guardian Name" SortExpression="RegistrantData.ParentName"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="RegistrantData.ParentCell" HeaderText="Parent/Guardian Cell" SortExpression="RegistrantData.ParentCell"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="RegistrantData.EmName" HeaderText="Emergency Contact Name" SortExpression="RegistrantData.EmName"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="RegistrantData.EmCell" HeaderText="Emergency Contact Cell" SortExpression="RegistrantData.EmCell"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="School" HeaderText="School" SortExpression="School"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="RegistrantData.OthersInGroup" HeaderText="Others In Group" SortExpression="RegistrantData.OthersInGroup"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="RegistrantData.Church" HeaderText="Home Church" SortExpression="RegistrantData.Church"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="RegistrantData.Blankenbakerhour" HeaderText="MSM Service" SortExpression="RegistrantData.Campus"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="RegistrantData.Allergies" HeaderText="Allergies" SortExpression="RegistrantData.Allergies"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="RegistrantData.AllergySeverity" HeaderText="Allergy Severity" SortExpression="RegistrantData.AllergySeverity"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="RegistrantData.HowManaged" HeaderText="How Managed?" SortExpression="RegistrantData.HowManaged"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="RegistrantData.OtherAllergies" HeaderText="Other Allergies" SortExpression="RegistrantData.OtherAllergies"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="RegistrantData.MedicalInfo" HeaderText="Medical Info" SortExpression="RegistrantData.MedicalInfo"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="RegistrantData.OTCMeds" HeaderText="OTC Meds" SortExpression="RegistrantData.OTCMeds"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="RegistrantData.Insurance" HeaderText="Insurance" SortExpression="RegistrantData.Insurance"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="RegistrantData.Physiciansname" HeaderText="Physician" SortExpression="RegistrantData.Physiciansname"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="GroupMemberData.Room" HeaderText="Room" SortExpression="GroupMemberData.Room"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="GroupMemberData.Bus" HeaderText="Bus" SortExpression="GroupMemberData.Bus"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="GroupMemberData.Group" HeaderText="Believe Group" SortExpression="GroupMemberData.Group"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="Role" HeaderText="Role" SortExpression=""></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="GroupMemberData.GeneralNotes" HeaderText="General Notes" SortExpression="GroupMemberData.GeneralNotes"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="GroupMemberData.RoomingNotes" HeaderText="Rooming Notes" SortExpression="GroupMemberData.RoomingNotes"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="GroupMemberData.MedicalNotes" HeaderText="Medical Notes" SortExpression="GroupMemberData.MedicalNotes"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="GroupMemberData.TravelNotes" HeaderText="Travel Notes" SortExpression="GroupMemberData.TravelNotes"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="GroupMemberData.TravelExceptions" HeaderText="Travel Exceptions" SortExpression="GroupMemberData.TravelExceptions"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="MSMGroup" HeaderText="MSM Group" SortExpression=""></Rock:RockBoundField>
                        <Rock:LinkButtonField HeaderText="Registration" CssClass="btn btn-default btn-sm fa fa-file-image-o" OnClick="btnRegistration_Click" HeaderStyle-HorizontalAlign="Center" />
                    </Columns>
                </Rock:Grid>
            </div>
            <div class="clearfix">
                <div class="pull-right">
                    <small>
                        <asp:Literal ID="lStats" runat="server"></asp:Literal>
                    </small>
                </div>
            </div>
        </asp:Panel>
        <Rock:ModalDialog runat="server" ID="mdEditRow">
            <Content>
                <iframe runat="server" id="iGroupMemberIframe" style="width: 100%; height: 500px; border: 0px;"></iframe>
            </Content>
        </Rock:ModalDialog>
    </ContentTemplate>
</asp:UpdatePanel>
