<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GroupRecommit.ascx.cs" Inherits="RockWeb.Plugins.org_secc.GroupManager.GroupRecommit" ViewStateMode="Enabled" EnableViewState="true" %>


<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <Rock:NotificationBox runat="server" ID="nbWarning" Visible="false"
            NotificationBoxType="Warning">

        </Rock:NotificationBox>
        <div class="panel panel-default">
            <div class="panel-heading">Information</div>
            <div class="panel-body">
                <Rock:RockTextBox runat="server" ID="tbName" Label="Group Name" Help="The name of your group."></Rock:RockTextBox>

                <Rock:DayOfWeekPicker ID="dowWeekly" runat="server" CssClass="input-width-md" Label="Day of the Week" />

                <Rock:TimePicker ID="timeWeekly" runat="server" Label="Time of Day" />

                <Rock:LocationPicker runat="server" ID="lopAddress" Label="Address" AllowedPickerModes="Address" />
            </div>
        </div>
        <div class="panel panel-default">
            <div class="panel-heading">Filters</div>
            <div class="panel-body">
                <asp:PlaceHolder runat="server" ID="phAttributes" EnableViewState="false"></asp:PlaceHolder>
            </div>
        </div>

        <div class="panel panel-default">
            <div class="panel-heading">Members</div>
            <div class="panel-body">
            </div>
        </div>

        <Rock:BootstrapButton runat="server" ID="btnSave" Text="Sign Up For Group" CssClass="btn btn-primary"></Rock:BootstrapButton>


    </ContentTemplate>
</asp:UpdatePanel>
