<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CampusLockDownAlerts.ascx.cs" Inherits="RockWeb.Plugins.org_secc.SafetyAndSecurity.CampusLockDownAlerts" %>


<style type="text/css">

    .campusClass h3 {
        padding: 30px;
        margin-bottom: 5px;
        font-weight: 700;
        text-transform: uppercase;
        position: absolute;
        top: 50%;
        left: 50%;
        transform: translate(-50%, -50%);
    }

    .panel-title {
        font-weight: bold;
    }

    div.panel-heading,
    div.NotificationsMessages {
        display: flex;
        flex-direction: row;
    }

    .panel-flex {
        flex: 1 1 0px !important;
    }
    .center-text {
        text-align: center !important;
    }
</style>



<asp:UpdatePanel ID="upCampusAlerts" runat="server">
    <ContentTemplate>


        <Rock:ModalDialog ID="mdSendUpdate" runat="server" Title="Send Update" OnSaveClick="mdSendUpdate_SendClick"  SaveButtonText="Send">
            <Content>
                <%-- multi-line textbox --%>
                <Rock:RockTextBox ID="tbAlertMessage" Label="Message" runat="server" CssClass="js-sms-text-message" TextMode="MultiLine" Rows="3" Placeholder="Type a message" Required="false"  ValidateRequestMode="Disabled" />
                <asp:HiddenField ID="hfAlertID" runat="server" Value="" />
                <asp:HiddenField ID="hfAllClear" runat="server" Value="" />
            </Content>
        </Rock:ModalDialog>

        <Rock:ModalAlert ID="maPopup" runat="server" />



        <div class="panel-heading">
                    <i class="fas fa-shield-alt" style="padding-right:1em;"></i>
                    Active Notifications
      
                </div>
        <div class="panel-group" id="accordion-id-active-notifications" role="tablist" aria-multiselectable="true">
        <asp:Repeater ID="rNotifications" runat="server" OnItemDataBound="ItemBound">
            <ItemTemplate>
                <asp:HiddenField ID="hfAlertNotificationId" runat="server" Value='<%# Eval("Id") %>' />
                
                <div class="panel panel-default alert-panel">
                    <div class="panel-heading" role="tab" id='heading1-id-<%# Eval("Id") %>'>

                        <h4 class="panel-title panel-flex">
                            <a class="collapsed" role="button" data-toggle="collapse" data-parent="#accordion-id-active-notifications" href='#collapse1-id-<%# Eval("Id") %>' aria-expanded="false" aria-controls="collapse1" class=""><asp:Literal Text='<%# Eval("Title") %>' runat="server" /></a></h4>   
                        <p class="panel-flex"><asp:Literal Text='<%# Eval("AudienceValue.Value") %>' runat="server" /></p>
                        <p class="panel-flex"><asp:Literal Text='<%# Eval("CreatedDateTime") %>' runat="server" /></p>
                        <p class="panel-flex">Created By: <asp:Literal Text='<%# Eval("CreatedByPersonAlias.Person.FullName") %>' runat="server" /></p>
                        <p class="panel-flex">
                            <asp:LinkButton CssClass="btn btn-danger" ID="SendUpdate" runat="server" CommandName="SendUpdate_Click" CommandArgument='<%# Eval("Id") %>' OnCommand="SendUpdate_Click" Text="Send Update" CausesValidation="false"/>
                            <asp:LinkButton CssClass="btn btn-success" ID="AllClear" runat="server" CommandName="AllClear_Click" CommandArgument='<%# Eval("Id") %>' OnCommand="AllClear_Click" Text="All Clear" CausesValidation="false" />
                        </p>
                    </div>
                    <div id='collapse1-id-<%# Eval("Id") %>' class="panel-collapse collapse" role="tabpanel" aria-labelledby='heading1-id-<%# Eval("Id") %>' aria-expanded="true" style="">
                        <div class="panel-body">
                            <asp:Repeater ID="rNotificationsMessages" runat="server">
                                <ItemTemplate>
                                    <div class="NotificationsMessages">
                                        <p class="panel-flex"><%# Eval("Message") %></p>
                                        <p class="panel-flex center-text">Sent: <%# Eval("CreatedDateTime") %></p>
                                        <p class="panel-flex center-text">By: <%# Eval("CreatedByPersonAlias.Person.FullName") %></p>
                                    </div>
                                    
                                </ItemTemplate>
                            </asp:Repeater>
                        </div>
                    </div>
                </div>

                    
            </ItemTemplate>
        </asp:Repeater>
            </div>

        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
