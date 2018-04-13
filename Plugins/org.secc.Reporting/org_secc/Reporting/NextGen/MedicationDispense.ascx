<%@ Control Language="C#" AutoEventWireup="true" CodeFile="MedicationDispense.ascx.cs" Inherits="RockWeb.Blocks.Reporting.NextGen.MedicationDispense" %>

<asp:UpdatePanel ID="upReport" runat="server">
    <ContentTemplate>
        <Rock:NotificationBox runat="server" ID="nbAlert" Dismissable="true" NotificationBoxType="Danger" />
        <div class="panel panel-default">
            <div class="panel-heading">

                <div class="row">
                    <div class="col-md-4">
                        <Rock:RockDropDownList runat="server" ID="ddlSchedule" DataTextField="Value" DataValueField="Guid" Label="Schedule" AutoPostBack="true" />
                    </div>
                    <div class="col-md-4"></div>
                    <div class="col-md-4">
                        <Rock:DatePicker runat="server" ID="dpDate" Label="Day" />
                    </div>
                </div>

            </div>
            <div class="panel-body">
                <Rock:Grid runat="server" ID="gGrid" TooltipField="History" DataKeyNames="Key">
                    <Columns>
                        <asp:BoundField DataField="Person" HeaderText="Person" />
                        <asp:BoundField DataField="Medication" HeaderText="Medication" />
                        <asp:BoundField DataField="Instructions" HeaderText="Instructions" />
                        <asp:BoundField DataField="Schedule" HeaderText="Schedule" />
                        <Rock:BoolField DataField="Distributed" HeaderText="Distributed" />
                        <Rock:LinkButtonField Text="Distribute" CssClass="btn btn-default" OnClick="Distribute_Click" />
                    </Columns>
                </Rock:Grid>
            </div>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>
