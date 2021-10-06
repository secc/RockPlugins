<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ChapterList.ascx.cs" Inherits="RockWeb.Plugins.org.secc.Equip.ChapterList" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <div class="panel panel-block">

            <div class="panel-heading">
                <h1 class="panel-title pull-left">
                    <i class="fa fa-book"></i>
                    Chapters
                </h1>

                <div class="panel-labels">
                    <asp:LinkButton runat="server" ID="btnAddChapter" Text="Add Chapter" CausesValidation="false"
                        CssClass="btn btn-default" OnClick="btnAddChapter_Click" />
                </div>
            </div>

            <div class="panel-body">
                <div class="grid grid-panel">
                    <Rock:Grid ID="gList" runat="server" DataKeyNames="Id" OnRowSelected="gList_RowSelected" ShowActionRow="false">
                        <Columns>
                            <Rock:ReorderField />
                            <Rock:RockBoundField DataField="Name" HeaderText="Name" />
                            <Rock:DeleteField ID="btnDelete" OnClick="btnDelete_Click" />
                        </Columns>
                    </Rock:Grid>
                </div>
            </div>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>
