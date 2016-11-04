<%@ Control Language="C#" AutoEventWireup="true" CodeFile="SignCategoryList.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Microframe.SignCategoryList" %>

<asp:UpdatePanel ID="upDevice" runat="server">
    <ContentTemplate>

        <Rock:ModalAlert ID="mdGridWarning" runat="server" />
        
        <div class="panel panel-block">
            <div class="panel-heading">
                <h1 class="panel-title"><i class="fa fa-server"></i> Sign Category List</h1>
            </div>
            <div class="panel-body">

                <div class="grid grid-panel">
                    <Rock:Grid ID="gSignCategories" runat="server" AllowSorting="true" OnRowSelected="gSignCategories_Edit">
                        <Columns>
                            <Rock:RockBoundField DataField="Name" HeaderText="Name" SortExpression="Name" />
                            <Rock:DeleteField OnClick="gSignCategories_Delete" />
                        </Columns>
                    </Rock:Grid>
                </div>

            </div>
        </div>

    </ContentTemplate>
</asp:UpdatePanel>
