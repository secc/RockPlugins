<%@ Control Language="C#" AutoEventWireup="true" CodeFile="BibleAndBeach.ascx.cs" Inherits="RockWeb.Blocks.Reporting.NextGen.BibleAndBeach" %>

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
                    <h1 class="panel-title"><i class="fa fa-bus"></i> Bible & Beach Trip Manager</h1>
                </div>
                <Rock:GridFilter runat="server" ID="gfReport" OnApplyFilterClick="gfReport_ApplyFilterClick" OnClearFilterClick="gfReport_ClearFilterClick">
                    <Rock:RockTextBox Label="Person Name" id="txtPersonName" runat="server" />
                    <Rock:RockDropDownList Label="POA Received:" ID="ddlPOA" runat="server" />
                    <Rock:NumberRangeEditor Label="Balance Owed" ID="nreBalanceOwed" runat="server" />
                </Rock:GridFilter>
                <Rock:Grid ID="gReport" runat="server" AllowSorting="true" EmptyDataText="No Results" DataKeyNames="Id, RegistrationId" OnRowSelected="gReport_RowSelected" ExportSource="ColumnOutput">
                    <Columns>
                        <Rock:PersonField DataField="Person" HeaderText="Person" SortExpression="Person.LastName" />
                        <Rock:RockBoundField DataField="Person.Age" HeaderText="Age" SortExpression="Person.Age"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="DOB" HeaderText="DOB (Person)" SortExpression="DOB"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="Registrant.BirthDate" HeaderText="DOB (Registration)" SortExpression="Registrant.BirthDate"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="Address" HeaderText="Address" SortExpression="Address"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="Email" HeaderText="Email" SortExpression=""></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="Gender" HeaderText="Gender" SortExpression=""></Rock:RockBoundField> 
                        <Rock:RockBoundField DataField="Person.GraduationYear" HeaderText="Graduation Year (Person)" SortExpression="Person.GraduationYear"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="Registrant.GraduationYear" HeaderText="Graduation Year (Registration)" SortExpression="Registrant.GraduationYear"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="HomePhone" HeaderText="Home Phone" SortExpression="HomePhone"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="CellPhone" HeaderText="Cell Phone" SortExpression="CellPhone"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="Registrant.ParentName" HeaderText="Parent/Guardian Name" SortExpression="Registrant.ParentName"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="Registrant.ParentCell" HeaderText="Parent/Guardian Cell" SortExpression="Registrant.ParentCell"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="Registrant.EmName" HeaderText="Emergency Contact Name" SortExpression="Registrant.EmName"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="Registrant.EmCell" HeaderText="Emergency Contact Cell" SortExpression="Registrant.EmCell"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="Registrant.School" HeaderText="School" SortExpression="Registrant.School"></Rock:RockBoundField>
                        <Rock:BoolField DataField="Registrant.FirstBB" HeaderText="1st B&B" HeaderStyle-HorizontalAlign="Center" SortExpression="Registrant.FirstBB"></Rock:BoolField>
                        <Rock:RockBoundField DataField="Registrant.Church" HeaderText="Home Church" SortExpression="Registrant.Church"></Rock:RockBoundField>
                        <Rock:BoolField DataField="Registrant.GroupRoom" HeaderText="Room with Group" SortExpression="Registrant.GroupRoom"></Rock:BoolField>
                        <Rock:RockBoundField DataField="Registrant.Roommate" HeaderText="Roommate 1" SortExpression="Registrant.Roommate"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="Registrant.Roommate2" HeaderText="Roommate 2" SortExpression="Registrant.Roommate2"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="Registrant.TShirtSize" HeaderText="T&#8209;Shirt Size" SortExpression="Registrant.TShirtSize"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="Registrant.DietaryInfo" HeaderText="Dietary/Medical Info" SortExpression="Registrant.DietaryInfo"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="LegalRelease" HeaderText="Legal Release" SortExpression=""></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="GroupMember.POA" HeaderText="POA" SortExpression="GroupMember.POA"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="Registrant.BalanceOwed" HeaderText="Balance Owed" DataFormatString="{0:C}" SortExpression="Registrant.BalanceOwed"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="GroupMember.RoomCode" HeaderText="Room Code" SortExpression="GroupMember.RoomCode"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="GroupMember.Bus" HeaderText="Bus" SortExpression="GroupMember.Bus"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="Departure" HeaderText="Departure" SortExpression=""></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="GroupMember.FamilyGroup" HeaderText="Family Group" SortExpression="GroupMember.FamilyGroup"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="Campus" HeaderText="Campus" SortExpression=""></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="Role" HeaderText="Role" SortExpression=""></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="GroupMember.GeneralNotes" HeaderText="General Notes" SortExpression="GroupMember.GeneralNotes"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="GroupMember.RoomingNotes" HeaderText="Rooming Notes" SortExpression="GroupMember.RoomingNotes"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="GroupMember.MedicalNotes" HeaderText="Medical Notes" SortExpression="GroupMember.MedicalNotes"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="GroupMember.TravelNotes" HeaderText="Travel Notes" SortExpression="GroupMember.TravelNotes"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="GroupMember.TravelExceptions" HeaderText="Travel Exceptions" SortExpression="GroupMember.TravelExceptions"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="HSMGroup" HeaderText="HSM Group" SortExpression=""></Rock:RockBoundField>
                        <Rock:LinkButtonField HeaderText="Registration" cssclass="btn btn-default btn-sm fa fa-file-image-o" OnClick="btnRegistration_Click" HeaderStyle-HorizontalAlign="Center" />
                    </Columns>
                </Rock:Grid>
            </div>
        </asp:Panel>
        <Rock:ModalDialog runat="server" ID="mdEditRow">
            <Content>
                <iframe runat="server" id="iGroupMemberIframe" style="width: 100%; height:500px; border:0px;"></iframe>
            </Content>
        </Rock:ModalDialog>
    </ContentTemplate>
</asp:UpdatePanel>
