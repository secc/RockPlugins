<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ChangeRequestDetail.ascx.cs" Inherits="RockWeb.Plugins.org_secc.ChangeManager.ChangeRequestDetail" %>
<asp:UpdatePanel runat="server" ID="upContent">
    <ContentTemplate>
        <div class="panel panel-default">
            <div class="panel-heading">
                <asp:Literal runat="server" ID="lName" />
            </div>
            <div class="panel-body">
                <asp:HiddenField runat="server" ID="hfChangeId" />
                <div class="col-md-12">
                    <Rock:Grid runat="server" ID="gRecords" DataKeyNames="Id">
                        <Columns>
                            <Rock:EnumField DataField="Action" HeaderText="Change Type" />
                            <Rock:RockBoundField DataField="Property" HeaderText="Property" HtmlEncode="false" />
                            <Rock:RockBoundField DataField="OldValue" HeaderText="Old Value" HtmlEncode="false" />
                            <Rock:RockBoundField DataField="NewValue" HeaderText="New Value" HtmlEncode="false" />
                            <Rock:BoolField DataField="WasApplied" HeaderText="Was Applied" />
                            <Rock:ToggleField DataField="IsRejected" HeaderText="Is Rejected" OffText="No" OnText="Yes" OffCssClass="btn-success" OnCssClass="btn-danger" OnCheckedChanged="gRecords_CheckedChanged" />
                        </Columns>
                    </Rock:Grid>
                    <br />
                    <Rock:RockTextBox runat="server" Label="Comments" ID="tbComment" Height="300" TextMode="MultiLine" />
                    <br />
                    <Rock:BootstrapButton runat="server" ID="btnComplete" CssClass="btn btn-primary pull-right" Text="Update Items and Complete Request" OnClick="btnComplete_Click" />
                </div>
            </div>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>
