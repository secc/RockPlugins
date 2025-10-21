<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GuestCheckin.ascx.cs" Inherits="RockWeb.Plugins.org_secc.SportsAndFitness.ControlCenter.GuestCheckin" %>

<asp:UpdatePanel ID="upMain" runat="server">
    <ContentTemplate>
        <asp:Panel ID="pnlMain" runat="server" Visible="false">
            <Rock:NotificationBox ID="nbCheckinMessage" runat="server" Dismissable="true"  Visible="false" NotificationBoxType="Success" />

            <Rock:Grid ID="gPendingCheckins" runat="server" AllowSorting="true" >
                <Columns>
                    <Rock:PersonField HeaderText="Guest Name" DataField="Guest" SortExpression="Guest.LastName, Guest.NickName" />
                    <Rock:RockBoundField HeaderText="Age" DataField="Guest.Age" SortExpression="Guest.Age" />
                    <Rock:TimeField HeaderText="Registered At" DataField="CreatedDateTime" SortExpression="CreatedDateTime" />
                    <Rock:BoolField HeaderText="Signed Waiver" DataField="HasSignedWaiver" SortExpression="HasSignedWaiver" />
                    <Rock:BoolField HeaderText="Emergency Contacts" DataField="HasEmergencyContacts" SortExpression="HasEmergencyContacts" />
                    <Rock:RockTemplateField>
                        <ItemTemplate>
                            <span class="pull-right">
                                <asp:LinkButton ID="lbCheckin" runat="server" CommandName="checkin" CommandArgument='<%# Eval("WorkflowGuid") %>' CssClass="btn btn-default">
                                    <i class="fa fa-check"></i>
                                </asp:LinkButton>
                                <asp:LinkButton ID="lbCancel" runat="server" CommandName="cancelCheckin" CommandArgument='<%# Eval("WorkflowGuid") %>' CssClass="btn btn-default">
                                    <i class="fa fa-times" ></i>
                                </asp:LinkButton>
                            </span>
                        </ItemTemplate>
                    </Rock:RockTemplateField>
                </Columns>
            </Rock:Grid>
        </asp:Panel>
        <asp:Panel ID="pnlSelectHost" runat="server" Visible="false">
            <asp:LinkButton ID="lbReturnToGuest" runat="server" CssClass="btn btn-default"><i class="fa fa-arrow-left"></i> Return</asp:LinkButton>
            <h3><asp:Literal ID="lSelectHostHeader" runat="server" /></h3>
            <asp:HiddenField ID="hfWorkflowGuid" runat="server" />

            <div class="row">
                <div class="col-sm-3">
                    <Rock:RockDropDownList ID="ddlLocation" runat="server" AutoPostBack="true" Label="Location" />
                </div>
                <div class="col-sm-3">
                    <Rock:RockTextBox ID="tbNameSearch" runat="server" Label="Name" Placeholder="Name" onkeydown="javascript: return handleFilterText(this, event.keyCode);" />
                </div>
            </div>
            <Rock:Grid ID="gHosts" runat="server" AllowSorting="true">
                <Columns>
                    <Rock:RockBoundField HeaderText="Host Name" DataField="Host.FullName" SortExpression="Host.FullNameReversed" />
                    <Rock:RockBoundField HeaderText="Age" DataField="Host.Age" SortExpression="Host.Age" />
                    <Rock:RockBoundField HeaderText="Location" DataField="Location" SortExpression="Location" />
                    <Rock:RockBoundField HeaderText="Guests" DataField="GuestCount" HeaderStyle-HorizontalAlign="Center" ItemStyle-HorizontalAlign="Center" />
                    <Rock:RockTemplateField>
                        <ItemTemplate>
                            <span class="pull-right">
                                <asp:LinkButton ID="lbSelect" runat="server" CommandName="selecthost" CommandArgument='<%# Eval("AttendanceId") %>' CssClass="btn btn-default">
                                    <i class="fa fa-arrow-right"></i>
                                </asp:LinkButton>
                                <Rock:HighlightLabel ID="hlMaxGuests" runat="server" Visible="false" LabelType="Warning" Text="Has Maximum Guests" />
                            </span>
                            
                        </ItemTemplate>
                    </Rock:RockTemplateField>
                </Columns>
            </Rock:Grid>
        </asp:Panel>

        <script type="text/javascript">
            function handleFilterText ( element, keyCode )
            {
                if ( keyCode == 13 && element.value.length != 1 )
                {
                    window.location = "javascript:__doPostBack('<%=upMain.ClientID %>','filterByName')";

                    // prevent double-postback
                    $( element ).prop( 'disabled', true )
                        .attr( 'disabled', 'disabled' )
                        .addClass( 'disabled' );
                    return true;
                }
            }
        </script>
    </ContentTemplate>
</asp:UpdatePanel>