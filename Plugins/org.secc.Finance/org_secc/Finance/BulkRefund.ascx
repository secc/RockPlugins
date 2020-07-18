<%@ Control Language="C#" AutoEventWireup="true" CodeFile="BulkRefund.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Finance.BulkRefund" %>

<script src="/SignalR/hubs"></script>
<script type="text/javascript">
    $(function ()
    {
        var proxy = $.connection.rockMessageHub;

        proxy.client.receiveNotification = function (name, message, results)
        {
            if (name == '<%=this.SignalRNotificationKey %>') {
                $('#<%=pnlProgress.ClientID%>').show();

                if (message) {
                    $('#<%=lProgressMessage.ClientID %>').html(message);
                }

                if (results) {
                    $('#<%=lProgressResults.ClientID %>').html(results);
                }
            }
        }

        proxy.client.showButtons = function (name, visible)
        {
            if (name == '<%=this.SignalRNotificationKey %>') {

                if (visible) {
                    $('#<%=pnlActions.ClientID%>').show();
                }
                else {
                    $('#<%=pnlActions.ClientID%>').hide();
                }
            }
        }

        $.connection.hub.start().done(function ()
        {
            // hub started... do stuff here if you want to let the user know something
        });
    })
</script>
<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <asp:Panel ID="pnlView" runat="server" CssClass="panel panel-block">

            <div class="panel-heading">
                
                <h1 class="panel-title"><i class="fa fa-undo"></i>&nbsp;Bulk Registration Refund Tool</h1>
                <div class="label label-info pull-right"><asp:label runat="server" ID="lAlert" >0 Registrations - $0 Total</asp:label></div>
            </div>
            <div class="panel-body">
                <asp:ValidationSummary ID="vsBulkRegistrationRefund" runat="server" HeaderText="Please correct the following:" CssClass="alert alert-validation"/>
                
                <div class="row">
                    <div class="col-md-6">
                        <label class="control-label">Refund Type</label>
                        <ul class="nav nav-pills">
                            <li runat="server" id="liRegistration" class="active">
                                <asp:LinkButton ID="btnRegistration" runat="server" CssClass="show-pill" Text="Registration" CommandArgument="Registration" OnClick="btnRefundType_Click" CausesValidation="false">
                                        </asp:LinkButton>
                            </li>
                            <li runat="server" id="liTransactionCodes">
                                <asp:LinkButton ID="btnTransactionCodes" runat="server" CssClass="show-pill" Text="Transaction Codes" CommandArgument="Codes" OnClick="btnRefundType_Click" CausesValidation="false">
                                        </asp:LinkButton>
                            </li>
                        </ul>
                        <asp:Panel runat="server" ID="pnlRegistration" Visible="true">
                            <Rock:RegistrationTemplatePicker runat="server" ID="rtpRegistrationTemplate" Required="true" OnSelectItem="rtpRegistrationTemplate_SelectItem" AllowMultiSelect="True" Label="Registration Template(s):" />
                            <Rock:RockDropDownList runat="server" ID="ddlRegistrationInstance" Label="Registration Instance" Required="false" Title="All Instances" AutoPostBack="true" OnSelectionChanged="ddlRegistrationInstance_SelectionChanged" />
                        </asp:Panel>
                        <asp:Panel runat="server" ID="pnlTransactionCodes" Visible="true">
                            <Rock:RockTextBox runat="server" ID="tbTransactionCodes" Rows="5" TextMode="MultiLine" Required="true" Label="Transaction Codes" Help="Enter a list of transaction codes with each on its own line." AutoPostBack="true" />
                        </asp:Panel>
                    </div>
                    <div class="col-md-6">
                        <Rock:EmailBox runat="server" ID="ebEmail" Label="From Email" Required="false" Help="Who should the refund email be sent from.  This is required for transaction code refunds but, if left blank, registration refund emails will be sent from the contact email on the registration." />
                        <Rock:RockDropDownList runat="server" ID="ddlSystemCommunication" Label="System Communication" Required="false" Help="If an system communication is selected here, a copy of this email will be sent to the confirmation email address for every registration that is issued a refund. <span class='tip tip-lava'></span>" />
                        <Rock:DefinedValuePicker runat="server" ID="dvpRefundReason" Label="Refund Reason" Required="true" />
                    </div>
                    <div class="col-md-12">
                        <Rock:RockTextBox runat="server" id="tbRefundSummary" Label="Refund Summary" Rows="3" TextMode="MultiLine" />
                    </div>
                </div>
                
                <asp:Panel ID="pnlProgress" runat="server" CssClass="js-messageContainer" Style="display: none">
                    <strong>Progress</strong><br />
                    <div class="alert alert-info">
                        <asp:Label ID="lProgressMessage" CssClass="js-progressMessage" runat="server" />
                    </div>

                    <strong>Details</strong><br />
                    <div class="alert alert-info">
                        <pre><asp:Label ID="lProgressResults" CssClass="js-progressResults" runat="server" /></pre>
                    </div>
                </asp:Panel>

                <asp:Panel ID="pnlActions" runat="server" CssClass="actions">
                    <asp:LinkButton ID="btnProcessRefunds" runat="server" CssClass="btn btn-primary pull-right" Text="Process Refunds" OnClick="btnProcessRefunds_Click" />
                </asp:Panel>
            </div>

        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>
