<%@ Control Language="C#" AutoEventWireup="true" CodeFile="Notes.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Purchasing.Notes" %>

<asp:UpdatePanel ID="upNoteList" runat="server" UpdateMode="Conditional">
    <ContentTemplate>
        <Rock:Grid ID="dgNote" runat="server" CssClass="list" AllowPaging="false" AddEnabled="false" OnItemDataBound="dgNote_ItemDataBound" OnItemCommand="dgNote_ItemCommand"
            ExportEnabled="false" DataKeyField="NoteID" DeleteEnabled="false" NoResultText="No Notes Found">
            <Columns>
                <Rock:RockBoundField DataField="NoteID" Visible="false" />
                <Rock:RockBoundField DataField="Body" HeaderText="Note" ItemStyle-Width="55%" />
                <Rock:RockBoundField DataField="ModifiedBy" HeaderText="Last Updated By" ItemStyle-Width="20%" />
                <Rock:RockBoundField DataField="LastModifiedDate" HeaderText="Last Updated" ItemStyle-Width="15%" />
                <Rock:RockTemplateField ItemStyle-HorizontalAlign="Right" ItemStyle-Width="10%">
                    <ItemTemplate>
                        <asp:LinkButton ID="lbEdit" runat="server" Visible="false" CommandName="EditNote" style="text-decoration:none;">
                            <img src="/images/edit.gif" alt="Edit" />
                        </asp:LinkButton>
                        &nbsp;
                        <asp:LinkButton ID="lbHide" runat="server" Visible="false" CommandName="HideNote">
                            <img src="/images/delete.png" alt="Hide" />
                        </asp:LinkButton>

                    </ItemTemplate>
                </Rock:RockTemplateField>
            </Columns>
        </Rock:Grid>
    </ContentTemplate>
    <%--Triggers>
        <asp:AsyncPostBackTrigger ControlID="btnSave" />
        <asp:AsyncPostBackTrigger ControlID="btnReset" />
        <asp:AsyncPostBackTrigger ControlID="btnCancel" />
    </--%>
</asp:UpdatePanel>
<Rock:ModalDialog ID="mpNoteDetails" runat="server">
    <Content>
        <asp:UpdatePanel ID="upNoteDetails" runat="server" UpdateMode="Conditional">
            <ContentTemplate>
                <asp:HiddenField ID="hfNoteID" runat="server" />
                <div style="width: 500px;">
                    <h3>Note Details</h3>
                    <asp:Label ID="lblPopupError" runat="server" Visible="true" CssClass="smallText" Style="color: Red;" />
                    <asp:Label ID="lblInstruction" runat="server" CssClass="smallText" />
                    <div class="row">
                        <div class="popupLabel formLabel">
                            Note:</div>
                        <div class="popupItem formItem">
                            <asp:TextBox ID="txtNote" runat="server" TextMode="MultiLine" Style="width: 350px;
                                height: 50px;" /></div>
                    </div>
                    <div style="text-align:right">
                                <asp:Button ID="btnSave" runat="server" Text="Save" OnClick="btnSave_Click" CssClass="smallText" />
                                <asp:Button ID="btnReset" runat="server" Text="Reset" OnClick="btnReset_Click" CssClass="smallText" />
                                <asp:Button ID="btnCancel" runat="server" Text="Cancel" OnClick="btnCancel_Click" CssClass="smallText" />
                    </div>
                </div>
            </ContentTemplate>
            <%-- Triggers>
                <asp:AsyncPostBackTrigger ControlID="dgNote" EventName="ItemCommand" />
            </--%>
        </asp:UpdatePanel>
    </Content>
</Rock:ModalDialog>

<script type="text/javascript">
    function setReadOnlyClass(isReadOnly) {
        var cName = "readOnly";
        if (isReadOnly == true) {
            $("#[id*=txtNote]").addClass(cName);
            $("#[id*=txtDisplayAtTopEnd]").addClass(cName);
        }
        else {
            $("#[id*=txtNote]").removeClass(cName);
            $("#[id*=txtDisplayAtTopEnd]").removeClass(cName);          
        }
    }


</script>