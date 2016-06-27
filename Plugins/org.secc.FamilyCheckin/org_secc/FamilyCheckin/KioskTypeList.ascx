<%@ Control Language="C#" AutoEventWireup="true" CodeFile="KioskTypeList.ascx.cs" Inherits="RockWeb.Plugins.org_secc.FamilyCheckin.KioskTypeList" %>

<asp:UpdatePanel ID="upDevice" runat="server">
    <ContentTemplate>

        <Rock:ModalAlert ID="mdGridWarning" runat="server" />
        
        <div class="panel panel-block">
            <div class="panel-heading">
                <h1 class="panel-title"><i class="fa fa-hand-pointer-o"></i> Kiosk Type List</h1>
            </div>
            <div class="panel-body">

                <div class="grid grid-panel">
                    <Rock:Grid ID="gKioskTypes" runat="server" AllowSorting="true" OnRowSelected="gKioskType_Edit">
                        <Columns>
                            <Rock:RockBoundField DataField="Name" HeaderText="Name" SortExpression="Name" />
                            <Rock:DeleteField OnClick="gKioskType_Delete" />
                        </Columns>
                    </Rock:Grid>
                </div>

            </div>
        </div>

    </ContentTemplate>
</asp:UpdatePanel>
