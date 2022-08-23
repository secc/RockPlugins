<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GroupMemberTracker.ascx.cs" Inherits="RockWeb.Plugins.org_secc.GroupManager.GroupMemberTracker" %>
<asp:UpdatePanel ID="upMain" runat="server">
    <ContentTemplate>
        <Rock:NotificationBox ID="nbMain" runat="server" NotificationBoxType="Info" />
        <asp:Panel ID="pnlMain" runat="server" Visible="false">
            <asp:Panel ID="pnlGroupMember" runat="server" Visible="true">
                <div class="row">
                    <div class="col-md-12 ">
                        <span class="pull-right">
                            <Rock:ButtonDropDownList ID="ddlFilter" Label="Display Options" runat="server" OnSelectionChanged="ddlFilter_SelectionChanged">
                                <asp:ListItem Value="1" Text="Enroute" Selected="True" />
                                <asp:ListItem Value="2" Text="Arrived" />
                                <asp:ListItem Value="-1" Text="All Checked-in" />
                            </Rock:ButtonDropDownList>
                        </span>
                    </div>
                </div>
                <h2>Group Members</h2>
                <div class="row">
                    <asp:Repeater ID="rGroupMember" runat="server" OnItemDataBound="rGroupMember_ItemDataBound">
                        <ItemTemplate>

                            <div class="col-md-6">
                                <div class="panel panel-default" style="min-height: 150px;">
                                    <div class="panel-heading">
                                        <asp:Label ID="lName" runat="server" Style="font-weight: bold;"
                                            Text='<%# Eval("Name") %>' />
                                        <Rock:HighlightLabel runat="server" ID="hlCheckinStatus" />
                                    </div>
                                    <div class="panel-body">
                                        <div class="col-sm-4 thumbnail" style="border-style: none;">
                                            <img src='<%# Eval("PhotoUrl") %>' style="max-height: 100px;" alt="Photo" />
                                        </div>
                                        <div class="col-sm-8">
                                            <Rock:RockLiteral ID="lPhoneNumber" Label="Mobile Phone" runat="server" />
                                            <Rock:RockLiteral ID="lIcons" Label="Notifications" runat="server" />
                                            <div style="text-align: center;">
                                                <Rock:BootstrapButton ID="btnCheckin" runat="server" CssClass="btn-lg btn-success" Text="Has Arrived" />
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </ItemTemplate>
                    </asp:Repeater>
                </div>
               <Rock:NotificationBox ID="nbNoResults" runat="server" Visible="false" NotificationBoxType="Default" />
            </asp:Panel>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
