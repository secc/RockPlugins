<%@ Control Language="C#" AutoEventWireup="true" CodeFile="StaffPicker.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Purchasing.StaffPicker" %>

<asp:UpdatePanel ID="upStaffPicker" runat="server" >
    <ContentTemplate>
        <script type="text/javascript">


            function checkAll() {
                var isChecked = false;
                if ($("#[id*=chkAllItems]").attr("checked")) {
                    isChecked = true;
                }

                var table = $("#[id*=chkAllItems]").closest("table");
                if (isChecked == true) {
                    $("td input:checkbox", table).attr("checked", "true")
                }
                else {
                    $("td input:checkbox", table).removeAttr("checked");
                }
            }

            function chooseStaffMembers(delimPersonIDs, controlToUpdate, refreshButton) {
                $("#" + controlToUpdate).val(delimPersonIDs);
                $("#" + refreshButton).click();
            }
        </script>
        <asp:HiddenField ID="hdnPersonAliasId" runat="server" />
        <asp:Label ID="lblRequesterName" runat="server" />
        <asp:Button ID="btnChangeRequester" runat="server" CssClass="btn btn-default hidden-print" Text="..."
            Visible="false" OnClick="btnChangeRequester_Click" CausesValidation="false" />
        <asp:LinkButton ID="btnRemoveRequester" runat="server" CssClass="btn btn-default hidden-print" Visible="false" CausesValidation="false" OnClick="btnRemoveRequester_Click"><i class="fa fa-remove"></i></asp:LinkButton>
        <Rock:ModalDialog ID="mpStaffSearch" runat="server" Title="Staff Picker" Content-DefaultButton="btnSubmit">
            <Content>
                <asp:UpdatePanel ID="upStaffSearchMain" runat="server" >
                    <ContentTemplate>
                        <style>
                            #<%=mpStaffSearch.ClientID%>_modal_dialog_panel .modal-footer {display: none}
                        </style>
                        <asp:Panel runat="server" DefaultButton="btnSubmit" />
                        <div id="modelContent">
                        <h4><%= Title %></h4>
                            <div id="error" runat="server" class="alert alert-danger" Visible="false">
                                <asp:Label ID="lblError" runat="server" Visible="false" />
                            </div>
                            <div class="row">
                                <asp:Panel ID="pnlSearch" runat="server" DefaultButton="btnSubmit" class="col-md-4">
                                    <Rock:RockTextBox label="Name" ID="txtName" runat="server"/>
                                    <Rock:RockDropDownList label="Ministry" ID="ddlMinistry" runat="server" />
                                    <div runat="server" class="pull-right" style="margin-bottom: 10px">
                                        <asp:Button ID="btnSubmit" runat="server" CssClass="btn btn-primary" OnClick="btnSubmit_Click"
                                            Style="width: 100px;" CausesValidation="false" Text="Search"/>
                                    </div>
                                </asp:Panel>
                                <asp:Panel ID="pnlResults" runat="server" class="col-md-8">
                                    <div id="instructions" class="smallText">
                                        <asp:Label ID="lblInstructions" runat="server" CssClass="smallText" Visible="true" />
                                    </div>
                                    <Rock:Grid ID="dgSearchResults" runat="server" CssClass="list" AllowPaging="false"
                                        AllowSorting="true" OnItemDataBound="dgSearchResults_ItemDataBound" ShowActionRow="false" DataKeyNames="PersonAliasId">
                                        <Columns>
                                            <Rock:RockBoundField HeaderText="ID" DataField="PersonAliasId" Visible="false" />
                                            <Rock:RockBoundField HeaderText="Name" DataField="Name" />
                                            <Rock:RockTemplateField HeaderText="Name" ItemStyle-Wrap="false" Visible="false">
                                                <ItemTemplate>
                                                    <asp:PlaceHolder ID="phPersonName" runat="server" />
                                                </ItemTemplate>
                                            </Rock:RockTemplateField>
                                            <Rock:DefinedValueField HeaderText="Ministry Area" DataField="Ministry Area" SortExpression="Ministry Area" />
                                            <Rock:DefinedValueField HeaderText="Position" DataField="Position" SortExpression="Position" />
                                            <Rock:LinkButtonField Text="Select" CssClass="btn btn-primary" OnClick="SelectButton_Click"/>
                                        </Columns>
                                    </Rock:Grid>
                                </asp:Panel>
                            </div>
                            <div class="pull-right">
                                <a href="#" onclick="Rock.controls.modal.closeModalDialog($('#<%=mpStaffSearch.ClientID%>_modal_dialog_panel')); return false;" class="btn">Cancel</a>
                            </div>
                        </div>
                    </ContentTemplate>
                </asp:UpdatePanel>
            </Content>
        </Rock:ModalDialog>
    </ContentTemplate>
</asp:UpdatePanel>                
                