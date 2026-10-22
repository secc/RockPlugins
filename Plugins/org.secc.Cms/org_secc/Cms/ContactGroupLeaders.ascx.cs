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
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using org.secc.GroupManager.Model;

using Rock;
using Rock.Attribute;
using Rock.Communication;
using Rock.Data;
using Rock.Logging;
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
    [EmailField( "From Email", "The address the notification is sent from. The visitor's name is used as the display name and the visitor's address is set as Reply-To so leaders can reply directly. Leave blank to use the Organization Email global attribute.", false, "noreply@secc.org", "", 4, "FromEmail" )]
    [CodeEditorField( "Message Body", "The email message body. <span class='tip tip-lava'></span>", CodeEditorMode.Lava, CodeEditorTheme.Rock, 400, false, "", "", 7 )]
    [CodeEditorField( "Response Message", "The message the user will see when they submit the form if no response page if provided. Lava merge fields are available for you to use in your message.", CodeEditorMode.Lava, CodeEditorTheme.Rock, 200, false, @"<div class=""alert alert-info"">
    Thank you for your interest in this group!
</div>", "", 8 )]
    [CodeEditorField( "Error Message", "The message the user will see when the message could not be delivered (no recipient could be resolved, or the send failed). Lava merge fields are available.", CodeEditorMode.Lava, CodeEditorTheme.Rock, 200, false, @"<div class=""alert alert-danger"">
    We were unable to deliver your message. Please try again later or contact the church office.
</div>", "", 9, "ErrorMessage" )]
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
        /// <summary>
        /// Route/query parameters that may carry a Group or PublishGroup guid. Both entity types
        /// are tried for each, because the public routes have historically named the parameter
        /// inconsistently (a PublishGroup guid arriving as {GroupGuid} sent nothing for 20 months).
        /// </summary>
        private static readonly string[] GuidParameterKeys = { "GroupGuid", "PublishGroupGuid" };

        /// <summary>
        /// Route/query parameter that may carry a PublishGroup slug. Slugs survive edits; guids do not.
        /// </summary>
        private const string SlugParameterKey = "PublishGroupSlug";

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
        /// Resolves the Group and/or PublishGroup the contact form is for.
        /// </summary>
        /// <returns>true when at least one target parameter was supplied in the request (whether or not it resolved).</returns>
        private bool ResolveTargets( GroupService groupService, PublishGroupService publishGroupService, out Group group, out PublishGroup publishGroup )
        {
            group = null;
            publishGroup = null;
            bool targetRequested = false;

            foreach ( var key in GuidParameterKeys )
            {
                var value = PageParameter( key );
                if ( value.IsNullOrWhiteSpace() )
                {
                    continue;
                }

                // A malformed value still counts as a request, so a broken link is logged even when a fallback delivers.
                targetRequested = true;

                var guid = value.AsGuidOrNull();
                if ( !guid.HasValue )
                {
                    continue;
                }

                if ( group == null )
                {
                    group = groupService.Get( guid.Value );
                }

                if ( group == null && publishGroup == null )
                {
                    publishGroup = publishGroupService.Get( guid.Value );
                }
            }

            if ( group == null && publishGroup == null )
            {
                var slug = PageParameter( SlugParameterKey );
                if ( slug.IsNotNullOrWhiteSpace() )
                {
                    targetRequested = true;
                    var normalizedSlug = slug.ToLower().RemoveAllNonAlphaNumericCharacters();

                    // Only a live listing may be reached by slug. Drafts and pending/denied requests are not public.
                    publishGroup = publishGroupService.Queryable()
                        .Where( pg => pg.Slug == normalizedSlug && pg.PublishGroupStatus == PublishGroupStatus.Approved )
                        .OrderByDescending( pg => pg.Id )
                        .FirstOrDefault();
                }
            }

            return targetRequested;
        }

        /// <summary>
        /// Adds a person to the email or SMS recipient list based on preference,
        /// skipping (and remembering) anyone with no usable address.
        /// </summary>
        private void AddRecipient( Person person, bool allowSMS, List<Person> emailRecipients, List<Person> smsRecipients, List<string> skipped, string sourceLabel )
        {
            if ( person == null )
            {
                skipped.Add( $"{sourceLabel}: no person record" );
                return;
            }

            var phoneNumber = person.PhoneNumbers?.GetFirstSmsNumber();
            if ( allowSMS && person.CommunicationPreference == CommunicationType.SMS && phoneNumber.IsNotNullOrWhiteSpace() )
            {
                smsRecipients.Add( person );
                return;
            }

            if ( person.Email.IsNotNullOrWhiteSpace() )
            {
                emailRecipients.Add( person );
                return;
            }

            skipped.Add( $"{sourceLabel}: {person.FullName} (PersonId {person.Id}) has no email" );
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
            bool allowSMS = GetAttributeValue( "EnableSMS" ).AsBoolean();
            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );

            List<Person> emailRecipients = new List<Person>();
            List<Person> smsRecipients = new List<Person>();
            List<string> skippedRecipients = new List<string>();

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
                    AddRecipient( person, allowSMS, emailRecipients, smsRecipients, skippedRecipients, "PersonGuid " + personGuid.Value );
                }
            }

            // get group and/or publish group from url
            Group group;
            PublishGroup publishGroup;
            bool targetRequested = ResolveTargets( groupService, publishGroupService, out group, out publishGroup );
            bool targetUnresolved = targetRequested && group == null && publishGroup == null;

            // group route: every active leader
            if ( group != null )
            {
                foreach ( var leader in GetGroupLeaders( group ) )
                {
                    AddRecipient( leader, allowSMS, emailRecipients, smsRecipients, skippedRecipients, $"leader of group {group.Id}" );
                }
            }

            // publish group route: the published contact email, else the contact person
            if ( publishGroup != null )
            {
                if ( publishGroup.ContactEmail.IsNotNullOrWhiteSpace() )
                {
                    emailRecipients.Add( new Person { Email = publishGroup.ContactEmail.Trim() } );
                }
                else
                {
                    var contactPerson = publishGroup.ContactPersonAlias?.Person
                        ?? personAliasService.GetPerson( publishGroup.ContactPersonAliasId );
                    AddRecipient( contactPerson, allowSMS, emailRecipients, smsRecipients, skippedRecipients, $"contact person of publish group {publishGroup.Id} (alias {publishGroup.ContactPersonAliasId})" );
                }
            }

            bool usedFallback = false;

            if ( !emailRecipients.Any() && !smsRecipients.Any() )
            {
                Guid defaultRecipient = this.GetAttributeValue( "DefaultRecipient" ).AsGuid();
                if ( defaultRecipient != Guid.Empty )
                {
                    var defaultPerson = personAliasService.Queryable()
                            .Where( pa => pa.Guid == defaultRecipient )
                            .Select( pa => pa.Person )
                            .FirstOrDefault();
                    AddRecipient( defaultPerson, allowSMS, emailRecipients, smsRecipients, skippedRecipients, "Default Recipient" );
                    usedFallback = emailRecipients.Any() || smsRecipients.Any();
                }
            }

            if ( !emailRecipients.Any() && !smsRecipients.Any() )
            {
                Guid defaultGroupGuid = GetAttributeValue( "LeaderGroup" ).AsGuid();
                if ( defaultGroupGuid != Guid.Empty )
                {
                    var defaultGroup = groupService.Get( defaultGroupGuid );
                    foreach ( var leader in GetGroupLeaders( defaultGroup ) )
                    {
                        AddRecipient( leader, allowSMS, emailRecipients, smsRecipients, skippedRecipients, "Default Contact Group leader" );
                    }
                    usedFallback = emailRecipients.Any() || smsRecipients.Any();
                }
            }

            // Diagnostics that should be visible but must not flood the Exception Log on a public form.
            if ( targetUnresolved )
            {
                RockLogger.Log.Warning( RockLogDomains.Cms, "ContactGroupLeaders: no Group or PublishGroup matched the request {Url}. Fallback used: {UsedFallback}.", Request.RawUrl, usedFallback );
            }

            if ( skippedRecipients.Any() )
            {
                RockLogger.Log.Warning( RockLogDomains.Cms, "ContactGroupLeaders: skipped {Count} recipient(s) with no usable address on {Url}: {Skipped}", skippedRecipients.Count, Request.RawUrl, skippedRecipients.AsDelimited( "; " ) );
            }

            mergeFields.Add( "NickName", tbFirstName.Text );
            mergeFields.Add( "LastName", tbLastName.Text );
            mergeFields.Add( "Phone", tbPhone.Text );
            mergeFields.Add( "Email", tbEmail.Text );
            mergeFields.Add( "PublishGroup", publishGroup );
            // Existing Message Body templates use {{ Group.Name }}; make that work on the publish group route too.
            mergeFields.Add( "Group", group ?? publishGroup?.Group );
            mergeFields.Add( "Message", tbMessage.Text );
            mergeFields.Add( "FromEmail", tbEmail.Text );
            mergeFields.Add( "FromName", tbFirstName.Text + " " + tbLastName.Text );

            if ( cpCaptcha.IsAvailable && !cpCaptcha.IsResponseValid() )
            {
                lResponse.Visible = true;
                pnlContactGroupLeaders.Visible = true;
                lResponse.Text = "<div class='alert alert-danger'>You appear to be a computer. Check the box above and then click Submit.</div>";
                return false;
            }

            if ( !emailRecipients.Any() && !smsRecipients.Any() )
            {
                // Nothing resolved and no fallback configured: a real failure worth an Exception Log entry.
                LogWithContext( $"ContactGroupLeaders: no recipient could be resolved for {Request.RawUrl}; message was not sent. Configure Default Recipient on block {BlockId}." );
                ShowError( mergeFields );
                return false;
            }

            var errorMessages = new List<string>();

            if ( emailRecipients.Any() )
            {
                List<string> emailErrors;
                SendEmail( emailRecipients, mergeFields, out emailErrors );
                errorMessages.AddRange( emailErrors );
            }

            if ( smsRecipients.Any() )
            {
                List<string> smsErrors;
                SendTextMessage( smsRecipients, mergeFields, out smsErrors );
                errorMessages.AddRange( smsErrors );
            }

            if ( errorMessages.Any() )
            {
                LogWithContext( $"ContactGroupLeaders: send failed for {Request.RawUrl}. {errorMessages.AsDelimited( " | " )}" );
                ShowError( mergeFields );
                return false;
            }

            // display response message
            lResponse.Visible = true;
            pnlContactGroupLeaders.Visible = false;
            lResponse.Text = GetAttributeValue( "ResponseMessage" ).ResolveMergeFields( mergeFields, GetAttributeValue( "EnabledLavaCommands" ) );
            return true;
        }

        private void ShowError( Dictionary<string, object> mergeFields )
        {
            lResponse.Visible = true;
            pnlContactGroupLeaders.Visible = true;
            lResponse.Text = GetAttributeValue( "ErrorMessage" ).ResolveMergeFields( mergeFields, GetAttributeValue( "EnabledLavaCommands" ) );
        }

        /// <summary>
        /// Logs to the Rock exception log with the current request attached, so the
        /// entry carries the page URL (the transports log without request context).
        /// Reserved for real failures; routine diagnostics go to RockLogger.
        /// </summary>
        private void LogWithContext( string message )
        {
            ExceptionLogService.LogException( new Exception( message ), HttpContext.Current );
        }

        private bool SendEmail( List<Person> emailrecipients, Dictionary<string, object> mergeFields, out List<string> errorMessages )
        {
            errorMessages = new List<string>();

            var message = new RockEmailMessage();
            message.EnabledLavaCommands = GetAttributeValue( "EnabledLavaCommands" );

            foreach ( var person in emailrecipients )
            {
                if ( person.Email.IsNullOrWhiteSpace() )
                {
                    continue;
                }

                if ( person.Id > 0 )
                {
                    message.AddRecipient( new RockEmailMessageRecipient( person, mergeFields ) );
                }
                else
                {
                    message.AddRecipient( RockEmailMessageRecipient.CreateAnonymous( person.Email, mergeFields ) );
                }
            }

            if ( !message.GetRecipients().Any() )
            {
                errorMessages.Add( "No recipient with a valid email address." );
                return false;
            }

            // Send from a fixed, authenticated address so the visitor's own domain (yahoo, aol, ...) is
            // never spoofed through Mailgun. The visitor stays reachable via Reply-To.
            var fromEmail = GetAttributeValue( "FromEmail" );
            if ( fromEmail.IsNullOrWhiteSpace() )
            {
                fromEmail = GlobalAttributesCache.Get().GetValue( "OrganizationEmail" );
            }

            var visitorEmail = ( tbEmail.Text ?? string.Empty ).Trim();

            message.FromEmail = fromEmail;
            message.FromName = ( tbFirstName.Text + " " + tbLastName.Text ).Trim();
            if ( visitorEmail.IsNotNullOrWhiteSpace() )
            {
                message.ReplyToEmail = visitorEmail;
            }
            message.Subject = GetAttributeValue( "Subject" );
            message.Message = GetAttributeValue( "MessageBody" );
            message.AppRoot = ResolveRockUrl( "~/" );
            message.ThemeRoot = ResolveRockUrl( "~~/" );
            message.CreateCommunicationRecord = GetAttributeValue( "SaveCommunicationHistory" ).AsBoolean();

            return message.Send( out errorMessages );
        }

        private bool SendTextMessage( List<Person> smsRecipients, Dictionary<string, object> mergeFields, out List<string> errorMessages )
        {
            errorMessages = new List<string>();

            // Resolve recipients before anything is persisted so a miss never leaves an orphan Communication row.
            var recipientAliasIds = smsRecipients
                .Where( p => p.PrimaryAliasId.HasValue )
                .Select( p => p.PrimaryAliasId.Value )
                .Distinct()
                .ToList();

            if ( !recipientAliasIds.Any() )
            {
                errorMessages.Add( "No SMS recipient with a person record." );
                return false;
            }

            try
            {
                var rockContext = new RockContext();
                var communicationService = new CommunicationService( rockContext );
                var smsMediumEntityTypeId = EntityTypeCache.Get( Rock.SystemGuid.EntityType.COMMUNICATION_MEDIUM_SMS.AsGuid() ).Id;

                var communication = new Rock.Model.Communication();
                communication.Status = CommunicationStatus.Approved;
                communication.SenderPersonAliasId = CurrentPerson?.PrimaryAliasId;
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

                communication.SmsFromSystemPhoneNumberId = SystemPhoneNumberCache.Get( GetAttributeValue( "SMSFromNumber" ).AsGuid() )?.Id;
                communication.SMSMessage = GetAttributeValue( "SMSMessageBody" );
                communication.FutureSendDateTime = null;

                foreach ( var aliasId in recipientAliasIds )
                {
                    communication.Recipients.Add( new CommunicationRecipient()
                    {
                        PersonAliasId = aliasId,
                        MediumEntityTypeId = smsMediumEntityTypeId,
                        AdditionalMergeValues = mergeFields
                    } );
                }

                communicationService.Add( communication );
                rockContext.SaveChanges();

                var transaction = new ProcessSendCommunication.Message();
                transaction.CommunicationId = communication.Id;
                transaction.Send();
                return true;
            }
            catch ( Exception ex )
            {
                ExceptionLogService.LogException( ex, HttpContext.Current );
                errorMessages.Add( ex.Message );
                return false;
            }
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

            if ( communication == null || person == null || !communication.Recipients.Where( r => r.PersonAlias.PersonId == person.Id ).Any() )
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
