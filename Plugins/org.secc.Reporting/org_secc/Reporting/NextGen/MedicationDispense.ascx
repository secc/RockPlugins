<%@ Control Language="C#" AutoEventWireup="true" CodeFile="MedicationDispense.ascx.cs" Inherits="RockWeb.Blocks.Reporting.NextGen.MedicationDispense" %>
<asp:UpdatePanel ID="upReport" runat="server">
    <ContentTemplate>
        <Rock:ModalDialog runat="server" ID="mdNotes">
            <Content>
                <asp:HiddenField runat="server" ID="hfPersonId" />
                <div class="row">
                    <div class="col-md-12">
                        <Rock:Grid runat="server" ID="gNotes" ShowActionRow="false" DataKeyNames="Id" AllowPaging="false">
                            <Columns>
                                <asp:BoundField DataField="Text" HeaderText="Distribution" HtmlEncode="false" HtmlEncodeFormatString="false" />
                                <Rock:EditField HeaderText="Edit" ID="btnEdit" OnClick="btnEdit_Click" />
                                <Rock:DeleteField HeaderText="Remove" ID="btnDelete" OnClick="btnDelete_Click" />
                            </Columns>
                        </Rock:Grid>
                        <Rock:BootstrapButton runat="server" ID="btnAddNote" CssClass="pull-right btn btn-primary"
                            Text="<i class='fa fa-plus'></i>" OnClick="btnAddNote_Click" />
                    </div>
                </div>
            </Content>
        </Rock:ModalDialog>

        <Rock:ModalDialog runat="server" ID="mdEdit" OnSaveClick="mdEdit_SaveClick" CancelLinkVisible="false" SaveThenAddButtonText="Cancel"
            OnSaveThenAddClick="mdEdit_SaveThenAddClick" SaveButtonText="Save">
            <Content>
                <div class="row">
                    <div class="col-md-12">
                        <asp:HiddenField runat="server" ID="hfNoteId" />
                        <Rock:RockDropDownList runat="server" ID="ddlMatrixItem" Label="Medication"
                            DataValueField="Key" DataTextField="Value">
                        </Rock:RockDropDownList>
                        <Rock:DateTimePicker runat="server" ID="dtpDateTime" Required="true" Label="Date and Time" />
                    </div>
                </div>
            </Content>
        </Rock:ModalDialog>

        <Rock:ModalDialog runat="server" ID="mdCustomNotes" Title="Custom Notes">
            <Content>
                <asp:HiddenField runat="server" ID="hfCustomNotesPersonId" />
                <asp:Literal ID="ltCustomNotes" runat="server" />
                <br />
                <Rock:RockTextBox ID="tbCustomNotes" runat="server" TextMode="MultiLine" Rows="10" Label="Add Note"></Rock:RockTextBox>
                <Rock:BootstrapButton runat="server" ID="btnCustomNotes" Text="Add Note" CssClass="btn btn-primary" OnClick="btnCustomNotes_Click" />
            </Content>
        </Rock:ModalDialog>

        <Rock:NotificationBox runat="server" ID="nbAlert" Dismissable="true" NotificationBoxType="Danger" />
        <div class="panel panel-default">
            <div class="panel-heading">

                <div class="row">
                    <div class="col-md-9">
                        <div class="col-md-6">
                            <Rock:RockDropDownList runat="server" ID="ddlSchedule" DataTextField="Value" DataValueField="Guid"
                                Label="Schedule" AutoPostBack="true" OnSelectedIndexChanged="ddlSchedule_SelectedIndexChanged" />
                        </div>
                        <div class="col-md-6">
                            <Rock:RockTextBox runat="server" ID="tbName" Label="Name" AutoPostBack="true" OnTextChanged="tbName_TextChanged" />
                        </div>
                        <div class="col-md-6">
                            <Rock:CampusPicker runat="server" ID="cpCampus" Label="Campus" AutoPostBack="true" OnSelectedIndexChanged="cpCampus_SelectedIndexChanged" />
                        </div>
                        <asp:Panel CssClass="col-md-6" ID="pnlAttribute" runat="server">
                            <Rock:RockDropDownList runat="server" ID="ddlAttribute" AutoPostBack="true" DataTextField="Value" DataValueField="Key" OnSelectedIndexChanged="ddlAttribute_SelectedIndexChanged" />
                        </asp:Panel>
                    </div>
                    <div class="col-md-3">
                        <div class="col-md-12" style="padding: 7px 0px 15px 0px">
                            <Rock:DatePicker runat="server" ID="dpDate" Label="Day" OnTextChanged="dpDate_TextChanged" AutoPostBack="true" />
                        </div>
                        <div class="col-md-12">
                            <Rock:Toggle runat="server" ID="cbHideDistributed" Label="Hide Distributed"
                                OnText="Yes" OffText="No" OnCheckedChanged="cbHideDistributed_CheckedChanged" AutoPostBack="true" />
                        </div>
                    </div>
                </div>

            </div>
            <div class="panel-body">
                <Rock:Grid runat="server" ID="gGrid" TooltipField="History" DataKeyNames="Key" AllowSorting="true" OnRowSelected="gGrid_RowSelected">
                    <Columns>
                        <asp:BoundField DataField="Person" HeaderText="Person" SortExpression="Person" />
                        <asp:BoundField DataField="Medication" HeaderText="Medication" />
                        <asp:BoundField DataField="Instructions" HeaderText="Instructions" />
                        <asp:BoundField DataField="Schedule" HeaderText="Schedule" />
                        <Rock:BoolField DataField="Distributed" HeaderText="Distributed" ExcelExportBehavior="NeverInclude" />
                        <Rock:LinkButtonField Text="Distribute" CssClass="btn btn-default confirm" OnClick="Distribute_Click" ExcelExportBehavior="NeverInclude" />
                        <Rock:LinkButtonField Text="<i class='fa fa-sticky-note'></i>" CssClass="btn btn-default" OnClick="Note_Click" ExcelExportBehavior="NeverInclude" />
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
            var schedule = $(this).parent().siblings()[3].innerHTML;
            Rock.dialogs.confirmPreventOnCancel(e, "<b>Please Confirm</b> <br> Person: <span class=\"field-name\">" + person + "</span> <br> Medication: <span class=\"field-name\">" + medication + "</span> <br> Schedule: <span class=\"field-name\">" + schedule + "</span>");
        });
    }
</script>
