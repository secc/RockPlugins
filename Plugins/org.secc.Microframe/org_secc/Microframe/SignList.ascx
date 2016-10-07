<%@ Control Language="C#" AutoEventWireup="true" CodeFile="SignList.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Microframe.SignList" %>

<asp:UpdatePanel ID="upDevice" runat="server">
    <ContentTemplate>

        <Rock:ModalAlert ID="mdGridWarning" runat="server" />
        
        <div class="panel panel-block">
            <div class="panel-heading">
                <h1 class="panel-title"><i class="fa fa-wifi"></i> Sign List</h1>
            </div>
            <div class="panel-body">
                <div class="grid grid-panel">
                    <Rock:Grid ID="gSigns" runat="server" AllowSorting="true" OnRowSelected="gSigns_Edit">
                        <Columns>
                            <Rock:RockBoundField DataField="Name" HeaderText="Name" SortExpression="Name" />
                            <Rock:RockBoundField DataField="IPAddress" HeaderText="IP Address" SortExpression="Address" />
                            <Rock:RockBoundField DataField="Categories" HeaderText="Sign Categories" SortExpression="Categories" />
                            <Rock:DeleteField OnClick="gSigns_Delete" />
                        </Columns>
                    </Rock:Grid>
                </div>
                
            </div>
        </div>

    </ContentTemplate>
</asp:UpdatePanel>
