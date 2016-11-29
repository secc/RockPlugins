<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GroupManagerAttendance.ascx.cs" Inherits="RockWeb.Plugins.org_secc.GroupManager.GroupManagerAttendance" %>

<style>
    label.btn span {
        font-size: 1.5em;
    }

    label input[type="checkbox"] ~ i.fa.fa-square-o {
        color: #c8c8c8;
        display: inline;
    }

    label input[type="checkbox"] ~ i.fa.fa-check-square-o {
        display: none;
    }

    label input[type="checkbox"]:checked ~ i.fa.fa-square-o {
        display: none;
    }

    label input[type="checkbox"]:checked ~ i.fa.fa-check-square-o {
        color: #7AA3CC;
        display: inline;
    }

    label:hover input[type="checkbox"] ~ i.fa {
        color: #7AA3CC;
    }

    div[data-toggle="buttons"] label.active {
        color: #7AA3CC;
    }

    div[data-toggle="buttons"] label {
        display: inline-block;
        padding: 6px 12px;
        margin-bottom: 0;
        font-size: 14px;
        font-weight: normal;
        line-height: 2em;
        text-align: left;
        white-space: nowrap;
        vertical-align: top;
        cursor: pointer;
        background-color: none;
        border: 0px solid #c8c8c8;
        border-radius: 3px;
        color: #c8c8c8;
        -webkit-user-select: none;
        -moz-user-select: none;
        -ms-user-select: none;
        -o-user-select: none;
        user-select: none;
    }

        div[data-toggle="buttons"] label:hover {
            color: #7AA3CC;
        }

        div[data-toggle="buttons"] label:active, div[data-toggle="buttons"] label.active {
            -webkit-box-shadow: none;
            box-shadow: none;
        }
</style>

<script>

    var doCount = false;

    var updateCount = function ()
    {
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
                        <div class="col-sm-6">
                            <Rock:RockDropDownList runat="server" Label="Filter By" ID="ddlFilter" AutoPostBack="true"
                                OnSelectedIndexChanged="ddlFilter_SelectedIndexChanged" DataTextField="Name" DataValueField="Id"
                                Visible="false" />
                        </div>
                    </div>

                    <div class="row">
                        <asp:Panel runat="server" ID="pnlDidNotMeet" class="col-sm-12 btn-group btn-group-vertical" data-toggle="buttons">
                            <label runat="server" id="lbDidNotMeet" class="btn">
                                <input type="checkbox" id="cbDidNotMeet" runat="server" />
                                <i class="fa fa-square-o fa-2x"></i>
                                <i class="fa fa-check-square-o fa-2x"></i>
                                <span>We Did Not Meet</span>
                            </label>
                        </asp:Panel>
                    </div>

                    <div class="row">
                        <div class="col-md-6">

                            <div class="js-roster">
                                <h4>
                                    <asp:Literal ID="lMembers" runat="server" />
                                </h4>
                                <div class="btn-group btn-group-vertical" data-toggle="buttons">
                                    <asp:ListView ID="lvMembers" runat="server">
                                        <ItemTemplate>
                                            <asp:HiddenField ID="hfMember" runat="server" Value='<%# Eval("PersonId") %>' />
                                            <label class='btn <%# Eval("Active") %>'>
                                                <input type="checkbox" id="cbMember" runat="server" checked='<%# Eval("Attended") %>' />
                                                <i class="fa fa-square-o fa-2x"></i>
                                                <i class="fa fa-check-square-o fa-2x"></i>
                                                <span><%# Eval("FullName") %></span>
                                            </label>
                                        </ItemTemplate>
                                    </asp:ListView>
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
