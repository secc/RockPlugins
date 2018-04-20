<%@ Control Language="C#" AutoEventWireup="true" CodeFile="MedicationInformation.ascx.cs" Inherits="RockWeb.Blocks.Reporting.NextGen.MedicationInformation" %>

<asp:UpdatePanel ID="upReport" runat="server">
    <ContentTemplate>
        <div class="panel panel-default">
            <div class="panel-heading">
                Trip Medication Information
              
            </div>
            <div class="panel-body">
                <Rock:NotificationBox runat="server" ID="nbAlert" Dismissable="true" NotificationBoxType="Danger" />
                <asp:Panel runat="server" ID="pnlSelectPerson">
                    <Rock:Grid runat="server" ID="gGrid" ShowActionRow="false" DataKeyNames="Id">
                        <Columns>
                            <Rock:RockBoundField DataField="Name" HeaderText="Person" />
                            <Rock:RockBoundField DataField="Group" HeaderText="Group" />
                            <Rock:RockBoundField DataField="Medications" HeaderText="Medications" HtmlEncode="false" />
                            <Rock:LinkButtonField OnClick="SelectMember_Click" CssClass="btn btn-primary" Text="Update Medications" />
                        </Columns>
                    </Rock:Grid>
                </asp:Panel>
                <asp:Panel runat="server" ID="pnlChangeInformation" Visible="false">
                   
                </asp:Panel>
            </div>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>
