<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CheckinMonitor.ascx.cs" Inherits="RockWeb.Plugins.org_secc.CheckinMonitor.CheckinMonitor" %>

<script type="text/javascript">
    var timer;

    var UpdPanelUpdate = function ()
    {
        console.log("Updating!");
        //__doPostBack("<%= hfReloader.ClientID %>", "");
    }

    var startTimer = function ()
    {
        timer = setInterval(function () { UpdPanelUpdate() }, 10000);
    }

    var stopTimer = function ()
    {
        clearInterval(timer);
    }

</script>


<asp:UpdatePanel ID="upDevice" runat="server" UpdateMode="Conditional">
    <ContentTemplate>
        <asp:HiddenField ID="hfReloader" runat="server" />
        <Rock:ModalAlert ID="maError" runat="server" />

        <Rock:ModalDialog runat="server" ID="mdMove" SaveButtonText="Cancel" OnSaveClick="mdMove_CancelClick" CancelLinkVisible="false">
            <Content>
                <h1>
                    <asp:Literal ID="ltMove" runat="server" /></h1>
                <asp:DropDownList runat="server" ID="ddlMove" CssClass="btn btn-default" Label="Move To:"></asp:DropDownList>
                <Rock:BootstrapButton ID="btnMove" runat="server" Text="Move" OnClick="btnMove_Click" CssClass="btn btn-success"></Rock:BootstrapButton>
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
        <div class="col-md-6">
            <Rock:BootstrapButton runat="server" ID="btnBack" Text="Back" OnClick="btnBack_Click" CssClass="btn btn-warning"></Rock:BootstrapButton>
            <Rock:BootstrapButton runat="server" ID="btnRefresh" Text="Refresh" OnClick="btnRefresh_Click" CssClass="btn btn-primary"></Rock:BootstrapButton>
        </div>
        <div class="col-md-6">
            <Rock:RockDropDownList runat="server" ID="ddlSchedules" DataValueField="Id" DataTextField="Name"
                CssClass="btn btn-default" OnSelectedIndexChanged="ddlSchedules_SelectedIndexChanged" AutoPostBack="true">
            </Rock:RockDropDownList>
        </div>
        <br /><br />
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

