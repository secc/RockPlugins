<%@ Control Language="C#" AutoEventWireup="true" CodeFile="VendorSelect.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Purchasing.VendorSelect" %>
<style type="text/css">
    .readOnly
    {
        background-color: #CECECE;
    }
</style>

<script type="text/javascript">
    function setReadOnly(isReadOnly) {
        var cName = "";
        if (isReadOnly) {
            cName = "readOnly";
        }
        document.getElementById("<%= txtName.ClientID %>").className = cName;
        document.getElementById("<%= txtAddress.ClientID %>").className = cName;
        document.getElementById("<%= txtCity.ClientID %>").className = cName;
        document.getElementById("<%= txtState.ClientID %>").className = cName;
        document.getElementById("<%= txtZip.ClientID %>").className = cName;
        document.getElementById("<%= txtVendorPhone.ClientID %>").className = cName;
        document.getElementById("<%= txtVendorPhoneExt.ClientID %>").className = cName;
        document.getElementById("<%= txtWebAddress.ClientID %>").className = cName;
    }

    function disableVendorDropdown(isDisabled) {
        document.getElementById("<%= ddlVendor.ClientID %>").disabled = isDisabled;
    }


</script>
<div id="vendorChooser" runat="server" >
    <div ID="lblStatus" CssClass="alert alert-danger" runat="server" />

    <Rock:RockDropDownList CssClass="form-control" Label="Choose Vendor:" ID="ddlVendor" runat="server" AutoPostBack="true" OnSelectedIndexChanged="ddlVendor_SelectedIndexChanged" style="max-width:95%;" ValidationGroup="VendorSelect" />
    <table border="0" style="width:100%; border: 1px solid grey;">
        <tr>
            <td class="formLabel">Name:</td>
            <td class="formItem"><asp:TextBox ID="txtName"  runat="server" style="width:250px;" ValidationGroup="VendorSelect" /> </td>
        </tr>
        <tr>
            <td class="formLabel" style="vertical-align:top;">Address:</td>
            <td class="formItem">
                <asp:TextBox ID="txtAddress" runat="server" style="width:250px" ValidationGroup="VendorSelect" /><br />
                City: <asp:TextBox ID="txtCity" runat="server" style="width:125px;" ValidationGroup="VendorSelect" />
                State: <asp:TextBox ID="txtState" runat="server" MaxLength="2" style="width:25px;" ValidationGroup="VendorSelect" />
                Zip: <asp:TextBox ID="txtZip" runat="server" MaxLength="10" style="width:50px;" ValidationGroup="VendorSelect" />
            </td>
        </tr>
        <tr>
            <td class="formLabel">Phone:</td>
            <td class="formItem">
                <asp:TextBox ID="txtVendorPhone" runat="server" style="width:150px;" ValidationGroup="VendorSelect" /> ext <asp:TextBox ID="txtVendorPhoneExt" runat="server" style="width:50px;" ValidationGroup="VendorSelect" />
            </td>
        </tr>
        <tr>
            <td class="formLabel">Web Address:</td>
            <td class="formItem">
                <asp:TextBox ID="txtWebAddress" runat="server" style="width:300px;" ValidationGroup="VendorSelect" />
            </td>
        </tr>
    </table>
</div>
