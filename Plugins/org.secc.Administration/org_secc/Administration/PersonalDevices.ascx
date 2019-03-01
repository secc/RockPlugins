<%@ Control Language="C#" AutoEventWireup="true" CodeFile="PersonalDevices.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Administration.PersonalDevices" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <asp:Literal ID="lContent" runat="server"></asp:Literal>
        <asp:Literal ID="Literal1" runat="server"></asp:Literal>
        <asp:Literal ID="lDebug" runat="server" Visible="false"></asp:Literal>
        <Rock:ModalDialog ID="mdDialog" runat="server" Title="Edit Device" OnSaveClick="mdDialog_SaveClick">
            <Content>
                <fieldset>
                    <asp:HiddenField ID="hField" runat="server"></asp:HiddenField>
                    <Rock:RockTextBox ID="tbalias" runat="server" Label="Person Alias Id" TextMode="Number" />
                    <Rock:RockTextBox ID="tbdeviceregistration" runat="server" Label="Device Registration Id" />
                    <Rock:RockRadioButtonList ID="rblnotifications" runat="server" Label="Notifications Enabled">
                        <asp:ListItem Text="True" Value="true"></asp:ListItem>
                        <asp:ListItem Text="False" Value="false"></asp:ListItem>
                    </Rock:RockRadioButtonList>
                    <Rock:RockDropDownList ID="ddldevicetype" runat="server" Label="Personal Device Type" />
                    <Rock:RockDropDownList ID="ddlplatform" runat="server" Label="Platform"  />
                    <Rock:RockTextBox ID="tbdeviceuniqueid" runat="server" Label="Device Unique Identifier" />
                    <Rock:RockTextBox ID="tbdeviceversion" runat="server" Label="Device Version" />
                    <Rock:RockTextBox ID="tbmaddress" runat="server" Label="MAC Address" />
                </fieldset>
            </Content>
        </Rock:ModalDialog>
        <Rock:ModalDialog ID="mdDialog2" runat="server" Title="Add Device" OnSaveClick="mdDialog2_SaveClick">
            <Content>
                <fieldset>
                    <asp:HiddenField ID="hField2" runat="server"></asp:HiddenField>
                    <Rock:RockTextBox ID="tbdeviceregistration2" runat="server" Label="Device Registration Id" />
                    <Rock:RockRadioButtonList ID="rblnotifications2" runat="server" Label="Notifications Enabled">
                        <asp:ListItem Text="True" Value="true"></asp:ListItem>
                        <asp:ListItem Text="False" Value="false"></asp:ListItem>
                    </Rock:RockRadioButtonList>
                    <Rock:RockDropDownList ID="ddldevicetype2" runat="server" Label="Personal Device Type" />
                    <Rock:RockDropDownList ID="ddlplatform2" runat="server" Label="Platform" />
                    <Rock:RockTextBox ID="tbdeviceuniqueid2" runat="server" Label="Device Unique Identifier" />
                    <Rock:RockTextBox ID="tbdeviceversion2" runat="server" Label="Device Version" />
                    <Rock:RockTextBox ID="tbmaddress2" runat="server" Label="MAC Address" />
                </fieldset>
            </Content>
        </Rock:ModalDialog>
    </ContentTemplate>
</asp:UpdatePanel>
