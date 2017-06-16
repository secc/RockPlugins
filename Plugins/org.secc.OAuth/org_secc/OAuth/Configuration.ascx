<%@ Control Language="C#" AutoEventWireup="true" CodeFile="Configuration.ascx.cs" Inherits="RockWeb.Plugins.org_secc.OAuth.Configuration" %>

    <asp:UpdatePanel ID="pnlConfiguration" runat="server" Class="panel panel-block">
        <ContentTemplate>
            <div class="panel-heading"><h1 class="panel-title"><i class="fa fa-cloud"></i> OAuth Server Configuration</h1></div>
            <div class="panel-body">
                
                <asp:ValidationSummary ID="vsDetails" runat="server" HeaderText="Please Correct the Following" CssClass="alert alert-danger" />

                <div class="row">
                    <div class="col-md-6">
                        <Rock:RockDropDownList ID="ddlAuthorizeRoute" runat="server" Label="Authorize Route" Help="Route which points to the page containing the Authorize block.  This setting is also used by the OAuth middleware to configure the server and is the endpoint the client will need to access your OAuth server. " />
                        <Rock:RockDropDownList ID="ddlLoginRoute" runat="server" Label="Login Route" Help="Route which points to the page containing the Login block." />
                        <Rock:RockDropDownList ID="ddlLogoutRoute" runat="server" Label="Logout Route" Help="Route which points to the page containing the Logout block." />

                    </div>
                    <div class="col-md-6">
                        <Rock:RockDropDownList ID="ddlTokenRoute" runat="server" Label="Token Route" Help="Route which points to the page containing the Logout block.  This setting is also used by the OAuth middleware to configure the server and is the endpoint the client will need to renew OAuth tokens." />
                        <Rock:RockCheckBox ID="cbSSLRequired" runat="server" Label="SSL Required" Checked="true" Help="This should default to true and only be set to false when in debug mode!" />
                        <div class="row">
                            <div class="col-md-6">
                                <Rock:RockTextBox ID="tbTokenLifespan" runat="server" Label="Token Lifespan (minutes)" Help="The OAuth token lifespan in minutes." Required="true" DisplayRequiredIndicator="false"/>
                            </div>
                            <div class="col-md-6">
                                <Rock:RockTextBox ID="tbRefreshTokenLifespan" runat="server" Label="Refresh Token Lifespan (hours)" Help="The OAuth refresh token lifespan in hours." Required="true" DisplayRequiredIndicator="false"/>
                            </div>
                        </div>
                        <asp:RegularExpressionValidator ControlToValidate="tbTokenLifespan" ValidationExpression="^\d+" ErrorMessage="Please enter a numeric token lifespan." runat="server" CssClass="hidden" />
                    </div>
                    <div class="col-md-12">
                        <Rock:BootstrapButton ID="btnSave" CssClass="btn btn-primary pull-right" runat="server" Text="Save" OnClick="btnSave_Click" />
                    </div>
                </div>
                <div>
                    <h3>OAuth Clients</h3>
                </div>
                <Rock:Grid id="gOAuthClients" runat="server" AllowPaging="false" DataKeyNames="Id" OnRowSelected="gOAuthClients_RowSelected" >
                    <Columns>
                        <Rock:RockBoundField DataField="ClientName" HeaderText="Client Name" />
                        <Rock:RockBoundField DataField="APIKey" HeaderText="Api Key" />
                        <Rock:RockBoundField DataField="APISecret" HeaderText="Api Secret" />
                        <Rock:RockBoundField DataField="CallBackURL" HeaderText="Callback Url" />
                        <Rock:BoolField DataField="Active" HeaderText="Active" HeaderStyle-HorizontalAlign="Center" />
                        <Rock:DeleteField OnClick="gOAuthClientsDelete_Click" HeaderText="Delete" HeaderStyle-HorizontalAlign="Center" />
                    </Columns>
                </Rock:Grid>
                <div>
                    <h3>OAuth Scopes</h3>
                </div>
                <Rock:Grid id="gOAuthScopes" runat="server" AllowPaging="false" DataKeyNames="Id" OnRowSelected="gOAuthScopes_RowSelected">
                    <Columns>
                        <Rock:RockBoundField DataField="Identifier" HeaderText="Identifier" />
                        <Rock:RockBoundField DataField="Description" HeaderText="Description" />
                        <Rock:BoolField DataField="Active" HeaderText="Active" HeaderStyle-HorizontalAlign="Center" />
                        <Rock:DeleteField OnClick="gOAuthScopesDelete_Click" HeaderText="Delete" HeaderStyle-HorizontalAlign="Center" />
                    </Columns>
                </Rock:Grid>
                <Rock:ModalDialog ID="gOAuthClientEdit" runat="server" OnSaveClick="gOAuthClientEdit_SaveClick" Title="OAuth Client" >
                    <Content>
                        <div  id="divErrors" runat="server" class="alert alert-danger" visible="false"></div>
                        <asp:HiddenField id="hfClientId" runat="server" />
                        <div class="row">
                            <div class="col-md-12">
                                <Rock:RockTextBox ID="tbClientName" runat="server" Label="Client Name" Require="true" />
                            </div>
                            <div class="col-md-6">
                                <Rock:RockTextBox ID="tbApiKey" runat="server" Label="Api Key" />
                            </div>
                            <div class="col-md-6">
                                <Rock:RockTextBox ID="tbApiSecret" runat="server" Label="Api Secret" />
                            </div>
                            <div class="col-md-6">
                                <Rock:RockTextBox ID="tbCallbackUrl" runat="server" Label="Callback Url" />
                            </div>
                            <div class="col-md-6">
                                <Rock:Toggle ID="cbActive" runat="server" Label="Active" OnText="Active" OffText="Inactive" />
                            </div>
                            <div class="col-md-12">
                                <Rock:RockCheckBoxList ID="cblClientScopes" runat="server" Label="Allowed Scopes" DataValueField="Id" DataTextField="Value" />
                            </div>
                        </div>
                    </Content>
                </Rock:ModalDialog>
                <Rock:ModalDialog ID="gOAuthScopeEdit" runat="server" OnSaveClick="gOAuthScopeEdit_SaveClick" Title="OAuth Scope" >
                    <Content>
                        <div class="row">
                            <div class="col-md-12">
                                <asp:HiddenField id="hfScopeId" runat="server" />
                                <Rock:RockTextBox ID="tbIdentifier" runat="server" Label="Identifier" />
                                <Rock:RockTextBox ID="tbDescription" runat="server" Label="Description" />
                                <Rock:Toggle ID="cbScopeActive" runat="server" Label="Active" OnText="Active" OffText="Inactive" />
                            </div>
                        </div>
                    </Content>
                </Rock:ModalDialog>
            </div>

        </ContentTemplate>
    </asp:UpdatePanel>

