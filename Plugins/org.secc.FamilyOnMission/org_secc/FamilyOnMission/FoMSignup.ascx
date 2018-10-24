<%@ Control Language="C#" AutoEventWireup="true" CodeFile="FoMSignup.ascx.cs" Inherits="RockWeb.Plugins.org_secc.FamilyOnMission.FoMSignup" %>
<style>
    .time-panel h1 {
        text-shadow: 1px 1px 3px #8C8C8C;
    }

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
        margin: 0px 10px 7px 10px;
    }

    .class-panel .label {
        margin: 5px;
    }

    .set-panel {
        background: none !important;
    }

    .class-panel {
        background: #fff !important;
        box-shadow: 4px 5px 5px #888888 !important;
        border-radius: 5px !important;
        margin: 0px !important;
        padding-bottom: 35px;
    }

    .content-panel .btn.btn-primary {
        float: right;
    }

    .track {
        height: 30px;
        background-color: #dbd5cb;
        display: inline-block;
        border-radius: 4px 4px;
        padding: 3px 7px 5px 7px;
        color: #7d7d7d;
        text-shadow: 1px 1px 3px white;
        float: right;
        margin-top: 6px;
    }

        .track img {
            height: 20px;
            margin-right: 5px;
        }

    .spots {
        height: 30px;
        text-align: center;
        display: inline-block;
        color: #7d7d7d;
        text-shadow: 1px 1px 3px white;
        float: left;
        line-height: 13px;
        margin: 10px 10px 10px 10px;
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
