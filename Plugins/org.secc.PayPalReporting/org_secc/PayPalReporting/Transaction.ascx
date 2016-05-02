<%@ Control Language="C#" AutoEventWireup="true" CodeFile="Transaction.ascx.cs" Inherits="RockWeb.Plugins.org_secc.PayPalReporting.Transaction" %>

<div class="panel panel-block">
    <div class="panel-heading">
        <h1 class="panel-title"><i class="fa fa-money"></i> PayPal Reporting Transaction</h1>
    </div>
    <div class="panel-body">
        <Rock:RockUpdatePanel id="pnlDetails" runat="server" >
            <ContentTemplate>
                    <div class="row">
                        <div class="col-md-6">
                            <Rock:RockLiteral runat="server" id="lGatewayTransactionId" Label="Gateway Transaction Id"></Rock:RockLiteral>
                            <Rock:RockLiteral runat="server" id="lMerchantTransactionId" Label="Merchant Transaction Id"></Rock:RockLiteral>
                
                            <label class="control-label">Name</label>
                            <div class="form-inline">
                                <asp:literal runat="server" id="lBillingFirstName"></asp:literal>
                                <asp:literal runat="server" id="lBillingLastName"></asp:literal>
                            </div>
                            <br />
                            <Rock:RockLiteral runat="server" id="lAmount" Label="Amount"></Rock:RockLiteral>
                            <Rock:RockLiteral runat="server" id="lFees" Label="Fees"></Rock:RockLiteral>
                            <Rock:RockLiteral runat="server" id="lTimeCreated" Label="Transaction Date/Time"></Rock:RockLiteral>
                        </div>
                        <div class="col-md-6">
                            <Rock:RockLiteral runat="server" id="lTenderType" Label="Tender Type"></Rock:RockLiteral>
                            <Rock:RockLiteral runat="server" id="lType" Label="Type"></Rock:RockLiteral>
                            <Rock:RockLiteral runat="server" id="lComment1" Label="Comment 1"></Rock:RockLiteral>
                            <Rock:RockLiteral runat="server" id="lComment2" Label="Comment 2"></Rock:RockLiteral>
                            <Rock:RockLiteral runat="server" id="lBatchId" Label="Batch ID"></Rock:RockLiteral>
                            <Rock:RockLiteral runat="server" id="lIsZeroFee" Label="Is Zero Fee"></Rock:RockLiteral>
                        </div>
                    </div>
       
            </ContentTemplate>
        </Rock:RockUpdatePanel>

        <Rock:RockUpdatePanel id="pnlEdit" runat="server" Visible="false">
            <ContentTemplate>
                <asp:HiddenField ID="hdnTransactionId" runat="server" />
                <div class="row">
                    <div class="col-md-6">
                        <Rock:RockTextBox runat="server" id="tbGatewayTransactionId" Label="Gateway Transaction Id"></Rock:RockTextBox>
                        <Rock:RockTextBox runat="server" id="tbMerchantTransactionId" Label="Merchant Transaction Id"></Rock:RockTextBox>
                
                        <label class="control-label">Name</label>
                        <div class="form-inline">
                            <Rock:RockTextBox runat="server" id="tbBillingFirstName"></Rock:RockTextBox>
                            <Rock:RockTextBox runat="server" id="tbBillingLastName"></Rock:RockTextBox>
                        </div>
                        <br />
                        <Rock:CurrencyBox runat="server" id="tbAmount" Label="Amount"></Rock:CurrencyBox>
                        <Rock:CurrencyBox runat="server" id="tbFees" Label="Fees"></Rock:CurrencyBox>
                        <Rock:DateTimePicker runat="server" id="tbTimeCreated" Label="Transaction Date/Time"></Rock:DateTimePicker>
                    </div>
                    <div class="col-md-6">
                        <Rock:RockTextBox runat="server" id="tbTenderType" Label="Tender Type"></Rock:RockTextBox>
                        <Rock:RockTextBox runat="server" id="tbType" Label="Type"></Rock:RockTextBox>
                        <Rock:RockTextBox runat="server" id="tbComment1" Label="Comment 1"></Rock:RockTextBox>
                        <Rock:RockTextBox runat="server" id="tbComment2" Label="Comment 2"></Rock:RockTextBox>
                        <Rock:RockTextBox runat="server" id="tbBatchId" Label="Batch ID"></Rock:RockTextBox>
                        <Rock:RockCheckBox runat="server" id="tbIsZeroFee" Label="Is Zero Fee"></Rock:RockCheckBox>
                    </div>
                </div>
            </ContentTemplate>
        </Rock:RockUpdatePanel>
        <Rock:BootstrapButton CssClass="btn btn-primary" ID="btnEdit" runat="server" Text="Edit" OnClick="Edit_Click" />
        <Rock:BootstrapButton CssClass="btn btn-primary" ID="btnSave" runat="server" Text="Save" OnClick="Save_Click" Visible="false" />
    </div>

</div>