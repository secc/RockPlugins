<%@ Control Language="C#" AutoEventWireup="true" CodeFile="PaymentMethodList.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Purchasing.PaymentMethodList" %>

<asp:UpdatePanel ID="upMain" runat="server" class="panel panel-block">
    <ContentTemplate>
        <div class="panel-heading">
            <h1 class="panel-title"><i class="fa fa-credit-card"></i>&nbsp;Payment Methods</h1>
            <asp:placeholder ID="phNewVendor" runat="server">
                <a href="#" ID="lbNewVendor" class="btn-add btn btn-default btn-sm pull-right"><i class="fa fa-plus"></i> New Payment Method</a>
            </asp:placeholder>
        </div>
        <div class="panel-body">
            <div class="grid grid-panel">
                <Rock:GridFilter runat="server" OnApplyFilterClick="btnApplyFilter_Click" OnClearFilterClick="btnClearFilter_Click">
                    <Rock:RockTextBox ID="txtFilterPaymentMethodName" Label="Payment Method Name:" runat="server" MaxLength="100" Columns="50" />
                    <Rock:RockCheckBox ID="chkFilterCreditCard" Label="Payment Type:" runat="server" class="formItem" Text="Credit Cards Only" />
                    <Rock:RockCheckBox ID="chkActiveOnly" Label="Active/Inactive:" runat="server" AutoPostBack="true" OnCheckedChanged="chkActiveOnly_Changed" Checked="true" CssClass="formLabel" Text="Active Only" />
                </Rock:GridFilter>                        

                <Rock:Grid ID="dgPayMethods" runat="server" AllowSorting="true" AllowPaging="true" DataKeyNames="PaymentMethodID" CssClass="list" AutoGenerateColumns="false">
                    <Columns>
                        <Rock:RockBoundField HeaderText="ID" DataField="PaymentMethodID" />
                        <Rock:RockTemplateField SortExpression="Name" HeaderText="Name" >
                            <ItemTemplate>
                                <asp:LinkButton ID="lnkName" runat="server" Text='<%# Eval("Name") %>' CommandArgument='<%# Eval("PaymentMethodID") %>' />
                            </ItemTemplate>
                        </Rock:RockTemplateField>
                        <Rock:RockBoundField HeaderText="Description" DataField="Description" />
                        <Rock:BoolField DataField="IsCreditCard" HeaderText="Credit Card" SortExpression="IsCreditCard" />
                        <Rock:RockBoundField HeaderText="Owner" DataField="Owner" SortExpression="Owner" />
                        <Rock:RockBoundField HeaderText="Expiration Date" DataField="ExpirationDate" SortExpression="ExpirationDate" />
                        <Rock:BoolField HeaderText="Active" SortExpression="Active" DataField="ActiveString" />
                        <Rock:EditField HeaderText="Edit" OnClick="Edit_Click" />
                    </Columns>
                </Rock:Grid>
            </div>
        </div>

        <Rock:ModalDialog ID="pnlDetail" runat="server" Title="Payment Method Detail">
            <Content>
                        <style>
                            #<%=pnlDetail.ClientID%>_modal_dialog_panel .modal-footer {display: none}
                        </style>
                        <div ID="lblStatus" runat="server" CssClass="alert" />
                        <div class="row">
                            <div class="col-md-6">
                                <asp:HiddenField ID="hdnPaymentMethodId" runat="server" />
                                <div class="form-group">
                                    <label class="control-label">
                                        Name:
                                    </label>
                                    <div class="formItem">
                                        <asp:Label ID="lblName" runat="server" />
                                        <asp:TextBox ID="txtName" runat="server" Columns="50" CssClass="form-control" />
                                    </div>
                                </div>
                                <div class="form-group">
                                    <label class="control-label">
                                        Description:
                                    </label>
                                    <div class="formItem">
                                        <asp:Label ID="lblDescription" runat="server" />
                                        <asp:TextBox ID="txtDescription" runat="server" TextMode="MultiLine" Rows="3" CssClass="form-control" />
                                    </div>
                                </div>
                                <div class="form-group">
                                    <label class="control-label">
                                        Credit Card:
                                    </label>
                                    <div>
                                        <asp:Label ID="lblCreditCard" runat="server" />
                                        <Rock:Toggle ID="tglCreditCard" runat="server" OnCheckedChanged="tglCreditCard_Changed" OffText="No" OnText="Yes" />
                                        &nbsp;
                                    </div>
                                </div>
                            </div>
                            <div class="col-md-6">
                                <div id="pnlCC" runat="server" visible="true" >
                                    <div class="form-group">
                                        <label class="control-label">
                                            Account Owner:
                                        </label>
                                        <div class="formItem">
                                            <asp:Label ID="lblAccountOwner" runat="server" />
                                            <Rock:PersonPicker ID="pickerAccountOwner" runat="server" Visible="false" />
                                            &nbsp;                      
                                        </div>
                                    </div>
                                    <div class="form-group">
                                        <label class="control-label">
                                            Account Last Four:
                                        </label>
                                        <div class="formItem">
                                            <asp:Label ID="lblAccountLastFour" runat="server" />
                                            <asp:TextBox ID="txtAccountLastFour" runat="server" Columns="6" MaxLength="4" CssClass="form-control" />
                                        </div>
                                        <div class="form-group">
                                            <label class="control-label">
                                                Expriation Date:
                                            </label>
                                            <div class="formItem">
                                                <asp:Label ID="lblExpMonth" runat="server" /> <asp:DropDownList ID="ddlExpMonth" runat="server" CssClass="form-control" /> 
                                                <asp:Label ID="lblExpYear" runat="server" /> <asp:DropDownList ID="ddlExpYear" runat="server" CssClass="form-control" />
                                            </div>
                                        </div>
                                    </div>
                                    <div class="form-group">
                                        <label class="control-label">
                                            Active Flag:
                                        </label>
                                        <div>      
                                            <Rock:RockCheckBox ID="chkActive" runat="server" AutoPostBack="false" Text="Active"/>
                                        </div>

                                    </div>
                                </div>
                            </div>
                        </div>
                        <div class="clearfix">
                            <div class="pull-right">
                                <asp:button ID="btnEdit" runat="server" CommandName="Edit" Text="Save" CssClass="btn btn-primary" OnClick="btnEdit_Click" CausesValidation="false" />
                                <Rock:BootstrapButton ID="btnReset" runat="server" Text="Reset" CssClass="btn btn-default" OnClick="btnReset_Click" />
                                <Rock:BootstrapButton ID="btnReturn" runat="server" Text="Close" CssClass="btn btn-default" OnClick="btnReturn_Click" />
                            </div>
                        </div>
            </Content>
        </Rock:ModalDialog>
    </ContentTemplate>
</asp:UpdatePanel>