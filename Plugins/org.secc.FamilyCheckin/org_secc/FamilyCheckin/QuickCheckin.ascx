<%@ Control Language="C#" AutoEventWireup="true" CodeFile="QuickCheckin.ascx.cs" Inherits="RockWeb.Plugins.org_secc.FamilyCheckin.QuickCheckin" %>
<style>
.row {
    margin-right: 0px;
    margin-left: 0px;
}

.modal {
    width:90vw;
    height:90vh;
}

.close {
    visibility:hidden;
}

.header {
    border-radius:3px;
    background-color:#F5F5F5;
    padding:20px;
    margin-bottom:15px;
}
.ParentGroupTypeHeader a, .ParentGroupTypeHeader a:hover {
    text-shadow: 2px 2px 4px rgba(150, 150, 150, 1);
    display:inline;
    cursor:pointer;
    font-weight:bold;
    font-size:4em;
    color:#DB542D;
    text-decoration:none;
}

.successModal {
    display:none;
    position:absolute;
    width:70vw;
    left:15vw;
    top:100vh;
}
</style>

<asp:UpdatePanel ID="upContent" runat="server">
<ContentTemplate>

    <script>
        
        Sys.Application.add_load(function () {
            $('.tenkey a.digit').click(function () {
                $phoneNumber = $("input[id$='tbPhone']");
                $phoneNumber.val($phoneNumber.val() + $(this).html());
                findFamilies($phoneNumber.val());
            });
            $('.tenkey a.back').click(function () {
                $phoneNumber = $("input[id$='tbPhone']");
                $phoneNumber.val($phoneNumber.val().slice(0, -1));
                findFamilies($phoneNumber.val());
            });
            $('.tenkey a.clear').click(function () {
                $phoneNumber = $("input[id$='tbPhone']");
                $phoneNumber.val('');
                findFamilies($phoneNumber.val());
            });

            // set focus to the input unless on a touch device
            var isTouchDevice = 'ontouchstart' in document.documentElement;
            if (!isTouchDevice) {
                $('.checkin-phone-entry').focus();
            }
        });

        var doCheckin = function () {
            var content = document.getElementById("content");
            document.body.style.overflow = "hidden";
            content.style.transitionDuration = "0.2s";
            content.style.transform = "translateY(100vh)";

            var success = document.getElementById("success");
            success.style.display = "block";
            success.style.transform = "translateY(-90vh)";
            __doPostBack("<%= btnCheckin.UniqueID%>", "OnClick");
        }
        
    </script>
    <asp:Panel ID="pnlMain" runat="server" style="margin-top:10px;">
    <Rock:ModalDialog ID="mdChoose" CssClass="modal" runat="server">
        <Content>
            <div class="row">
                <asp:PlaceHolder runat="server" ID="phModal" />
            </div>
        </Content>
    </Rock:ModalDialog>
    <Rock:ModalAlert ID="maNotice" runat="server" />
<div class="container" id="content">
    <div class="col-xs-12" id="padding">
        <div class="header">
            <span class="ParentGroupTypeHeader">
                <Rock:BootstrapButton  ID="btnParentGroupTypeHeader" OnClick="btnParentGroupTypeHeader_Click"
                    runat="server">
                </Rock:BootstrapButton>
            </span>
            <span class="pull-right" style="padding-top:10px;">
                <a href="javascript:doCheckin()" class="btn btn-lg btn-primary">Check-In</a>
                <Rock:BootstrapButton runat="server" Visible="false" ID="btnCheckin" CssClass="btn btn-lg btn-primary" OnClick="btnCheckin_Click" ></Rock:BootstrapButton>
                <Rock:BootstrapButton runat="server" ID="btnCancel" CssClass="btn btn-lg btn-default " OnClick="btnCancel_Click"  DataLoadingText="<i class='fa fa-refresh fa-spin'></i> Canceling">Cancel</Rock:BootstrapButton>
            </span>
        </div>
    </div>
    
    <asp:PlaceHolder runat="server" ID="phPeople"/>
   
</div>
    </asp:Panel>
    <div id="success" class="text-center alert alert-success successModal">
        
        <h2>Welcome.</h2>
        <h2>We are printing your name tags now.</h2>
    </div>
    </ContentTemplate>
</asp:UpdatePanel>
