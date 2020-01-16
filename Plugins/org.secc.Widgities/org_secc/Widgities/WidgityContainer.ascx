<%@ Control Language="C#" AutoEventWireup="true" CodeFile="WidgityContainer.ascx.cs" Inherits="RockWeb.Plugins.org.secc.Widgities.WidgityContainer" %>

<style>
    .widgityMenu {
        background-color: #3d3d3d;
        padding: 20px;
        min-height: 100vh;
        display: block;
        overflow: auto;
        color: #fafafa;
    }

    .widgityContent {
        border: 0px;
        display: flex;
    }

    .widgity-edit {
        background-color: #3d3d3d;
        width: 0px;
        text-align: center;
        padding-top: 10px;
        transition: width 0.1s ease-in-out;
        min-height: 45px;
    }

    .widgityContent .widgity-edit-pencil {
        display: none;
        color: #fafafa;
    }

    .widgityContent:hover {
        box-shadow: 0 4px 8px 0 rgba(0, 0, 0, 0.2), 0 6px 20px 0 rgba(0, 0, 0, 0.19);
    }

        .widgityContent:hover .widgity-edit-pencil {
            display: block;
        }

        .widgityContent:hover .widgity-edit {
            width: 45px;
        }

    .active-edit {
        box-shadow: 0 4px 8px 0 rgba(0, 0, 0, 0.2), 0 6px 20px 0 rgba(0, 0, 0, 0.19);
    }

        .active-edit .widgity-edit {
            width: 45px;
        }

        .active-edit .widgity-edit-pencil {
            display: block !important;
        }

    #widgititem-controls {
        color: #3d3d3d;
    }
</style>

<asp:UpdatePanel ID="upnlContent" runat="server" CssClass="panel panel-block">
    <ContentTemplate>
        <asp:LinkButton ID="lbDragCommand" runat="server" CssClass="hidden" />

        <asp:Panel runat="server" Visible="false" ID="pnlMenu" CssClass="widgityMenu col-md-3">
            <asp:Panel runat="server" ID="pnlWidgityTypes">
                <div id="widgitySource">
                    <asp:Repeater runat="server" ID="rpWidgityTypes">
                        <ItemTemplate>
                            <div class="btn btn-default btn-block" data-component-id="<%# Eval( "Id" ) %>">
                                <i class="<%# Eval( "Icon" ) %>"></i>
                                <br />
                                <%# Eval( "Name" ) %>
                            </div>
                        </ItemTemplate>
                    </asp:Repeater>
                </div>
                <hr />
                <Rock:BootstrapButton runat="server" ID="btnSave" CssClass="btn btn-primary" Text="Save" OnClick="btnSave_Click" />
                <asp:LinkButton Text="Cancel" runat="server" ID="btnCancel" OnClick="btnCancel_Click" />
            </asp:Panel>

            <asp:Panel runat="server" ID="pnlWidgityEdit" Visible="false">
                <h3>
                    <asp:Literal runat="server" ID="ltWidgityTypeName" />
                </h3>
                <asp:PlaceHolder runat="server" ID="phAttributesEdit" />
                <asp:Panel ID="pnlWidgitItems" runat="server">
                    <hr />
                    <h4>Items</h4>
                    <div id="widgititem-controls">
                        <asp:PlaceHolder ID="phItems" runat="server" />
                    </div>
                    <asp:LinkButton ID="btnAddItem" Text="Add Item" CausesValidation="false" OnClick="btnAddItem_Click"
                        runat="server" CssClass="btn btn-default btn-xs" />
                </asp:Panel>
                <hr />
                <Rock:BootstrapButton runat="server" ID="btnSaveEdit" Text="Save" CssClass="btn btn-primary" OnClick="btnSaveEdit_Click" />
                <asp:LinkButton runat="server" ID="btnCancelEdit" Text="Cancel" OnClick="btnCancelEdit_Click" />
                <Rock:BootstrapButton runat="server" ID="btnDeleteWidgity" CssClass="btn btn-danger pull-right" Text="Delete" OnClick="btnDeleteWidgity_Click" />
            </asp:Panel>
        </asp:Panel>
        <asp:Panel runat="server" ID="pnlContent">
            <div id="widgityDestination" style="min-height: 50px">
                <asp:PlaceHolder runat="server" ID="phWidgities" />
            </div>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>

<script>
    function InitDrag() {
        var widgityDrake = dragula([document.getElementById('widgitySource'), document.getElementById('widgityDestination')], {
            revertOnSpill: true,
            copySortSource: true,
            accepts: function (el, target) {
                return target.id === 'widgityDestination';
            }
        });

        widgityDrake.on('drop', function (el, target, source, sibling) {
            var component = $(el).data('component-id');
            var order = $(target).children().index(el);
            if (source.id === "widgitySource") {
                var postback = "javascript:__doPostBack('<%= lbDragCommand.ClientID %>', 'add-widgity|" + component + "|" + order + "')";
            }
            else {
                var postback = "javascript:__doPostBack('<%= lbDragCommand.ClientID %>', 'move-widgity|" + component + "|" + order + "')";
            }
            window.location = postback;
        });

        var widgityItemDrake = dragula([document.getElementById('widgititem-controls')], {
            revertOnSpill: true,
            copySortSource: true,
            moves: function (el, container, handle) {
                return handle.classList.contains('ui-sortable-handle');
            },
            accepts: function (el, target) {
                return target.id === 'widgititem-controls';
            }
        });

        widgityItemDrake.on('drop', function (el, target, source, sibling) {
            var component = $(el).data('component-id');
            var order = $(target).children().index(el);

            var postback = "javascript:__doPostBack('<%= lbDragCommand.ClientID %>', 'move-widgity-item|" + component + "|" + order + "')";
            window.location = postback;
        });

    }

</script>


