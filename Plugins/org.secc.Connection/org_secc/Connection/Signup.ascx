<%@ Control Language="C#" AutoEventWireup="true" CodeFile="Signup.ascx.cs" Inherits="org.secc.Connection.Signup" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        
        <asp:Literal ID="lBody" runat="server" ></asp:Literal> 

        
        <Rock:ModalDialog ID="mdEdit" runat="server" OnSaveClick="mdEdit_SaveClick" Title="Edit Signup Settings">
            <Content>
                <asp:UpdatePanel ID="upEditControls" runat="server" UpdateMode="Conditional">
                    <ContentTemplate>
                        <div class="container-fluid">
                            <div style="margin-bottom: 10px">
                                <ul class="nav nav-pills">
                                    <li class="active" id="liSettings" runat="server">
                                        <asp:LinkButton ID="lbSettings" runat="server" Text="Settings" OnClick="lbSettings_Click"></asp:LinkButton>
                                    </li>
                                    <li id="liCounts" runat="server">
                                        <asp:LinkButton ID="lbCounts" runat="server" Text="Totals" OnClick="lbCounts_Click"></asp:LinkButton>
                                    </li>
                                    <li id="liLava" runat="server">
                                        <asp:LinkButton ID="lbLava" runat="server" Text="Lava" OnClick="lbLava_Click"></asp:LinkButton>
                                    </li>
                                </ul>
                            </div>
                            
                            <asp:Panel id="pnlSettings" runat="server">
                                <div class="panel panel-block" >
                                    <div class="panel-heading">
                                        <h1 class="panel-title">Sign-up Settings</h1>
                                    </div>
                                    <div class="panel-body">
                                        <div class="row">
                                            <div class="col-md-6">
                                                <Rock:RockDropDownList ID="rddlType" Label="Select the type of Volunteer Signup" runat="server" OnSelectedIndexChanged="RddlType_SelectedIndexChanged" AutoPostBack="true">
                                                    <asp:ListItem Value="">Select One . . .</asp:ListItem>
                                                    <asp:ListItem Value="Connection">Connection</asp:ListItem>
                                                    <asp:ListItem Value="Group">Group</asp:ListItem>
                                                </Rock:RockDropDownList>
                                            </div>
                                            <div class="col-md-6">
                                                <Rock:RockDropDownList ID="rddlConnection" Label="Select the Connection Opportunity" runat="server" Visible="false" OnSelectedIndexChanged="ConnectionRddl_SelectedIndexChanged" AutoPostBack="true" />
                                                <Rock:GroupPicker ID="gpGroup" Label="Select the Group" runat="server" Visible="false" OnSelectItem="GPicker_SelectItem" />
                                            </div>
                                        </div>
                                        <asp:PlaceHolder ID="phEditControls" runat="server" EnableViewState="false" />
                                    </div>
                                </div>
                                <asp:HiddenField ID="hdnSettings" runat="server" />
                                <asp:Panel class="panel panel-block" id="pnlPartitions" runat="server" visible="false" EnableViewState="false">
                                    <div class="panel-heading">
                                        <h1 class="panel-title">Partitions</h1>
                                        <div class="pull-right">
                                            <asp:LinkButton ID="btnAddPartition" runat="server" CssClass="hidden" OnClick="btnAddPartition_Click" />
                                            <asp:HiddenField ID="hdnPartitionType" runat="server" />
                                            <Rock:ButtonDropDownList ID="bddlAddPartition" OnSelectionChanged="BddlAddParition_SelectionChanged" runat="server">
                                                <asp:ListItem Value="">New Partition</asp:ListItem>
                                                <asp:ListItem Value="Campus">Campus</asp:ListItem>
                                                <asp:ListItem Value="DefinedType">Defined Type</asp:ListItem>
                                                <asp:ListItem Value="Schedule">Schedule</asp:ListItem>
                                                <asp:ListItem Value="Role">Group Type Role</asp:ListItem>
                                            </Rock:ButtonDropDownList>
                                        </div>
                                    </div>
                                    <div class="panel-body">
                                        <asp:Repeater ID="rptPartions" runat="server" OnItemDataBound="rptPartions_ItemDataBound">
                                            <ItemTemplate>
                                                <div class="row">
                                                    <div class="col-md-2"><strong><%# Eval("PartitionType") %></strong></div>
                                                    <div class="col-md-6"><asp:PlaceHolder ID="phPartitionControl" runat="server"></asp:PlaceHolder></div>
                                                    <div class="col-md-3"><Rock:RockDropDownList ID="ddlAttribute" runat="server" Placeholder="Attribute Mapping" /></div>
                                                    <div class="col-md-1"><asp:LinkButton ID="bbPartitionDelete" runat="server" OnClick="bbPartitionDelete_Click" ToolTip="Remove" CssClass="btn btn-danger" OnClientClick="Rock.dialogs.confirmPreventOnCancel(event, 'Making changes to partition settings will clear all existing counts!  Are you sure you want to proceed?');"><i class="fa fa-remove"></i></asp:LinkButton></div>
                                                </div>
                                            </ItemTemplate>
                                            <SeparatorTemplate>
                                                <hr />
                                            </SeparatorTemplate>
                                        </asp:Repeater>
                                        <asp:PlaceHolder ID="phPartitionControls" runat="server" EnableViewState="false" />
                                    </div>
                                </asp:Panel>
                            </asp:Panel>
                            <asp:Panel class="panel panel-block" id="pnlCounts" runat="server" visible="false">
                                <div class="panel-heading">
                                    <h1 class="panel-title">Volunteers Needed</h1>
                                </div>
                                <div class="panel-body">
                                    <Rock:Grid ID="gCounts" runat="server" AllowPaging="false" DataKeyNames="RowId" AllowSorting="false" ShowActionRow="false" OnRowDataBound="gCounts_RowDataBound">
                                        <Columns>
                                            <asp:TemplateField HeaderText="Total" ItemStyle-Width="75px"></asp:TemplateField>
                                        </Columns>
                                    </Rock:Grid>
                                </div>
                            </asp:Panel>
                            <asp:Panel class="panel panel-block" id="pnlLava" runat="server" visible="false">
                                <div class="panel-heading">
                                    <h1 class="panel-title">Display Lava</h1>
                                </div>
                                <div class="panel-body">
                                    <Rock:CodeEditor ID="ceLava" runat="server" EditorMode="Lava" Rows="100" ></Rock:CodeEditor>
                                </div>
                            </asp:Panel>
                        </div>
                    </ContentTemplate>
                </asp:UpdatePanel>
            </Content>
        </Rock:ModalDialog>

    </ContentTemplate>
</asp:UpdatePanel>
