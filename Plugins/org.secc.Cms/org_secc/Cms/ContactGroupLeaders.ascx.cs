// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Communication;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;
using Group = Rock.Model.Group;

namespace RockWeb.Plugins.org_secc.Cms
{
    /// <summary>
    /// Block  where home group leaders can be reached from their home group displayed on the home group page.  (Home Group Web Project).
    /// </summary>
    [DisplayName( "Contact Group Leaders" )]
    [Category( "CMS" )]
    [Description( "Options to contact home group leaders." )]
    [GroupField( "Default Leader Group", "A pre-configured group (all leaders will be emailed)", true, "", "", 0, "LeaderGroup" )]
    [GroupField( "Group Via Guid", "A group via group guid passed through the query string (all leaders will be emailed)", false, "", "", 1, "GroupViaGuid" )]
    [PersonField( "Default Recipient ", "A pre-configured person", true, "", "", 2, "DefaultRecipient" )]
    [PersonField( "Recipient Via Guid", "Recipient person via person guid passed through the query", false, "", "", 3, "Recipient Via Guid" )]
    [TextField( "Recipient Email(s)", "Email addresses (comma delimited) to send the contents to.", false, "", "", 0, "RecipientEmail" )]
    [TextField( "Subject", "The subject line for the email. <span class='tip tip-lava'></span>", true, "", "", 3 )]
    [TextField( "From Email", "The email address to use for the from. <span class='tip tip-lava'></span>", true, "", "", 4 )]
    [TextField( "From Name", "The name to use for the from address. <span class='tip tip-lava'></span>", true, "", "", 5 )]
    [CodeEditorField( "HTML Form", "The HTML for the form the user will complete. <span class='tip tip-lava'></span>", CodeEditorMode.Lava, CodeEditorTheme.Rock, 400, false,"", "", 6 )]
    [CodeEditorField( "Message Body", "The email message body. <span class='tip tip-lava'></span>", CodeEditorMode.Lava, CodeEditorTheme.Rock, 400, false, "", "", 7 )]
    [CodeEditorField( "Response Message", "The message the user will see when they submit the form if no response page if provided. Lava merge fields are available for you to use in your message.", CodeEditorMode.Lava, CodeEditorTheme.Rock, 200, false, @"<div class=""alert alert-info"">
    Thank you for your interest in this group!
</div>", "", 8 )]
    [TextField( "Submit Button Text", "The text to display for the submit button.", true, "Submit", "", 10 )]
    [TextField( "Submit Button Wrap CSS Class", "CSS class to add to the div wrapping the button.", false, "", "", 11, key: "SubmitButtonWrapCssClass" )]
    [TextField( "Submit Button CSS Class", "The CSS class add to the submit button.", false, "btn btn-primary", "", 12, key: "SubmitButtonCssClass" )]
    [BooleanField( "Enable Debug", "Shows the fields available to merge in lava.", false, "", 13 )]
    [BooleanField( "Save Communication History", "Should a record of this communication be saved to the recipient's profile", false, "", 14 )]
    [LavaCommandsField( "Enabled Lava Commands", "The Lava commands that should be enabled for this HTML block.", false, order: 15 )]
    
 


    public partial class ContactGroupLeaders : Rock.Web.UI.RockBlock
    {

        #region Base Control Methods
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );

            pnlContactGroupLeaders.Visible = this.ContextEntity<Rock.Model.Person>() == null;

            RockContext rockContext = new RockContext();

 
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack )
            {
                    if ( CurrentPerson != null )
                    {
                        tbFirstName.Text = CurrentPerson.NickName;
                        tbLastName.Text = CurrentPerson.LastName;
                        tbEmail.Text = CurrentPerson.Email;
                    
                }

                int? groupId = this.PageParameter( "GroupId" ).AsIntegerOrNull();
                GetGroupLeaders( groupId);
                if ( groupId.HasValue )
                {
                    string groupName = GetGroupName( groupId );
                }
                ShowForm();
                pnlContactGroupLeaders.Visible = true;
     
                if ( !string.IsNullOrWhiteSpace( GetAttributeValue( "SubmitButtonText" ) ) )
                {
                    btnSubmit.Text = GetAttributeValue( "SubmitButtonText" );
                }

                if ( !string.IsNullOrWhiteSpace( GetAttributeValue( "SubmitButtonWrapCssClass" ) ) )
                {
                    divButtonWrap.Attributes.Add( "class", GetAttributeValue( "SubmitButtonWrapCssClass" ) );
                }

                if ( !string.IsNullOrWhiteSpace( GetAttributeValue( "SubmitButtonCssClass" ) ) )
                {
                    btnSubmit.CssClass = GetAttributeValue( "SubmitButtonCssClass" );
                }

                if ( string.IsNullOrWhiteSpace( GetAttributeValue( "RecipientEmail" ) ) )
                {
                    lError.Text = "<div class='alert alert-warning'>A recipient has not been provided for this form.</div>";
                }

                if ( string.IsNullOrWhiteSpace( GetAttributeValue( "Subject" ) ) )
                {
                    lError.Text += "<div class='alert alert-warning'>A subject has not been provided for this form.</div>";
                }
            }

           
        }

        #endregion

        #region Events

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            ShowForm();
        }

        /// <summary>
        /// Handles the Click event of the btnSubmit control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnSubmit_Click( object sender, EventArgs e )
        {
            bool result = SendEmail();

            if ( result == true )
            {
                pnlContactGroupLeaders.Visible = false;
            }
            else
            {
                pnlContactGroupLeaders.Visible = true;
                cpCaptcha.Visible = cpCaptcha.IsAvailable;
            }
           
        }

        #endregion
        #region Methods

        /// <summary>
        /// Shows the form.
        /// </summary>
        private void ShowForm()
        {
            cpCaptcha.Visible = cpCaptcha.IsAvailable;
           
        }

        /// <summary>
        /// Sends the email.
        /// </summary>
        private bool SendEmail()
        {
            var rockContext = new RockContext();
            var serverVarList = Context.Request.ServerVariables;
            var personService = new PersonService( rockContext );
            var groupService = new GroupService( rockContext );
            Person person = null;
            Group group = null;
            int? groupId = null;
            bool isValid = cpCaptcha.IsResponseValid();

            // get person from url
            if ( this.PageParameter( "PersonGuid" ) != null && !string.IsNullOrWhiteSpace( this.PageParameter( "PersonGuid" ) ) )
            {
                Guid? personGuid = this.PageParameter( "PersonGuid" ).AsGuidOrNull();

                if ( personGuid.HasValue )
                {
                    person = personService.Get( personGuid.Value );
                    SetAttributeValue( "RecipientEmail", person.Email );
                }
            }

            // get group id from url
            if ( this.PageParameter( "GroupGuid" ) != null && !string.IsNullOrWhiteSpace( this.PageParameter( "GroupGuid" ) ) )
            { 
                Guid? groupGuid = PageParameter( "GroupGuid" ).AsGuidOrNull();
            
                if ( groupGuid.HasValue )
                {
                    group = groupService.Queryable().Where( g => g.Guid == groupGuid.Value ).FirstOrDefault();
                    groupId = group.Id;
              
                }
            }
            if ( this.PageParameter( "GroupId" ) != null && !string.IsNullOrWhiteSpace( this.PageParameter( "GroupId" ) ) )
            {
                
                    groupId = this.PageParameter( "GroupId" ).AsIntegerOrNull();
                
            }
            
            
                string groupName = GetGroupName( groupId );
            



            if ( isValid )
                {
                var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );
                mergeFields.Add( "NickName",tbFirstName.Text );
                mergeFields.Add( "LastName",tbLastName.Text );
                mergeFields.Add( "Email", tbEmail.Text );
                mergeFields.Add( "GroupName", groupName );
                mergeFields.Add( "Message",tbMessage.Text );
                mergeFields.Add( "FromEmail", tbEmail.Text );
                mergeFields.Add( "FromName", tbFirstName.Text + " " + tbLastName.Text );
               
                
                var message = new RockEmailMessage();
                message.EnabledLavaCommands = GetAttributeValue( "EnabledLavaCommands" );

                


                // send email
                foreach ( string recipient in GetAttributeValue( "RecipientEmail" ).Split( ',' ).ToList() )
                    {
                        message.AddRecipient( new RecipientData( recipient, mergeFields ) );
                    }

                    message.FromEmail = GetAttributeValue( "FromEmail" ).ResolveMergeFields( mergeFields );
                    message.FromName = GetAttributeValue( "FromName" ).ResolveMergeFields( mergeFields );
                    message.Subject = GetAttributeValue( "Subject" );
                    message.Message = GetAttributeValue( "MessageBody").ResolveMergeFields(mergeFields);
                    message.AppRoot = ResolveRockUrl( "~/" );
                    message.ThemeRoot = ResolveRockUrl( "~~/" );
                    message.CreateCommunicationRecord = GetAttributeValue( "SaveCommunicationHistory" ).AsBoolean();
                    message.Send();
                
                // set response
    
                // display response message
                lResponse.Visible = true;
                pnlContactGroupLeaders.Visible = false;
                lResponse.Text = GetAttributeValue( "ResponseMessage" ).ResolveMergeFields( mergeFields, GetAttributeValue( "EnabledLavaCommands" ) );
                return true;
                // show debug info
                if ( GetAttributeValue( "EnableDebug" ).AsBoolean() && IsUserAuthorized( Authorization.EDIT ) )
                {
                    lDebug.Visible = true;
 
                }
            }
            else
            {
                lResponse.Visible = true;
                pnlContactGroupLeaders.Visible = true;
                lResponse.Text = "<div class='alert alert-danger'>You appear to be a computer. Check the box above and then click Submit.</div>";
                return false;
             
            }
        }


        public void GetGroupLeaders( int? groupId )
        {
             var rockContext = new RockContext();
    
                var groupMemberList = new GroupMemberService( rockContext ).Queryable()
                    .Where( m =>
                            m.GroupId == groupId &&
                            m.GroupMemberStatus == GroupMemberStatus.Active &&
                            m.GroupRole.IsLeader == true &&
                            m.Person.Email != null &&
                            m.Person.Email != "" )
                        .Select( m => m.Person.Email )
                        .ToList();
            if ( groupMemberList.Count != 0 )
            {
                string leaderList = string.Join( ",", groupMemberList );
                SetAttributeValue( "RecipientEmail", leaderList );
            }
            else
            {
                Guid? defaultRecipient = this.GetAttributeValue( "DefaultRecipient" ).AsGuidOrNull();
                Person person = NewMethod( rockContext ).Queryable()
                    .Where( a => a.Guid.Equals( defaultRecipient.Value ) )
                    .Select( a => a.Person )
                    .FirstOrDefault();
                if ( person != null && !string.IsNullOrWhiteSpace( person.Email ) )
                {
                    SetAttributeValue( "RecipientEmail", person.Email );

                }

            }

        }

        public string GetGroupName( int? groupId )
        {
            if ( groupId != null )
            {
                var rockContext = new RockContext();
                var groupName = new GroupService( rockContext ).GetSelect( groupId.Value, a => a.Name );
                return groupName;
            }
            return " ";    
           
        }

        private static PersonAliasService NewMethod( RockContext rockContext )
        {
            return new PersonAliasService( rockContext );
        }

 
        #endregion
    }
}