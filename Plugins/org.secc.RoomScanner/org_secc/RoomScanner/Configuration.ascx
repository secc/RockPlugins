<%@ Control Language="C#" AutoEventWireup="true" CodeFile="Configuration.ascx.cs" Inherits="RockWeb.Plugins.org_secc.RoomScanner.Configuration" %>

<asp:UpdatePanel ID="pnlConfiguration" runat="server" Class="panel panel-block">
    <ContentTemplate>
        <div class="panel-heading">
            <h1 class="panel-title"><i class="se se-kids"></i>RoomScanner Server Configuration</h1>
        </div>
        <div class="panel-body">

            <asp:ValidationSummary ID="vsDetails" runat="server" HeaderText="Please Correct the Following" CssClass="alert alert-danger" />

            <div class="row">
                <div class="col-md-6">
                  <Rock:RockTextBox runat="server" ID="tbAllowedGroupId" Label="Approved Override Group Id" Help="The id of the group which contains people able to allow the override of a move" />
                </div>
                <div class="col-md-6">
                  <Rock:RockTextBox runat="server" ID="tbSubroomLocationTypeId" Label="Subroom Location Type Id" Help="The id of the defined value which represents the subroomlocation type" />
                </div>
                <div class="col-md-12">
                    <Rock:BootstrapButton ID="btnSave" CssClass="btn btn-primary pull-right" runat="server" Text="Save" OnClick="btnSave_Click" />
                </div>
            </div>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>

