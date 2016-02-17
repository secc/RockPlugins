<%@ Control Language="C#" AutoEventWireup="true" CodeFile="StaffSearch.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Purchasing.StaffSearch" %>

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
<Rock:ModalDialog ID="mpStaffSearch" runat="server" Title="Staff Search">
    <Content>
        <asp:UpdatePanel ID="upStaffSearchMain" runat="server">
            <ContentTemplate>
                <div id="modelContent" style="width: 500px; max-height:600px;">
                <h3><%= Title %></h3>
                    <div id="instructions" class="smallText">
                        <asp:Label ID="lblInstructions" runat="server" CssClass="smallText" Visible="true" />
                    </div>
                    <div id="error" runat="server">
                        <asp:Label ID="lblError" runat="server" Visible="false" CssClass="smallText" style="color:Red;" />
                    </div>
                    <asp:Panel ID="pnlSearch" runat="server" DefaultButton="btnSubmit" Visible="false"  >
                        <div id="grid">
                            <div class="row">
                                <div class="col1 formLabel">
                                    Name</div>
                                <div class="col2 formItem">
                                    <asp:TextBox ID="txtName" runat="server" style="width:200px;"/>
                                </div>
                            </div>
                            <div class="row">
                                <div class="col1 formLabel">
                                    Ministry:</div>
                                <div class="col2 formItem">
                                    <asp:DropDownList ID="ddlMinistry" runat="server" CssClass="smallText" style="width:200px;" />
                                </div>
                            </div>
                        </div>
                    </asp:Panel>
                    <asp:Panel ID="pnlResults" runat="server" Visible="false" >
                        <Rock:Grid ID="dgSearchResults" runat="server" CssClass="list" AllowPaging="true" PageSize="15" 
                            AllowSorting="true" OnItemDataBound="dgSearchResults_ItemDataBound">
                            <Columns>
                                <Rock:RockBoundField HeaderText="ID" DataField="person_id" Visible="false" />
                                <Rock:RockTemplateField HeaderStyle-CssClass="reportHeader" HeaderStyle-VerticalAlign="Bottom" Visible="true"
                                    HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center" ItemStyle-VerticalAlign="Top"
                                    ItemStyle-Wrap="false">
                                    <HeaderTemplate>
                                        <asp:CheckBox ID="chkAllItems" runat="server"
                                            onClick="checkAll();" />
                                    </HeaderTemplate>
                                    <ItemTemplate>
                                        <asp:CheckBox ID="chkItem" runat="server" />
                                        <asp:RadioButton ID="rdoItem" runat="server" />
                                    </ItemTemplate>
                                </Rock:RockTemplateField>
                                <Rock:RockBoundField HeaderText="Name" DataField="full_name_last_first" />
                                <Rock:RockTemplateField HeaderText="Name" ItemStyle-Wrap="false" Visible="false">
                                    <ItemTemplate>
                                        <asp:PlaceHolder ID="phPersonName" runat="server" />
                                    </ItemTemplate>
                                </Rock:RockTemplateField>
                                <Rock:RockBoundField HeaderText="Ministry Area" DataField="ministry_area" SortExpression="ministry_area" />
                                <Rock:RockBoundField HeaderText="Position" DataField="position" SortExpression="position" />
                            </Columns>
                        </Rock:Grid>
                    </asp:Panel>
                    <div style="text-align: right; padding-top:3px;">
                        <asp:Button ID="btnSubmit" runat="server" CssClass="smallText" OnClick="btnSubmit_Click"
                            Style="width: 100px;" />
                        <asp:Button ID="btnClear" runat="server" CssClass="smallText" OnClick="btnClear_Click"
                            Style="width: 100px;" />
                        <asp:Button ID="btnCancel" runat="server" Text="Cancel"  CssClass="smallText" OnClick="btnCancel_click" style="width:100px;" />
                    </div>
                </div>
            </ContentTemplate>
        </asp:UpdatePanel>
    </Content>
</Rock:ModalDialog>
