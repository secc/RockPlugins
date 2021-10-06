<%@ Control Language="C#" AutoEventWireup="true" CodeFile="WidgityTypeDetail.ascx.cs" Inherits="RockWeb.Plugins.org.secc.Widgities.WidgityTypeDetail" %>


<script type="text/javascript">
    function clearActiveDialog() {
        $('#<%=hfActiveDialog.ClientID %>').val('');
    }
</script>

<asp:UpdatePanel ID="upnlContent" runat="server" CssClass="panel panel-block">
    <ContentTemplate>
        <asp:HiddenField ID="hfActiveDialog" runat="server" />

        <Rock:ModalDialog ID="dlgWidgityTypeAttributes" runat="server" Title="Widgity Type Attributes"
            OnSaveClick="dlgWidgityTypeAttributes_SaveClick" OnSaveThenAddClick="dlgWidgityTypeAttributes_SaveThenAddClick"
            OnCancelScript="clearActiveDialog();" ValidationGroup="Attributes">
            <Content>
                <Rock:AttributeEditor ID="edtWidgityAttributes" runat="server" ShowActions="false" ValidationGroup="Attributes" />
            </Content>
        </Rock:ModalDialog>

        <Rock:ModalDialog ID="dlgWidgityItemAttributes" runat="server" Title="Widgity Item Attributes"
            OnSaveClick="dlgWidgityItemAttributes_SaveClick" OnSaveThenAddClick="dlgWidgityItemAttributes_SaveThenAddClick"
            OnCancelScript="clearActiveDialog();" ValidationGroup="ItemAttributes">
            <Content>
                <Rock:AttributeEditor ID="edtWidgityItemAttributes" runat="server" ShowActions="false" ValidationGroup="ItemAttributes" />
            </Content>
        </Rock:ModalDialog>

        <div class="panel panel-block">

            <div class="panel-heading">
                <h1 class="panel-title">Edit Widgity Type
                </h1>
                <div class="panel-labels">
                    <asp:LinkButton CssClass="btn btn-default" Text="<i class='fa fa-download'></i>" ID="btnExport" OnClick="btnExport_Click" runat="server" />
                </div>
            </div>

            <Rock:PanelDrawer ID="pdAuditDetails" runat="server"></Rock:PanelDrawer>
            <div class="panel-body">
                <Rock:RockTextBox runat="server" Label="Name" ID="tbName" Required="true" />
                <Rock:RockListBox runat="server" ID="lbEntityTypes" DisplayDropAsAbsolute="true"
                    Label="Entity Types" DataValueField="Id" DataTextField="FriendlyName" />
                <Rock:CategoryPicker runat="server" ID="pCategory" />
                <Rock:RockTextBox runat="server" ID="tbIcon" Label="Icon Class" Required="true" />
                <Rock:RockTextBox runat="server" Label="Description" ID="tbDescription" TextMode="MultiLine" Height="100" />
                <Rock:CodeEditor runat="server" Label="Lava Markup" ID="ceMarkup" EditorMode="Lava" EditorHeight="400" Required="true" />
                <Rock:LavaCommandsPicker runat="server" ID="lcCommands" Label="Lava Commands" />
                <Rock:Toggle runat="server" ID="cbHasItems" OnText="Yes" OffText="No" Label="Widgities Have Items" OnCheckedChanged="cbHasItems_CheckedChanged" />
            </div>
            <div class="row">
                <div class="col-lg-6">
                    <div class="col-md-12">
                        <div class="well">
                            <h3>Widgity Attributes</h3>

                            <div class="grid">
                                <Rock:Grid ID="gWidgityAttributes" runat="server" DataKeyNames="Guid" AllowPaging="false" DisplayType="Light" RowItemText="Attribute">
                                    <Columns>
                                        <Rock:ReorderField />
                                        <Rock:RockBoundField DataField="Name" HeaderText="Attribute" />
                                        <Rock:RockBoundField DataField="Key" HeaderText="Key" />
                                        <Rock:RockBoundField DataField="FieldType" HeaderText="Field Type" />
                                        <Rock:BoolField DataField="IsRequired" HeaderText="Required" />
                                        <Rock:EditField OnClick="gWdigityAttributes_Edit" />
                                        <Rock:DeleteField OnClick="gWdigityAttributes_Delete" />
                                    </Columns>
                                </Rock:Grid>
                            </div>
                        </div>
                    </div>
                </div>
                <asp:Panel runat="server" ID="pnlWidgityItemAttributes" CssClass="col-lg-6">
                    <div class="col-md-12">
                        <div class="well">
                            <h3>Widgity Item Attributes</h3>
                            <div class="grid">
                                <Rock:Grid ID="gWidgityItemAttributes" runat="server" DataKeyNames="Guid" AllowPaging="false" DisplayType="Light" RowItemText="Attribute">
                                    <Columns>
                                        <Rock:ReorderField />
                                        <Rock:RockBoundField DataField="Name" HeaderText="Attribute" />
                                        <Rock:RockBoundField DataField="Key" HeaderText="Key" />
                                        <Rock:RockBoundField DataField="FieldType" HeaderText="Field Type" />
                                        <Rock:BoolField DataField="IsRequired" HeaderText="Required" />
                                        <Rock:EditField OnClick="gWdigityItemAttributes_Edit" />
                                        <Rock:DeleteField OnClick="gWdigityItemAttributes_Delete" />
                                    </Columns>
                                </Rock:Grid>
                            </div>
                        </div>
                    </div>
                </asp:Panel>
                <div class="col-xs-12">
                    <Rock:BootstrapButton runat="server" Text="Save" CssClass="btn btn-primary" ID="btnSave" OnClick="btnSave_Click" />
                    <asp:LinkButton Text="Cancel" ID="btnCancel" runat="server" OnClick="btnCancel_Click" />
                    <br />
                    <br />
                </div>
            </div>
    </ContentTemplate>
    <Triggers>
        <asp:PostBackTrigger ControlID="btnExport" />
    </Triggers>
</asp:UpdatePanel>
