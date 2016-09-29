<%@ Control Language="C#" AutoEventWireup="true" CodeFile="PersonalGroupInformationPanel.ascx.cs" Inherits="RockWeb.Plugins.org_secc.GroupManager.PersonalGroupInformationPanel" %>

<script>
    var groupMemberDeleteId = "";

    var deleteGroupMember = function(confirm){
        if (confirm){
            __doPostBack('DeleteGroup', groupMemberDeleteId);
        }
    }
</script>

<asp:UpdatePanel ID="upnlAttributeValues" runat="server" class="context-attribute-values">
    <ContentTemplate>
        <input type="hidden" id="DeleteGroup" />
        <Rock:ModalDialog runat="server" ID="mdSettings" OnSaveClick="mdSettings_SaveClick">
            <Content>
                <Rock:RockTextBox runat="server" ID="tbTitle" Label="Panel Title"></Rock:RockTextBox>
                <Rock:RockTextBox runat="server" ID="tbIcon" Label="CSS Icon"></Rock:RockTextBox>
                <Rock:GroupPicker runat="server" AllowMultiSelect="true" ID="gpGroups" Label="Groups" />
            </Content>
        </Rock:ModalDialog>

        <asp:HiddenField runat="server" ID="hfEditMode" Value="0" />
        <section class="panel panel-persondetails">
            <div class="panel-heading rollover-container clearfix">
                <h3 class="panel-title pull-left">
                    <asp:Literal ID="ltTitle" runat="server" />
                </h3>
                <div class="actions rollover-item pull-right">
                    <asp:LinkButton ID="lbEdit" runat="server" CssClass="edit" ToolTip="Edit Attributes" OnClick="lbEdit_Click"><i class="fa fa-pencil"></i></asp:LinkButton>
                </div>
            </div>
            <div class="panel-body">
                <asp:PlaceHolder ID="phGroups" runat="server" />
            </div>
        </section>

    </ContentTemplate>
</asp:UpdatePanel>

