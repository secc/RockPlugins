<%@ Control Language="C#" AutoEventWireup="true" CodeFile="MedicationDispense.ascx.cs" Inherits="RockWeb.Blocks.Reporting.NextGen.MedicationDispense" %>

<asp:UpdatePanel ID="upReport" runat="server">
    <ContentTemplate>
        <Rock:NotificationBox runat="server" ID="nbAlert" Dismissable="true" NotificationBoxType="Danger" />
        <div class="panel panel-default">
            <div class="panel-heading">

                <div class="row">
                    <div class="col-md-4">
                        <Rock:RockDropDownList runat="server" ID="ddlSchedule" DataTextField="Value" DataValueField="Guid"
                            Label="Schedule" AutoPostBack="true" OnSelectedIndexChanged="ddlSchedule_SelectedIndexChanged" />
                    </div>
                    <div class="col-md-4">
                        <Rock:RockTextBox runat="server" ID="tbName" Label="Name" AutoPostBack="true" OnTextChanged="tbName_TextChanged" />
                    </div>
                    <div class="col-md-4">
                        <Rock:DatePicker runat="server" ID="dpDate" Label="Day" OnTextChanged="dpDate_TextChanged" AutoPostBack="true" />
                    </div>
                </div>

            </div>
            <div class="panel-body">
                <Rock:Grid runat="server" ID="gGrid" TooltipField="History" DataKeyNames="Key" ShowActionRow="false">
                    <Columns>
                        <asp:BoundField DataField="Person" HeaderText="Person" />
                        <asp:BoundField DataField="Medication" HeaderText="Medication" />
                        <asp:BoundField DataField="Instructions" HeaderText="Instructions" />
                        <asp:BoundField DataField="Schedule" HeaderText="Schedule" />
                        <Rock:BoolField DataField="Distributed" HeaderText="Distributed" />
                        <Rock:LinkButtonField Text="Distribute" CssClass="btn btn-default confirm" OnClick="Distribute_Click" />
                    </Columns>
                </Rock:Grid>
            </div>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>
<script>
    function pageLoad() {
        $(".confirm").click(function (e) {
            var person = $(this).parent().siblings()[0].innerHTML;
            var medication = $(this).parent().siblings()[1].innerHTML;
            var schedule =  $(this).parent().siblings()[3].innerHTML;
            Rock.dialogs.confirmPreventOnCancel(e, "<b>Please Confirm</b> <br> Person: <span class=\"field-name\">" + person + "</span> <br> Medication: <span class=\"field-name\">" + medication + "</span> <br> Schedule: <span class=\"field-name\">" + schedule + "</span>");
        });
    }
</script>