<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GuestCheckin.ascx.cs" Inherits="RockWeb.Plugins.org_secc.SportsAndFitness.ControlCenter.GuestCheckin" %>
<script src="https://cdnjs.cloudflare.com/ajax/libs/vuetify/3.3.23/vuetify.min.js" integrity="sha512-rHI9vttaBjyZBzh6nY8wMSDdHhdIN8d1SIvtTVPgqRfuz516LwMrXU1gsH5wywJ9OZYHJ6FFDc4pn2vc1BCmxA==" crossorigin="anonymous" referrerpolicy="no-referrer"></script>
<script src="https://cdnjs.cloudflare.com/ajax/libs/vue/3.3.8/vue.cjs.min.js" integrity="sha512-++0EroP8rYirWvSa4s1YJFyfPBkbMX052exD1cKG/+CO+y7S3GIkMeuPbvEWdL6vnj4+meU7Ac4RCObf/zPMyw==" crossorigin="anonymous" referrerpolicy="no-referrer"></script>

<asp:UpdatePanel ID="upMain" runat="server">
    <ContentTemplate>
        <asp:Panel ID="pnlMain" runat="server" Visible="false">
            <Rock:Grid ID="gPendingCheckins" runat="server" AllowSorting="true" >
                <Columns>
                    <Rock:PersonField HeaderText="Guest Name" DataField="Guest" SortExpression="Guest.LastName, Guest.NickName" />
                    <Rock:TimeField HeaderText="Registered At" DataField="CreatedDateTime" SortExpression="CreatedDateTime" />
                    <Rock:BoolField HeaderText="Signed Waiver" DataField="HasSignedWaiver" SortExpression="HasSignedWaiver" />
                    <Rock:BoolField HeaderText="Emergency Contacts" DataField="HasEmergencyContacts" SortExpression="HasEmergencyContacts" />
                    <Rock:RockTemplateField>
                        <ItemTemplate>
                            <span class="pull-right">
                                <asp:LinkButton ID="lbCheckin" runat="server" CommandName="checkin" CommandArgument='<%# Eval("WorkflowGuid") %>' CssClass="btn btn-default">
                                    <i class="fa fa-check"></i>
                                </asp:LinkButton>
                                <asp:LinkButton ID="lbCancel" runat="server" CommandName="cancel" CommandArgument='<%# Eval("WorkflowGuid") %>' CssClass="btn btn-default">
                                    <i class="fa fa-times" ></i>
                                </asp:LinkButton>
                            </span>
                        </ItemTemplate>
                    </Rock:RockTemplateField>
                </Columns>
            </Rock:Grid>
        </asp:Panel>
        <asp:Panel ID="pnlSelectHost" runat="server" Visible="false">
            <h3>Select Host</h3>
            <div class="row">
                <div class="col-sm-3">
                    <Rock:RockDropDownList ID="ddlLocation" runat="server" AutoPostBack="true" Label="Location" />
                </div>
                <div class="col-sm-3">
                    <Rock:RockTextBox ID="tbNameSearch" runat="server" Label="Name" Placeholder="Name" />
                </div>
            </div>
            <Rock:Grid ID="gHosts" runat="server" >
                <Columns>
                    <Rock:RockBoundField HeaderText="Host Name" DataField="Host.FullName" SortExpression="Host.FullNameReversed" />
                    <Rock:RockBoundField HeaderText="Location" DataField="Location" SortExpression="Location" />
                    <Rock:RockTemplateField>
                        <ItemTemplate>
                            <span class="pull-right">
                                <asp:LinkButton ID="lbSelect" runat="server" CommandName="selecthost" CommandArgument='<%# Eval("AttendanceId") %>' CssClass="btn btn-default">
                                    <i class="fa fa-arrow-right"></i>
                                </asp:LinkButton>
                            </span>
                        </ItemTemplate>
                    </Rock:RockTemplateField>
                </Columns>
            </Rock:Grid>
        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>