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

        <%-- Manage Medications Modal --%>
        <Rock:ModalDialog runat="server" ID="mdManageMeds" Title="Manage Medications" SaveButtonText="Done" OnSaveClick="mdManageMeds_SaveClick"
            CancelLinkVisible="false" CssClass="modal-manage-meds">
            <Content>
                <asp:HiddenField runat="server" ID="hfManageMedsPersonId" />
                <asp:HiddenField runat="server" ID="hfManageMedsMatrixGuid" />
                <div class="row">
                    <div class="col-md-12">
                        <Rock:Grid runat="server" ID="gMedications" ShowActionRow="false" DataKeyNames="Id" AllowPaging="false">
                            <Columns>
                                <asp:BoundField DataField="Medication" HeaderText="Medication" />
                                <asp:BoundField DataField="Instructions" HeaderText="Instructions" />
                                <asp:BoundField DataField="Schedule" HeaderText="Schedule" />
                            </Columns>
                        </Rock:Grid>
                        <div class="margin-t-sm text-muted small">
                            <i class="fa fa-info-circle"></i> Medications are managed through the registration process. Contact an administrator to make changes.
                        </div>
                    </div>
                </div>
            </Content>
        </Rock:ModalDialog>

        <%-- Add Medication Sub-Modal --%>
        <Rock:ModalDialog runat="server" ID="mdAddMedication" Title="Add Medication" SaveButtonText="Save" OnSaveClick="mdAddMedication_SaveClick"
            CancelLinkVisible="true" OnCancelScript="cancelAddMedication();" CssClass="modal-add-med">
            <Content>
                <div class="row">
                    <div class="col-md-12">
                        <Rock:RockTextBox runat="server" ID="tbNewMedication" Label="Medication" Required="true" />
                        <Rock:RockTextBox runat="server" ID="tbNewInstructions" Label="Instructions" />
                        <Rock:RockListBox runat="server" ID="lbNewSchedule" Label="Schedule"
                            DataValueField="Guid" DataTextField="Value" />
                    </div>
                </div>
            </Content>
        </Rock:ModalDialog>

        <asp:LinkButton runat="server" ID="btnCancelAddMed" OnClick="btnCancelAddMed_Click" Style="display:none;" />

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
                <div class="margin-b-sm">
                    <button type="button" id="btnDistributeSelectedUI" class="btn btn-primary btn-sm" disabled="disabled">
                        <i class="fa fa-check-square"></i> Distribute Selected
                    </button>
                    <asp:LinkButton runat="server" ID="btnDispenseSelected" OnClick="btnDispenseSelected_Click" Style="display:none" />
                </div>
                <Rock:Grid runat="server" ID="gGrid" TooltipField="History" DataKeyNames="Key" AllowSorting="true" OnRowSelected="gGrid_RowSelected">
                    <Columns>
                        <Rock:SelectField />
                        <asp:BoundField DataField="Person" HeaderText="Person" SortExpression="Person" />
                        <asp:BoundField DataField="SmallGroup" HeaderText="Small Group" />
                        <asp:BoundField DataField="SmallGroupLeader" HeaderText="Leader" />
                        <asp:BoundField DataField="Medication" HeaderText="Medication" />
                        <asp:BoundField DataField="Instructions" HeaderText="Instructions" />
                        <asp:BoundField DataField="Schedule" HeaderText="Schedule" />
                        <Rock:BoolField DataField="Distributed" HeaderText="Distributed" ExcelExportBehavior="NeverInclude" />
                        <Rock:LinkButtonField Text="Distribute" CssClass="btn btn-default confirm" OnClick="Distribute_Click" ExcelExportBehavior="NeverInclude" />
                        <Rock:LinkButtonField Text="<i class='fa fa-pills'></i>" CssClass="btn btn-default" HeaderText="Manage" OnClick="ManageMeds_Click" ExcelExportBehavior="NeverInclude" />
                        <Rock:LinkButtonField Text="<i class='fa fa-sticky-note'></i>" CssClass="btn btn-default" OnClick="Note_Click" ExcelExportBehavior="NeverInclude" />
                    </Columns>
                </Rock:Grid>
            </div>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>
<style>
    .modal-manage-meds .modal-header,
    .modal-add-med .modal-header {
        background: linear-gradient(135deg, #e8f4fd, #d0eafb);
        border-bottom: 2px solid #b8d9f0;
        border-radius: 5px 5px 0 0;
    }
    .modal-manage-meds .modal-header h3,
    .modal-add-med .modal-header h3,
    .modal-manage-meds .modal-header .modal-title,
    .modal-add-med .modal-header .modal-title {
        color: #2c6e9e;
        font-weight: 600;
    }
    .modal-manage-meds .modal-dialog,
    .modal-add-med .modal-dialog {
        position: absolute;
        top: 50%;
        left: 50%;
        transform: translate(-50%, -50%) !important;
        margin: 0;
        width: 600px;
        max-width: 90%;
    }
    .modal-manage-meds .modal-content,
    .modal-add-med .modal-content {
        border-radius: 8px;
        box-shadow: 0 8px 30px rgba(0, 0, 0, 0.2);
        overflow: hidden;
    }
    .modal-manage-meds .modal-header .close,
    .modal-add-med .modal-header .close {
        color: #2c6e9e;
        opacity: 0.6;
    }
    .modal-manage-meds .modal-header .close:hover,
    .modal-add-med .modal-header .close:hover {
        opacity: 1;
    }
    .modal-manage-meds .modal-footer,
    .modal-add-med .modal-footer {
        background: #fafafa;
        border-top: 1px solid #e8e8e8;
    }
    /* Style the grid inside manage meds modal */
    .modal-manage-meds .grid-table thead th {
        background: #f5f8fa !important;
        color: #2c6e9e !important;
        font-weight: 600;
        font-size: 12px;
        text-transform: uppercase;
        border-bottom: 2px solid #d0eafb !important;
    }
    .modal-manage-meds .grid-table tbody td {
        border-bottom: 1px solid #eee !important;
    }
    /* Style the add medication sub-modal similarly */
    .modal-add-med .modal-body .control-label {
        color: #2c6e9e;
        font-weight: 600;
    }

    /* Disable distribute button on already-distributed rows */
    .row-distributed .btn.confirm {
        pointer-events: none;
        opacity: 0.4;
        cursor: not-allowed;
    }
    .row-distributed td:first-child input[type='checkbox'] {
        pointer-events: none !important;
        opacity: 0.2 !important;
        cursor: not-allowed !important;
    }
    .row-distributed td:first-child {
        pointer-events: none !important;
    }

    /* Custom distribution history tooltip */
    .med-tooltip {
        position: absolute;
        z-index: 10600;
        background: #fff;
        border-radius: 6px;
        box-shadow: 0 4px 16px rgba(0,0,0,0.18);
        max-width: 360px;
        pointer-events: none;
        overflow: hidden;
    }
    .med-tooltip .tooltip-header {
        background: linear-gradient(135deg, #e8f4fd, #d0eafb);
        padding: 8px 14px;
        border-bottom: 2px solid #b8d9f0;
        font-weight: 600;
        color: #2c6e9e;
        font-size: 12px;
        display: flex;
        align-items: center;
        gap: 6px;
    }
    .med-tooltip .tooltip-body {
        padding: 10px 14px;
    }
    .med-tooltip .tooltip-body .tooltip-entry {
        padding: 5px 0;
        border-bottom: 1px solid #f0f0f0;
        font-size: 13px;
        color: #333;
    }
    .med-tooltip .tooltip-body .tooltip-entry:last-child {
        border-bottom: none;
    }
    .med-tooltip .tooltip-body .field-name {
        font-weight: 600;
        color: #2c6e9e;
    }

    /* Distribute confirmation modal */
    #bulk-confirm-overlay {
        display: none;
        position: fixed;
        top: 0;
        left: 0;
        width: 100vw;
        height: 100vh;
        background: rgba(0,0,0,0.45);
        z-index: 10500;
        align-items: center;
        justify-content: center;
    }
    #bulk-confirm-overlay.show {
        display: flex !important;
    }
    #bulk-confirm-box {
        background: #fff;
        border-radius: 8px;
        width: 520px;
        max-width: 90%;
        max-height: 80vh;
        box-shadow: 0 8px 30px rgba(0,0,0,0.25);
        overflow: hidden;
        display: flex;
        flex-direction: column;
        margin: auto;
    }
    #bulk-confirm-box .confirm-header {
        background: linear-gradient(135deg, #e8f4fd, #d0eafb);
        border-bottom: 2px solid #b8d9f0;
        padding: 14px 20px;
        display: flex;
        align-items: center;
        gap: 10px;
    }
    #bulk-confirm-box .confirm-header i {
        font-size: 20px;
        color: #2c6e9e;
    }
    #bulk-confirm-box .confirm-header h4 {
        margin: 0;
        color: #2c6e9e;
        font-weight: 600;
        font-size: 16px;
    }
    #bulk-confirm-box .confirm-body {
        padding: 16px 20px;
        overflow-y: auto;
        flex: 1;
    }
    #bulk-confirm-box .confirm-body p {
        margin: 0 0 10px;
        color: #555;
        font-size: 14px;
    }
    #bulk-confirm-box .confirm-body table {
        width: 100%;
        border-collapse: collapse;
    }
    #bulk-confirm-box .confirm-body table th {
        background: #f5f8fa;
        color: #2c6e9e;
        font-weight: 600;
        font-size: 12px;
        text-transform: uppercase;
        padding: 8px 10px;
        border-bottom: 2px solid #d0eafb;
        text-align: left;
    }
    #bulk-confirm-box .confirm-body table td {
        padding: 7px 10px;
        border-bottom: 1px solid #eee;
        font-size: 13px;
    }
    #bulk-confirm-box .confirm-body table tr:last-child td {
        border-bottom: none;
    }
    #bulk-confirm-box .confirm-footer {
        padding: 12px 20px;
        border-top: 1px solid #e8e8e8;
        display: flex;
        justify-content: flex-end;
        gap: 8px;
        background: #fafafa;
    }
    #bulk-confirm-box .confirm-footer .btn-cancel {
        background: #fff;
        border: 1px solid #ccc;
        color: #555;
        padding: 7px 18px;
        border-radius: 4px;
        cursor: pointer;
        font-size: 13px;
    }
    #bulk-confirm-box .confirm-footer .btn-cancel:hover {
        background: #f0f0f0;
    }
    #bulk-confirm-box .confirm-footer .btn-confirm {
        background: linear-gradient(135deg, #3a8fc2, #2c6e9e);
        border: none;
        color: #fff;
        padding: 7px 18px;
        border-radius: 4px;
        cursor: pointer;
        font-weight: 600;
        font-size: 13px;
    }
    #bulk-confirm-box .confirm-footer .btn-confirm:hover {
        background: linear-gradient(135deg, #2c6e9e, #1d5a84);
    }

</style>

<!-- Bulk distribute confirmation overlay -->
<div id="bulk-confirm-overlay">
    <div id="bulk-confirm-box">
        <div class="confirm-header">
            <i class="fa fa-medkit"></i>
            <h4>Confirm Distribution</h4>
        </div>
        <div class="confirm-body">
            <p>You are about to distribute the following medications:</p>
            <table>
                <thead><tr><th>Person</th><th>Medication</th><th>Schedule</th></tr></thead>
                <tbody id="bulk-confirm-list"></tbody>
            </table>
        </div>
        <div class="confirm-footer">
            <button type="button" class="btn-cancel" id="bulk-confirm-cancel">Cancel</button>
            <button type="button" class="btn-confirm" id="bulk-confirm-ok"><i class="fa fa-check"></i> Distribute</button>
        </div>
    </div>
</div>

<script>
    function cancelAddMedication() {
        // Notify server that Add Medication was cancelled so modal state stays clean
        var btn = document.getElementById('<%= btnCancelAddMed.ClientID %>');
        if (btn) btn.click();
    }

    function pageLoad() {
        var $btn = $("#btnDistributeSelectedUI");
        var $hiddenBtn = $("[id$='btnDispenseSelected']");
        var $grid = $("[id$='gGrid']");
        var $overlay = $("#bulk-confirm-overlay");

        // Find the Distributed column index dynamically and disable already-distributed rows
        if ($grid.length) {
            var distColIdx = -1;
            $grid.find("thead th").each(function (i) {
                if ($(this).text().trim().toLowerCase() === "distributed") {
                    distColIdx = i;
                }
            });
            if (distColIdx >= 0) {
                $grid.find("tbody tr").each(function () {
                    var $tds = $(this).find("td");
                    var $distCell = $tds.eq(distColIdx);
                    if ($distCell.find("i.fa-check, .fa-check").length > 0 || $distCell.text().trim().toLowerCase() === "true") {
                        $(this).addClass("row-distributed");
                        var $cb = $tds.eq(0).find("input[type='checkbox']");
                        $cb.prop("checked", false);
                    }
                });

                // Block clicks on distributed row checkboxes (scoped to grid only)
                $grid.off("click.distBlock").on("click.distBlock", "tr.row-distributed input[type='checkbox']", function (e) {
                    e.preventDefault();
                    $(this).prop("checked", false);
                    return false;
                });

                // After header "select all", uncheck distributed rows
                $grid.on("change", "thead input[type='checkbox']", function () {
                    setTimeout(function () {
                        $grid.find("tbody tr.row-distributed input[type='checkbox']").prop("checked", false);
                        updateDistributeBtn();
                    }, 0);
                });
            }
        }

        // Custom styled tooltips for distribution history
        $(".med-tooltip").remove();
        var $tooltip = $("<div class='med-tooltip'></div>").appendTo("body").hide();

        if ($grid.length) {
            $grid.find("tbody tr[title]").each(function () {
                var $row = $(this);
                var title = $row.attr("title");
                if (!title) return;
                $row.removeAttr("title").attr("data-med-history", title);
            });

            $grid.off("mouseenter.medtip mouseleave.medtip").on("mouseenter.medtip", "tbody tr[data-med-history]", function (e) {
                var history = $(this).attr("data-med-history");
                if (!history) return;
                var entries = history.split("<br>");
                var html = "<div class='tooltip-header'><i class='fa fa-history'></i> Distribution History</div><div class='tooltip-body'>";
                for (var i = 0; i < entries.length; i++) {
                    if (entries[i].trim()) {
                        html += "<div class='tooltip-entry'>" + entries[i] + "</div>";
                    }
                }
                html += "</div>";
                $tooltip.html(html).css({ top: e.pageY + 12, left: Math.min(e.pageX + 10, $(window).width() - 380) }).show();
            }).on("mouseleave.medtip", "tbody tr[data-med-history]", function () {
                $tooltip.hide();
            });
        }

        // Single-row confirm - use Rock's built-in confirm dialog (styled via CSS below)
        $(".confirm").off("click.singleConfirm").on("click.singleConfirm", function (e) {
            var $row = $(this).closest("tr");
            var $tds = $row.find("td");
            var colMap = {};
            $grid.find("thead th").each(function (i) {
                var t = $(this).text().trim().toLowerCase();
                if (t) colMap[t] = i;
            });
            var person = $tds.eq(colMap["person"] || 1).text().trim();
            var medication = $tds.eq(colMap["medication"] || 2).text().trim();
            var schedule = $tds.eq(colMap["schedule"] || 4).text().trim();
            var msg = "<div style='margin-bottom:12px;color:#555;'>You are about to distribute the following medication:</div>" +
                "<table style='width:100%;border-collapse:collapse;'>" +
                "<thead><tr>" +
                "<th style='background:#f5f8fa;color:#2c6e9e;font-weight:600;font-size:12px;text-transform:uppercase;padding:8px 10px;border-bottom:2px solid #d0eafb;text-align:left;'>Person</th>" +
                "<th style='background:#f5f8fa;color:#2c6e9e;font-weight:600;font-size:12px;text-transform:uppercase;padding:8px 10px;border-bottom:2px solid #d0eafb;text-align:left;'>Medication</th>" +
                "<th style='background:#f5f8fa;color:#2c6e9e;font-weight:600;font-size:12px;text-transform:uppercase;padding:8px 10px;border-bottom:2px solid #d0eafb;text-align:left;'>Schedule</th>" +
                "</tr></thead><tbody><tr>" +
                "<td style='padding:7px 10px;font-size:13px;'>" + $("<span>").text(person).html() + "</td>" +
                "<td style='padding:7px 10px;font-size:13px;'>" + $("<span>").text(medication).html() + "</td>" +
                "<td style='padding:7px 10px;font-size:13px;'>" + $("<span>").text(schedule).html() + "</td>" +
                "</tr></tbody></table>";
            Rock.dialogs.confirmPreventOnCancel(e, msg);
        });

        // Enable/disable Distribute Selected button based on checkbox selection (excluding distributed rows)
        function updateDistributeBtn() {
            if (!$grid.length) return;
            var anyChecked = $grid.find("tbody tr:not(.row-distributed) input[type='checkbox']:checked").length > 0;
            $btn.prop("disabled", !anyChecked);
        }

        if ($grid.length) {
            $grid.on("change", "input[type='checkbox']", function () {
                updateDistributeBtn();
            });
        }

        updateDistributeBtn();

        // Intercept bulk distribute click to show confirmation
        $btn.off("click.confirm").on("click.confirm", function (e) {
            e.preventDefault();
            e.stopImmediatePropagation();

            var rows = [];
            if ($grid.length) {
                // Find column indexes by header text so hidden columns don't break things
                var colMap = {};
                $grid.find("thead th").each(function (i) {
                    var text = $(this).text().trim().toLowerCase();
                    if (text) colMap[text] = i;
                });
                var personIdx = colMap["person"] || 1;
                var medIdx = colMap["medication"] || 2;
                var schedIdx = colMap["schedule"] || 4;

                $grid.find("tbody tr").each(function () {
                    var $cb = $(this).find("input[type='checkbox']");
                    if ($cb.is(":checked")) {
                        var $tds = $(this).find("td");
                        rows.push({
                            person: $tds.eq(personIdx).text().trim(),
                            medication: $tds.eq(medIdx).text().trim(),
                            schedule: $tds.eq(schedIdx).text().trim()
                        });
                    }
                });
            }

            if (rows.length === 0) return;

            var $list = $("#bulk-confirm-list").empty();
            for (var i = 0; i < rows.length; i++) {
                $list.append("<tr><td>" + $("<span>").text(rows[i].person).html() +
                    "</td><td>" + $("<span>").text(rows[i].medication).html() +
                    "</td><td>" + $("<span>").text(rows[i].schedule).html() + "</td></tr>");
            }

            $overlay.addClass("show");
        });

        // Cancel
        $("#bulk-confirm-cancel").off("click").on("click", function () {
            $overlay.removeClass("show");
        });

        // Confirm - trigger the real postback via hidden server button
        $("#bulk-confirm-ok").off("click").on("click", function () {
            $overlay.removeClass("show");
            __doPostBack('<%= btnDispenseSelected.UniqueID %>', '');
        });
    }
</script>
