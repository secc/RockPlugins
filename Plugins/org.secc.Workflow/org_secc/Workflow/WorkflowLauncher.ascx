<%@ Control Language="C#" AutoEventWireup="true" CodeFile="WorkflowLauncher.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Workflow.WorkflowLauncher" %>
<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <asp:Panel ID="pnlDetails" CssClass="panel panel-block" runat="server">

            <div class="panel-heading">
                <h1 class="panel-title pull-left">Workflow Launcher</h1>
            </div>
            <div class="panel-body">
                <div class="row">
                    <div class="col-md-2">
                        <Rock:WorkflowTypePicker ID="wtpWorkflowType" runat="server" Label="Workflow" Help="Workflow to launch" />
                    </div>
                    <div class="col-md-5">
                        <Rock:DataDropDownList ID="ddlRegistrationInstances" runat="server" Label="Registration (instances)" Help="Select a registration instance to launch a workflow for each existing registration." SourceTypeName="Rock.Model.RegistrationInstance, Rock" Required="false" DataTextField="Name" DataValueField="Id" PropertyName="Name" Visible="true" AutoPostBack="true" OnSelectedIndexChanged="ddlRegistrationInstances_SelectedIndexChanged" />
                    </div>
                    <div class="col-md-5">
                        <Rock:DataDropDownList ID="ddlRegistrations" runat="server" Label="Registration (optional)" Help="Select a specific registration to use to launch the workflow." SourceTypeName="Rock.Model.Registration, Rock"  Required="false" DataTextField="FirstName" DataValueField="Id" PropertyName="FirstName" Visible="false" AutoPostBack="true" />
                    </div>
                </div>
                <Rock:BootstrapButton ID="btnLaunch" runat="server" Text="Launch Workflow" CssClass="pull-right btn btn-primary" OnClick="Launch_Click"></Rock:BootstrapButton>
            
                <h3>Output</h3>
                <div class="well" style="background-color: #fff">
                    <asp:Literal ID="litOutput" runat="server"></asp:Literal>
                </div>
            </div>

        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
