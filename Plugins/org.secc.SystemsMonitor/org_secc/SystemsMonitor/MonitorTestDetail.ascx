<%@ Control Language="C#" AutoEventWireup="true" CodeFile="MonitorTestDetail.ascx.cs" Inherits="RockWeb.Plugins.org_secc.SystemsMonitor.MonitorTestDetail" %>
<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <asp:Panel ID="pnlView" runat="server" CssClass="panel panel-block">

            <div class="panel-heading">
                <h1 class="panel-title">
                    <i class="fa fa-tv"></i>
                    <asp:Literal ID="ltName" runat="server" />
                </h1>
            </div>
            <div class="panel-body">
                <Rock:RockDropDownList runat="server" ID="ddlComponent" Label="Test Type"
                    DataTextField="Name" DataValueField="TypeId"
                    AutoPostBack="true" OnSelectedIndexChanged="ddlComponent_SelectedIndexChanged" />
                <Rock:RockTextBox runat="server" ID="tbName" Label="Test Name" Required="true" />
                <div class="row">
                    <div class="col-md-4">
                        <Rock:RockTextBox runat="server" ID="tbInterval" Label="Test Interval" Help="Frequency of testing in minutes." />
                    </div>
                    <div class="col-md-4">
                        <Rock:RockDropDownList runat="server" ID="ddlAlarmCondition"
                            Label="Alarm Condition" DataTextField="Value" DataValueField="Key" />
                    </div>
                    <div class="col-md-4">
                        <Rock:RockCheckBoxList runat="server" ID="cblAlarmNotification"
                            Label="Alarm Notification" DataTextField="Value" DataValueField="Key" RepeatDirection="Horizontal" />
                    </div>
                </div>
                <asp:PlaceHolder runat="server" ID="phAttributes" />
                <Rock:BootstrapButton runat="server" ID="btnSave" Text="Save"
                    CssClass="btn btn-primary" OnClick="btnSave_Click" />
                <asp:LinkButton runat="server" ID="lbCancel" Text="Cancel"
                    CssClass="btn btn-link" CausesValidation="false" OnClick="lbCancel_Click" />
            </div>
        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>
