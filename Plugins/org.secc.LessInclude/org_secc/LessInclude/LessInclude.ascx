<%@ Control Language="C#" AutoEventWireup="true" CodeFile="LessInclude.ascx.cs" Inherits="RockWeb.Plugins.org_secc.LessInclude.LessInclude" %>

<script type="text/javascript">
    function clearDialog() {
        $('#rock-config-cancel-trigger').click();
    }
</script>

<asp:UpdatePanel ID="upnlContent" runat="server" UpdateMode="Conditional">
    <ContentTemplate>

 <%-- Edit Panel --%>
        <asp:Panel ID="pnlEditModal" runat="server" Visible="false">
            <Rock:ModalDialog ID="mdEdit" runat="server" OnSaveClick="mdEdit_SaveClick" Title="Dynamic LESS Configuration" OnCancelScript="clearDialog();">
                <Content>

                    <asp:UpdatePanel ID="upnlEdit" runat="server">
                        <ContentTemplate>
                            <div class="panel-group" id="accordion" role="tablist">


                                <div class="panel panel-default">
                                    <div class="panel-heading" role="tab" id="overrideHeading">
                                        <h4 class="panel-title">
                                            <a role="button" data-toggle="collapse" data-parent="#override"
                                                href="#override" aria-expanded="true" >
                                                Variable Orverrides <i class="fa fa-chevron-down"></i>
                                            </a>
                                        </h4>
                                    </div>
                                    <div id="override" class="panel-collapse collapse" role="tabpanel">
                                        <div class="panel-body">
                                            <Rock:CodeEditor ID="ceOverrides" EditorMode="Less" Label="LESS Variable Overrides"
                                                EditorHeight="400" runat="server"></Rock:CodeEditor>
                                        </div>
                                    </div>
                                </div>


                                <div class="panel panel-default">
                                    <div class="panel-heading" role="tab" id="variablesHeading">
                                        <h4 class="panel-title">
                                            <a role="button" data-toggle="collapse" data-parent="#variables"
                                                href="#variables" aria-expanded="true" >
                                                All Variables <i class="fa fa-chevron-down"></i>
                                            </a>
                                        </h4>
                                    </div>
                                    <div id="variables" class="panel-collapse collapse" role="tabpanel">
                                        <div class="panel-body">
                                            <Rock:CodeEditor ID="ceVariables" EditorMode="Less" Label="LESS Variables"
                                                EditorHeight="400" runat="server"></Rock:CodeEditor>
                                        </div>
                                    </div>
                                </div>


                                <div class="panel panel-default">
                                    <div class="panel-heading" role="tab" id="themeHeading">
                                        <h4 class="panel-title">
                                            <a role="button" data-toggle="collapse" data-parent="#theme"
                                                href="#theme" aria-expanded="true" >
                                                Theme <i class="fa fa-chevron-down"></i>
                                            </a>
                                        </h4>
                                    </div>
                                    <div id="theme" class="panel-collapse collapse" role="tabpanel">
                                        <div class="panel-body">
                                            <Rock:CodeEditor ID="ceTheme" EditorMode="Less" Label="Template LESS"
                                                EditorHeight="400" runat="server"></Rock:CodeEditor>
                                        </div>
                                    </div>
                                </div>


                                <div class="panel panel-default">
                                    <div class="panel-heading" role="tab" id="printHeading">
                                        <h4 class="panel-title">
                                            <a role="button" data-toggle="collapse" data-parent="#print"
                                                href="#print" aria-expanded="true" >
                                                Print <i class="fa fa-chevron-down"></i>
                                            </a>
                                        </h4>
                                    </div>
                                    <div id="print" class="panel-collapse collapse" role="tabpanel">
                                        <div class="panel-body">
                                            <Rock:CodeEditor ID="cePrint" EditorMode="Less" Label="Print LESS"
                                                EditorHeight="400" runat="server"></Rock:CodeEditor>
                                        </div>
                                    </div>
                                </div>
                            </div>
                            <div class="alert alert-warning">Note: A reload of the page may be required to refresh browser cached CSS.</div>
                        </ContentTemplate>
                    </asp:UpdatePanel>
                </Content>
            </Rock:ModalDialog>
<%-- End Edit Panel --%>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>