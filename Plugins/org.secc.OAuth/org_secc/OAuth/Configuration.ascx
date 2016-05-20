<%@ Control Language="C#" AutoEventWireup="true" CodeFile="Configuration.ascx.cs" Inherits="RockWeb.Plugins.org_secc.OAuth.Configuration" %>

    <asp:Panel ID="pnlConfiguration" runat="server" CssClass="panel panel-block">
        <div class="panel-heading"><h1 class="panel-title"><i class="fa fa-cloud"></i> OAuth Server Configuration</h1></div>
        <div class="panel-body">
            <div class="row">
                <div class="col-md-4">
                    <Rock:KeyValueList ID="kvlConfig" runat="server" Label="Configuration Settings" ValuePrompt="Setting" />
                </div>
                <div class="col-md-8">
                    <Rock:Grid id="gOauthClients" runat="server" AllowPaging="false">
                        <Columns>
                            <Rock:RockBoundField DataField="ClientName" HeaderText="Client Name" />
                            <Rock:RockBoundField DataField="APIKey" HeaderText="API Key" />
                            <Rock:RockBoundField DataField="APISecret" HeaderText="API Secret" />
                            <Rock:RockBoundField DataField="CallBackURL" HeaderText="CallBack URL" />
                            <Rock:BoolField DataField="Active" HeaderText="Active" />
                        </Columns>
                    </Rock:Grid>
                </div>
            </div>
        </div>
    </asp:Panel>

