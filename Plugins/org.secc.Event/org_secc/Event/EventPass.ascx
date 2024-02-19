<%@ Control Language="C#" AutoEventWireup="true" CodeFile="EventPass.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Event.EventPass" %>
<style>
    .QRImage {
        text-align:center;
        padding-bottom: 1.5em;
    }
    .QRImage img {
        height:250px;
        width:250px;
    }
    .personHeader {
        background-color:#000000;
        color:#ffffff;
        font-size:2em;
        font-weight:bold;
        text-align:center;
    }

    .carousel-indicators .active 
    {
    	background-color: #000;
    }
    .carousel-indicators li 
    {
    	border: 1px solid #000;
    }

    @media screen and (orientation:portrait) { 
        .top-row { width: 100%;} 
        .bottom-row { width: 100%; } 
    }
    @media screen and (orientation:landscape) { 
        .top-row {
            float: left;
            width: 49.5%;
        } 
        .bottom-row {
            float: right;
            width: 49.5%;
        } 
    }

</style>
<asp:UpdatePanel ID="upMain" runat="server">
    <ContentTemplate>
        <asp:Panel ID="pnlPass" runat="server" Visible="false" >
            <div Id="carousel-EventItem" class="carousel slide" data-ride="carousel" data-interval="300000">
                <asp:Repeater ID="rPassIndicator" runat="server">
                    <HeaderTemplate>
                        <ol class="carousel-indicators">
                    </HeaderTemplate>
                    <ItemTemplate>
                        <li data-target="#carousel-EventItem" data-slide-to='<%# Eval("ItemOrder") %>' class='<%# ((int)Eval("ItemOrder")) == 0 ? "active" : String.Empty %>' />
                    </ItemTemplate>
                    <FooterTemplate>
                        </ol>
                    </FooterTemplate>
                </asp:Repeater>
                <div class="carousel-inner" role="listbox">
                    <asp:Repeater ID="rPasses" runat="server" >
                        <ItemTemplate>
                            <asp:Panel ID="pnlItem" runat="server" CssClass="item">
                                <div class="row top-row">
                                    <div class="col-xs-12 personHeader">
                                        <%# Eval("RegistrantPerson.FullName") %>
                                    </div>
                                </div>
                                <div class="row">
                                <div class="col-xs-4 PersonPhoto">
                                    <img src="<%# Eval("RegistrantPerson.PhotoUrl") %>" style="width:100px;"   />
                                </div>
                                    <div class="col-xs-8">
                                        <i class="fas fa-ticket"></i> <%# Eval("EventName") %> <br />
                                        <i class="far fa-calendar-alt"></i> <%# ((DateTime)Eval("EventDate")).ToLongDateString() %><br />
                                        <i class="fas fa-map-marker-alt"></i> <%# Eval("EventLocation") %>
                                    </div>
                                </div>
                                <div class="row bottom-row">
                                    <div class="col-xs-12 QRImage">
                                        <p>Please scan the QR Code to check-in at the event</p>
                                        <img src='<%# Eval("QRUrl") %>' />
                                    </div>
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
        <asp:Panel ID="pnlAlert" runat="server" Visible="false">
            <div class="row">
                <div class="col-xs-12" style="text-align:center;">
                    <h2><asp:Literal ID="lPassNotFoundTitle" runat="server" /></h2>
                    <p><asp:Literal ID="lPassNotFoundMessasge" runat="server" /></p>
                </div>
            </div>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>