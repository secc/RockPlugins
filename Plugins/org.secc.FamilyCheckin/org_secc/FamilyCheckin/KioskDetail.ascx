<%@ Control Language="C#" AutoEventWireup="true" CodeFile="KioskDetail.ascx.cs" Inherits="RockWeb.Plugins.org_secc.FamilyCheckin.KioskDetail" %>

<asp:UpdatePanel ID="upnlDevice" runat="server">
    <ContentTemplate>
        <asp:Panel ID="pnlDetails" CssClass="panel panel-block" runat="server" Visible="false">

            <asp:HiddenField ID="hfKioskId" runat="server" />

            <div class="panel-heading">
                <h1 class="panel-title"><i class="fa fa-power-off"></i>
                    <asp:Literal ID="lActionTitle" runat="server" /></h1>
            </div>
            <div class="panel-body">

                <asp:ValidationSummary ID="ValidationSummary1" runat="server" HeaderText="Please Correct the Following" CssClass="alert alert-danger" />

                <fieldset>

                    <Rock:NotificationBox ID="nbEditModeMessage" runat="server" NotificationBoxType="Info" />
                    <Rock:NotificationBox ID="nbDuplicateDevice" runat="server" NotificationBoxType="Warning" Title="Sorry" Visible="false" />

                    <div class="row">
                        <div class="col-md-6">
                            <Rock:DataTextBox ID="tbName" runat="server" SourceTypeName="org.secc.FamilyCheckin.Model.Kiosk, org.secc.FamilyCheckin" PropertyName="Name" Required="true" />
                        </div>
                        <div class="col-md-6">
                            <Rock:DataTextBox ID="tbIPAddress" runat="server" SourceTypeName="org.secc.FamilyCheckin.Model.Kiosk, org.secc.FamilyCheckin" PropertyName="IPAddress" Required="true" />
                        </div>
                    </div>

                    <div class="row">
                        <div class="col-md-12">
                            <Rock:DataTextBox CausesValidation="false" ID="tbDescription" runat="server" SourceTypeName="org.secc.FamilyCheckin.Model.Kiosk, org.secc.FamilyCheckin" PropertyName="Description" TextMode="MultiLine" Rows="3" />
                        </div>
                    </div>

                    <Rock:DataDropDownList CausesValidation="false" runat="server" ID="ddlKioskType" Label="Kiosk Type" DataTextField="Name" DataValueField="Id" SourceTypeName="org.secc.FamilyCheckin.Model.Kiosk, org.secc.FamilyCheckin" PropertyName="KioskTypeId"></Rock:DataDropDownList>

                    <h4>Print Settings</h4>
                    <Rock:RockDropDownList ID="ddlPrintTo" runat="server" Label="Print Using" AutoPostBack="true" OnSelectedIndexChanged="ddlPrintTo_SelectedIndexChanged"
                        Help="When this device needs to print, should it use the printer configured in next setting (Device Printer), the printer configured for the location (Location Printer), or should the Group Type's 'Print Using' setting determine the printer to use (Group Type)?">
                        <asp:ListItem Text="Device Printer" Value="1" />
                        <asp:ListItem Text="Location Printer" Value="2" />
                        <asp:ListItem Text="Group Type" Value="0" />
                    </Rock:RockDropDownList>
                    <Rock:RockDropDownList ID="ddlPrinter" runat="server" Label="Printer" DataTextField="Name" DataValueField="Id"
                        Help="The printer that this device should use for printing" />
                    <Rock:RockDropDownList ID="ddlPrintFrom" runat="server" Label="Print From" Required="false"
                        Help="When this device needs to print, where should the printing be initiated from?  Either the server running Rock, or from the actual client device? " />


                </fieldset>

                <div class="actions">
                    <asp:LinkButton ID="btnSave" runat="server" AccessKey="s" Text="Save" CssClass="btn btn-primary" OnClick="btnSave_Click" />
                    <asp:LinkButton ID="btnCancel" runat="server" AccessKey="c" Text="Cancel" CssClass="btn btn-link" CausesValidation="false" OnClick="btnCancel_Click" />
                </div>

            </div>

        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>
