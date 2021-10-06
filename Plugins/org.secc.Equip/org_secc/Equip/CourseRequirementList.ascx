<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CourseRequirementList.ascx.cs" Inherits="RockWeb.Plugins.org.secc.Equip.CourseRequirementList" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <div class="panel panel-block">

            <div class="panel-heading">
                <h1 class="panel-title pull-left">
                    <i class="fa fa-user-check"></i>
                    Course Requirements
                </h1>
                <div class="panel-labels">
                </div>
            </div>

            <div class="panel-body">
                <div class="grid grid-panel">
                    <Rock:Grid ID="gList" runat="server" DataKeyNames="Id" OnRowSelected="gList_RowSelected">
                        <Columns>
                            <Rock:RockBoundField DataField="Course" HeaderText="Course" />
                            <Rock:RockBoundField DataField="DataView" HeaderText="DataView" />
                            <Rock:RockBoundField DataField="Group" HeaderText="Group" />
                            <Rock:DeleteField ID="btnDelete" OnClick="btnDelete_Click" />
                        </Columns>
                    </Rock:Grid>
                </div>
            </div>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>
