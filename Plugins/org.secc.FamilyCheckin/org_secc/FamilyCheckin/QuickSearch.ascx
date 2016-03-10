<%@ Control Language="C#" AutoEventWireup="true" CodeFile="QuickSearch.ascx.cs" Inherits="RockWeb.Plugins.org_secc.FamilyCheckin.QuickSearch" %>


<asp:UpdatePanel ID="upContent" runat="server">
<ContentTemplate>

    <script>
        var selectionActive=false;


        Sys.Application.add_load(function () {
            $('.tenkey a.digit').click(function () {
                $phoneNumber = $("input[id$='tbPhone']");
                $phoneNumber.val($phoneNumber.val() + $(this).html());
                findFamilies($phoneNumber.val());
            });
            $('.tenkey a.back').click(function () {
                $phoneNumber = $("input[id$='tbPhone']");
                $phoneNumber.val($phoneNumber.val().slice(0, -1));
                findFamilies($phoneNumber.val());
            });
            $('.tenkey a.clear').click(function () {
                $phoneNumber = $("input[id$='tbPhone']");
                $phoneNumber.val('');
                findFamilies($phoneNumber.val());
            });

            // set focus to the input unless on a touch device
            var isTouchDevice = 'ontouchstart' in document.documentElement;
            if (!isTouchDevice) {
                $('.checkin-phone-entry').focus();
            }
        });
        var findFamilies = function (phoneNumber) {
            phoneNumber = phoneNumber.replace(/\D/g, '');
            var minLength = <%= minLength.ToString()%>;
            if (phoneNumber.length>=minLength){

                $("#searchMsg").html("Searching for families...").css("visibility","unset");
                
                $.ajax({
                    url: "/api/org.secc/familycheckin/family/"+phoneNumber,
                    dataType: "json",
                    success: function(data){
                        showFamilies(data);},
                });
            } else {
                $("#familyDiv").empty();
            }
        }

        var showFamilies = function(families) {
            $("#searchMsg").css("visibility","hidden")

            var familyDiv = $("#familyDiv");
            familyDiv.empty();
            families.forEach(
                function(family){
                    var link = $("<a>");
                    link.html("<h4>"+family["Caption"] +"</h4>"+ family["SubCaption"]);
                    link.attr("id",family["Group"]["Id"]);
                    link.click(chooseFamily)
                    link.addClass("btn btn-primary btn-block familyButton");
                    familyDiv.append(link);
                }
            );
        }

        var chooseFamily = function(event){
            if (selectionActive){
                return;
            }
            selectionActive=true
            this.innerHTML = "<h4><i class='fa fa-refresh fa-spin'></i>Loading Family</h4>Please wait..."
            this.className = "btn btn-success btn-block"
            
            __doPostBack("ChooseFamily", this.id);
        }

        
    </script>

    <asp:PlaceHolder ID="phScript" runat="server"></asp:PlaceHolder>

    <Rock:HiddenFieldWithClass ID="hfRefreshTimerSeconds" runat="server" CssClass="js-refresh-timer-seconds" />

<span style="display: none">
            <asp:LinkButton ID="lbRefresh" runat="server" OnClick="lbRefresh_Click"></asp:LinkButton>
            <asp:Label ID="lblActiveWhen" runat="server" CssClass="active-when" />
        </span>

        <%-- Panel for no schedules --%>
        <asp:Panel ID="pnlNotActive" runat="server">
            <div class="checkin-header">
                <h1>Check-in Is Not Active</h1>
            </div>

            <div class="checkin-body">

                <div class="checkin-scroll-panel">
                    <div class="scroller">
                        <p>There are no current or future schedules for this kiosk!</p>

                    </div>
                </div>

            </div>
        </asp:Panel>

        <%-- Panel for schedule not active yet --%>
        <asp:Panel ID="pnlNotActiveYet" runat="server">
            <div class="checkin-header">
                <h1>Check-in Is Not Active Yet</h1>
            </div>

            <div class="checkin-body">

                <div class="checkin-scroll-panel">
                    <div class="scroller">

                        <p>This kiosk is not active yet.  Countdown until active: <span class="countdown-timer"></span></p>
                        <asp:HiddenField ID="hfActiveTime" runat="server" />

                    </div>
                </div>

            </div>
        </asp:Panel>

        <%-- Panel for location closed --%>
        <asp:Panel ID="pnlClosed" runat="server">
            <div class="checkin-header checkin-closed-header">
                <h1>Closed</h1>
            </div>

            <div class="checkin-body checkin-closed-body">
                <div class="checkin-scroll-panel">
                    <div class="scroller">
                        <p>This location is currently closed.</p>
                    </div>
                </div>
            </div>
        </asp:Panel>

    <%-- Panel for active checkin --%>
        <asp:Panel ID="pnlActive" runat="server">
<div class="checkin-header">

</div>
 <div class="checkin-body">
            <div class="scroller">
                    <div class="container">
                        <div class="row">
                            <div class="col-xs-4">
                                <div class="checkin-search-body">

                                        <asp:Panel ID="pnlSearchPhone" runat="server">
                                            <Rock:RockTextBox ID="tbPhone" MaxLength="10" CssClass="checkin-phone-entry" runat="server" Label="Phone Number"
                                                onkeyup="findFamilies(this.value)" autocomplete="off"/>
                                            <div class="tenkey checkin-phone-keypad">
                                                <div>
                                                    <a href="#" class="btn btn-default btn-lg digit">1</a>
                                                    <a href="#" class="btn btn-default btn-lg digit">2</a>
                                                    <a href="#" class="btn btn-default btn-lg digit">3</a>
                                                </div>
                                                <div>
                                                    <a href="#" class="btn btn-default btn-lg digit">4</a>
                                                    <a href="#" class="btn btn-default btn-lg digit">5</a>
                                                    <a href="#" class="btn btn-default btn-lg digit">6</a>
                                                </div>
                                                <div>
                                                    <a href="#" class="btn btn-default btn-lg digit">7</a>
                                                    <a href="#" class="btn btn-default btn-lg digit">8</a>
                                                    <a href="#" class="btn btn-default btn-lg digit">9</a>
                                                </div>
                                                <div>
                                                    <a href="#" class="btn btn-default btn-lg command back">Back</a>
                                                    <a href="#" class="btn btn-default btn-lg digit">0</a>
                                                    <a href="#" class="btn btn-default btn-lg command clear">Clear</a>
                                                </div>
                                            </div>
                                        </asp:Panel>
                                
                                </div>
                            </div>
                            <div class="col-xs-8">
                                <div class="alert alert-info" style="visibility:hidden" id="searchMsg"></div>
                            </div>
                            <div class="col-xs-8" id="familyDiv">
            
                            </div>
                        </div>
                    </div>
                </div>
        </div>
    </asp:Panel>
    <style type="text/css">
        .checkin-scroll-panel, .scroller {
            padding-right:0px;
            position:relative;
            top:0px;
            bottom: 0px;
        }
    </style>
    </ContentTemplate>
</asp:UpdatePanel>
