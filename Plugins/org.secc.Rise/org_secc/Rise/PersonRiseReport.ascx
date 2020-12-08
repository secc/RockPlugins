<%@ Control Language="C#" AutoEventWireup="true" CodeFile="PersonRiseReport.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Rise.PersonRiseReport" %>

<asp:UpdatePanel ID="upnlAttributeValues" runat="server" class="context-attribute-values">
<ContentTemplate>

    <section class="panel panel-persondetails">
        <div class="panel-heading rollover-container clearfix">
            <h3 class="panel-title pull-left">
                <i class="fa fa-chalkboard"></i> <asp:Literal ID="lBlockName" runat="server" />
            </h3>
        </div>
        <div class="panel-body">
            <asp:PlaceHolder ID="phCourses" runat="server" />
        </div>
    </section>

</ContentTemplate>
</asp:UpdatePanel>

