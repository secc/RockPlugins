<%@ Control Language="C#" AutoEventWireup="true" CodeFile="EventPass.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Event.EventPass" %>
<style>
    .QRImage {
        text-align:center;
    }
    .QRImage img {
        height:250px;
        width:250px;
    }
    img.PersonPhoto
    {
        display:block;
        height:150px;
        width:150px;
        margin-left:auto;
        margin-right:auto;
        
    }
    .personHeader {
        background-color:#000000;
        color:#ffffff;
        font-size:2em;
        font-weight:bold;
        text-align:center;
    }

</style>
<asp:UpdatePanel ID="upMain" runat="server">
    <ContentTemplate>
        <asp:Panel ID="pnlPass" runat="server" Visible="true" >
            <div Id="carousel-EventItem" class="carousel slide" data-ride="carousel" data-interval="false">
                <div class="carousel-inner" role="listbox">
                    <asp:Repeater ID="rPasses" runat="server" >
                        <ItemTemplate>
                            <asp:Panel ID="pnlItem" runat="server" CssClass="item">
                                <div class="col-xs-12 personHeader">
                                    <%# Eval("RegistrantPerson.FullName") %>
                                </div>
                                <div class="col-xs-6">
                                    <img src="<%# Eval("RegistrantPerson.PhotoUrl") %>" style="width:150px" />
                                </div>
                                <div class="col-xs-6">
                                    <i class="fas fa-ticket"></i> <%# Eval("EventName") %> <br />
                                    <i class="far fa-calendar-alt"></i> <%# ((DateTime)Eval("EventDate")).ToLongDateString() %> <br />
                                    <i class="far fa-map-marker"></i> <%# Eval("EventLocation") %>
                                </div>
                                <div class="col-xs-12 QRImage">
                                    <p>Please scan the QR Code to check-in at the event</p>
                                    <img src='<%# Eval("QRUrl") %>' />
                                </div>
                            </asp:Panel>
                        </ItemTemplate>
                    </asp:Repeater>
                </div>
                <a class="left carousel-control" href="#carousel-EventItem" role="button" data-slide="prev">
                    <span class="glyphicon glyphicon-chevron-left" aria-hidden="true"></span>
                    <span class="sr-only">Previous</span>
                </a>
                <a class="right carousel-control" href="#carousel-EventItem" role="button" data-slide="next">
                    <span class="glyphicon glyphicon-chevron-right" aria-hidden="true"></span>
                    <span class="sr-only">Next</span>
                </a>
            </div>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>