<%@ Control Language="C#" AutoEventWireup="true" CodeFile="RoomRatio.ascx.cs" Inherits="RockWeb.Plugins.org_secc.CheckinMonitor.RoomRatio" %>

<asp:UpdatePanel ID="upDevice" runat="server">
    <ContentTemplate>
        <Rock:ModalAlert ID="mdGridWarning" runat="server" />
        
        <div class="panel panel-block">
            <div class="panel-heading">
                <h1 class="panel-title"><i class="fa fa-desktop"></i> Room Manager</h1>
                
        <ul class="nav navbar-nav contextsetter contextsetter-campus pull-right">
            <li class="dropdown">

                <a class="dropdown-toggle navbar-link btn btn-default" href="#" data-toggle="dropdown">
                    <asp:Literal ID="lCurrentSelection" runat="server" />
                    <b class="fa fa-caret-down"></b>
                </a>

                <ul class="dropdown-menu">
                    <asp:Repeater runat="server" ID="rptCampuses" OnItemCommand="rptCampuses_ItemCommand">
                        <ItemTemplate>
                            <li>
                                <asp:LinkButton ID="btnCampus" runat="server" Text='<%# Eval("Name") %>' CommandArgument='<%# Eval("Id") %>' />
                            </li>
                        </ItemTemplate>
                    </asp:Repeater>
                </ul>
            </li>
        </ul>

            </div>
            <div class="panel-body">
                <div class="grid grid-panel">
                    <Rock:Grid ID="gLocations" DataKeyNames="Id" runat="server" AllowSorting="true">
                        <Columns>
                            <Rock:RockBoundField DataField="Name" HeaderText="Name" SortExpression="Name" />
                            <Rock:RockBoundField DataField="ChildCount" HeaderText="Children" SortExpression="Child Count" />
                            <Rock:RockBoundField DataField="AdultCount" HeaderText="Adults" SortExpression="Adult Count" />
                            <Rock:RockBoundField DataField="CurrentRatio" HeaderText="Current Ratio" SortExpression="Current Ratio" />
                            <Rock:RockBoundField DataField="Ratio" HeaderText="Room Ratio" SortExpression="Room Ratio" />
                            <Rock:BadgeField OnSetBadgeType="IsFull_SetBadgeType" DataField="IsFull" HeaderText="Status"
                                ImportantMin="0" ImportantMax="0"
                                InfoMin="1" InfoMax="1" SuccessMin="2"  />
                            <Rock:ToggleField DataField="IsActive"  HeaderText="Room Status" OnCssClass="btn-success" OffCssClass="btn-danger" OnCheckedChanged="IsActive_CheckedChanged" Enabled="True" OnText="Open" OffText="Closed" />
                        </Columns>
    </Rock:Grid>
</div>
            </div>
        </div>

    </ContentTemplate>
</asp:UpdatePanel>
