<%@ Control Language="C#" AutoEventWireup="true" CodeFile="Topper.ascx.cs" Inherits="RockWeb.Plugins.org_secc.CMS.Topper" %>
<asp:UpdatePanel runat="server" ID="upnlHtmlContent">
    <ContentTemplate>

        <Rock:ModalDialog ID="mdEdit" runat="server" OnSaveClick="mdEdit_SaveClick" Title="Edit Topper Settings">
            <Content>
                <Rock:ImageUploader runat="server" ID="imgupTitleBackground" Label="Background Image" OnImageUploaded="imgupTitleBackground_ImageUploaded" OnImageRemoved="imgupTitleBackground_ImageRemoved" />
                <Rock:RockTextBox runat="server" ID="tbTopperTitle" Label="Topper Title" Help="The title of the topper text." />
                <Rock:Toggle runat="server" ID="cbInfoPanel" Label="Optional Info Panel" Help="The optional info panel for the topper." OnCssClass="btn-success" OffCssClass="btn-warning" OnCheckedChanged="cbInfoPanel_CheckedChanged" />
                <Rock:HtmlEditor runat="server" ID="htmlInfoPanelContent" Label="Info Panel Content" Help="The html content for the Info Panel." />
                <Rock:ColorPicker runat="server" ID="cpBackground" Label="Info Panel Background" Help="The background color of the info panel." />
            </Content>
        </Rock:ModalDialog>

        <section id="PageHeader" class="parallax" style="background-image: <%=getPageHeaderUrl()%>">
            <div class="container">
                <div class="row">
                    <div class="col-sm-12">
                        <header>
                            <h1 style="<%=getH1Margin()%>">
                                <asp:Literal runat="server" ID="ltTopperTitle" />
                            </h1>
                        </header>
                    </div>
                    <asp:Panel runat="server" ID="pnlInfoPanel">
                        <div class="next-info" runat="server" id="nextInfo">
                            <div id="NextHeader" class="img-wrapper">
                                <%--<img src="/_/img/logos/next-logo.png" class="img-responsive">--%>
                                <!-- <img src="/_/img/home/promos/next-nov/next-logo.png" class="img-responsive logo hidden-xs" style="width: 50%;"/> -->
                                <div class="mobile-intro">
                                    <p><a href="javascript:void(0)" onclick="$('#NextContent .description').toggle();">Learn more about <i>NEXT</i></a></p>
                                </div>
                            </div>
                            <div id="NextContent" class="clearfix">
                                <div class="description">
                                    <asp:Literal runat="server" ID="ltHtmlContent" />
                                </div>
                            </div>
                        </div>
                    </asp:Panel>
                </div>
            </div>
        </section>


    </ContentTemplate>
</asp:UpdatePanel>
