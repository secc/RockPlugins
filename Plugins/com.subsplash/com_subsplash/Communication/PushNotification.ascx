<%@ Control Language="C#" AutoEventWireup="true" CodeFile="PushNotification.ascx.cs" Inherits="RockWeb.Plugins.com_subsplash.Communication.PushNotification" %>

<asp:UpdatePanel ID="upNotifications" runat="server" Visible="false">
    <ContentTemplate>
        <Rock:NotificationBox ID="nbAlert" runat="server" NotificationBoxType="Danger" />
    </ContentTemplate>
</asp:UpdatePanel>


<asp:UpdatePanel ID="upPanel" runat="server">
    <ContentTemplate>
        <script>
            //Sys.WebForms.PageRequestManager.getInstance().add_endRequest(scrollToGrid);
            function scrollToResults() {
        
                    $('html, body').animate({
                        scrollTop: $('.js-pnl-result')
                    }, 'fast');
        
            }
        </script>
        <asp:Panel ID="pnlEditForm" runat="server">
            <div class="panel panel-block">
                <div class="panel-heading">
                    <h1 class="panel-title"><i class="fa fa-comment-o"></i> <asp:Literal ID="lTitle" runat="server" /></h1>
                
                    <div class="panel-labels">
                        <Rock:HighlightLabel ID="hlStatus" runat="server" />
                    </div>
                </div>
                <div class="panel-body">

                    <asp:Panel ID="pnlEdit" runat="server">

                        <asp:HiddenField ID="hfCommunicationId" runat="server" />
                        <asp:HiddenField ID="hfMediumId" runat="server" />

                        <asp:ValidationSummary ID="ValidationSummary" runat="server" HeaderText="Please correct the following:" CssClass="alert alert-validation" />
                        <asp:CustomValidator ID="cvDelayDateTime" runat="server" />

                        <div id="divMediums" runat="server">
                            <ul class="nav nav-pills">
                                <asp:Repeater ID="rptMediums" runat="server">
                                    <ItemTemplate>
                                        <li class='<%# (int)Eval("Key") == MediumEntityTypeId ? "active" : "" %>'>
                                            <asp:LinkButton ID="lbMedium" runat="server" Text='<%# Eval("Value") %>' CommandArgument='<%# Eval("Key") %>' OnClick="lbMedium_Click" CausesValidation="false">
                                            </asp:LinkButton>
                                        </li>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </ul>
                        </div>

                        <Rock:NotificationBox ID="nbInvalidTransport" runat="server" NotificationBoxType="Warning" Dismissable="true" Title="Warning" Visible="false" />

                        <Rock:ButtonGroup runat="server" ID="bgRecipientOptions" Label="Notification Recipient" OnSelectedIndexChanged="bgRecipientOptions_SelectedIndexChanged" AutoPostBack="true">
                            <asp:ListItem Text="Individual(s)" Value="Individual" Selected="True"></asp:ListItem>
                            <asp:ListItem Text="Notification Group" Value="Topic"></asp:ListItem>
                        </Rock:ButtonGroup>
                        <br />
                        <asp:Panel ID="pnlRecipients" runat="server" class="panel panel-widget recipients">
                            <div class="panel-heading clearfix">
                                <div class="control-label pull-left">
                                    To: <asp:Literal ID="lNumRecipients" runat="server" />
                                </div> 
                    
                                <div class="pull-right">
                                    <Rock:PersonPicker ID="ppAddPerson" runat="server" CssClass="picker-menu-right" PersonName="Add Person" OnSelectPerson="ppAddPerson_SelectPerson" />
                                </div>

                                <asp:CustomValidator ID="valRecipients" runat="server" OnServerValidate="valRecipients_ServerValidate" Display="None" ErrorMessage="At least one recipient is required." />
                
                             </div>   
                
                             <div class="panel-body">

                                    <ul class="recipients list-unstyled">
                                        <asp:Repeater ID="rptRecipients" runat="server" OnItemCommand="rptRecipients_ItemCommand" OnItemDataBound="rptRecipients_ItemDataBound">
                                            <ItemTemplate>
                                                <li class='recipient <%# Eval("Status").ToString().ToLower() %>'><asp:Literal id="lRecipientName" runat="server"></asp:Literal> <asp:LinkButton ID="lbRemoveRecipient" runat="server" CommandArgument='<%# Eval("PersonId") %>' CausesValidation="false"><i class="fa fa-times"></i></asp:LinkButton></li>
                                            </ItemTemplate>
                                        </asp:Repeater>
                                    </ul>

                                    <div class="pull-right">
                                        <asp:LinkButton ID="lbShowAllRecipients" runat="server" CssClass="btn btn-action btn-xs" Text="Show All" OnClick="lbShowAllRecipients_Click" CausesValidation="false"/>
                                        <asp:LinkButton ID="lbRemoveAllRecipients" runat="server" Text="Remove All Pending Recipients" CssClass="remove-all-recipients btn btn-action btn-xs" OnClick="lbRemoveAllRecipients_Click" CausesValidation="false"/>
                                    </div>

                            </div>
                        </asp:Panel>
                        <asp:Panel ID="pnlTopic" runat="server" class="panel panel-widget pnlTopic" Visible="false">
                            <div class="panel-heading clearfix">
                                <div class="control-label pull-left clearfix">
                                    To: <asp:Literal ID="lNumTopicSubscribers" runat="server" Text="0 Subscribers" />
                                </div>

                             </div>   
                
                             <div class="panel-body">
                                 <Rock:RockDropDownList runat="server" ID="rddlTopics" OnSelectedIndexChanged="rddlTopics_SelectedIndexChanged" AutoPostBack="true" Required="true" Label="Notification Group" Help="Select a notification group.  For more information, these are created and managed in the Subsplash Dashboard." />
                            </div>
                        </asp:Panel>
                        <Rock:RockDropDownList ID="ddlTemplate" runat="server" Label="Template" AutoPostBack="true" OnSelectedIndexChanged="ddlTemplate_SelectedIndexChanged" EnhanceForLongLists="true" />

                        <asp:PlaceHolder ID="phContent" runat="server" />

                        <Rock:DateTimePicker ID="dtpFutureSend" runat="server" Label="Delay Send Until" SourceTypeName="Rock.Model.Communication" PropertyName="FutureSendDateTime" />

                        <div class="actions">
                            <asp:LinkButton ID="btnSubmit" runat="server" Text="Submit" CssClass="btn btn-primary" OnClick="btnSubmit_Click" />
                            <asp:LinkButton ID="btnTest" runat="server" Text="Send Test" CssClass="btn btn-link" OnClick="btnTest_Click" Visible="false" />
                            <asp:LinkButton ID="btnSave" runat="server" Text="Save as Draft" CssClass="btn btn-link" OnClick="btnSave_Click" />
                            <asp:LinkButton ID="btnCancel" runat="server" Text="Cancel" CssClass="btn btn-link" OnClick="btnCancel_Click" />
                        </div>

                        <Rock:NotificationBox ID="nbTestResult" CssClass="margin-t-md" runat="server" Visible="false" />

                    </asp:Panel>

                    <asp:Panel ID="pnlResult" runat="server" Visible="false" CssClass="js-pnl-result">
                        <Rock:NotificationBox ID="nbWarning" runat="server" NotificationBoxType="Warning" />
                        <Rock:NotificationBox ID="nbResult" runat="server" NotificationBoxType="Success" />
                        <br />
                        <asp:HyperLink ID="hlViewCommunication" runat="server" Text="View Communication" />
                    </asp:Panel>

                </div>
            </div>

        

            <script type="text/javascript">
                Sys.Application.add_load(function () {

                    // Set all recipients tooltip
                    $('.recipient span').tooltip();

                    // Set the display of any recipients that have preference of NoBulkEmail based on if this is a bulk communication
                    $('.js-bulk-option').click(function () {
                        if ($(this).is(':checked')) {
                            $('.js-no-bulk-email').addClass('text-danger');
                        } else {
                            $('.js-no-bulk-email').removeClass('text-danger');
                        }
                    });
                })
            </script>
        </asp:Panel>
        <asp:Panel ID="pnlMessage" runat="server">
        
            <div class="panel panel-block">
                <div class="panel-heading">
                    <h1 class="panel-title"><i class="fa fa-bell"></i> Push Notification Details</h1>

                </div>
                <Rock:PanelDrawer ID="pdAuditDetails" runat="server"></Rock:PanelDrawer>
                <div class="panel-body">
                    <div class="margin-t-lg">
                        <div class="form-horizontal">
                            <asp:Literal ID="lFutureSend" runat="server"></asp:Literal>
                            <div class="row margin-b-lg">
                                <div class="col-md-6">
                                    <asp:Literal ID="lCreatedBy" runat="server"></asp:Literal>
                                </div>
                                <div class="col-md-6 text-right">
                                    <asp:Literal ID="lApprovedBy" runat="server"></asp:Literal>
                                </div>
                            </div>
                        </div>

                        <%-- Message Content --%>
                        <asp:Literal ID="lDetails" runat="server" />

                        <%-- Message Actions --%>
                        <div class="actions">
                            <asp:LinkButton ID="btnApprove" runat="server" Text="Approve" CssClass="btn btn-success" OnClick="btnApprove_Click" />
                            <asp:LinkButton ID="btnDeny" runat="server" Text="Deny" CssClass="btn btn-danger" OnClick="btnDeny_Click" />
                            <asp:LinkButton ID="btnEdit" runat="server" Text="Edit" CssClass="btn btn-default" OnClick="btnEdit_Click" />
                            <asp:LinkButton ID="btnCancelSend" runat="server" Text="Cancel Send" CssClass="btn btn-link" OnClick="btnCancelSend_Click" />
                            <asp:LinkButton ID="btnCopy" runat="server" Text="Copy Communication" CssClass="btn btn-link" OnClick="btnCopy_Click" />

                        </div>
                    </div>
                </div>
            </div>
        </asp:Panel>


    </ContentTemplate>
</asp:UpdatePanel>
