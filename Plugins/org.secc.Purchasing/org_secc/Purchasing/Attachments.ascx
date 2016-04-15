<%@ Control Language="C#" AutoEventWireup="true" CodeFile="Attachments.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Purchasing.Attachments" %>
<style type="text/css">
    td img
    {
        border: 0px;
    }
</style>

<script type="text/javascript">
    function selectDocument(documentId, documentTitle, state, modalID, dstExpire)
    {
        document.getElementById('<%=ihBlobID.ClientID %>').value = documentId;    
	    $find(modalID).hide();
	    //Call Async Postback
	    <%-- =Page.ClientScript.GetPostBackEventReference(btnRefresh, null) --%>
    }


</script>
<asp:UpdatePanel ID="upAttachmentMain" runat="server" UpdateMode="Conditional">
    <ContentTemplate>
        <asp:HiddenField ID="ihBlobID" runat="server" />
        <asp:HiddenField ID="ihDisplayAtTopExpire" runat="server" />
        <asp:Button ID="btnRefresh" runat="server" OnClick="btnRefresh_Click" Style="visibility: hidden;
            display: none;" />
        <Rock:Grid ID="dgAttachment" runat="server" CssClass="list" AllowPaging="false"
            AddEnabled="false" ExportEnabled="false" DataKeyField="AttachmentID" DeleteEnabled="false" OnItemCommand="" OnRowDataBound="dgAttachment_RowDataBind"
            NoResultText="No Attachments found" ShowActionRow="false">
            <Columns>
                <Rock:RockBoundField DataField="AttachmentID" Visible="false" />
                <asp:HyperLinkField DataTextField="Title" HeaderText="Title" DataNavigateUrlFields="BlobGuid" DataNavigateUrlFormatString="{0}" ItemStyle-Width="20%" />
                <Rock:RockBoundField DataField="Description" HeaderText="Description" ItemStyle-Width="25%" />
                <Rock:RockBoundField DataField="FileType" HeaderText="File Type" ItemStyle-Width="15%" />
                <Rock:RockBoundField DataField="CreatedBy" HeaderText="Created By" ItemStyle-Width="10%" />
                <Rock:RockBoundField DataField="DateModified" HeaderText="Last Updated" ItemStyle-Width="20%" />
                <Rock:RockTemplateField ItemStyle-HorizontalAlign="Right" ItemStyle-Width="10%">
                    <ItemTemplate>
                        <asp:LinkButton ID="lbEdit" runat="server" OnCommand="dgAttachment_ItemCommand" CommandName="Edit" CommandArgument='<%# DataBinder.Eval(Container.DataItem, "AttachmentID"); %>'><i class="fa fa-pencil" title="Edit"></i></asp:LinkButton>
                        <asp:LinkButton ID="lbHide" runat="server" OnCommand="dgAttachment_ItemCommand" CommandName="Hide" CommandArgument='<%# DataBinder.Eval(Container.DataItem, "AttachmentID"); %>'><i class="fa fa-remove" title="Hide"></i></asp:LinkButton>
                    </ItemTemplate>
                </Rock:RockTemplateField>
            </Columns>
        </Rock:Grid>
    </ContentTemplate>
</asp:UpdatePanel>
