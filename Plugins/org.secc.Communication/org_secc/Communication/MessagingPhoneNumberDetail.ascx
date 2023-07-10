<%@ Control Language="C#" AutoEventWireup="true" CodeFile="MessagingPhoneNumberDetail.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Communication.MessagingPhoneNumberDetail" %>
<asp:UpdatePanel ID="upPhoneDetail" runat="server">
    <ContentTemplate>

        <asp:HiddenField ID="hfMessagingPhoneId" runat="server" />

        <asp:Panel ID="pnlDetails" runat="server" Visible="false" CssClass="panel panel-block">
            <div class="panel-heading">
                <h1 class="panel-title"><i class="fas fa-phone"></i> <asp:Literal ID="lTitle" runat="server" /></h1>
                <div class="panel-labels">
                    <Rock:HighlightLabel ID="hlStatus" runat="server" />
                </div>
            </div>
            <div id="pnlAuditDrawer" runat="server" class="panel-drawer rock-panel-drawer">
                <div class="drawer-content" style="display: none;">
                    <div class="row">
                        <div class="col-md-4">
                            <dl>
                                <dt>Created By</dt>
                                <dd><%= CreatedByDrawerItem %></dd
                            </dl>
                        </div>
                        <div class="col-md-4">
                            <dl>
                                <dt>Modified By</dt>
                                <dd><%= ModifiedByDrawerItem %></dd>
                            </dl>
                        </div>
                        <div class="col-md-4">
                            <dl>
                                <dt>Id</dt>
                                <dd><%= PhoneIdDrawerItem %></dd>
                            </dl>
                        </div>
                    </div>
                </div>
                <div class="drawer-pull js-drawerpull"><i class="fa fa-chevron-down" data-icon-closed="fa fa-chevron-down" data-icon-open="fa fa-chevron-up"></i></div>
            </div>
            <div class="panel-body">
                <Rock:NotificationBox ID="nbPhoneNotification" runat="server" Visible="false" NotificationBoxType="Info" />
                <asp:Panel ID="pnlEditDetails" runat="server">
                    <asp:ValidationSummary ID="vsDetails" runat="server" HeaderText="Please correct the following:" CssClass="alert alert-validation" />
                    <fieldset>
                        <div class="row">
                            <div class="col-md-6">
                                <Rock:RockTextBox ID="tbPhoneNumberName" runat="server" Label="Name" Required="true" RequiredErrorMessage="Phone Name is required." />
                            </div>
                            <div class="col-md-6">
                                <Rock:RockLiteral ID="lPhoneNumber" runat="server" Label="Phone Number" Visible="false" />
                                <Rock:RockDropDownList ID="ddlPhoneNumber" runat="server" Label="Phone Number" Visible="false"
                                    Required="true" RequiredErrorMessage="Phone Number is required." />
                            </div>
                        </div>
                        <div class="row">
                            <div class="col-md-12">
                                <Rock:RockTextBox ID="tbDescription" runat="server" Label="Description" Required="false" TextMode="MultiLine" />
                            </div>
                        </div>
                        <div class="row">
                            <div class="col-md-12">
                                <span class="control-label">Owner</span><br />
                                <asp:Literal ID="lOwnerNameEdit" runat="server" />
                                <span style="margin-left:1em;"><asp:LinkButton ID="lbChangeOwner" runat="server" CssClass="btn  btn-default btn-xs" Text="..." /></span>
                                <asp:HiddenField ID="hfOwner" runat="server" />
                            </div>
                        </div>
                        <div class="row">
                            <div class="col-md-12">
                                <Rock:Switch ID="switchActive" Label="Active" runat="server" />
                            </div>
                        </div>
                    </fieldset>
                    <div class="actions">
                        <asp:LinkButton ID="btnSavePhone" runat="server" AccessKey="s" ToolTip="Alt+s" Text="Save" CssClass="btn btn-primary" />
                        <asp:LinkButton ID="btnCancelPhone" runat="server" AccessKey="c" ToolTip="Alt+c" Text="Cancel" CssClass="btn btn-link" CausesValidation="false" />
                    </div>
                </asp:Panel>
                <asp:Panel ID="pnlViewDetails" runat="server">
                    <div class="row">
                        <div class="col-md-6">
                            <Rock:RockLiteral ID="lPhoneNumberView" runat="server" Label="Phone Number" />
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-12">
                            <Rock:RockLiteral ID="lDescription" runat="server" Label="Description" />
                        </div>
                    </div>
                    <asp:Panel ID="pnlViewOwner" runat="server" CssClass="row" Visible="false">
                        <div class="col-md-12">
                            <Rock:RockLiteral ID="lViewOwner" runat="server" Label="Owner" />
                        </div>

                    </asp:Panel>
                    <div class="actions">
                        <asp:LinkButton ID="btnEditPhone" runat="server" AccessKey="e" ToolTip="Alt+e" Text="Edit" CssClass="btn btn-primary" />
                    </div>

                </asp:Panel>

            </div>
        </asp:Panel>
        <Rock:ModalDialog ID="mdlAssignOwner" runat="server" Title="Set Owner">
            <Content>
                <asp:Panel ID="pnlOwner" runat="server" Visible="true">
                    <h3>Owner</h3>
                    <div class="row">
                        <div class="col-xs-12">
                            <span class="control-label"><asp:Literal ID="lblOwner" runat="server" /></span>
                                <span style="margin-left:2em;"><asp:LinkButton ID="lbDeleteOwner" runat="server" CssClass="btn-sm btn-delete" Text="X" /></span>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-xs-12">
                            <div class="actions">
                                <asp:LinkButton ID="lbSetOwnerPerson" runat="server" CssClass="btn btn-primary" Text="Set Owner Person" />
                                <asp:LinkButton ID="lbSetOwnerGroup" runat="server" CssClass="btn btn-primary" Text="Set Owner Group" />
                            </div>
                        </div>
                    </div>
                </asp:Panel>
                <asp:Panel ID="pnlOwnerPerson" runat="server" Visible="false">
                    <Rock:PersonPicker ID="ppSetOwner" runat="server" Label="Owner" />
                    <div class="actions">
                        <asp:LinkButton ID="lbUpdateOwnerPerson" runat="server" CssClass="btn btn-primary" Text="Update" />
                        <asp:LinkButton ID="lbCancelOwnerPerson" runat="server" CssClass="btn btn-cancel" Text="Cancel" />
                    </div>
                </asp:Panel>
                <asp:Panel ID="pnlOwnerGroup" runat="server" Visible="false">
                    <Rock:GroupPicker ID="gpSetOwner" runat="server" Label="Owner Group" />
                    <div class="actions">
                        <asp:LinkButton ID="lbUpdateOwnerGroup" runat="server" CssClass="btn btn-primary" Text="Update" />
                        <asp:LinkButton ID="lbCancelOwnerGroup" runat="server" CssClass="btn btn-cancel" Text="Cancel" />
                    </div>
                </asp:Panel>
            </Content>
        </Rock:ModalDialog>
    </ContentTemplate>
</asp:UpdatePanel>
