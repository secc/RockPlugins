<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GroupPublishRequest.ascx.cs" Inherits="RockWeb.Plugins.GroupManager.GroupPublishRequest" %>

<asp:UpdatePanel runat="server">
    <ContentTemplate>
        <asp:Panel runat="server" ID="pnlEdit" Visible="false">
            <div class="panel panel-default">
                <div class="panel-heading">
                    <h3 class="panel-title">Publish:
                        <asp:Literal ID="ltGroupName" runat="server" />
                    </h3>
                </div>
                <div class="panel-body">
                    <div class="row">
                        <div class="col-md-2">
                            <Rock:ImageUploader runat="server" ID="iGroupImage" Label="Image" />
                        </div>
                        <div class="col-md-10">
                            <Rock:RockTextBox runat="server" ID="tbDescription" TextMode="MultiLine" Label="Description" Required="true" Height="135" />
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-4">
                            <Rock:DateRangePicker runat="server" ID="drPublishDates" Label="Publish Dates" Required="true" Help="Dates the group will be published online. Dates are inclusive" />
                        </div>
                        <div class="col-md-8">
                            <Rock:DefinedValuesPickerEnhanced runat="server" EnhanceForLongLists="true" ID="ddlAudience" Label="Audience" />
                        </div>
                    </div>
                    <h3>Contact Information</h3>
                    <hr />
                    <Rock:PersonPicker runat="server" ID="pContactPerson" Label="Contact Person" Required="true" OnSelectPerson="pRequestor_SelectPerson" />
                    <Rock:EmailBox runat="server" ID="tbContactEmail" Label="Contact Email" Required="true" />
                    <Rock:PhoneNumberBox runat="server" ID="tbContactPhoneNumber" Label="Contact Phone Number" Required="true" />
                    <h3>Confirmation Email</h3>
                    <hr />
                    <Rock:RockTextBox runat="server" ID="tbConfirmationFromName" Label="Confirmation From Name" Required="true" />
                    <Rock:EmailBox runat="server" ID="tbConfirmationFromEmail" Label="Confirmation From Email" Required="true" />
                    <Rock:RockTextBox runat="server" ID="tbConfirmationSubject" Label="Confirmation Email Subject" Required="true" />
                    <Rock:HtmlEditor runat="server" ID="ceConfirmationBody" Label="Confirmation Email Body" Height="400" Required="true"/>

                    <Rock:BootstrapButton runat="server" ID="btnSave" Text="Publish" CssClass="btn btn-primary" OnClick="btnSave_Click" />
                </div>
            </div>
        </asp:Panel>
        <asp:Panel runat="server" ID="pnlSelectGroup" Visible="false">
            <Rock:GroupPicker runat="server" ID="gpGroup" OnSelectItem="gpGroup_SelectItem" />
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
