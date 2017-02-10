<%@ Control Language="C#" AutoEventWireup="true" CodeFile="RequisitionList.ascx.cs"
    Inherits="RockWeb.Plugins.org_secc.Purchasing.RequisitionList" %>
<%@ Register Src="~/Plugins/org_secc/Purchasing/StaffSearch.ascx" TagPrefix="secc" TagName="StaffSearch" %>
<%@ Register Src="StaffPicker.ascx" TagName="StaffPicker" TagPrefix="secc" %>
<asp:UpdatePanel ID="upMain" runat="server" UpdateMode="Conditional" class="panel panel-block">
    <ContentTemplate>
        <div class="panel-heading">
            <h1 class="panel-title"><i class="fa fa-file-text"></i>&nbsp;Requisition List</h1>
            <Rock:BootstrapButton runat="server" CssClass="btn-add btn btn-default btn-sm pull-right" id="btnAdd" OnClick="lbCreateRequisition_Click"><i class="fa fa-plus"></i>&nbsp;Add Requisition</Rock:BootstrapButton>
        </div>
        <div class="panel-body">
            <div class="grid grid-panel">
                <Rock:GridFilter runat="server" ID="gfRequisitions" OnApplyFilterClick="btnFilterApply_Click">
                    <asp:Panel runat="server">
                        <label>Status</label><br />
                        <a onclick="$('input[id*=\'cbListStatus\'][type=checkbox]').prop('checked', true); return false;" href="#">Check All</a>&nbsp;
                        <a onclick="$('input[id*=\'cbListStatus\'][type=checkbox]').prop('checked', false); return false;" href="#">Uncheck All</a>
                        <Rock:RockCheckBoxList ID="cbListStatus" runat="server" RepeatColumns="5" RepeatDirection="horizontal"
                            CssClass="smallText" TextAlign="Right" />
                    </asp:Panel>
                    <Rock:RockCheckBoxList ID="cbListType" runat="server" RepeatColumns="5" RepeatDirection="Horizontal"
                        CssClass="smallText" TextAlign="Right" Label="Type"/>
                    <Rock:RockCheckBoxList ID="cbShow" runat="server" CssClass="smallText" RepeatColumns="5"
                        RepeatDirection="Horizontal" Label="Show">
                        <asp:ListItem Text="Requested by Me" Value="Me" />
                        <asp:ListItem Text="Requested by My Ministry Team" Value="Ministry" />
                        <asp:ListItem Text="I am an approver" Value="Approver" />
                    </Rock:RockCheckBoxList>
                    <Rock:RockDropDownList ID="ddlLocation" runat="server" CssClass="smallText" Label="Location"/>
                    <Rock:RockDropDownList ID="ddlMinistry" runat="server" CssClass="smallText" Label="Ministry" />
                    <Rock:RockTextBox ID="txtPONumber" runat="server" Label="PO Number"/>
                
                    <Rock:DateRangePicker ID="txtFilterSubmitted" runat="server" Visible="true" Label="Submitted On" />
                    <asp:Panel runat="server" CssClass="form-group" ID="pnlRequester">
                        <label>Requester:</label>
                        <secc:StaffPicker ID="hfFilterSubmittedBy" runat="server" AllowMultipleSelections="false" 
                                                ShowPersonDetailLink="true" ShowPhoto="true" UserCanEdit="true"/>
                    </asp:Panel>
                    <Rock:RockCheckBox ID="chkShowInactive" runat="server" CssClass="smallText" Label="Show Inactive" />
                </Rock:GridFilter>
                <Rock:Grid ID="dgRequisitions" runat="server" CssClass="list" AllowPaging="true"
                    AllowSorting="true" DataKeyField="RequisitionID" AutoGenerateColumns="false">
                    <Columns>
                        <Rock:RockBoundField HeaderText="RequisitionID" DataField="RequisitionID" Visible="false" />
                        <Rock:RockTemplateField HeaderText="Req. Title" SortExpression="Title">                     
                            <ItemTemplate>
                                <asp:HyperLink runat="server" text='<%# Eval("Title") %>' NavigateUrl='<%# String.Format("{0}?RequisitionID={1}", RequisitionDetailPageSetting, Eval("RequisitionID")) %>'></asp:HyperLink> 
                            </ItemTemplate>
                        </Rock:RockTemplateField>
                        <Rock:RockBoundField HeaderText="Type" DataField="RequisitionType" SortExpression="RequisitionType" />
                        <Rock:RockBoundField HeaderText="Requester" DataField="Requester_Last_First" SortExpression="RequesterLastFirst" />
                        <Rock:RockBoundField HeaderText="Status" DataField="Status" SortExpression="Status" />
                        <Rock:RockBoundField HeaderText="Items" DataField="ItemCount" HeaderStyle-HorizontalAlign="Center"
                            SortExpression="ItemCount" ItemStyle-HorizontalAlign="Center" />
                        <Rock:RockBoundField HeaderText="Notes" DataField="NoteCount" HeaderStyle-HorizontalAlign="Center"
                            SortExpression="NoteCount" ItemStyle-HorizontalAlign="Center" />
                        <Rock:RockBoundField HeaderText="Attachments" DataField="AttachmentCount" HeaderStyle-HorizontalAlign="Center"
                            SortExpression="AttachmentCount" ItemStyle-HorizontalAlign="Center" />
                        <Rock:RockBoundField HeaderText="Submitted On" DataField="DateSubmitted" DataFormatString="{0:d}"
                            SortExpression="DateSubmitted" HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center" />
                        <Rock:BoolField HeaderText="Express Shipping" HeaderStyle-HorizontalAlign="Center"
                            ItemStyle-HorizontalAlign="Center" ItemStyle-VerticalAlign="Middle" DataField="IsExpedited"
                            SortExpression="IsExpedited" />
                        <Rock:BoolField HeaderText="Approved" HeaderStyle-HorizontalAlign="Center"
                            ItemStyle-HorizontalAlign="Center" ItemStyle-VerticalAlign="Middle" DataField="IsApproved"
                            SortExpression="IsApproved" />
                        <Rock:BoolField HeaderText="Accepted" HeaderStyle-HorizontalAlign="Center"
                            ItemStyle-HorizontalAlign="Center" ItemStyle-VerticalAlign="Middle" DataField="IsAccepted"
                            SortExpression="IsAccepted" />
                    </Columns>
                </Rock:Grid>
            </div>
        </div>
        <secc:StaffSearch ID="ucStaffSearch" runat="server" AllowMultipleSelections="false"
            ShowPersonDetailLink="false" />
        <script type="text/javascript">
            // Expand the filters area
            var expandFilters = function ()
            {
                el = $('.grid-filter header').get();
                $('i.toggle-filter', el).toggleClass('fa-chevron-down fa-chevron-up');
                $(el).siblings('div').slideDown(0);
            }
            $(document).ready(expandFilters);
            Sys.WebForms.PageRequestManager.getInstance().add_endRequest(expandFilters);
        </script>
    </ContentTemplate>
</asp:UpdatePanel>
