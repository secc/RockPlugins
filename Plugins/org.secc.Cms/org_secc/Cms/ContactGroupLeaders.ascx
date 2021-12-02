<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ContactGroupLeaders.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Cms.ContactGroupLeaders" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
   <ContentTemplate>

        <asp:Literal ID="lError" runat="server" />

        <asp:Panel ID="pnlContactGroupLeaders" runat="server" CssClass="emailform" Visible="false">
            
           <Rock:RockTextBox ID="tbFirstName" runat="server" Label="First Name" Required="true" />
           <Rock:RockTextBox ID="tbLastName" runat="server" Label="Last Name" Required="false" />
           <Rock:EmailBox ID="tbEmail" runat="server" Label="Email" Required="true" />
           <Rock:RockTextBox ID="tbMessage" runat="server" Label="Message" Textmode="MultiLine" Rows="5" Required="true" />

            <div id="divCaptchaWrap" runat="server">
             <Rock:Captcha ID="cpCaptcha" runat="server" Required="true" Label="Verification" Visible="true"  />
            </div>
            <asp:Literal ID="Literal1" runat="server" />

			<div id="divButtonWrap" runat="server">
				<asp:Button ID="btnSubmit" CssClass="btn btn-primary" runat="server" Text="Submit"  OnClick="btnSubmit_Click" />
			</div>		
            
        </asp:Panel>

        <asp:Panel ID="pnlViewMessage" runat="server" Visible="false">
            <asp:Literal ID="lMessageContent" runat="server" />
        </asp:Panel>
        
         <asp:Literal ID="lResponse" runat="server" Visible="false" />
 
    </ContentTemplate>
    <Triggers>
        <asp:PostBackTrigger ControlID="btnSubmit" />
    </Triggers>
</asp:UpdatePanel>
