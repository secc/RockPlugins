<%@ Control Language="C#" AutoEventWireup="true" CodeFile="QuickSearch.ascx.cs" Inherits="RockWeb.Plugins.org_secc.FamilyCheckin.QuickSearch" %>


<asp:UpdatePanel ID="upContent" runat="server">
<ContentTemplate>

    <script>
        
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
                $.ajax({
                    url: "/api/family/<%=CurrentKioskId.Value+"/"+BlockCache.Guid%>/"+phoneNumber,
                    dataType: "json",
                    success: function(data){
                        showFamilies(data);},
                });
            } else {
                $("#familyDiv").empty();
            }
        }

        var showFamilies = function(families) {
            var familyDiv = $("#familyDiv");
            familyDiv.empty();
            families.forEach(
                function(family){
                    var link = $("<a>");
                    link.html("<h4>"+family["Caption"] +"</h4>"+ family["SubCaption"]);
                    link.addClass("btn btn-default btn-block");
                    familyDiv.append(link);
                }
            );
        }

        
    </script>
<div class="container">
    <div class="row">
        <div class="col-md-4">
    <div class="checkin-header">
        <h1><asp:Literal ID="lPageTitle" runat="server" /></h1>
    </div>

    <div class="checkin-body">
        
        <div class="checkin-scroll-panel">
            <div class="scroller">

                <div class="checkin-search-body">

                <asp:Panel ID="pnlSearchPhone" runat="server">
                    <Rock:RockTextBox ID="tbPhone" MaxLength="10" CssClass="checkin-phone-entry" runat="server" Label="Phone Number"
                         onkeyup="findFamilies(this.value)"/>

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

                <asp:Panel ID="pnlSearchName" runat="server">
                    <Rock:RockTextBox ID="txtName" runat="server" Label="Name" CssClass="namesearch" />
                </asp:Panel>

                <div class="checkin-actions">
                <Rock:BootstrapButton CssClass="btn btn-primary" Visible="true" ID="lbSearch" runat="server" OnClick="lbSearch_Click" Text="Search" DataLoadingText="Searching..." ></Rock:BootstrapButton>
                    
                </div>

            </div>
            
            </div>
        </div>

    </div>
        </div>
        <div class="col-md-8 scroller" id="familyDiv">
            
        </div>
        </div>
    </div>
</div>
    </ContentTemplate>
</asp:UpdatePanel>
