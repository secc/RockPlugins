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
    }

    .content-panel {
        margin: 0px 10px 7px 10px;
    }

    .class-panel .label {
        margin: 5px;
    }
</style>
<asp:UpdatePanel runat="server">
    <ContentTemplate>
        <div class="row">
            <asp:PlaceHolder runat="server" ID="phContent" />
        </div>
    </ContentTemplate>
</asp:UpdatePanel>
