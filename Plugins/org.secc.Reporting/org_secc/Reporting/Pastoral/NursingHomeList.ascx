<%@ Control Language="C#" AutoEventWireup="true" CodeFile="NursingHomeList.ascx.cs" Inherits="RockWeb.Blocks.Reporting.NursingHomeList" %>

<asp:UpdatePanel ID="upReport" runat="server">
    <ContentTemplate>



            <div class="panel panel-block">
                <div class="panel-heading">
                    <h1 class="panel-title"><i class="fa fa-wheelchair"></i> Nursing Home List</h1>
                </div>
                
                <div class="grid">
                    <Rock:Grid ID="gReport" runat="server" AllowSorting="true" EmptyDataText="No Results">
                        <Columns>
                            <Rock:RockBoundField DataField="NursingHome" HeaderText="Nursing Home" SortExpression="NursingHome"></Rock:RockBoundField>
                            <Rock:PersonField DataField="Person" HeaderText="Person" SortExpression="Person.LastName" />
                            <Rock:RockBoundField DataField="Person.Age" HeaderText="Age" SortExpression="Person.Age"></Rock:RockBoundField>
                            <Rock:RockBoundField DataField="Room" HeaderText="Room" SortExpression="Room"></Rock:RockBoundField>
                            <Rock:RockBoundField DataField="AdmitDate" HeaderText="Admit Date" SortExpression="AdmitDate"></Rock:RockBoundField>
                            <Rock:RockBoundField DataField="Description" HeaderText="Description" SortExpression="Description"></Rock:RockBoundField>
                            <Rock:RockBoundField DataField="Visits" HeaderText="Visits" SortExpression="Visits"></Rock:RockBoundField>
                            <Rock:RockBoundField DataField="LastVisitor" HeaderText="Last Visitor" SortExpression="LastVisitor"></Rock:RockBoundField>
                            <Rock:RockBoundField DataField="LastVisitDate" HeaderText="Last Visit Date" SortExpression="LastVisitDate"></Rock:RockBoundField>
                            <Rock:RockBoundField DataField="LastVisitNotes" HeaderText="Last Visit Notes" SortExpression="LastVisitNotes"></Rock:RockBoundField>
                            <Rock:RockTemplateField HeaderText="Status" SortExpression="Status">
                                <ItemTemplate>
                                    <span class="label label-success"><%# Eval("Status") %></span>
                                </ItemTemplate>
                            </Rock:RockTemplateField>
                            <Rock:BoolField DataField="Communion" HeaderText="Communion" />
                            <Rock:RockTemplateField HeaderText="Actions" ItemStyle-Width="120px">
                                <ItemTemplate>
                                    <a href="<%# "https://maps.google.com/?q="+Eval("Address").ToString() %>" target="_blank" class="btn btn-default"><i class="fa fa-map-o" title="View Map"></i></a>
                                    <a href="<%# "/Pastoral/NursingHome/"+Eval("Workflow.Id") %>" class="btn btn-default"><i class="fa fa-pencil"></i></a>
                                </ItemTemplate>
                            </Rock:RockTemplateField>
                        </Columns>
                    </Rock:Grid>
                </div>
            </div>

    </ContentTemplate>
</asp:UpdatePanel>
