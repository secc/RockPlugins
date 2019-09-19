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
using org.secc.GroupManager.Model;

namespace RockWeb.Plugins.org_secc.Cms
{
    /// <summary>
    /// Block  where home group leaders can be reached from their home group displayed on the home group page.  (Home Group Web Project).
    /// </summary>
    [DisplayName( "Contact Group Leaders" )]
    [Category( "SECC > CMS" )]
    [Description( "Options to contact home group leaders." )]
    [GroupField( "Default Contact Group", "A pre-configured group (all leaders will be emailed)", false, "", "", 0, "LeaderGroup" )]
    [PersonField( "Default Recipient ", "A pre-configured person", false, "", "", 2, "DefaultRecipient" )]
    [TextField( "Subject", "The subject line for the email. <span class='tip tip-lava'></span>", true, "New Message For Your Group", "", 3 )]
    [CodeEditorField( "Message Body", "The email message body. <span class='tip tip-lava'></span>", CodeEditorMode.Lava, CodeEditorTheme.Rock, 400, false, "", "", 7 )]
    [CodeEditorField( "Response Message", "The message the user will see when they submit the form if no response page if provided. Lava merge fields are available for you to use in your message.", CodeEditorMode.Lava, CodeEditorTheme.Rock, 200, false, @"<div class=""alert alert-info"">
    Thank you for your interest in this group!
</div>", "", 8 )]
    [TextField( "Submit Button Text", "The text to display for the submit button.", true, "Submit", "", 10 )]
    [TextField( "Submit Button Wrap CSS Class", "CSS class to add to the div wrapping the button.", false, "", "", 11, key: "SubmitButtonWrapCssClass" )]
    [TextField( "Submit Button CSS Class", "The CSS class add to the submit button.", false, "btn btn-primary", "", 12, key: "SubmitButtonCssClass" )]
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
                ShowForm();
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
            }

        }

        #endregion
        #region Methods

        /// <summary>
        /// Shows the form.
        /// </summary>
        private void ShowForm()
        {
            //Preload form
            if ( CurrentPerson != null )
            {
                tbFirstName.Text = CurrentPerson.NickName;
                tbLastName.Text = CurrentPerson.LastName;
                tbEmail.Text = CurrentPerson.Email;
            }

            //Style Submit Button
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

            //Show captcha is available - you need to add two keys to global attributes to enable
            cpCaptcha.Visible = cpCaptcha.IsAvailable;

        }

        /// <summary>
        /// Sends the email.
        /// </summary>
        private bool SendEmail()
        {
            var rockContext = new RockContext();
            var personAliasService = new PersonAliasService( rockContext );
            var groupService = new GroupService( rockContext );
            var publishGroupService = new PublishGroupService( rockContext );
            Group group = null;
            PublishGroup publishGroup = null;

            List<Person> recipients = new List<Person>();
            var contactEmail = "";

            // get person from url
            if ( PageParameter( "PersonGuid" ).IsNotNullOrWhiteSpace() )
            {
                Guid? personGuid = this.PageParameter( "PersonGuid" ).AsGuidOrNull();
                if ( personGuid.HasValue )
                {
                    var person = personAliasService.Get( personGuid.Value );
                    if ( person != null )
                    {
                        recipients.Add( person.Person );
                    }
                }
            }

            // get group members from url
            if ( PageParameter( "GroupGuid" ).IsNotNullOrWhiteSpace() )
            {
                Guid? groupGuid = PageParameter( "GroupGuid" ).AsGuidOrNull();

                if ( groupGuid.HasValue )
                {
                    group = groupService.Get( groupGuid.Value );
                    recipients.AddRange( GetGroupLeaders( group ) );

                }
            }

            // get group members from url
            if ( PageParameter( "PublishGroupGuid" ).IsNotNullOrWhiteSpace() )
            {
                Guid? publishGroupGuid = PageParameter( "PublishGroupGuid" ).AsGuidOrNull();

                if ( publishGroupGuid.HasValue )
                {
                    publishGroup = publishGroupService.Get( publishGroupGuid.Value );
                    contactEmail = publishGroup.ContactEmail;
                }
            }

            if ( !recipients.Any() )
            {
                Guid defaultRecipient = this.GetAttributeValue( "DefaultRecipient" ).AsGuid();
                var defaultPerson = personAliasService.Get( defaultRecipient );
                if ( defaultPerson != null )
                {
                    recipients.Add( defaultPerson.Person );
                }
            }

            if ( !recipients.Any() )
            {
                Guid defaultGroupGuid = GetAttributeValue( "LeaderGroup" ).AsGuid();
                var defaultGroup = groupService.Get( defaultGroupGuid );
                recipients.AddRange( GetGroupLeaders( defaultGroup ) );
            }

            if ( !cpCaptcha.IsAvailable || cpCaptcha.IsResponseValid() )
            {
                var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );
                mergeFields.Add( "NickName", tbFirstName.Text );
                mergeFields.Add( "LastName", tbLastName.Text );
                mergeFields.Add( "Email", tbEmail.Text );
                mergeFields.Add( "Group", group );
                mergeFields.Add( "Message", tbMessage.Text );
                mergeFields.Add( "FromEmail", tbEmail.Text );
                mergeFields.Add( "FromName", tbFirstName.Text + " " + tbLastName.Text );

                var message = new RockEmailMessage();
                message.EnabledLavaCommands = GetAttributeValue( "EnabledLavaCommands" );

                // send email
                if ( contactEmail != null)
                {
                    message.AddRecipient( new RecipientData( contactEmail, mergeFields ) );
                } else {
                    foreach ( var recipient in recipients )
                    {
                        message.AddRecipient( new RecipientData( new CommunicationRecipient { PersonAlias = recipient.PrimaryAlias }, mergeFields ) );
                    }
                }

                message.FromEmail = tbEmail.Text;
                message.FromName = tbFirstName.Text + " " + tbLastName.Text;
                message.Subject = GetAttributeValue( "Subject" );
                message.Message = GetAttributeValue( "MessageBody" );
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
            }
            else
            {
                lResponse.Visible = true;
                pnlContactGroupLeaders.Visible = true;
                lResponse.Text = "<div class='alert alert-danger'>You appear to be a computer. Check the box above and then click Submit.</div>";
                return false;
            }
        }


        public List<Person> GetGroupLeaders( Group group )
        {
            if ( group != null )
            {

                return group.Members
                     .Where( gm => gm.GroupMemberStatus == GroupMemberStatus.Active && gm.GroupRole.IsLeader == true )
                     .Select( gm => gm.Person )
                     .ToList();
            }
            return new List<Person>();
        }
        #endregion
    }
}