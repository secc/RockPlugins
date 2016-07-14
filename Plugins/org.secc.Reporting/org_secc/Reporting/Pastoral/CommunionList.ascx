<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CommunionList.ascx.cs" Inherits="RockWeb.Blocks.Reporting.CommunionList" %>

<asp:UpdatePanel ID="upReport" runat="server">
    <ContentTemplate>



            <div class="panel panel-block">
                <div class="panel-heading">
                    <h1 class="panel-title"><i class="fa fa-certificate"></i> Communion List</h1>
                </div>
                
                <div class="grid">
                    <Rock:Grid ID="gReport" runat="server" AllowSorting="true" EmptyDataText="No Results">
                        <Columns>
                            <Rock:RockBoundField DataField="Campus" HeaderText="Campus"></Rock:RockBoundField>
                            <Rock:RockBoundField DataField="Location" HeaderText="Location"></Rock:RockBoundField>
                            <Rock:RockBoundField DataField="PersonName" HeaderText="Person To Visit"></Rock:RockBoundField>
                            <Rock:RockBoundField DataField="Address.Street1" HeaderText="Street Address"></Rock:RockBoundField>
                            <Rock:RockBoundField DataField="Address.City" HeaderText="City"></Rock:RockBoundField>
                            <Rock:RockBoundField DataField="Address.State" HeaderText="State" SortExpression="Address.State"></Rock:RockBoundField>
                            <Rock:RockBoundField DataField="Address.PostalCode" HeaderText="Zip"></Rock:RockBoundField>
                            <Rock:RockBoundField DataField="Age" HeaderText="Age"></Rock:RockBoundField>
                            <Rock:RockBoundField DataField="Room" HeaderText="Room"></Rock:RockBoundField>
                            <Rock:RockBoundField DataField="Date" HeaderText="Admit/Start Date"></Rock:RockBoundField>
                            <Rock:RockBoundField DataField="Description" HeaderText="Description"></Rock:RockBoundField>
                            <Rock:RockTemplateField HeaderText="Status">
                                <ItemTemplate>
                                    <span class="label label-success"><%# Eval("Status") %></span>
                                </ItemTemplate>
                            </Rock:RockTemplateField>
                            <Rock:BoolField DataField="Communion" HeaderText="Communion" />
                            <Rock:RockTemplateField HeaderText="Actions">
                                <ItemTemplate>
                                    <a href="<%# "https://maps.google.com/?q="+Eval("Address").ToString() %>" target="_blank" class="btn btn-default"><i class="fa fa-map-o" title="View Map"></i></a>                                </ItemTemplate>
                            </Rock:RockTemplateField>
                        </Columns>
                    </Rock:Grid>
                </div>
            </div>

    </ContentTemplate>
</asp:UpdatePanel>
