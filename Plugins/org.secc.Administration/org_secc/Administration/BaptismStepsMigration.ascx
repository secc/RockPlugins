<%@ Control Language="C#" AutoEventWireup="true" CodeFile="BaptismStepsMigration.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Administration.BaptismStepsMigration" %>
<script src="/SignalR/hubs"></script>
<script type="text/javascript">
    $(function () {
        var proxy = $.connection.rockMessageHub;

        proxy.client.receiveNotification = function (name, message, results) {
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

        proxy.client.showButtons = function (name, visible) {
            if (name == '<%=this.SignalRNotificationKey %>') {

                if (visible) {
                    $('#<%=pnlActions.ClientID%>').show();
                }
                else {
                    $('#<%=pnlActions.ClientID%>').hide();
                }


            }
        }

        proxy.client.showSummary = function (name, visible) {
            if (name == '<%=this.SignalRNotificationKey %>') {
                if (visible) {
                    $('#<%=pnlSummary.ClientID%>').show();
                }
                else {
                    $('#<%=pnlSummary.ClientID%>').hide();
                }

            }
        }

        proxy.client.updateProcessingFlag = function (name, value) {
            if (name == '<%=this.SignalRNotificationKey %>') {
                $('#<%=hfProcessingMigrations.ClientID %>').val(value);
            }
        }


        $.connection.hub.start().done(function () {
            // hub started... do stuff here if you want to let the user know something
        });

    });
</script>
<asp:UpdatePanel ID="upMain" runat="server">
    <ContentTemplate>
        <asp:HiddenField ID="hfProcessingMigrations" runat="server" Value="0" />
        <asp:Panel ID="pnlBaptismMigration" runat="server" CssClass="panel panel-block">
            <div class="panel-heading">
                <h1 class="panel-title"><i class="fas fa-truck-loading"></i>&nbsp; Migrate Baptism Data</h1>

            </div>
            <div class="panel-body">
                <Rock:NotificationBox ID="nbMain" runat="server" Visible="false" />
                <asp:Panel ID="pnlSummary" runat="server" Visible="false">
                    <asp:ValidationSummary ID="vsBaptismDataMigration" runat="server" HeaderText="Please Correct the following:" CssClass="alert alert-validation" />

                    <div class="row">
                        <div class="col-sm-6">
                            <h3>Total Records to Migrate:</h3>
                        </div>
                        <div class="col-sm-6">
                            <h3>
                                <asp:Literal ID="lRecordsToMigrate" runat="server" /></h3>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-6">
                            <h3>Records to process:</h3>
                        </div>
                        <div class="col-md-6">
                            <Rock:NumberBox ID="nbRecordsToProcess" runat="server" CssClass="form-control input-lg"
                                NumberType="Integer" Required="true" RequiredErrorMessage="Number to Process is required." MinimumValue="0" />

                        </div>
                    </div>
                </asp:Panel>
                <asp:Panel ID="pnlProgress" runat="server" CssClass="js-messageContainer" Style="display: none;">
                    <h3>Progress</h3>
                    <div class="alert alert-info">
                        <asp:Label ID="lProgressMessage" CssClass="js-progressMessage" runat="server" />
                    </div>
                    <h3>Details</h3>
                    <div class="alert alert-info">
                        <pre>
                            <asp:Label ID="lProgressResults" CssClass="js-progressResults" runat="server" />
                        </pre>
                    </div>

                </asp:Panel>
                <asp:Panel ID="pnlActions" runat="server" CssClass="actions" Visible="false">
                    <asp:LinkButton ID="btnMigrateRecords" runat="server" CssClass="btn btn-primary pull-right" Text="Migrate Records" OnClick="btnMigrateRecords_Click" />
                </asp:Panel>
            </div>
        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>
