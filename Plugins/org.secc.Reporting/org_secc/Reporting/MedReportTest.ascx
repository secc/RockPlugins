<%@ Control Language="C#" AutoEventWireup="true" CodeFile="MedReportTest.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Reporting.MedReportTest" %>
<asp:UpdatePanel ID="upMain" runat="server">
    <ContentTemplate>
        <div class="row">
            <div class="col-md-12">
                <h3>Test Medication Report</h3>
            </div>
            <div class="col-md-12">
                <Rock:Grid ID="gMedications" runat="server" RowItemText="Medication">
                    <Columns>
                        <Rock:RockBoundField DataField="FullName" HeaderText="Name" />
                        <Rock:RockBoundField DataField="BirthDate" HeaderText="BirthDate" DataFormatString="{0:d}" />
                        <Rock:RockBoundField DataField="Medication" HeaderText="Medication" />
                        <Rock:RockBoundField DataField="Instructions" HeaderText="Instructions" />
                        <Rock:BoolField DataField="Breakfast" HeaderText="Breakfast" />
                        <Rock:BoolField DataField="Lunch" HeaderText="Lunch" />
                        <Rock:BoolField DataField="Dinner" HeaderText="Dinner" />
                        <Rock:BoolField DataField="Bedtime" HeaderText="Bedtime" />
                        <Rock:BoolField DataField="AsNeeded" HeaderText="As Needed" />
                    </Columns>
                </Rock:Grid> 
            </div>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>