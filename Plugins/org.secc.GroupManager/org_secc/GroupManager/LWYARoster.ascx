<%@ Control Language="C#" AutoEventWireup="true" CodeFile="LWYARoster.ascx.cs" Inherits="RockWeb.Plugins.org_secc.GroupManager.LWYARoster" %>

<style>
    .columns {
        -webkit-column-count: 3;
        -webkit-column-gap: 10px;
        -webkit-column-fill: auto;
        -moz-column-count: 3;
        -moz-column-gap: 10px;
        column-count: 3;
        column-gap: 10px;
        column-fill: auto;
    }

    .panel {
        -webkit-column-break-inside: avoid;
        page-break-inside: avoid;
        break-inside: avoid;
    }

    @media (max-width: 990px) {
        .columns {
            -webkit-column-count: 2;
            -moz-column-count: 2;
            column-count: 2;
        }
    }

    @media (max-width: 480px) {
        .columns {
            -webkit-column-count: 1;
            -moz-column-count: 1;
            column-count: 1;
        }
    }

    @media print {
        @page {
            margin: 0cm;
        }

        .page_content_wrap {
            margin: 1rem;
        }
        header {
            top: 1rem !important;
            position: relative !important;
        }

        .content_wrap {
            width: 100%;
        }
        .breadcrumbs {
            position: relative;
            left: 2rem;
            margin: 0cm;
        }

        #cd-primary-nav, footer, .cd-nav-trigger, .btn {
            display: none;
        }
        #cms-admin-footer {
            display: none;
        }

        article, div.panel {
            margin-top: 1rem;
            page-break-inside: avoid;
        }
        .collapse {
            display: block !important;
            height: auto !important;
        }
        .title_divider_before {
            -webkit-print-color-adjust: exact;
            background: #F14A52 !important;
        }
        .thumbnail {
            display: table-cell;
            width: 22rem;
        }
        .columns {
            width: 90%;
            -webkit-column-count: 2;
            -moz-column-count: 2;
            column-count: 2;
            -webkit-column-gap: 2px;
            -moz-column-gap: 2px;
            column-count: 2;
            column-gap: 2px;
            column-fill: auto;
        }

        tfoot {
            display: none;
        }
    }
    .image-teardrop
    {
        overflow: hidden;
        margin: 0px 10px 10px 0px;
    }
    .image-cropper {
        width: 100px;
        height: 125px;
        position: relative;
        overflow: hidden;
    }
    
    .image-cropper > img {
        display: inline;
        margin: 0 auto;
        height: 100%;
        width: auto;
    }
    .thumbnail {
        padding: 10px;
        margin: 10px;
        height: 100%;
        display: block;
        background: #fff;
        box-shadow: 0px 1px 1px rgba(0, 0, 0, 0.2);
        transition: all .3s;
        border-radius: 0;
        border: 3px solid #f4f4f4;
    }
    .thumbnail:hover {
        transform: translateY(-10px);
        box-shadow: 0 22px 43px rgba(0, 0, 0, 0.15);
    }
    .thumbnail .header {
        background-color: #f4f4f4;
        padding: 10px;
        margin: -10px;
        margin-bottom: 10px;
    }
    .label {
        margin: 0px 3px;
    }
    a[id$="_btnRemoveMember"] {
        margin-top:-5px;
    }
</style>
<script>
$(window).load(function() {
    // Initialize Isotope
    var $grid = $('.isotope_wrap').isotope({
        // options
        percentPosition: true,
        itemSelector: '.isotope_item',
        layoutMode: 'fitRows'
    });
    // reveal all items after init
    var $items = $grid.find('.isotope_item');
    $grid.addClass('is-showing-items')
    .isotope( 'revealItemElements', $items );
    });
    var confirmedRemove = false;
    var confirmRemove = function (el, e) {
        if (confirmedRemove) {
            confirmedRemove = false;
            return true;
        }
        // make sure the element that triggered this event isn't disabled
        if (e.currentTarget && e.currentTarget.disabled) {
            return false;
        }
        
        Rock.dialogs.confirm('Are you sure you want to deactivate this group member?', function (el, e) {
            return function (response) {
                if (response) {
                    confirmedRemove = true;
                    el.click();
                }
            }
        }(el, e));
        return false;
    }
</script>
<asp:UpdatePanel ID="upDevice" runat="server">
    <ContentTemplate>
        <asp:HiddenField ID="hfCommunication" runat="server" Value="" />
        <Rock:NotificationBox runat="server" ID="nbAlert" NotificationBoxType="Danger" Visible="false"></Rock:NotificationBox>

        <Rock:ModalAlert ID="maSent" runat="server"></Rock:ModalAlert>

        <Rock:ModalDialog ID="mdMember" runat="server">
            <Content>
                <asp:HiddenField ID="hfMemberId" runat="server" />
                <div class="container">
                    <div>
                        <Rock:HighlightLabel ID="lbStatus" runat="server"></Rock:HighlightLabel>
                        <Rock:HighlightLabel ID="lbRole" LabelType="Default" runat="server"></Rock:HighlightLabel>
                    </div>
                    <div class="row">
                        <Rock:RockLiteral ID="ltAddress" runat="server" Label="Address" CssClass="col-sm-6 col-xs-12"></Rock:RockLiteral>
                        <Rock:RockLiteral ID="ltPhone" runat="server" Label="Phone Number" CssClass="col-sm-6 col-xs-12"></Rock:RockLiteral>
                    </div>
                    <div class="row">
                        <Rock:RockLiteral ID="ltEmail" runat="server" Label="Email" CssClass="col-sm-6 col-xs-12"></Rock:RockLiteral>
                        <Rock:RockLiteral ID="ltDateAdded" runat="server" Label="Date Added" CssClass="col-sm-6 col-xs-12"></Rock:RockLiteral>
                    </div>
                    <div class="row">
                        <Rock:RockLiteral ID="ltDateInactive" runat="server" Label="Date Inactivated"
                            CssClass="col-sm-6 col-xs-12"></Rock:RockLiteral>
                        <div class="col-sm-6 col-xs-12">
                            <Rock:BootstrapButton ID="btnActivate" runat="server" Text="Activate Member"
                                OnClick="btnActivate_Click" CssClass="btn btn-primary"></Rock:BootstrapButton>
                            <Rock:BootstrapButton ID="btnDeactivate" runat="server" Text="Deactivate Member"
                                OnClick="btnDeactivate_Click" CssClass="btn btn-danger"></Rock:BootstrapButton>

                            <Rock:BootstrapButton ID="btnCloseModal" CssClass="btn btn-default"
                                OnClick="btnCloseModal_Click" runat="server" Text="Close"></Rock:BootstrapButton>
                        </div>
                    </div>
                </div>
            </Content>
        </Rock:ModalDialog>
        
        <Rock:ModalDialog ID="mdAddMember" ValidationGroup="AddMember" CancelLinkVisible="false" runat="server" Title="Add Group Member">
            <Content>
                <asp:Panel runat="server" ID="pnlForm">
                    <Rock:NotificationBox runat="server" ID="nbInvalid" NotificationBoxType="Warning" Dismissable="true" Visible="true">
                        First and Last name is required and one of Birthday, Phone Number, or Email.
                    </Rock:NotificationBox>
                    <Rock:RockTextBox ID="tbFirstName" runat="server" Label="First Name" ValidationGroup="AddMemeber" Required="True"></Rock:RockTextBox>
                    <Rock:RockTextBox ID="tbLastName" runat="server" Label="Last Name" ValidationGroup="AddMember" Required="True"></Rock:RockTextBox>

                    <Rock:DatePicker runat="server" ID="dpBirthday" Label="Birthday"></Rock:DatePicker>

                    <Rock:PhoneNumberBox ID="pnCell" runat="server" Label="Cell Phone"></Rock:PhoneNumberBox>

                    <Rock:EmailBox ID="tbEmail" runat="server" Label="Email"></Rock:EmailBox>

                    <div style="padding-bottom: 50px;">
                        <Rock:BootstrapButton ID="btnCancel" runat="server"
                            CssClass="btn btn-default pull-right btn-lg" OnClick="btnClose_Click">Close</Rock:BootstrapButton>
                        <div class="pull-right">&nbsp;</div>
                        <Rock:BootstrapButton ID="btnRegister" OnClick="btnRegister_Click" runat="server" Text="Add"
                            CssClass="btn btn-primary pull-right btn-lg" ValidationGroup="AddMember" CausesValidation="true" />
                        
                    </div>
                </asp:Panel>
                <asp:Panel runat="server" ID="pnlResults" Visible="false">
                    <Rock:RockLiteral runat="server" ID="ltResults"></Rock:RockLiteral>

                    <div>
                        <Rock:BootstrapButton ID="btnAddAnother" runat="server" CssClass="btn btn-primary"
                            OnClick="btnAddAnother_Click">Add Another Member</Rock:BootstrapButton>
                        <Rock:BootstrapButton ID="btnClose" runat="server" CssClass="btn btn-default m-l-1"
                            OnClick="btnClose_Click">Finished</Rock:BootstrapButton>
                    </div>
                </asp:Panel>
            </Content>
        </Rock:ModalDialog>

        <asp:Panel ID="pnlMain" runat="server">
            <div class="pull-right text-right" style="width:300px">
                <Rock:BootstrapButton runat="server" ID="btnAddMember" Text="Add Member" CssClass="btn s-btn--primary-bg btn-primary" OnClick="btnAddMember_Click" />
                <Rock:BootstrapButton runat="server" ID="btnEmailGroup" Text="Email Group" CssClass="btn s-btn--dark-brd btn-default" OnClick="btnEmail_Click"  />
            </div>
            <asp:Literal ID="ltTitle" runat="server" /><br />

            <asp:Panel runat="server" ID="pnlMembership" Visible="false">
                <div class="hidden-print">
                    <Rock:GridFilter ID="rFilter" runat="server" OnApplyFilterClick="rFilter_ApplyFilterClick">
                        <Rock:RockTextBox ID="txtFirstName" runat="server" Label="First Name"></Rock:RockTextBox>
                        <Rock:RockTextBox ID="txtLastName" runat="server" Label="Last Name"></Rock:RockTextBox>
                        <br>
                        <Rock:RockCheckBoxList ID="cblRole" runat="server" Label="Member Role"></Rock:RockCheckBoxList>
                        <Rock:RockCheckBoxList ID="cblGender" runat="server" Label="Gender">
                        </Rock:RockCheckBoxList>
                        <Rock:RockCheckBoxList runat="server" ID="cblStatus" Label="Status">
                        </Rock:RockCheckBoxList>
                    </Rock:GridFilter>
                </div>

                <Rock:Grid ID="gMembers" runat="server" OnRowSelected="gMembers_RowSelected"
                    OnRowDataBound="gMembers_RowDataBound" HeaderStyle-CssClass="hidden-print">
                    <Columns>
                        <Rock:SelectField ItemStyle-CssClass="hidden-print grid-select-field select-all"></Rock:SelectField>
                        <Rock:RockBoundField DataField="Name" HeaderText="Name" ColumnPriority="AlwaysVisible" />
                        <Rock:SelectField ItemStyle-CssClass="hidden-xs visible-print grid-select-field" ControlStyle-CssClass="hidden-xs"></Rock:SelectField>
                        <Rock:SelectField ItemStyle-CssClass="hidden-xs visible-print grid-select-field" ControlStyle-CssClass="hidden-xs"></Rock:SelectField>
                        <Rock:SelectField ItemStyle-CssClass="hidden-xs visible-print grid-select-field" ControlStyle-CssClass="hidden-xs"></Rock:SelectField>
                        <Rock:SelectField ItemStyle-CssClass="hidden-xs visible-print grid-select-field" ControlStyle-CssClass="hidden-xs"></Rock:SelectField>
                        <Rock:SelectField ItemStyle-CssClass="hidden-xs visible-print grid-select-field" ControlStyle-CssClass="hidden-xs"></Rock:SelectField>
                        <Rock:RockBoundField DataField="DateAdded" HeaderText="Date Added" ColumnPriority="Desktop" />
                        <Rock:RockBoundField DataField="Address" HeaderText="Address" ColumnPriority="Desktop" />
                        <Rock:RockBoundField DataField="City" HeaderText="City" ColumnPriority="Desktop" />
                        <Rock:RockBoundField DataField="State" HeaderText="State" ColumnPriority="Desktop" />
                        <Rock:RockBoundField DataField="Role" HeaderText="Role" ColumnPriority="Tablet" />
                        <Rock:RockBoundField DataField="Status" HeaderText="Status" ColumnPriority="Desktop" />
                    </Columns>
                </Rock:Grid>

                <Rock:BootstrapButton runat="server" ID="btnEmail" CssClass="btn btn-default btn-lg pull-right hidden-print" OnClick="btnEmail_Click"><i class="fa fa-laptop"></i> Email</Rock:BootstrapButton>
                <Rock:BootstrapButton runat="server" ID="btnSMS" CssClass="btn btn-default btn-lg pull-right hidden-print" OnClick="btnSMS_Click"><i class="fa fa-mobile-phone"></i> Text</Rock:BootstrapButton>
            </asp:Panel>

            <asp:Panel runat="server" ID="pnlRoster" CssClass="wrapper isotope_wrap">
                <asp:Repeater runat="server" ID="rRoster" OnItemDataBound="rRoster_ItemDataBound">
                    <ItemTemplate>
                        <div class="isotope_item nopadding col-xs-12 col-sm-6 col-md-4">
                            <div class="thumbnail">
                                <div class="header">
                                    <asp:Label runat="server" Style="font-weight: bold"
                                        Text='<%# Eval("Name") %>' />
                                    <asp:LinkButton runat="server" ID="btnRemoveMember" CommandArgument='<%# Eval("Id") %>' OnClientClick="return confirmRemove(this, event);" CssClass="btn btn-sm btn-danger pull-right" OnClick="btnRemoveMember_Click"><i class="fa fa-times"></i></asp:LinkButton>
                                </div>
                                <div class="image-teardrop pull-left">
                                    <div class="image-cropper">
                                        <img src='<%# Eval("PhotoUrl") %>&h=125&w=100&mode=crop&scale=both&anchor=topcenter' alt="Photo">
                                    </div>
                                </div>
                                    <Rock:HighlightLabel runat="server" Text='<%# Eval("Status") %>'
                                        CssClass='<%# StyleStatusLabel(Eval("Status"))%>' />
                                    <Rock:HighlightLabel runat="server" Text='<%# Eval("Role") %>'
                                        CssClass='<%# StyleLeaderLabel(Eval("IsLeader"))%>' />
                                    <br /><br />
                                        <asp:Label runat="server" ID="Label1"
                                            Text='<%# Eval("FormattedAddress") %>' />
                                        <br />
                                        <asp:Label runat="server" ID="Label5"
                                            Text='<%# Eval("Phone") %>' />
                                        <br>
                                        <Rock:BootstrapButton runat="server" ID="btnRosterEmail"></Rock:BootstrapButton>
                                        <Rock:RockLiteral ID="ltLava" runat="server" Text='<%# RosterLava(Eval("Person"))%>'></Rock:RockLiteral>
                                    
                            </div>
                        </div>
                    </ItemTemplate>
                </asp:Repeater>
            </asp:Panel>
        </asp:Panel>


        <!-- SMS Panel -->
        <asp:Panel ID="pnlSMS" runat="server" Visible="false">
            <h1>Text Message:</h1>
            <Rock:RockLiteral ID="ltSMSRecipients" runat="server"></Rock:RockLiteral>
            <Rock:Toggle ID="cbSMSSendToParents" runat="server" OnText="Parents" OffText="Members" Label="Send To:"
                OnCssClass="btn-primary" OffCssClass="btn-primary" Checked="false" OnCheckedChanged="cbSMSSendToParents_CheckedChanged" />
            <div id="charCount"></div>
            <Rock:RockTextBox ID="tbMessage" runat="server" TextMode="MultiLine" Rows="5"></Rock:RockTextBox>
            <Rock:BootstrapButton ID="btnSMSSend" runat="server" OnClick="btnSMSSend_Click" CssClass="btn btn-primary">Send</Rock:BootstrapButton>
            <Rock:BootstrapButton ID="btnSMSCancel" runat="server" OnClick="btnCancel_Click" CssClass="btn btn-default">Cancel</Rock:BootstrapButton>
        </asp:Panel>

        <!-- Email Panel -->
        <asp:Panel ID="pnlEmail" runat="server" Visible="false">
            <h1>Email</h1>
            <Rock:RockLiteral ID="ltEmailRecipients" runat="server"></Rock:RockLiteral>
            <Rock:Toggle ID="cbEmailSendToParents" runat="server" OnText="Parents" OffText="Members" Label="Send To:"
                OnCssClass="btn-primary" OffCssClass="btn-primary" Checked="false" OnCheckedChanged="cbEmailSendToParents_CheckedChanged" />
            <Rock:RockTextBox ID="tbSubject" runat="server" Label="Subject"></Rock:RockTextBox>
            <Rock:RockTextBox ID="tbBody" runat="server" TextMode="MultiLine" Rows="5" Label="Message"></Rock:RockTextBox>
            <Rock:BootstrapButton ID="btnEmailSend" runat="server" OnClick="btnEmailSend_Click" CssClass="btn btn-primary">Send</Rock:BootstrapButton>
            <Rock:BootstrapButton ID="btnEmailCancel" runat="server" OnClick="btnCancel_Click" CssClass="btn btn-default">Cancel</Rock:BootstrapButton>
        </asp:Panel>
    <pre><asp:Literal runat="server" ID="debug"></asp:Literal></pre>
    </ContentTemplate>
</asp:UpdatePanel>
