<%@ Control Language="C#" AutoEventWireup="true" CodeFile="MedicationInformation.ascx.cs" Inherits="RockWeb.Blocks.Reporting.NextGen.MedicationInformation" %>

<asp:UpdatePanel ID="upReport" runat="server">
    <ContentTemplate>
        <div class="panel panel-default">
            <div class="panel-heading">
                Trip Medication Information
              
            </div>
            <div class="panel-body">
                <Rock:NotificationBox runat="server" ID="nbAlert" Dismissable="true" NotificationBoxType="Danger" />
                <Rock:Grid runat="server" ID="gGrid" ShowActionRow="false" DataKeyNames="Id">
                    <Columns>
                        <Rock:RockBoundField DataField="Name" HeaderText="Person" />
                        <Rock:RockBoundField DataField="Group" HeaderText="Group" />
                        <Rock:RockBoundField DataField="Medications" HeaderText="Medications" HtmlEncode="false" />
                        <Rock:LinkButtonField OnClick="SelectMember_Click"  Text="Update Medications" />
                    </Columns>
                </Rock:Grid>
            </div>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>
