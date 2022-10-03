<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GroupMemberTracker.ascx.cs" Inherits="RockWeb.Plugins.org_secc.GroupManager.GroupMemberTracker" %>
<asp:UpdatePanel ID="upMain" runat="server">
    <ContentTemplate>
        <Rock:NotificationBox ID="nbMain" runat="server" NotificationBoxType="Info" />
        <asp:Panel ID="pnlMain" runat="server" Visible="false">
            <asp:Panel ID="pnlGroupMember" runat="server" Visible="true">
                <div class="row">
                    <div class="col-xs-8 col-sm-6  ">
                        <Rock:ButtonDropDownList ID="ddlFilter" Label="View" runat="server" OnSelectionChanged="ddlFilter_SelectionChanged">
                            <asp:ListItem Value="1" Text="Enroute" Selected="True" />
                            <asp:ListItem Value="2" Text="Arrived" />
                            <asp:ListItem Value="-1" Text="All Checked-in" />
                        </Rock:ButtonDropDownList>

                    </div>
                    <div class="col-xs-4 col-sm-6">
                        <span class="pull-right">
                            <label class="control-label">&nbsp;</label>

                            <div class="control-wrapper ">
                                <asp:LinkButton ID="lbShowNotes" runat="server" OnClick="lbShowNotes_Click" CssClass="btn btn-default"><i class="far fa-sticky-note"></i></asp:LinkButton>
                                <asp:LinkButton ID="lbRefresh" runat="server" OnClick="lbRefresh_Click" CssClass="btn btn-default"><i class="far fa-sync-alt"></i></asp:LinkButton>
                            </div>
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
                                                <Rock:BootstrapButton ID="btnCheckin" runat="server" CssClass="btn-lg btn-success" Text="Has Arrived" DataLoadingText="Checking In" />
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </ItemTemplate>
                    </asp:Repeater>
                </div>
                <Rock:NotificationBox ID="nbNoResults" runat="server" Visible="false" NotificationBoxType="Info" />
            </asp:Panel>
        </asp:Panel>
        <Rock:ModalDialog ID="mdlNoteDialog" runat="server" Title="Meeting Notes" OnSaveClick="mdlNoteDialog_SaveClick">
            <Content>
                <asp:HiddenField ID="hfOccurrenceId" runat="server" />
                <Rock:RockTextBox ID="tbGroupNote" runat="server" Required="false" TextMode="MultiLine" Rows="4" MaxLength="1000" ShowCountDown="true" Label="Notes" />
            </Content>
        </Rock:ModalDialog>
        <asp:Timer ID="tmrRefresh" runat="server" Interval="10000" OnTick="tmrRefresh_Tick" Enabled="false" />
    </ContentTemplate>
</asp:UpdatePanel>

