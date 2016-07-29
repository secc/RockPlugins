<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CommunionList.ascx.cs" Inherits="RockWeb.Blocks.Reporting.CommunionList" %>

<asp:UpdatePanel ID="upReport" runat="server">
    <ContentTemplate>



            <div class="panel panel-block">
                <div class="panel-heading">
                    <h1 class="panel-title"><i class="fa fa-certificate"></i> Communion List</h1>
                </div>
                
                <Rock:Grid ID="gReport" runat="server" AllowSorting="true" EmptyDataText="No Results">
                    <Columns>
                        <Rock:RockBoundField DataField="Campus" HeaderText="Campus" SortExpression="Campus"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="Location" HeaderText="Location" SortExpression="Location"></Rock:RockBoundField>
                        <Rock:PersonField DataField="Person" HeaderText="Person" SortExpression="Person" />
                        <Rock:RockBoundField DataField="Person.Age" HeaderText="Age" SortExpression="Person.Age"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="Address.Street1" HeaderText="Street Address"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="Address.City" HeaderText="City"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="Address.State" HeaderText="State"></Rock:RockBoundField>
                        <Rock:RockBoundField DataField="PostalCode" HeaderText="Zip" SortExpression="PostalCode"></Rock:RockBoundField>
                        <Rock:RockTemplateField HeaderText="Status">
                            <ItemTemplate>
                                <span class="label label-success"><%# Eval("Status") %></span>
                            </ItemTemplate>
                        </Rock:RockTemplateField>
                        <Rock:BoolField DataField="Communion" HeaderText="Communion" />
                        <Rock:RockTemplateField HeaderText="Actions">
                            <ItemTemplate>
                                <a href="<%# "https://maps.google.com/?q="+Eval("Address") %>" target="_blank" class="btn btn-default"><i class="fa fa-map-o" title="View Map"></i></a>                                </ItemTemplate>
                        </Rock:RockTemplateField>
                    </Columns>
                </Rock:Grid>
            </div>

    </ContentTemplate>
</asp:UpdatePanel>
