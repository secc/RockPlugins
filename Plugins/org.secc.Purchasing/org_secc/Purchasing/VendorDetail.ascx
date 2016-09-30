<%@ Control Language="C#" AutoEventWireup="true" CodeFile="VendorDetail.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Purchasing.VendorDetail" %>
<asp:UpdatePanel ID="updVendor" runat="server">
    <ContentTemplate>
        <script>
            function setHeight()
            {
                parent.document.getElementById('modal-popup_iframe').style.height = document['body'].offsetHeight + 'px';
            }
            $(document).ready(setHeight);
        </script>
        <style>
            .modal-content.rock-modal .modal-footer, .rock-modal .modal-content .modal-footer {
                height: 50px;
            }
        </style>
        <div Class="alert" ID="lblStatus" runat="server" Visible="false" />        
        <Rock:RockTextBox Label="Vendor Name:" id="txtVendorName" runat="server" MaxLength="100" Columns="50" /></td>
        <div class="row">
            <div class="col-sm-6">
                <Rock:RockTextBox Label="Address:" ID="txtAddressStreet1" runat="server" />
                <div class="row">
                    <div class="col-sm-5">
                        <Rock:RockTextBox Label="City:&nbsp;" ID="txtAddressCity" runat="server" />
                    </div>
                    <div class="col-sm-3">
                        <Rock:RockTextBox Label="State:&nbsp;" ID="txtAddressState" runat="server" columns="2" />
                    </div>
                    <div class="col-sm-4">
                        <Rock:RockTextBox Label="Zip:&nbsp;" ID="txtAddressZip" runat="server" columns="10" />
                    </div>
                </div>
                <div class="row">
                    <div class="col-xs-4">
                        <label class="form-label">Phone:&nbsp;</label>
                        <div class="form-inline">
                            <Rock:RockTextBox ID="txtPhone" runat="server" />

                        </div>
                    </div>
                    <div class="col-xs-2">
                        <Rock:RockTextBox Label="Ext:&nbsp;" ID="txtExt" runat="server" Columns="3"/>
                    </div>
                </div>
            </div>
            <div class="col-sm-6">
                <Rock:RockTextBox Label="Web Address:&nbsp;" ID="txtWebAddress" runat="server" />
                <Rock:RockTextBox Label="Terms:" ID="txtTerms" runat="server" />
                <Rock:RockCheckBox Label="Active:" ID="chkActiveEdit" runat="server" Checked="true"/>
            </div>
        </div>
        <div class="pull-right" style="margin-top: 30px;">
            <asp:Button id="btnSave" runat="server" Text="Save" CssClass="btn btn-primary" OnClick="btnSave_click" />
            <asp:Button ID="btnReset" runat="server" Text="Reset" CssClass="btn btn-default" OnClick="btnReset_click" />
            <a href="#" class="btn btn-default" OnClick="window.parent.Rock.controls.modal.close($(this).closest('.modal-content').find('.modal-close-message').first().val()); window.parent.location.reload(false); return false;">Cancel</a>

        </div>
    </ContentTemplate>
</asp:UpdatePanel>