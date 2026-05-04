<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CampPlacementImport.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Event.CampPlacementImport" %>

<asp:UpdatePanel ID="upMain" runat="server">
    <ContentTemplate>

        <asp:Panel ID="pnlView" runat="server" CssClass="panel panel-block">

            <div class="panel-heading">
                <h1 class="panel-title"><i class="fa fa-campground"></i> Camp Placement Import</h1>
            </div>

            <div class="panel-body">

                <Rock:NotificationBox ID="nbWarning" runat="server" NotificationBoxType="Warning" Visible="false" />
                <Rock:NotificationBox ID="nbInfo" runat="server" NotificationBoxType="Info" Visible="false" />
                <Rock:NotificationBox ID="nbSuccess" runat="server" NotificationBoxType="Success" Visible="false" />

                <%-- Step 1: Select Registration Instance --%>
                <asp:Panel ID="pnlSelectInstance" runat="server">
                    <h4>Step 1: Select Registration Instance</h4>
                    <p>Choose the camp registration instance that contains the campers you want to place into groups.</p>
                    <Rock:RegistrationInstancePicker ID="ripInstance" runat="server" Label="Registration Instance" Required="true" />
                    <div class="actions margin-t-md">
                        <asp:LinkButton ID="btnInstanceNext" runat="server" CssClass="btn btn-primary" Text="Next" OnClick="btnInstanceNext_Click" />
                    </div>
                </asp:Panel>

                <%-- Step 2: Upload CSV --%>
                <asp:Panel ID="pnlUpload" runat="server" Visible="false">
                    <h4>Step 2: Upload CSV File</h4>
                    <p>Upload a CSV file containing camper names and their placement assignments. The first row should contain column headers.</p>
                    <Rock:FileUploader ID="fuCsvFile" runat="server" IsBinaryFile="true" Label="CSV File" Required="true" OnFileUploaded="fuCsvFile_FileUploaded" />
                    <div class="actions margin-t-md">
                        <asp:LinkButton ID="btnUploadBack" runat="server" CssClass="btn btn-default" Text="Back" OnClick="btnUploadBack_Click" CausesValidation="false" />
                    </div>
                </asp:Panel>

                <%-- Step 3: Map Columns --%>
                <asp:Panel ID="pnlMapping" runat="server" Visible="false">
                    <h4>Step 3: Map Columns</h4>

                    <div class="well">
                        <h5>Camper Identification</h5>
                        <p>Select the CSV columns that identify each camper. These will be matched against registrants in the selected registration instance.</p>
                        <div class="row">
                            <div class="col-md-6">
                                <Rock:RockDropDownList ID="ddlFirstNameCol" runat="server" Label="First Name Column" Required="true" />
                            </div>
                            <div class="col-md-6">
                                <Rock:RockDropDownList ID="ddlLastNameCol" runat="server" Label="Last Name Column" Required="true" />
                            </div>
                        </div>
                    </div>

                    <div class="well">
                        <h5>Placement Mappings</h5>
                        <p>For each placement type, select the CSV column that contains the group name and the parent group whose children are the valid placement targets.</p>
                        
                        <div class="row margin-b-md">
                            <div class="col-md-12">
                                <Rock:GroupPicker ID="gpBasePlacementGroup" runat="server" 
                                    Label="Base Parent Group (Optional)" 
                                    Help="Select a common parent group (e.g., 'Camp Manager'). When set, each mapping below will show only direct children of this group instead of the full group tree."
                                    OnSelectItem="gpBasePlacementGroup_SelectItem" />
                            </div>
                        </div>
                        
                        <asp:Repeater ID="rptMappings" runat="server" OnItemDataBound="rptMappings_ItemDataBound">
                            <ItemTemplate>
                                <div class="row margin-b-sm placement-mapping-row">
                                    <div class="col-md-5">
                                        <Rock:RockDropDownList ID="ddlCsvColumn" runat="server" Label="CSV Column" Required="false" />
                                    </div>
                                    <div class="col-md-5">
                                        <Rock:GroupPicker ID="gpParentGroup" runat="server" Label="Parent Group" Required="false" />
                                        <Rock:RockDropDownList ID="ddlParentGroupChild" runat="server" Label="Parent Group" Required="false" Visible="false" />
                                    </div>
                                    <div class="col-md-2">
                                        <asp:LinkButton ID="btnRemoveMapping" runat="server" CssClass="btn btn-danger btn-sm margin-t-lg" OnClick="btnRemoveMapping_Click" CausesValidation="false">
                                            <i class="fa fa-times"></i>
                                        </asp:LinkButton>
                                    </div>
                                </div>
                            </ItemTemplate>
                        </asp:Repeater>
                        <asp:LinkButton ID="btnAddMapping" runat="server" CssClass="btn btn-default btn-xs margin-t-sm" OnClick="btnAddMapping_Click" CausesValidation="false">
                            <i class="fa fa-plus"></i> Add Mapping
                        </asp:LinkButton>
                    </div>

                    <div class="actions margin-t-md">
                        <asp:LinkButton ID="btnMappingBack" runat="server" CssClass="btn btn-default" Text="Back" OnClick="btnMappingBack_Click" CausesValidation="false" />
                        <asp:LinkButton ID="btnPreview" runat="server" CssClass="btn btn-primary" Text="Preview" OnClick="btnPreview_Click" />
                    </div>
                </asp:Panel>

                <%-- Step 4: Preview & Validate --%>
                <asp:Panel ID="pnlPreview" runat="server" Visible="false">
                    <h4>Step 4: Preview Placements</h4>
                    <p>Review the proposed placements below. Rows with errors or duplicate names will be skipped during processing.</p>

                    <Rock:NotificationBox ID="nbDuplicates" runat="server" NotificationBoxType="Warning" Visible="false" />

                    <Rock:Grid ID="gPreview" runat="server" AllowSorting="false" DataKeyNames="RowIndex" ShowActionRow="false" DisplayType="Light">
                        <Columns>
                            <Rock:RockBoundField DataField="CsvName" HeaderText="Camper (CSV)" />
                            <Rock:RockBoundField DataField="MatchedPerson" HeaderText="Matched Person" />
                            <Rock:RockBoundField DataField="PlacementSummary" HeaderText="Placements" />
                            <Rock:RockBoundField DataField="Status" HeaderText="Status" />
                        </Columns>
                    </Rock:Grid>

                    <div class="actions margin-t-md">
                        <asp:LinkButton ID="btnPreviewBack" runat="server" CssClass="btn btn-default" Text="Back" OnClick="btnPreviewBack_Click" CausesValidation="false" />
                        <asp:LinkButton ID="btnProcess" runat="server" CssClass="btn btn-primary" Text="Process Placements" OnClick="btnProcess_Click" OnClientClick="return Rock.dialogs.confirmPreventOnCancel(event, 'Are you sure you want to process these placements?');" />
                    </div>
                </asp:Panel>

                <%-- Step 5: Results --%>
                <asp:Panel ID="pnlResults" runat="server" Visible="false">
                    <h4>Results</h4>

                    <div class="row">
                        <div class="col-md-4">
                            <div class="well text-center">
                                <h2 class="text-success"><asp:Literal ID="lSuccessCount" runat="server" /></h2>
                                <p>Placements Made</p>
                            </div>
                        </div>
                        <div class="col-md-4">
                            <div class="well text-center">
                                <h2 class="text-info"><asp:Literal ID="lSkippedCount" runat="server" /></h2>
                                <p>Already Placed (Skipped)</p>
                            </div>
                        </div>
                        <div class="col-md-4">
                            <div class="well text-center">
                                <h2 class="text-danger"><asp:Literal ID="lErrorCount" runat="server" /></h2>
                                <p>Errors</p>
                            </div>
                        </div>
                    </div>

                    <h5 class="margin-t-md">Detailed Results</h5>
                    <p>
                        <span class="label label-success">Added</span> = placed into group &nbsp;
                        <span class="label label-info">Skipped</span> = already a member &nbsp;
                        <span class="label label-danger">Error</span> = could not place &nbsp;
                        <span class="label label-default">Empty</span> = no value in CSV
                    </p>

                    <asp:Literal ID="lResultsTable" runat="server" />

                    <div class="actions margin-t-md">
                        <asp:LinkButton ID="btnStartOver" runat="server" CssClass="btn btn-default" Text="Start Over" OnClick="btnStartOver_Click" CausesValidation="false" />
                    </div>
                </asp:Panel>

                <%-- Step 6: Processing --%>
                <asp:Panel ID="pnlProcessing" runat="server" Visible="false">
                    <h4>Processing…</h4>
                    <p>Your import is running in the background. This page updates automatically every 2 seconds.</p>

                    <Rock:NotificationBox ID="nbProcessing" runat="server" NotificationBoxType="Info" Visible="true" Text="Starting…" />

                    <div class="progress">
                        <asp:Literal ID="lProgressBar" runat="server" />
                    </div>
                </asp:Panel>

                <%-- Inside the UpdatePanel, add a timer and an async trigger for it: --%>
                <asp:Timer ID="tmrRunStatus" runat="server" Interval="2000" Enabled="false" OnTick="tmrRunStatus_Tick" />

            </div>

        </asp:Panel>

    </ContentTemplate>
    <Triggers>
        <asp:AsyncPostBackTrigger ControlID="tmrRunStatus" EventName="Tick" />
        <asp:PostBackTrigger ControlID="btnProcess" />
    </Triggers>
</asp:UpdatePanel>
