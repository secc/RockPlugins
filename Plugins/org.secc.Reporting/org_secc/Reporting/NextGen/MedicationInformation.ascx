<%@ Control Language="C#" AutoEventWireup="true" CodeFile="MedicationInformation.ascx.cs" Inherits="RockWeb.Blocks.Reporting.NextGen.MedicationInformation" %>

<asp:UpdatePanel ID="upReport" runat="server">
    <ContentTemplate>

        <Rock:NotificationBox runat="server" ID="nbAlert" Dismissable="true" NotificationBoxType="Danger" />
        <Rock:Grid runat="server" ID="gGrid" ShowActionRow="false" DataKeyNames="Id" RowItemText="Student"
            ShowFooter="false" ShowActionsInHeader="false" AllowPaging="false" AllowSorting="false" OnRowSelected="gGrid_RowSelected">
            <Columns>
                <Rock:RockBoundField DataField="Name" HeaderText="Person" />
                <Rock:RockBoundField DataField="Group" ColumnPriority="Desktop" HeaderText="Camp" />
                <Rock:RockBoundField DataField="Medications" HeaderText="Medications" ColumnPriority="Desktop" HtmlEncode="false" />
                <Rock:LinkButtonField OnClick="SelectMember_Click" Text="Update Medications" />
            </Columns>
        </Rock:Grid>

    </ContentTemplate>
</asp:UpdatePanel>
