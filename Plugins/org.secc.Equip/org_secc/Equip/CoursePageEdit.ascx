<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CoursePageEdit.ascx.cs" Inherits="RockWeb.Plugins.org.secc.Equip.CoursePageEdit" %>

<style>
    /* The container */
    .answer-container {
        display: inline-block;
        position: relative;
        padding-left: 35px;
        margin-bottom: 19px;
        cursor: pointer;
        font-size: 22px;
        -webkit-user-select: none;
        -moz-user-select: none;
        -ms-user-select: none;
        user-select: none;
    }

        /* Hide the browser's default radio button */
        .answer-container .hidden {
            position: absolute;
            opacity: 0;
            cursor: pointer;
        }

    /* Create a custom radio button */
    .checkmark, .radiobutton {
        position: absolute;
        top: 0;
        left: 0;
        height: 25px;
        width: 25px;
        background-color: #eee;
        font-family: 'FontAwesome';
        font-weight: '900';
        font-style: normal;
        font-variant: normal;
        text-rendering: auto;
        line-height: 1;
        font-size:.9em;
        text-align:center;
    }

    .checkmark{
        border-radius:10%;
    }

    .radiobutton {
        border-radius: 50%;
    }

    /* On mouse-over, add a grey background color */
    .answer-container:hover input ~ .checkmark,.answer-container:hover input ~ .radiobutton {
        background-color: #ccc;
    }

    /* When the radio button is checked, add a blue background */
    .answer-container input:checked ~ .checkmark,.answer-container input:checked ~ .radiobutton {
        background-color: #2196F3;
    }

        /* Show the indicator (dot/circle) when checked */
        .answer-container input:checked ~ .checkmark:before,.answer-container input:checked ~ .radiobutton:before {
            content: "\f00c";
            top: 2px;
            position: relative;
            color: #ccc;
        }
</style>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <asp:Panel runat="server" ID="pnlNew">
            <div class="block-content">
                <ul>
                    <asp:Repeater runat="server" ID="rComponents" OnItemDataBound="rComponents_ItemDataBound">
                        <ItemTemplate>
                            <div class="row">
                                <div class="well clearfix col-xs-11">
                                    <asp:LinkButton runat="server" ID="btnComponent" CssClass="btn btn-default col-md-4"
                                        OnClick="btnComponent_Click" CausesValidation="false" />
                                    <div class="col-md-8 text-center">
                                        <h3>
                                            <asp:Literal Text='<%# Eval("Name") %>' runat="server" />
                                        </h3>
                                        <asp:HiddenField runat="server" Value='<%# Eval("TypeId") %>' ID="hfComponentId" />
                                        <asp:Literal Text='<%# Eval("Description") %>' runat="server" />
                                        <asp:LinkButton Text="[Select]" runat="server" CausesValidation="false" ID="btnComponentSelect" OnClick="btnComponent_Click" />
                                    </div>
                                </div>
                            </div>
                        </ItemTemplate>
                    </asp:Repeater>
                </ul>
            </div>
        </asp:Panel>

        <asp:Panel runat="server" ID="pnlEdit">
            <div class="panel panel-block">

                <div class="panel-heading">
                    <h1 class="panel-title">
                        <asp:Literal ID="ltTitle" runat="server" />
                    </h1>
                </div>

                <Rock:PanelDrawer ID="pdAuditDetails" runat="server"></Rock:PanelDrawer>
                <div class="panel-body">
                    <Rock:RockTextBox runat="server" ID="tbName" Label="Name" Required="true" />
                    <div class="clearfix">
                        <asp:PlaceHolder runat="server" ID="phEdit" />
                    </div>
                    <br />
                    <br />
                    <asp:LinkButton runat="server" ID="btnSavePage" CssClass="btn btn-primary"
                        OnClick="btnSavePage_Click" Text="Save" />
                    <asp:LinkButton Text="Cancel" runat="server" ID="btnCancelPage" OnClick="btnCancelPage_Click" CausesValidation="false" />
                </div>
            </div>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
