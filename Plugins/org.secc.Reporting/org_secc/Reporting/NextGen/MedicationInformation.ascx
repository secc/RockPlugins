<%@ Control Language="C#" AutoEventWireup="true" CodeFile="MedicationInformation.ascx.cs" Inherits="RockWeb.Blocks.Reporting.NextGen.MedicationInformation" %>

<asp:UpdatePanel ID="upReport" runat="server">
    <ContentTemplate>

        <Rock:NotificationBox runat="server" ID="nbAlert" Dismissable="true" NotificationBoxType="Danger" />
        <Rock:Grid runat="server" ID="gGrid" ShowActionRow="false" DataKeyNames="Id" RowItemText="Student">
            <Columns>
                <Rock:RockBoundField DataField="Name" HeaderText="Person" />
                <Rock:RockBoundField DataField="Group" HeaderText="Camp" />
                <Rock:RockBoundField DataField="Medications" HeaderText="Medications" HtmlEncode="false" />
                <Rock:LinkButtonField OnClick="SelectMember_Click" Text="Update Medications" />
            </Columns>
        </Rock:Grid>

    </ContentTemplate>
</asp:UpdatePanel>
