<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CheckinMonitor.ascx.cs" Inherits="RockWeb.Plugins.org_secc.CheckinMonitor.CheckinMonitor" %>
<style>
    #htmlAutoRefresh {
        position: fixed;
        top: 0px;
        left: 0px;
        display: none;
    }


    .close {
        visibility: hidden;
    }
</style>

<div id="htmlAutoRefresh">
    <a href="javascript:startTimer();" class="btn btn-danger">Auto-refresh Stopped
    </a>
</div>


<script type="text/javascript">
    var timer;

    var UpdPanelUpdate = function ()
    {
        console.log("Updating!");
        //__doPostBack("<%= hfReloader.ClientID %>", "");
    }

    var startTimer = function ()
    {
        clearInterval(timer);
        timer = setInterval(function () { UpdPanelUpdate() }, 30000);
        document.getElementById("htmlAutoRefresh").style.display = "none";
    }

    var stopTimer = function ()
    {
        document.getElementById("htmlAutoRefresh").style.display = "block";
        clearInterval(timer);
    }
</script>



<asp:UpdatePanel ID="upDevice" runat="server" UpdateMode="Conditional">
    <ContentTemplate>
        <asp:HiddenField ID="hfReloader" runat="server" />
        <Rock:ModalAlert ID="maError" runat="server" />

        <Rock:ModalDialog runat="server" ID="mdConfirmClose" Title="Confirm Close All" SaveButtonText="Close All" CancelLinkVisible="true" OnSaveClick="mdConfirmClose_SaveClick">
            <Content>
                <asp:Literal Text="" ID="ltConfirmClose" runat="server" />
            </Content>
        </Rock:ModalDialog>

        <Rock:ModalDialog runat="server" ID="mdSearch" SaveButtonText="Done" OnSaveClick="mdSearch_SaveClick" CancelLinkVisible="false" Title="Search">
            <Content>
                <div class="container">
                    <div class="row">
                        <div class="col-xs-6">
                            <Rock:RockTextBox runat="server" ID="tbSearch"></Rock:RockTextBox>
                        </div>
                        <div class="col-xs-6">
                            <Rock:BootstrapButton runat="server" ID="btnCodeSearch" Text="Search Code" OnClick="btnCodeSearch_Click" CssClass="btn btn-primary"></Rock:BootstrapButton>
                            <Rock:BootstrapButton runat="server" ID="btnNameSearch" Text="Search Name" OnClick="btnNameSearch_Click" CssClass="btn btn-primary"></Rock:BootstrapButton>
                        </div>
                    </div>
                </div>
                <hr />
                <asp:Literal Text="" runat="server" ID="ltSearch" />
                <asp:PlaceHolder runat="server" ID="phSearchResults" />

            </Content>
        </Rock:ModalDialog>

        <Rock:ModalDialog runat="server" ID="mdOccurrence" SaveButtonText="Done" OnSaveClick="mdOccurrence_SaveClick" CancelLinkVisible="false">
            <Content>
                <h1>
                    <asp:Literal ID="ltLocation" runat="server" />
                </h1>
                <asp:PlaceHolder runat="server" ID="phLocation" />
            </Content>
        </Rock:ModalDialog>

        <Rock:ModalDialog runat="server" ID="mdLocation" SaveButtonText="Save" OnSaveClick="mdLocation_SaveClick" CancelLinkVisible="false">
            <Content>
                <h1>
                    <asp:Literal ID="ltLocationName" runat="server" />
                </h1>
                <asp:HiddenField ID="hfLocationId" runat="server" />
                <Rock:RockTextBox ID="tbRatio" runat="server" Label="Number of kids for every adult" Help="The number of kids per adult allowed."></Rock:RockTextBox>
                <Rock:RockTextBox ID="tbSoftThreshold" runat="server" Label="Child Limit" Help="Total number of children who can check into a room."></Rock:RockTextBox>
                <Rock:RockTextBox ID="tbFirmThreshold" runat="server" Label="Hard Limit" Help="Total number of people who can occupy a room."></Rock:RockTextBox>
            </Content>
        </Rock:ModalDialog>

        <Rock:ModalDialog runat="server" ID="mdMove" SaveButtonText="Cancel" OnSaveClick="mdMove_CancelClick" CancelLinkVisible="false">
            <Content>
                <h1>
                    <asp:Literal ID="ltMove" runat="server" />
                </h1>
                <h3>
                    <asp:Literal ID="ltMoveInfo" runat="server" />
                </h3>
                <div class="row">
                    <div class="col-xs-12">
                        <Rock:RockDropDownList runat="server" ID="ddlMove" Label="Move To:"></Rock:RockDropDownList>
                        <Rock:BootstrapButton ID="btnMove" runat="server" Text="Move" OnClick="btnMove_Click" CssClass="btn btn-success"></Rock:BootstrapButton>
                    </div>
                </div>
            </Content>
        </Rock:ModalDialog>

        <div class="col-md-6">
            <Rock:BootstrapButton runat="server" ID="btnBack" Text="Back" OnClick="btnBack_Click" CssClass="btn btn-warning"></Rock:BootstrapButton>
            <Rock:BootstrapButton runat="server" ID="btnRefresh" Text="Refresh" OnClick="btnRefresh_Click" CssClass="btn btn-primary"></Rock:BootstrapButton>
            <Rock:BootstrapButton runat="server" ID="btnSearch" Text="Search" OnClick="btnSearch_Click" CssClass="btn btn-info"></Rock:BootstrapButton>
            <Rock:BootstrapButton runat="server" ID="btnCloseAll" Visible="false" CssClass="btn btn-danger"
                OnClick="btnCloseAll_Click" Text="Close All For This Schedule"></Rock:BootstrapButton>
        </div>
        <div class="col-md-6">
            <Rock:RockDropDownList runat="server" ID="ddlSchedules" DataValueField="Id" DataTextField="Name"
                CssClass="btn btn-default" OnSelectedIndexChanged="ddlSchedules_SelectedIndexChanged" AutoPostBack="true">
            </Rock:RockDropDownList>

        </div>
        <br />
        <br />
        <asp:PlaceHolder runat="server" ID="phContent" />
    </ContentTemplate>
</asp:UpdatePanel>

<script type="text/javascript">
    var prm = Sys.WebForms.PageRequestManager.getInstance();
    prm.add_initializeRequest(InitializeRequest);

    function InitializeRequest(sender, args)
    {
        var updateProgress = $get('updateProgress');
        var postBackElement = args.get_postBackElement();
        if (postBackElement.id == '<%= hfReloader.ClientID %>')
        {
            updateProgress.control._associatedUpdatePanelId = 'dummyId';
        }
        else
        {
            updateProgress.control._associatedUpdatePanelId = null;
        }
    }

</script>

