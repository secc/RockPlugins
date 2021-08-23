﻿<%@ Control Language="C#" AutoEventWireup="true" CodeFile="Attachments.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Purchasing.Attachments" %>

<asp:UpdatePanel ID="upAttachmentMain" runat="server">
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
                <asp:HyperLinkField DataTextField="Title" HeaderText="Title" DataNavigateUrlFields="BlobGuid" DataNavigateUrlFormatString="/GetFile.ashx?guid={0}" Target="_blank" ItemStyle-Width="20%" />
                <Rock:RockBoundField DataField="Description" HeaderText="Description" ItemStyle-Width="25%" />
                <Rock:RockBoundField DataField="FileType" HeaderText="File Type" ItemStyle-Width="15%" />
                <Rock:RockBoundField DataField="CreatedBy" HeaderText="Created By" ItemStyle-Width="10%" />
                <Rock:RockBoundField DataField="DateModified" HeaderText="Last Updated" ItemStyle-Width="20%" />
                <Rock:RockTemplateField ItemStyle-HorizontalAlign="Right" ItemStyle-Width="10%" HeaderStyle-CssClass="hidden-print" FooterStyle-CssClass="hidden-print" ItemStyle-CssClass="hidden-print">
                    <ItemTemplate>
                        <asp:LinkButton ID="lbEdit" runat="server" CssClass="btn btn-default" OnCommand="dgAttachment_ItemCommand" CommandName="EditAttachment" CommandArgument='<%# DataBinder.Eval(Container.DataItem, "AttachmentID") %>'><i class="fa fa-pencil" title="Edit"></i></asp:LinkButton>
                        <asp:LinkButton ID="lbHide" runat="server" CssClass="btn btn-default" OnClientClick="return Rock.dialogs.confirmDelete(event, 'Attachment');" OnCommand="dgAttachment_ItemCommand" CommandName="Hide" CommandArgument='<%# DataBinder.Eval(Container.DataItem, "AttachmentID") %>'><i class="fa fa-remove" title="Hide"></i></asp:LinkButton>
                    </ItemTemplate>
                </Rock:RockTemplateField>
            </Columns>
        </Rock:Grid>

        <Rock:ModalDialog ID="mdAttachment" Title="Add Attachment" runat="server"
             SaveButtonText="Save" OnSaveClick="mdAttachment_SaveClick" ValidationGroup="Attachment">
            <Content>
                 <Rock:NotificationBox ID="nbInvalid" runat="server" NotificationBoxType="Danger" Visible="false" />

                <asp:HiddenField id="hdnAttachmentId" runat="server" />
                <div class="container">
                    <div class="row">
                        <div class="col-sm-2 text-center">
                            <div class="required form-group" style="width: 110px;">
                                <label class="control-label">Drag File or<br /> Click Upload</label>
                                <Rock:FileUploader  runat="server" ValidationGroup="Attachment"
                                     Required="true" id="fuprAttachment" OnFileUploaded="fuprAttachment_FileUploaded"></Rock:FileUploader>
                            </div>
                        </div>
                            <div class="col-sm-9">
                                <Rock:RockTextBox runat="server"  Label="Title"  ID="tbTitle" Required="true"></Rock:RockTextBox>
                                <Rock:RockTextBox runat="server"  Rows="5" Label="Description"
                                     TextMode="MultiLine" ID="tbAttachmentDesc"></Rock:RockTextBox>
                        </div>
                    </div>
                </div>
            </Content>
        </Rock:ModalDialog>

    </ContentTemplate>
</asp:UpdatePanel>
