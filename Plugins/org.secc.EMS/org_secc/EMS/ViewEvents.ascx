<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ViewEvents.ascx.cs" Inherits="RockWeb.Plugins.org_secc.EMS.ViewEvents" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <asp:Panel ID="pnlList" CssClass="panel panel-block EventPanel" runat="server">
            <style>
                .EventTable {
                    font-size: 1.8vw;
                    position: relative;
                    width:100%;
                }
                .EventTable td {
                    line-height: 1 !important;
                }
                .EventTable small {
                    font-size:1.5vw;
                }
                .container {
                    width: 100% !important;
                }
                .EventTable .grid-actions {
                    display:none;
                }
                .EventPanel .grid-filter .btn-link {
                    margin: 0px 8px !important;
                }
                .table-responsive {
                    padding: 1px
                }
                @media print {
                    .EventTable tfoot {
                        display: none;
                    }
                    .EventTable thead {
                        margin-top: -5px;
                    }
                    .grid-filter, .panel-heading, .EventTable {
                        border: 1px solid #ddd;
                    }
                    .panel {
                        border: 0px;
                    }
                }
            </style>
            <div class="panel-heading">
                <h1 class="panel-title pull-left"><i class="fa fa-list-ul"></i> View Events</h1>
                <div class="pull-right">
                    <a href="#" class="btn btn-primary hidden-print" onClick="window.print();"><i class="fa fa-print"></i> Print</a> 
                </div>
            </div>

            <div class="panel-body">

                <div class="grid grid-panel">
                    <Rock:GridFilter ID="gfSettings" runat="server">
                        <Rock:DatePicker ID="dpDate" runat="server" Label="Events For" />
                        <Rock:RockCheckBox ID="cbShowAll" runat="server" Label="Show All Events"/>
                    </Rock:GridFilter>
                                
                    <Rock:Grid ID="gScroll" CssClass="EventTable" Runat="Server" EmptyDataText="No Events Found" RowItemText="Event" AllowSorting="true" ShowActionRow="false">
                        <Columns>
                            <Rock:RockTemplateField HeaderText="Event Time" SortExpression="TimeEventStart">
                                <ItemTemplate>
                                    <%# ((DateTime)Eval("TimeEventStart")).ToString("hh:mm tt") + " - " + ((DateTime)Eval("TimeEventEnd")).ToString("hh:mm tt") %>
                                    <%# ((DateTime)Eval("TimeEventStart") != (DateTime)Eval("TimeBookingStart") || (DateTime)Eval("TimeEventEnd") != (DateTime)Eval("TimeBookingEnd"))?"<br /><small>(Access: " + ((DateTime)Eval("TimeBookingStart")).ToString("hh:mm tt") + " - " + ((DateTime)Eval("TimeBookingEnd")).ToString("hh:mm tt") + ")</small>":"" %>
                                </ItemTemplate>
                            </Rock:RockTemplateField>
                            <Rock:RockBoundField DataField="TimeEventStart" HeaderText="Date" SortExpression="TimeEventStart" DataFormatString ="{0:d}" />
                            <Rock:RockBoundField DataField="ActivityName" HeaderText="Activity Name" SortExpression="ActivityName" />
                            <Rock:RockBoundField DataField="LocationName" HeaderText="Location" SortExpression="LocationName" />
                        </Columns>
                    </Rock:Grid>
                </div>

            </div>

        </asp:Panel>

        <Rock:NotificationBox ID="nbMessage" runat="server" Title="Error" NotificationBoxType="Danger" Visible="false" />

    </ContentTemplate>
</asp:UpdatePanel>
