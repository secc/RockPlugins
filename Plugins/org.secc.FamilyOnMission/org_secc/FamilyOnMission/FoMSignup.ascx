<%@ Control Language="C#" AutoEventWireup="true" CodeFile="FoMSignup.ascx.cs" Inherits="RockWeb.Plugins.org_secc.FamilyOnMission.FoMSignup" %>
<style>
    .set-panel {
        background-color: #FCFAF8;
    }

    .class-panel {
        box-shadow: 10px 10px 5px #888888;
    }

    .fom-spacer {
        border-bottom: solid 5px #EDEAE6;
        display: none;
    }

    .content-panel {
        margin: 15px;
    }

    .class-panel .label {
        margin: 5px;
    }

    .set-panel {
        background: none !important;
    }

    .class-panel {
        background-color: #fff !important;
        box-shadow: none !important;
        border-radius: 5px !important;
        margin-bottom: 32px;
        border: 1px solid #e6e6e6;
        padding: 0px;
    }
    .content-panel .btn.btn-primary {
        float: right;
        margin-left: 10px;
        margin-bottom: 10px;
    }

    .track {
        height: 30px;
        background-color: white;
        display: inline-block;
        padding: 4px 8px;
        color: black;
        border-left: 5px solid #e6e6e6;
        text-transform: uppercase;
    }

    .track img {
        height: 20px;
        margin-right: 5px;
    }

    .spots {
        text-align: center;
        display: inline-block;
        color: #7d7d7d;
        text-shadow: 1px 1px 3px white;
        float: right;
        line-height: 13px;
        font-size: 0.8em;
        margin-top: -5px;        
    }

    .spots span {
        font-size: 2em;
    }
</style>
<asp:UpdatePanel runat="server">
    <ContentTemplate>
        <div class="row">
            <asp:PlaceHolder runat="server" ID="phContent" />
        </div>
    </ContentTemplate>
</asp:UpdatePanel>
