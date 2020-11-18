<%@ Control Language="C#" AutoEventWireup="true" CodeFile="RiseWebhooks.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Rise.RiseWebhooks" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <Rock:ModalDialog runat="server" ID="mdAddWebhook" Title="Add New Webhook" OnSaveClick="mdAddWebhook_SaveClick">
            <Content>
                <Rock:RockDropDownList runat="server" ID="ddlEventType" DataTextField="Value" DataValueField="Key"
                    Label="Event Type" Required="true" AutoPostBack="true" OnSelectedIndexChanged="ddlEventType_SelectedIndexChanged" />
                <Rock:RockTextBox runat="server" ID="tbUrl" Label="Webhook Url" Required="true" />
            </Content>
        </Rock:ModalDialog>


        <asp:Panel ID="pnlView" runat="server" CssClass="panel panel-block">
            <div class="panel-heading">
                <h1 class="panel-title">
                    <i class="fa fa-chalkboard"></i>
                    Rise Webhooks
                </h1>
            </div>
            <div class="panel-body">
                <div class="grid grid-panel">
                    <Rock:Grid ID="gList" runat="server" AllowSorting="true" DataKeyNames="Id">
                        <Columns>
                            <Rock:RockBoundField DataField="TargetUrl" HeaderText="Target Url" />
                            <Rock:RockBoundField DataField="EventsFormatted" HeaderText="Events" />
                            <Rock:DeleteField ID="btnDelete" OnClick="btnDelete_Click" />
                        </Columns>
                    </Rock:Grid>
                </div>
            </div>
        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>
