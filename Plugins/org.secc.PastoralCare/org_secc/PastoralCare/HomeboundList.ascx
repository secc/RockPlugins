﻿<%@ Control Language="C#" AutoEventWireup="true" CodeFile="HomeboundList.ascx.cs" Inherits="RockWeb.Plugins.org_secc.PastoralCare.HomeboundList" %>

<asp:UpdatePanel ID="upReport" runat="server">
    <ContentTemplate>
        <Rock:NotificationBox runat="server" ID="nbError" Text="Please configure block" NotificationBoxType="Validation" Visible="false" />
        <asp:Panel runat="server" ID="pnlInfo" Visible="false">
            <div class="panel-heading">
                <asp:Literal Text="Information" runat="server" ID="ltHeading" />
            </div>
            <div class="panel-body">
                <asp:Literal Text="" runat="server" ID="ltBody" />
            </div>
        </asp:Panel>
        <asp:Panel runat="server" ID="pnlMain" Visible="true">

            <div class="panel panel-block">
                <div class="panel-heading">
                    <h1 class="panel-title"><i class="fa fa-bed"></i> Homebound List</h1>
                </div>
                
                <Rock:Grid ID="gReport" runat="server" AllowSorting="true" EmptyDataText="No Results" DataKeyNames="Id" OnRowSelected="gReport_RowSelected">
                    <Columns>
                        <Rock:PersonField DataField="HomeboundPerson" HeaderText="Person" SortExpression="HomeboundPerson.LastName" />
                        <Rock:RockBoundField DataField="Age" HeaderText="Age" SortExpression="Age"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="Birthday" HeaderText="Birthday" SortExpression="Birthday" Visible="false"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="PhoneNumber" HtmlEncode="false" HeaderText="Phone Number" SortExpression="PhoneNumber"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="StartDate" DataFormatString="{0:MM/dd/yyyy}" HeaderText="Start Date" SortExpression="StartDate"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="Description" HeaderText="Description" SortExpression="Description"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="Visits" HeaderText="Visits" SortExpression="visits"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="LastVisitor" HeaderText="Last Visitor" SortExpression="LastVisitor"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="LastVisitDate" HeaderText="Last Visit Date" SortExpression="LastVisitDate"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="LastVisitNotes" HeaderText="Last Visit Notes" SortExpression="LastVisitNotes"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="EndDate" HeaderText="End Date" SortExpression="EndDate" Visible="false"></Rock:RockBoundField>
                        <Rock:RockTemplateField HeaderText="Status">
                            <ItemTemplate>
                                <span class="label <%# Convert.ToString(Eval("Status"))=="Active"?"label-success":"label-default" %>"><%# Eval("Status") %></span>
                            </ItemTemplate>
                        </Rock:RockTemplateField>
                        <Rock:BoolField DataField="Communion" HeaderText="Com." />
                        <Rock:RockTemplateField HeaderText="Actions" ItemStyle-Width="160px">
                            <ItemTemplate>
                                <a href="<%# "https://maps.google.com/?q="+Eval("Address").ToString() %>" target="_blank" class="btn btn-default"><i class="fa fa-map-o" title="View Map"></i></a>
                                <a href="<%# "/Pastoral/Homebound/"+Eval("Workflow.Id") %>" class="btn btn-default"><i class="fa fa-pencil"></i></a>
                                <Rock:BootstrapButton id="btnReopen" runat="server" CommandArgument='<%# Eval("Workflow.Id") %>' CssClass="btn btn-warning" ToolTip="Reopen Workflow" OnCommand="btnReopen_Command" Visible='<%# Convert.ToString(Eval("Status"))!="Active" %>'><i class="fa fa-undo"></i></Rock:BootstrapButton>
                            </ItemTemplate>
                        </Rock:RockTemplateField>
                    </Columns>
                </Rock:Grid>
            </div>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
