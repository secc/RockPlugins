<%@ Control Language="C#" AutoEventWireup="true" CodeFile="HomeboundList.ascx.cs" Inherits="RockWeb.Blocks.Reporting.HomeboundList" %>

<asp:UpdatePanel ID="upReport" runat="server">
    <ContentTemplate>



            <div class="panel panel-block">
                <div class="panel-heading">
                    <h1 class="panel-title"><i class="fa fa-bed"></i> Homebound List</h1>
                </div>
                
                <div class="grid">
                    <Rock:Grid ID="gReport" runat="server" AllowSorting="true" EmptyDataText="No Results">
                        <Columns>
                            <Rock:RockBoundField DataField="HomeboundPerson" HeaderText="Person To Visit"></Rock:RockBoundField>
                            <Rock:RockBoundField DataField="Age" HeaderText="Age"></Rock:RockBoundField>
                            <Rock:RockBoundField DataField="StartDate" HeaderText="Start Date"></Rock:RockBoundField>
                            <Rock:RockBoundField DataField="Description" HeaderText="Description"></Rock:RockBoundField>
                            <Rock:RockBoundField DataField="Visits" HeaderText="Visits"></Rock:RockBoundField>
                            <Rock:RockBoundField DataField="LastVisitor" HeaderText="Last Visitor"></Rock:RockBoundField>
                            <Rock:RockBoundField DataField="LastVisitDate" HeaderText="Last Visit Date"></Rock:RockBoundField>
                            <Rock:RockBoundField DataField="LastVisitNotes" HeaderText="Last Visit Notes"></Rock:RockBoundField>
                            <Rock:RockTemplateField HeaderText="Status">
                                <ItemTemplate>
                                    <span class="label label-success"><%# Eval("Status") %></span>
                                </ItemTemplate>
                            </Rock:RockTemplateField>
                            <Rock:BoolField DataField="Communion" HeaderText="Communion" />
                            <Rock:RockTemplateField HeaderText="Actions" ItemStyle-Width="120px">
                                <ItemTemplate>
                                    <a href="<%# "https://maps.google.com/?q="+Eval("Address").ToString() %>" target="_blank" class="btn btn-default"><i class="fa fa-map-o" title="View Map"></i></a>
                                    <a href="<%# "/Pastoral/Homebound/"+Eval("Workflow.Id") %>" class="btn btn-default"><i class="fa fa-pencil"></i></a>
                                </ItemTemplate>
                            </Rock:RockTemplateField>
                        </Columns>
                    </Rock:Grid>
                </div>
            </div>

    </ContentTemplate>
</asp:UpdatePanel>
