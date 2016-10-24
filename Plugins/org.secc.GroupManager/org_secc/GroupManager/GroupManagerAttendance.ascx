<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GroupManagerAttendance.ascx.cs" Inherits="RockWeb.Plugins.org_secc.GroupManager.GroupManagerAttendance" %>

<script>

    var doCount = false;

    var updateCount = function ()
    {
        console.log("Activate!!")
        if (doCount)
        {
            var count = 0;
            var boxes = $("[id$=cbMember]").toArray();
            boxes.forEach(
            function (el)
            {
                if (el.checked)
                {
                    count++;
                }
            })

            if ($("[id$=cbDidNotMeet]")[0].checked)
            {
                $("[id$=tbCount]").val("");
            } else
            {
                $("[id$=tbCount]").val(count);
            }
        }
    }
</script>

<asp:UpdatePanel ID="pnlContent" runat="server">
    <ContentTemplate>

        <div class="panel panel-block">

            <div class="panel-body">

                <Rock:NotificationBox ID="nbNotice" runat="server" />

                <asp:Panel ID="pnlDetails" runat="server">

                    <div class="row">
                        <div class="col-sm-6">
                            <Rock:RockDropDownList runat="server" Label="Attendance Date" ID="ddlOccurence" AutoPostBack="true"
                                OnSelectedIndexChanged="ddlOccurence_SelectedIndexChanged" DataTextField="Name" DataValueField="Id" />
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
                                <h4>
                                    <asp:Literal ID="lMembers" runat="server" />
                                </h4>
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

                            <asp:Panel ID="pnlPendingMembers" runat="server" Visible="false">
                                <h4>
                                    <asp:Literal ID="lPendingMembers" runat="server" /></h4>
                                <asp:ListView ID="lvPendingMembers" runat="server" OnItemCommand="lvPendingMembers_ItemCommand">
                                    <ItemTemplate>
                                        <div class="form-group">
                                            <asp:HiddenField ID="hfMember" runat="server" Value='<%# Eval("Id") %>' />
                                            <asp:Label ID="lName" runat="server" Text='<%# Eval("FullName") %>' />
                                            <asp:LinkButton ID="lbAdd" runat="server" ToolTip="Add Person to Group" CausesValidation="false" CommandName="Add" CommandArgument='<%# Eval("Id") %>' CssClass="js-add-member"><i class="fa fa-plus"></i></asp:LinkButton>
                                        </div>
                                    </ItemTemplate>
                                </asp:ListView>
                            </asp:Panel>

                        </div>
                    </div>
                    <div class="actions">
                        <asp:LinkButton ID="lbSave" runat="server" Text="Save Attendance" CssClass="btn btn-primary" OnClick="lbSave_Click" CausesValidation="false" />
                    </div>

                </asp:Panel>

            </div>

        </div>

    </ContentTemplate>
</asp:UpdatePanel>
