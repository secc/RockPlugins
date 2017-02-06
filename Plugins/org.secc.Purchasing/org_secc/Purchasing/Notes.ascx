<%@ Control Language="C#" AutoEventWireup="true" CodeFile="Notes.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Purchasing.Notes" %>

<asp:UpdatePanel ID="upNoteList" runat="server" UpdateMode="Conditional">
    <ContentTemplate>
        <style>
            .wrapword{
            white-space: -moz-pre-wrap !important;  /* Mozilla, since 1999 */
            white-space: -webkit-pre-wrap; /*Chrome & Safari */ 
            white-space: -pre-wrap;      /* Opera 4-6 */
            white-space: -o-pre-wrap;    /* Opera 7 */
            white-space: pre-wrap;       /* css-3 */
            word-wrap: break-word;       /* Internet Explorer 5.5+ */
            word-break: break-all;
            white-space: normal;
            }
        </style>
        <Rock:Grid ID="dgNote" runat="server" CssClass="list" AllowPaging="false" AddEnabled="false" OnRowDataBound="dgNote_RowDataBound"
            ExportEnabled="false" DataKeyField="NoteID" DeleteEnabled="false" NoResultText="No Notes Found" ShowActionRow="false">
            <Columns>
                <Rock:RockBoundField DataField="NoteID" Visible="false" />
                <Rock:RockBoundField DataField="Body" HeaderText="Note" ItemStyle-Width="600px" ItemStyle-CssClass="wrapword" />
                <Rock:RockBoundField DataField="ModifiedBy" HeaderText="Last Updated By" ItemStyle-Width="20%" />
                <Rock:RockBoundField DataField="LastModifiedDate" HeaderText="Last Updated" ItemStyle-Width="15%" />
                <Rock:RockTemplateField ItemStyle-HorizontalAlign="Right" ItemStyle-Width="10%" HeaderStyle-CssClass="hidden-print" FooterStyle-CssClass="hidden-print" ItemStyle-CssClass="hidden-print">
                    <ItemTemplate>
                        <asp:LinkButton ID="lbEdit" runat="server" Visible="false" OnCommand="dgNote_ItemCommand" CommandName="EditNote" CommandArgument='<%# DataBinder.Eval(Container.DataItem, "NoteId") %>' CssClass="btn btn-default">
                            <i class="fa fa-pencil"></i>
                        </asp:LinkButton>
                        &nbsp;
                        <asp:LinkButton ID="lbHide" runat="server" Visible="false" OnClientClick="return Rock.dialogs.confirmDelete(event, 'Note');" OnCommand="dgNote_ItemCommand" CommandName="HideNote" CommandArgument='<%# DataBinder.Eval(Container.DataItem, "NoteId") %>' CssClass="btn btn-default">
                           <i class="fa fa-remove"></i>
                        </asp:LinkButton>

                    </ItemTemplate>
                </Rock:RockTemplateField>
            </Columns>
        </Rock:Grid>
        <Rock:ModalDialog ID="mpNoteDetails" runat="server" OnSaveClick="btnSave_Click" Title="Note Details" SaveButtonText="Save" Content-Height="250px">
            <Content>
                <asp:UpdatePanel ID="upNoteDetails" runat="server" UpdateMode="Conditional">
                    <ContentTemplate>
                        <asp:HiddenField ID="hfNoteID" runat="server" />
                        <div id="popupError" runat="server" visible="false" class="alert alert-danger">
                            <asp:Label ID="lblPopupError" runat="server" Visible="true" />
                        </div>
                        <asp:Label ID="lblInstruction" runat="server" CssClass="smallText" />
                        <Rock:RockTextBox Label="Note" ID="txtNote" runat="server" TextMode="MultiLine" Height="200px"/>
                        <div runat="server" visible="false">
                                    <asp:Button ID="btnSave" runat="server" Text="Save" OnClick="btnSave_Click" CssClass="smallText" />
                                    <asp:Button ID="btnReset" runat="server" Text="Reset" OnClick="btnReset_Click" CssClass="smallText" />
                                    <asp:Button ID="btnCancel" runat="server" Text="Cancel" OnClick="btnCancel_Click" CssClass="smallText" />
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
                    $("[id*=txtNote]").addClass(cName);
                    $("[id*=txtDisplayAtTopEnd]").addClass(cName);
                }
                else {
                    $("[id*=txtNote]").removeClass(cName);
                    $("[id*=txtDisplayAtTopEnd]").removeClass(cName);          
                }
            }


        </script>
    </ContentTemplate>
    <%--Triggers>
        <asp:AsyncPostBackTrigger ControlID="btnSave" />
        <asp:AsyncPostBackTrigger ControlID="btnReset" />
        <asp:AsyncPostBackTrigger ControlID="btnCancel" />
    </--%>
</asp:UpdatePanel>