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
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;

using org.secc.GroupManager.Model;

using Rock;
using Rock.Attribute;
using Rock.Communication;
using Rock.Data;
using Rock.Model;
using Rock.Tasks;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;
using Group = Rock.Model.Group;

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
    [BooleanField( "Enable SMS", "Enable messages being sent by SMS when it is the user's preferred communication method.", false, order: 16 )]
    [DefinedValueField( "611bde1f-7405-4d16-8626-ccfedb0e62be", "SMS From Number", "The SMS Phone Number that the message should come from", false, false, "86604119-a222-4b35-9cd3-1a78db1b7b17", "", order: 17 )]
    [CodeEditorField( "SMS Message Body", "The SMS message body.", CodeEditorMode.Lava, CodeEditorTheme.Rock, 200, false, "", "", 18 )]

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
                if ( PageParameter( "c" ).AsGuidOrNull().HasValue )
                {
                    ShowCommunication();
                }
                else
                {
                    ShowForm();
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
            bool result = SendMessage();

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
            pnlContactGroupLeaders.Visible = true;

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
        private bool SendMessage()
        {
            var rockContext = new RockContext();
            var personAliasService = new PersonAliasService( rockContext );
            var groupService = new GroupService( rockContext );
            var publishGroupService = new PublishGroupService( rockContext );
            Group group = null;
            PublishGroup publishGroup = null;
            bool allowSMS = GetAttributeValue( "EnableSMS" ).AsBoolean();
            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );

            List<Person> emailRecipients = new List<Person>();
            List<Person> smsRecipients = new List<Person>();
            // get person from url
            if ( PageParameter( "PersonGuid" ).IsNotNullOrWhiteSpace() )
            {
                Guid? personGuid = this.PageParameter( "PersonGuid" ).AsGuidOrNull();
                if ( personGuid.HasValue )
                {
                    var person = personAliasService.Queryable()
                        .Where( pa => pa.Guid == personGuid.Value )
                        .Select( pa => pa.Person )
                        .FirstOrDefault();
                    var phoneNumber = person.PhoneNumbers.GetFirstSmsNumber();
                    if ( person != null && allowSMS && person.CommunicationPreference == CommunicationType.SMS && phoneNumber.IsNotNullOrWhiteSpace() )
                    {
                        smsRecipients.Add( person );
                    }
                    else
                    {
                        emailRecipients.Add( person );
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

                    var leaders = GetGroupLeaders( group );
                    foreach ( var leader in leaders )
                    {
                        var phonenumber = leader.PhoneNumbers.GetFirstSmsNumber();
                        if ( allowSMS && leader.CommunicationPreference == CommunicationType.SMS && phonenumber.IsNotNullOrWhiteSpace() )
                        {
                            smsRecipients.Add( leader );
                        }
                        else
                        {
                            emailRecipients.Add( leader );
                        }

                    }
                }
            }

            // get group members from url
            if ( PageParameter( "PublishGroupGuid" ).IsNotNullOrWhiteSpace() )
            {
                Guid? publishGroupGuid = PageParameter( "PublishGroupGuid" ).AsGuidOrNull();

                if ( publishGroupGuid.HasValue )
                {
                    publishGroup = publishGroupService.Get( publishGroupGuid.Value );
                    if ( publishGroup != null )
                    {
                        var person = new Person { Email = publishGroup.ContactEmail };
                        emailRecipients.Add( person );
                    }
                }
            }

            if ( !emailRecipients.Any() && !smsRecipients.Any() )
            {
                Guid defaultRecipient = this.GetAttributeValue( "DefaultRecipient" ).AsGuid();
                var defaultPerson = personAliasService.Queryable()
                        .Where( pa => pa.Guid == defaultRecipient )
                        .Select( pa => pa.Person )
                        .FirstOrDefault();
                if ( defaultPerson != null )
                {
                    var phonenumber = defaultPerson.PhoneNumbers.GetFirstSmsNumber();
                    if ( allowSMS && defaultPerson.CommunicationPreference == CommunicationType.SMS && phonenumber.IsNotNullOrWhiteSpace() )
                    {
                        smsRecipients.Add( defaultPerson );
                    }
                    else
                    {
                        emailRecipients.Add( defaultPerson );
                    }
                }
            }

            if ( !emailRecipients.Any() && !smsRecipients.Any() )
            {
                Guid defaultGroupGuid = GetAttributeValue( "LeaderGroup" ).AsGuid();
                var defaultGroup = groupService.Get( defaultGroupGuid );
                var leaders = GetGroupLeaders( defaultGroup );
                foreach ( var leader in leaders )
                {
                    var phonenumber = leader.PhoneNumbers.GetFirstSmsNumber();
                    if ( allowSMS && leader.CommunicationPreference == CommunicationType.SMS && phonenumber.IsNotNullOrWhiteSpace() )
                    {
                        smsRecipients.Add( leader );
                    }
                    else
                    {
                        emailRecipients.Add( leader );
                    }
                }
            }

            if ( !cpCaptcha.IsAvailable || cpCaptcha.IsResponseValid() )
            {
                mergeFields.Add( "NickName", tbFirstName.Text );
                mergeFields.Add( "LastName", tbLastName.Text );
                mergeFields.Add( "Email", tbEmail.Text );
                mergeFields.Add( "PublishGroup", publishGroup );
                mergeFields.Add( "Group", group );
                mergeFields.Add( "Message", tbMessage.Text );
                mergeFields.Add( "FromEmail", tbEmail.Text );
                mergeFields.Add( "FromName", tbFirstName.Text + " " + tbLastName.Text );

                if ( emailRecipients.Any() )
                {
                    SendEmail( emailRecipients, mergeFields );
                }

                if ( smsRecipients.Any() )
                {
                    SendTextMessage( smsRecipients, mergeFields );
                }

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

        private void SendEmail( List<Person> emailrecipients, Dictionary<string, object> mergeFields )
        {
            var message = new RockEmailMessage();
            message.EnabledLavaCommands = GetAttributeValue( "EnabledLavaCommands" );

            foreach ( var person in emailrecipients )
            {
                if ( person.Id > 0 )
                {
                    message.AddRecipient( new RockEmailMessageRecipient( person, mergeFields ) );
                }
                else
                {
                    message.AddRecipient( RockEmailMessageRecipient.CreateAnonymous( person.Email, mergeFields ) );
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

        }

        private void SendTextMessage( List<Person> smsRecipients, Dictionary<string, object> mergeFields )
        {
            var rockContext = new RockContext();
            var communicationService = new CommunicationService( rockContext );
            var communicationRecipientService = new CommunicationRecipientService( rockContext );

            var communication = new Rock.Model.Communication();
            communication.Status = CommunicationStatus.Approved;
            communication.SenderPersonAliasId = CurrentPerson.PrimaryAliasId;
            communicationService.Add( communication );
            communication.EnabledLavaCommands = GetAttributeValue( "EnabledLavaCommands" );
            communication.IsBulkCommunication = false;
            communication.CommunicationType = CommunicationType.SMS;
            communication.ListGroup = null;
            communication.ListGroupId = null;
            communication.ExcludeDuplicateRecipientAddress = true;
            communication.CommunicationTemplateId = null;
            communication.FromName = mergeFields["FromName"].ToString().TrimForMaxLength( communication, "FromName" );
            communication.FromEmail = mergeFields["FromEmail"].ToString().TrimForMaxLength( communication, "FromEmail" );
            communication.Subject = GetAttributeValue( "Subject" );
            communication.Message = GetAttributeValue( "MessageBody" );

            communication.SMSFromDefinedValueId = DefinedValueCache.GetId( GetAttributeValue( "SMSFromNumber" ).AsGuid() );
            communication.SMSMessage = GetAttributeValue( "SMSMessageBody" );
            communication.FutureSendDateTime = null;

            communicationService.Add( communication );

            rockContext.SaveChanges();

            foreach ( var smsPerson in smsRecipients )
            {
                communication.Recipients.Add( new CommunicationRecipient()
                {
                    PersonAliasId = smsPerson.PrimaryAliasId,
                    MediumEntityTypeId = EntityTypeCache.Get( Rock.SystemGuid.EntityType.COMMUNICATION_MEDIUM_SMS.AsGuid() ).Id,
                    AdditionalMergeValues = mergeFields
                } );

            }
            rockContext.SaveChanges();

            var transaction = new ProcessSendCommunication.Message();
            transaction.CommunicationId = communication.Id;
            transaction.Send();

        }

        private void ShowCommunication()
        {

            pnlViewMessage.Visible = true;

            var communicationGuid = PageParameter( "c" ).AsGuid();

            var rockContext = new RockContext();
            var communicationService = new CommunicationService( rockContext );
            var personService = new PersonService( rockContext );
            Person person = null;

            if ( PageParameter( "p" ).IsNotNullOrWhiteSpace() )
            {
                person = personService.GetByImpersonationToken( PageParameter( "p" ), true, null );
            }

            if ( person == null )
            {
                person = CurrentPerson;
            }

            var communication = communicationService.Get( communicationGuid );

            if ( communication == null || !communication.Recipients.Where( r => r.PersonAlias.PersonId == person.Id ).Any() )
            {
                var message = "<div class='alert alert-info'>This message is currently unavailable</div>";
                lMessageContent.Text = message;

                return;
            }

            var recipient = communication.Recipients.Where( r => r.PersonAlias.PersonId == person.Id ).FirstOrDefault();
            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( null );
            mergeFields.Add( "Communication", "communication" );
            mergeFields.Add( "Person", person );

            foreach ( var mergeField in recipient.AdditionalMergeValues )
            {
                if ( !mergeFields.ContainsKey( mergeField.Key ) )
                {
                    mergeFields.Add( mergeField.Key, mergeField.Value );
                }
            }

            string body = communication.Message.ResolveMergeFields( mergeFields, communication.EnabledLavaCommands );
            body = System.Text.RegularExpressions.Regex.Replace( body, @"\[\[\s*UnsubscribeOption\s*\]\]", string.Empty );
            lMessageContent.Text = body;


            // write an 'opened' interaction
            var interactionService = new InteractionService( rockContext );

            InteractionComponent interactionComponent = new InteractionComponentService( rockContext )
                                .GetComponentByEntityId( Rock.SystemGuid.InteractionChannel.COMMUNICATION.AsGuid(),
                                    communication.Id, communication.Subject );
            rockContext.SaveChanges();

            var ipAddress = Rock.Web.UI.RockPage.GetClientIpAddress();

            var userAgent = Request.UserAgent ?? "";

            UAParser.ClientInfo client = UAParser.Parser.GetDefault().Parse( userAgent );
            var clientOs = client.OS.ToString();
            var clientBrowser = client.UA.ToString();
            var clientType = InteractionDeviceType.GetClientType( userAgent );

            interactionService.AddInteraction( interactionComponent.Id, recipient.Id, "Opened", "", recipient.PersonAliasId, RockDateTime.Now, clientBrowser, clientOs, clientType, userAgent, ipAddress, null );

            rockContext.SaveChanges();

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