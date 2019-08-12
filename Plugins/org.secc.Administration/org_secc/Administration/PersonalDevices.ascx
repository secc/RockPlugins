<%@ Control Language="C#" AutoEventWireup="true" CodeFile="PersonalDevices.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Administration.PersonalDevices" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <asp:Literal ID="lDebug" runat="server" Visible="false"></asp:Literal>
        <Rock:ModalDialog ID="mdEdit" runat="server" Title="Edit Device" OnSaveClick="mdEdit_SaveClick">
            <Content>
                <fieldset>
                    <asp:HiddenField ID="hfDeviceId" runat="server"></asp:HiddenField>
                    <Rock:PersonPicker runat="server" ID="ppPerson" Required="true"  Label="Device Owner"/>
                    <Rock:RockTextBox ID="tbdeviceregistration" runat="server" Label="Device Registration Id" />
                    <Rock:RockRadioButtonList ID="rblnotifications" runat="server" Label="Notifications Enabled">
                        <asp:ListItem Text="True" Value="true"></asp:ListItem>
                        <asp:ListItem Text="False" Value="false"></asp:ListItem>
                    </Rock:RockRadioButtonList>
                    <Rock:RockDropDownList ID="ddldevicetype" runat="server" Label="Personal Device Type" />
                    <Rock:RockDropDownList ID="ddlplatform" runat="server" Label="Platform" />
                    <Rock:RockTextBox ID="tbdeviceuniqueid" runat="server" Label="Device Unique Identifier" />
                    <Rock:RockTextBox ID="tbdeviceversion" runat="server" Label="Device Version" />
                    <Rock:RockTextBox ID="tbmaddress" runat="server" Label="MAC Address" />
                </fieldset>
            </Content>
        </Rock:ModalDialog>
        <div class="panel panel-block">
            <div class="panel-heading">
                <h4 class="panel-title">
                    <div class="panel-panel-title">
                        <i class="fa fa-mobile"></i>
                        <asp:Literal ID="lPanelHeader" runat="server" />
                    </div>
                </h4>
                <Rock:BootstrapButton runat="server" ID="lbAddDevice" Text="<i class='fa fa-plus'></i> Add Device"
                    CssClass="pull-right btn btn-xs btn-primary" OnClick="lbAddDevice_Click" />
            </div>
            <div class="panel-body">
                <div class="row display-flex">
                    <asp:Repeater runat="server" ID="rDevices" OnItemDataBound="rDevices_ItemDataBound">
                        <ItemTemplate>
                            <div class="col-md-3 col-sm-4">
                                <div class="well margin-b-xs rollover-container">
                                    <asp:LinkButton ID="btnInactivateDevice" Text="<i class='fa fa-times'></i>" runat="server"
                                        CssClass="pull-right rollover-item btn btn-xs btn-danger" OnClientClick="Rock.dialogs.confirmPreventOnCancel(event, 'Are you sure you want to inactivate this device?')"
                                        CommandArgument='<%# Eval("PersonalDevice.Id") %>' OnCommand="btnInactivateDevice_Command" />
                                    <asp:LinkButton ID="btnEditDevice" Text="<i class='fa fa-edit'></i>" runat="server"
                                        CssClass="pull-right rollover-item btn btn-xs btn-primary"
                                        CommandArgument='<%# Eval("PersonalDevice.Id") %>' OnCommand="btnEditDevice_Command" />
                                    <asp:Literal ID="ltLava" runat="server" />
                                    <asp:LinkButton Text="Interactions" ID="btnInteraction" runat="server" CssClass="btn btn-default btn-xs"
                                        OnCommand="btnInteraction_Command" CommandArgument='<%# Eval("PersonalDevice.Id") %>' />
                                </div>
                            </div>
                        </ItemTemplate>
                    </asp:Repeater>
                </div>
            </div>
        </div>

    </ContentTemplate>
</asp:UpdatePanel>
