<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ContactGroupLeaders.ascx.cs" Inherits="RockWeb.Plugins.org_secc.Cms.ContactGroupLeaders" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
   <ContentTemplate>

        <asp:Literal ID="lError" runat="server" />

        <asp:Panel ID="pnlContactGroupLeaders" runat="server" CssClass="emailform">
            
           <Rock:RockTextBox ID="tbFirstName" runat="server" Label="First Name" Required="true" />
           <Rock:RockTextBox ID="tbLastName" runat="server" Label="Last Name" Required="false" />
           <Rock:EmailBox ID="tbEmail" runat="server" Label="Email" Required="false" />
           <Rock:RockTextBox ID="tbMessage" runat="server" Label="Message" Textmode="MultiLine" Rows="5" Required="false" />

            <div id="divCaptchaWrap" runat="server">
             <Rock:Captcha ID="cpCaptcha" runat="server" Required="false" Label="Verification" Visible="true"  />
            </div>
            <asp:Literal ID="Literal1" runat="server" />

			<div id="divButtonWrap" runat="server">
				<asp:Button ID="btnSubmit" CssClass="btn btn-primary" runat="server" Text="Submit"  OnClick="btnSubmit_Click" />
			</div>		
            
        </asp:Panel>

      
       
        
         <asp:Literal ID="lResponse" runat="server" Visible="false" />
        <asp:Literal ID="lDebug" runat="server" />
 
    </ContentTemplate>
    <Triggers>
        <asp:PostBackTrigger ControlID="btnSubmit" />
    </Triggers>
</asp:UpdatePanel>
