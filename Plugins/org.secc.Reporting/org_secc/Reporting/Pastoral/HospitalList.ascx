<%@ Control Language="C#" AutoEventWireup="true" CodeFile="HospitalList.ascx.cs" Inherits="RockWeb.Blocks.Reporting.HospitalList" %>

<asp:UpdatePanel ID="upReport" runat="server">
    <ContentTemplate>



            <div class="panel panel-block">
                <div class="panel-heading">
                    <h1 class="panel-title"><i class="fa fa-hospital-o"></i> Hospitalization List</h1>
                </div>
                
                <div class="grid">
                    <Rock:Grid ID="gReport" runat="server" AllowSorting="true" EmptyDataText="No Results">
                        <Columns>
                            <Rock:RockBoundField DataField="Hospital" HeaderText="Hospital"></Rock:RockBoundField>
                            <Rock:RockBoundField DataField="PersonToVisit" HeaderText="Person To Visit"></Rock:RockBoundField>
                            <Rock:RockBoundField DataField="Age" HeaderText="Age"></Rock:RockBoundField>
                            <Rock:RockBoundField DataField="Room" HeaderText="Room"></Rock:RockBoundField>
                            <Rock:RockBoundField DataField="AdmitDate" HeaderText="Admit Date"></Rock:RockBoundField>
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
                                    <a href="<%# "https://maps.google.com/?q="+Eval("HospitalAddress").ToString() %>" target="_blank" class="btn btn-default"><i class="fa fa-map-o" title="View Map"></i></a>
                                    <a href="<%# "/Pastoral/Hospitalization/"+Eval("Workflow.Id") %>" class="btn btn-default"><i class="fa fa-pencil"></i></a>
                                </ItemTemplate>
                            </Rock:RockTemplateField>
                        </Columns>
                    </Rock:Grid>
                </div>
            </div>

    </ContentTemplate>
</asp:UpdatePanel>
