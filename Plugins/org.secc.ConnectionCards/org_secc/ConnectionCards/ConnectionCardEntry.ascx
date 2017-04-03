<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ConnectionCardEntry.ascx.cs" Inherits="RockWeb.Plugins.org_secc.ConnectionCards.ConnectionCardEntry" %>

<Rock:RockUpdatePanel ID="upMain" runat="server">
    <ContentTemplate>
        <asp:HiddenField ID="hfImageGuid" runat="server" />
        <Rock:NotificationBox runat="server" Visible="false" ID="nbSuccess" NotificationBoxType="Success" Text="Workflows have been started"></Rock:NotificationBox>
        <asp:Panel runat="server" ID="pnlUpload">
            <div class="panel panel-default">
                <div class="panel-heading">
                    <h3 class="panel-title">Connection Card Workflow
                    </h3>
                </div>
                <div class="panel-body">
                    <Rock:FileUploader ID="fMainSheet" runat="server" OnFileUploaded="fMainSheet_FileUploaded" />
                    To begin, click or drag a pdf onto "<i class="fa fa-upload"></i> Upload".
                </div>
            </div>
        </asp:Panel>
        <asp:Panel runat="server" ID="pnlEdit" Visible="false">
            <div class="panel panel-default">
                <div class="panel-heading">
                    <Rock:BootstrapButton runat="server" ID="btnRotLeft" CssClass="btn btn-lg btn-primary" ToolTip="Rotate Left"
                        OnClick="btnRotLeft_Click" DataLoadingText="<i class='fa fa-undo fa-spin' style='animation-direction: reverse;'></i>"
                        Text="<i class='fa fa-undo'></i>"></Rock:BootstrapButton>
                    <Rock:BootstrapButton runat="server" ID="btnRotRight" CssClass="btn btn-lg btn-primary"  ToolTip="Rotate Right"
                        OnClick="btnRotRight_Click" Text="<i class='fa fa-repeat'></i>"
                        DataLoadingText="<i class='fa fa-repeat fa-spin'></i>"></Rock:BootstrapButton>
                    <div class="pull-right">

                        <Rock:BootstrapButton runat="server" ID="btnCrop" OnClick="btnCrop_Click" Text="Finished" CssClass="btn btn-lg btn-success">
                        </Rock:BootstrapButton>

                        <Rock:BootstrapButton runat="server" ID="btnBack" OnClick="btnBack_Click" Text="Cancel" CssClass="btn btn-lg btn-danger">
                        </Rock:BootstrapButton>
                    </div>
                </div>
                <div class="panel-body">
                    <asp:Image runat="server" ID="iPicture" CssClass="img-responsive center-block" />
                </div>
            </div>
        </asp:Panel>
    </ContentTemplate>
</Rock:RockUpdatePanel>
