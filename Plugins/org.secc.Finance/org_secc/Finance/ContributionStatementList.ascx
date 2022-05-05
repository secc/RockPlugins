<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ContributionStatementList.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Finance.ContributionStatementList" %>

<asp:UpdatePanel ID="upBinaryFile" runat="server">
    <ContentTemplate>
        <asp:label runat="server" ID="lTmp" />
        <div class="panel panel-block">
            <div class="panel-heading">
                <h1 class="panel-title"><i class="fa fa-files-o"></i> Contribution Statement List</h1>
                <div class="pull-right"><asp:LinkButton ID="lbExportPdf" runat="server" CssClass="btn btn-info btn-sm pull-right" OnClick="ExportPdfs_Click"><i class="fa fa-download"></i> Export Combined PDF</asp:LinkButton></div>
            </div>
            <div class="panel-body">

                <div class="grid grid-panel">
                    <Rock:GridFilter ID="fBinaryFile" runat="server">
                        <Rock:RockTextBox ID="tbName" runat="server" Label="File Name" />
                        <Rock:PersonPicker ID="ppPerson" runat="server" Label="Person" IncludeBusinesses="true" />
                        <Rock:RockCheckBoxList ID="cbDeliveryPreference" runat="server" Label="Statement Delivery Preference" />
                    </Rock:GridFilter>
                    
                            <!--Rock:SelectField></Rock:SelectField -->
                    <Rock:Grid ID="gBinaryFile" runat="server" AllowSorting="true" OnRowSelected="gBinaryFile_Edit">
                        <Columns>
                            <Rock:RockBoundField DataField="BinaryFile.FileName" HeaderText="File Name" SortExpression="BinaryFile.FileName" />
                            <Rock:RockBoundField DataField="PersonNames" HeaderText="Person Names" SortExpression="PersonNames" />
                            <Rock:RockBoundField DataField="GivingId" HeaderText="Giving Id" SortExpression="GivingId" />
                            <Rock:RockBoundField DataField="StatementDelivery" HeaderText="Statement Delivery" SortExpression="StatementDelivery" />
                            <Rock:DateTimeField DataField="BinaryFile.ModifiedDateTime" HeaderText="Last Modified" SortExpression="ModifiedDateTime" />
                            <Rock:DeleteField OnClick="gBinaryFile_Delete" />
                        </Columns>
                    </Rock:Grid>
                </div>

            </div>
        </div>

        <Rock:ModalAlert ID="mdGridWarning" runat="server" />
        <Rock:NotificationBox ID="nbBadFiles" runat="server" NotificationBoxType="Warning" Visible="false" />
        

    </ContentTemplate>
    <Triggers>
        <asp:PostBackTrigger ControlID="lbExportPdf" />
    </Triggers>
</asp:UpdatePanel>
