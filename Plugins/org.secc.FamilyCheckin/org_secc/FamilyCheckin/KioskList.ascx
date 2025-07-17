﻿<%@ Control Language="C#" AutoEventWireup="true" CodeFile="KioskList.ascx.cs" Inherits="RockWeb.Plugins.org_secc.FamilyCheckin.KioskList" %>

<asp:UpdatePanel ID="upDevice" runat="server">
    <ContentTemplate>

        <Rock:ModalAlert ID="mdGridWarning" runat="server" />
        
        <div class="panel panel-block">
            <div class="panel-heading">
                <h1 class="panel-title"><i class="fa fa-power-off"></i> Kiosk List</h1>
            </div>
            <div class="panel-body">

                <div class="grid grid-panel">
                    <Rock:Grid ID="gKiosks" runat="server" AllowSorting="true" OnRowSelected="gKiosk_Edit">
                        <Columns>
                            <Rock:RockBoundField DataField="Name" HeaderText="Name" SortExpression="Name" />
                            <Rock:RockBoundField DataField="KioskType" HeaderText="Kiosk Type" SortExpression="KioskType" />
                            <Rock:RockBoundField DataField="Description" HeaderText="Description" />
                            <Rock:RockBoundField DataField="Category.Name" HeaderText="Category" NullDisplayText="[Uncategorized]" SortExpression="Category" />
                            <Rock:RockBoundField DataField="PrintFrom" HeaderText="Print From" SortExpression="PrintFrom" />
                            <Rock:DeleteField OnClick="gKiosk_Delete" />
                        </Columns>
                    </Rock:Grid>
                </div>

            </div>
        </div>

    </ContentTemplate>
</asp:UpdatePanel>
