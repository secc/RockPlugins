<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GroupManagerAttendance.ascx.cs" Inherits="RockWeb.Plugins.org_secc.GroupManager.GroupManagerAttendance" %>

<asp:UpdatePanel ID="pnlContent" runat="server">
    <ContentTemplate>

        <div class="panel panel-block">

            <div class="panel-heading clearfix">
                <h1 class="panel-title pull-left">
                    <i class="fa fa-check-square-o"></i>
                    <asp:Literal ID="lHeading" runat="server" Text="Group Attendance" />
                </h1>
            </div>
            
            <div class="panel-body">

                <Rock:NotificationBox ID="nbNotice" runat="server" />

                <asp:Panel id="pnlDetails" runat="server">

                    <div class="row">
                        <div class="col-sm-3">
                            <Rock:RockLiteral ID="lOccurrenceDate" runat="server" Label="Attendance For" />
                            <Rock:DatePicker ID="dpOccurrenceDate" runat="server" Label="Attendance For" Required="true" />
                        </div>
                        <div class="col-sm-3">
                            <Rock:RockLiteral ID="lOccurrenceTime" runat="server" Label=" "/>
                            <Rock:TimePicker ID="tpOccurrenceTime" runat="server"  Required="true" Label=" "/>
                        </div>
                        <div class="col-sm-3">
                            <Rock:ButtonDropDownList runat="server" ID="ddlPastOccurrences" Label=" " Title="Previous Attendance" OnSelectionChanged="ddlPastOccurrences_SelectionChanged">
                            </Rock:ButtonDropDownList>
                        </div>
                    </div>

                    <div class="row">
                        <div class="col-sm-12">
                            <Rock:RockCheckBox ID="cbDidNotMeet" runat="server" Text="We Did Not Meet" />
                        </div>
                    </div>

                    <div class="row">
                        <div class="col-md-6">

                            <div class="js-roster">
                                <h4><asp:Literal ID="lMembers" runat="server" /></h4>
                                <asp:ListView ID="lvMembers" runat="server">
                                    <ItemTemplate>
                                        <asp:HiddenField ID="hfMember" runat="server" Value='<%# Eval("PersonId") %>' />
                                        <Rock:RockCheckBox ID="cbMember" runat="server" Checked='<%# Eval("Attended") %>' Text='<%# Eval("FullName") %>' />
                                    </ItemTemplate>
                                </asp:ListView>
                                <div class="pull-right margin-b-lg">
                                    <Rock:PersonPicker ID="ppAddPerson" runat="server" CssClass="picker-menu-right" PersonName="Add New Attendee" OnSelectPerson="ppAddPerson_SelectPerson" />
                                </div>
                            </div>

                        </div>
                        <div class="col-md-6">

                            <asp:panel id="pnlPendingMembers" runat="server" visible="false">
                                <h4><asp:Literal ID="lPendingMembers" runat="server" /></h4>
                                <asp:ListView ID="lvPendingMembers" runat="server" OnItemCommand="lvPendingMembers_ItemCommand">
                                    <ItemTemplate>
                                        <div class="form-group">
                                            <asp:HiddenField ID="hfMember" runat="server" Value='<%# Eval("Id") %>' />
                                            <asp:Label ID="lName" runat="server" Text='<%# Eval("FullName") %>' />
                                            <asp:LinkButton ID="lbAdd" runat="server" ToolTip="Add Person to Group" CausesValidation="false" CommandName="Add" CommandArgument='<%# Eval("Id") %>' CssClass="js-add-member" ><i class="fa fa-plus"></i></asp:LinkButton>
                                        </div>
                                    </ItemTemplate>
                                </asp:ListView>
                            </asp:panel>

                        </div>
                    </div>
                    <div>
                        <Rock:RockTextBox ID="tbCount" runat="server"  Width="60" Label="Head Count:"></Rock:RockTextBox>
                        <Rock:RockTextBox ID="tbNotes" runat="server" TextMode="MultiLine" Rows="5" Label="Notes:"></Rock:RockTextBox>
                    </div>
                    <div class="actions">
                        <asp:LinkButton ID="lbSave" runat="server" Text="Save Attendance" CssClass="btn btn-primary" OnClick="lbSave_Click" CausesValidation="false" />
                    </div>

                </asp:Panel>

            </div>

        </div>

    </ContentTemplate>
</asp:UpdatePanel>
