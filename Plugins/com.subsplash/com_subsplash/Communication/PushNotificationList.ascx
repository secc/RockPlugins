<%@ Control Language="C#" AutoEventWireup="true" CodeFile="PushNotificationList.ascx.cs" Inherits="RockWeb.Plugins.com_subsplash.Communication.PushNotificationList" %>
<script>
    Sys.Application.add_load(function () {
        $('.grid-table span.badge').tooltip({ html: true, container: 'body', delay: { show: 500, hide: 100 } });
    });
</script>
<style>
    .hide-button a {display: none;}
</style>
<asp:UpdatePanel ID="upFinancial" runat="server">
    <ContentTemplate>

        <Rock:NotificationBox ID="nbBindError" runat="server" NotificationBoxType="Warning" />

        <div class="panel panel-block">
            <div class="panel-heading">
                <h1 class="panel-title"><i class="fa fa-bell"></i> Push Notification List</h1>
            </div>
            <div class="panel-body">

                <div class="grid grid-panel">
                    <Rock:GridFilter ID="rFilter" runat="server">
                        <Rock:RockTextBox ID="tbSubject" runat="server" Label="Subject" />
                        <Rock:RockDropDownList ID="ddlStatus" runat="server" Label="Status" />
                        <Rock:PersonPicker ID="ppSender" runat="server" Label="Created By" />
                        <Rock:DateRangePicker ID="drpCreatedDates" runat="server" Label="Created Date Range" Help="Note: Leaving dates blank will default to last 7 days." />
                        <Rock:DateRangePicker ID="drpSentDates" runat="server" Label="Sent Date Range" />
                        <Rock:RockTextBox ID="tbContent" runat="server" Label="Content" />
                    </Rock:GridFilter>

                    <Rock:Grid ID="gCommunication" runat="server" AllowSorting="true" OnRowSelected="gCommunication_RowSelected" OnRowDataBound="gCommunication_RowDataBound">
                        <Columns>
                            <Rock:RockBoundField DataField="Subject" SortExpression="Subject" HeaderText="Subject" />
                            <Rock:EnumField DataField="Status" SortExpression="Status" HeaderText="Status" />
                            <Rock:RockLiteralField HeaderText="Details" ID="lDetails" />
                            <Rock:RockBoundField  DataField="SendDateTimeFormat" HtmlEncode="false" HeaderStyle-HorizontalAlign="Right" ItemStyle-HorizontalAlign="Right" SortExpression="SendDateTime" ColumnPriority="Desktop" HeaderText="Sent" />
                            <Rock:RockLiteralField HeaderText="Recipients" ID="lRecipients" HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center" SortExpression="Recipients" />
                            <Rock:LinkButtonField ID="lbRemove" OnClick="gCommunication_Remove" ControlStyle-CssClass="btn btn-warning btn-sm fa fa-minus-square" ToolTip="This will remove this message from the SubSplash app inbox."></Rock:LinkButtonField>
                            <Rock:DeleteField OnClick="gCommunication_Delete" />
                        </Columns>
                    </Rock:Grid>
                </div>

            </div>
        </div>

        <Rock:ModalAlert ID="mdGridWarning" runat="server" />

        

    </ContentTemplate>
</asp:UpdatePanel>
