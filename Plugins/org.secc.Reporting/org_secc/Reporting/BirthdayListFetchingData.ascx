
<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <Rock:Grid ID="gPeople" runat="server" AllowSorting="true">
            <Columns>
                <Rock:RockBoundField DataField="FirstName" HeaderText="First Name" SortExpression="FullName" />
                <Rock:RockBoundField DataField="LastName" HeaderText="Last Name" SortExpression="LastName" />
                <Rock:RockBoundField DataField="BirthDate" HeaderText="Birth Date" SortExpression="BirthDate" DataFormatString="{0:M}"/>
            </Columns>
        </Rock:Grid>
        <asp:Literal ID="lLava" runat="server" />
    </ContentTemplate>
</asp:UpdatePanel>
