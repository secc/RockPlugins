using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Communication;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls.Communication;
using Rock.Web.UI.Controls;
using System.Data.Entity;
using RestSharp;
using Newtonsoft.Json;
using com.subsplash.Model;
using Newtonsoft.Json.Linq;

namespace RockWeb.Plugins.com_subsplash.Communication
{
    /// <summary>
    /// User control for creating a new push notification using the Subsplash API
    /// </summary>
    [DisplayName( "Push Notification Entry" )]
    [Category( "Subsplash > Communication" )]
    [Description( "Used for creating and sending a new push notifications to recipients or topics." )]

    [SecurityAction( Authorization.APPROVE, "The roles and/or users that have access to approve new push notifications." )]

    [LavaCommandsField( "Enabled Lava Commands", "The Lava commands that should be enabled for communcations.", false, order: 0 )]
    [ComponentField( "Rock.Communication.TransportContainer, Rock", "Transport", "The Subsplash transport that should be used.", true, "", "", 1 )]
    [CommunicationTemplateField( "Default Template", "The default template to use for a new communication.  (Note: This will only be used if the template is for the same medium as the communication.)", false, "", "", 2 )]
    [IntegerField( "Display Count", "The initial number of recipients to display prior to expanding list", false, 0, "", 4 )]
    [BooleanField( "Send When Approved", "Should push notification be sent once it's approved (vs. just being queued for scheduled job to send)?", true, "", 5 )]
    public partial class PushNotification : RockBlock
    {

        #region Fields

        private bool _fullMode = true;
        private bool _editingApproved = false;

        #endregion

        #region Properties

        protected int? CommunicationId
        {
            get { return (int?)ViewState["CommunicationId"] ?? PageParameter( "CommunicationId" ).AsIntegerOrNull(); }
            set { ViewState["CommunicationId"] = value; }
        }

        /// <summary>
        /// Gets or sets the medium entity type id.
        /// </summary>
        /// <value>
        /// The medium entity type id.
        /// </value>
        protected int? MediumEntityTypeId
        {
            get { return ViewState["MediumEntityTypeId"] as int?; }
            set { ViewState["MediumEntityTypeId"] = value; }
        }

        /// <summary>
        /// Gets or sets the entity types that have been viewed. If entity type has not been viewed, the control will be initialized to current person
        /// </summary>
        /// <value>
        /// The initialized entity types.
        /// </value>
        protected List<int> ViewedEntityTypes
        {
            get
            {
                var viewedEntityTypes = ViewState["ViewedEntityTypes"] as List<int>;
                if ( viewedEntityTypes == null )
                {
                    viewedEntityTypes = new List<int>();
                    ViewedEntityTypes = viewedEntityTypes;
                }
                return viewedEntityTypes;
            }
            set { ViewState["ViewedEntityTypes"] = value; }
        }

        /// <summary>
        /// Gets or sets the recipients.
        /// </summary>
        /// <value>
        /// The recipient ids.
        /// </value>
        protected List<Recipient> Recipients
        {
            get
            {
                var recipients = ViewState["Recipients"] as List<Recipient>;
                if ( recipients == null )
                {
                    recipients = new List<Recipient>();
                    ViewState["Recipients"] = recipients;
                }
                return recipients;
            }

            set { ViewState["Recipients"] = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [show all recipients].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [show all recipients]; otherwise, <c>false</c>.
        /// </value>
        protected bool ShowAllRecipients
        {
            get { return ViewState["ShowAllRecipients"] as bool? ?? false; }
            set { ViewState["ShowAllRecipients"] = value; }
        }

        /// <summary>
        /// Gets or sets the communication data.
        /// </summary>
        /// <value>
        /// The communication data.
        /// </value>
        protected CommunicationDetails CommunicationData
        {
            get
            {
                var communicationData = ViewState["CommunicationData"] as CommunicationDetails;
                if ( communicationData == null )
                {
                    communicationData = new CommunicationDetails();
                    ViewState["CommunicationData"] = communicationData;
                }
                return communicationData;
            }

            set { ViewState["CommunicationData"] = value; }
        }

        /// <summary>
        /// Gets or sets any additional merge fields.
        /// </summary>
        public List<string> AdditionalMergeFields
        {
            get
            {
                var mergeFields = ViewState["AdditionalMergeFields"] as List<string>;
                if ( mergeFields == null )
                {
                    mergeFields = new List<string>();
                    ViewState["AdditionalMergeFields"] = mergeFields;
                }
                return mergeFields;
            }

            set { ViewState["AdditionalMergeFields"] = value; }
        }

        public bool ApproverEditing
        {
            get { return ViewState["ApproverEditing"] as bool? ?? false; }
            set { ViewState["ApproverEditing"] = value; }
        }

        #endregion

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            string script = @"
    $('a.remove-all-recipients').click(function( e ){
        e.preventDefault();
        Rock.dialogs.confirm('Are you sure you want to remove all of the pending recipients from this communication?', function (result) {
            if (result) {
                window.location = e.target.href ? e.target.href : e.target.parentElement.href;
            }
        });
    });
";
            ScriptManager.RegisterStartupScript( lbRemoveAllRecipients, lbRemoveAllRecipients.GetType(), "ConfirmRemoveAll", script, true );

            string mode = GetAttributeValue( "Mode" );
            _fullMode = string.IsNullOrWhiteSpace( mode ) || mode != "Simple";
            ppAddPerson.Visible = _fullMode;
            ddlTemplate.Visible = _fullMode;
            dtpFutureSend.Visible = _fullMode;
            btnTest.Visible = false;
            btnSave.Visible = _fullMode;

            _editingApproved = PageParameter( "Edit" ).AsBoolean() && IsUserAuthorized( "Approve" );

        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            nbTestResult.Visible = false;
            pnlMessage.Visible = false;
            upNotifications.Visible = false;
            upPanel.Visible = true;

            if ( !GetAttributeValue( "Transport" ).AsGuidOrNull().HasValue )
            {
                upNotifications.Visible = true;
                upPanel.Visible = false;
                nbAlert.Text = "This block must be configured by selecting the Subsplash push notification communication transport.";
            }
            else if ( Page.IsPostBack )
            {
                LoadMediumControl( false );
            }
            else
            {
                ShowAllRecipients = false;

                // Check if CommunicationDetail has already loaded existing communication
                var communication = RockPage.GetSharedItem( "Communication" ) as Rock.Model.Communication;
                if ( communication == null )
                {
                    if ( CommunicationId.HasValue )
                    {
                        communication = new CommunicationService( new RockContext() ).Get( CommunicationId.Value );
                    }
                }
                else
                {
                    CommunicationId = communication.Id;
                }

                if ( communication == null )
                {
                    // if this is a new communication, create a communication object temporarily so we can do the auth and edit logic
                    communication = new Rock.Model.Communication() { Status = CommunicationStatus.Transient };
                    communication.CreatedByPersonAlias = this.CurrentPersonAlias;
                    communication.CreatedByPersonAliasId = this.CurrentPersonAliasId;
                    communication.SenderPersonAlias = this.CurrentPersonAlias;
                    communication.SenderPersonAliasId = CurrentPersonAliasId;
                }

                // If viewing a new, transient, draft, or are the approver of a pending-approval communication, use this block
                // otherwise, set this block visible=false and if there is a communication detail block on this page, it'll be shown instead
                CommunicationStatus[] editableStatuses = new CommunicationStatus[] { CommunicationStatus.Transient, CommunicationStatus.Draft, CommunicationStatus.Denied };
                if ( editableStatuses.Contains( communication.Status ) || ( communication.Status == CommunicationStatus.PendingApproval && _editingApproved ) )
                {
                    // Make sure they are authorized to edit, or the owner, or the approver/editor
                    bool isAuthorizedEditor = communication.IsAuthorized( Rock.Security.Authorization.EDIT, CurrentPerson );
                    bool isCreator = ( communication.CreatedByPersonAlias != null && CurrentPersonId.HasValue && communication.CreatedByPersonAlias.PersonId == CurrentPersonId.Value );
                    bool isApprovalEditor = communication.Status == CommunicationStatus.PendingApproval && _editingApproved;

                    // If communication was just created only for authorization, set it to null so that Showing of details works correctly.
                    if ( communication.Id == 0 )
                    {
                        communication = null;
                    }

                    if ( isAuthorizedEditor || isCreator || isApprovalEditor )
                    {
                        // communication is either new or ok to edit
                        ShowDetail( communication );
                    }
                    else
                    {
                        // not authorized, so hide this block
                        this.Visible = false;
                    }
                }
                else
                {
                    // Not an editable communication, so hide this block. 
                    pnlEditForm.Visible = false;

                    // Show Message Details instead
                    pnlMessage.Visible = true;
                    ShowMessageDetails( communication );
                    ShowMessageActions( communication );

                }
            }
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.PreRender" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnPreRender( EventArgs e )
        {
            if ( GetAttributeValue( "Transport" ).AsGuidOrNull().HasValue )
            {
                BindRecipients();
                if (!IsPostBack)
                {
                    BindTopics();
                }
            }
        }

        #endregion

        #region Events

        protected void ddlTemplate_SelectedIndexChanged( object sender, EventArgs e )
        {
            GetMediumData();
            int? templateId = ddlTemplate.SelectedValue.AsIntegerOrNull();
            if ( templateId.HasValue )
            {
                GetTemplateData( templateId.Value );
            }
        }

        /// <summary>
        /// Handles the Click event of the lbMedium control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void lbMedium_Click( object sender, EventArgs e )
        {
            GetMediumData();
            var linkButton = sender as LinkButton;
            if ( linkButton != null )
            {
                int mediumId = int.MinValue;
                if ( int.TryParse( linkButton.CommandArgument, out mediumId ) )
                {
                    MediumEntityTypeId = mediumId;
                    BindMediums();

                    var control = LoadMediumControl( true );
                    InitializeControl( control );
                }
            }
        }

        /// <summary>
        /// Handles the SelectPerson event of the ppAddPerson control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void ppAddPerson_SelectPerson( object sender, EventArgs e )
        {
            if ( ppAddPerson.PersonId.HasValue )
            {
                if ( !Recipients.Any( r => r.PersonId == ppAddPerson.PersonId.Value ) )
                {
                    var context = new RockContext();
                    var Person = new PersonService( context ).Get( ppAddPerson.PersonId.Value );
                    if ( Person != null )
                    {
                        var HasPersonalDevice = new PersonalDeviceService( context ).Queryable()
                            .Where( pd => 
                                pd.PersonAliasId.HasValue && 
                                pd.PersonAliasId == Person.PrimaryAliasId && 
                                pd.NotificationsEnabled )
                            .Any();
                        Recipients.Add( new Recipient( Person, Person.PhoneNumbers.Any( a => a.IsMessagingEnabled ), HasPersonalDevice, CommunicationRecipientStatus.Pending ) );
                        ShowAllRecipients = true;
                    }
                }
            }
        }

        /// <summary>
        /// Handles the ItemDataBound event of the rptRecipients control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RepeaterItemEventArgs" /> instance containing the event data.</param>
        protected void rptRecipients_ItemDataBound( object sender, RepeaterItemEventArgs e )
        {
            // Hide the remove button for any recipient that is not pending.
            if ( e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem )
            {
                var recipient = e.Item.DataItem as Recipient;
                if ( recipient != null )
                {
                    var lRecipientName = e.Item.FindControl( "lRecipientName" ) as Literal;
                    if ( lRecipientName != null )
                    {
                        string textClass = string.Empty;
                        string textTooltip = string.Empty;

                        if ( recipient.IsDeceased )
                        {
                            textClass = "text-danger";
                            textTooltip = "Deceased";
                        }
                        else
                        {
                            if ( !recipient.HasNotificationsEnabled )
                            {
                                // No Notifications Enabled
                                textClass = "text-danger";
                                textTooltip = "Notifications not enabled for this number.";
                            }
                        }

                        lRecipientName.Text = String.Format( "<span data-toggle=\"tooltip\" data-placement=\"top\" title=\"{0}\" class=\"{1}\">{2}</span>",
                            textTooltip, textClass, recipient.PersonName );
                    }
                }
            }
        }

        /// <summary>
        /// Handles the ItemCommand event of the rptRecipients control.
        /// </summary>
        /// <param name="source">The source of the event.</param>
        /// <param name="e">The <see cref="RepeaterCommandEventArgs" /> instance containing the event data.</param>
        protected void rptRecipients_ItemCommand( object source, RepeaterCommandEventArgs e )
        {
            int personId = int.MinValue;
            if ( int.TryParse( e.CommandArgument.ToString(), out personId ) )
            {
                Recipients = Recipients.Where( r => r.PersonId != personId ).ToList();
            }
        }

        /// <summary>
        /// Handles the Click event of the lbShowAllRecipients control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void lbShowAllRecipients_Click( object sender, EventArgs e )
        {
            ShowAllRecipients = true;
        }

        /// <summary>
        /// Handles the Click event of the lbRemoveAllRecipients control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void lbRemoveAllRecipients_Click( object sender, EventArgs e )
        {
            Recipients = Recipients.Where( r => r.Status != CommunicationRecipientStatus.Pending ).ToList();
        }

        /// <summary>
        /// Handles the ServerValidate event of the valRecipients control.
        /// </summary>
        /// <param name="source">The source of the event.</param>
        /// <param name="args">The <see cref="ServerValidateEventArgs" /> instance containing the event data.</param>
        protected void valRecipients_ServerValidate( object source, ServerValidateEventArgs args )
        {
            if ( bgRecipientOptions.SelectedValue == "Individual" )
            {
                args.IsValid = Recipients.Any();
            }
        }

        /// <summary>
        /// Handles the Click event of the btnSubmit control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void btnTest_Click( object sender, EventArgs e )
        {
            ValidateFutureDelayDateTime();
            if ( Page.IsValid && CurrentPersonAliasId.HasValue && cvDelayDateTime.IsValid )
            {
                // Get existing or new communication record
                var communication = UpdateCommunication( new RockContext() );
                if ( communication != null )
                {
                    using ( var rockContext = new RockContext() )
                    {
                        // Using a new context (so that changes in the UpdateCommunication() are not persisted )
                        var testCommunication = communication.Clone( false );
                        testCommunication.Id = 0;
                        testCommunication.Guid = Guid.NewGuid();
                        testCommunication.CreatedByPersonAliasId = this.CurrentPersonAliasId;
                        testCommunication.CreatedByPersonAlias = new PersonAliasService( rockContext ).Queryable().Where( a => a.Id == this.CurrentPersonAliasId.Value ).Include( a => a.Person ).FirstOrDefault();

                        testCommunication.EnabledLavaCommands = GetAttributeValue( "EnabledLavaCommands" );
                        testCommunication.ForeignGuid = null;
                        testCommunication.ForeignId = null;
                        testCommunication.ForeignKey = null;

                        testCommunication.FutureSendDateTime = null;
                        testCommunication.Status = CommunicationStatus.Approved;
                        testCommunication.ReviewedDateTime = RockDateTime.Now;
                        testCommunication.ReviewerPersonAliasId = CurrentPersonAliasId;

                        testCommunication.Subject = string.Format( "[Test] {0}", testCommunication.Subject );

                        foreach ( var attachment in communication.Attachments )
                        {
                            var cloneAttachment = attachment.Clone( false );
                            cloneAttachment.Id = 0;
                            cloneAttachment.Guid = Guid.NewGuid();
                            cloneAttachment.ForeignGuid = null;
                            cloneAttachment.ForeignId = null;
                            cloneAttachment.ForeignKey = null;

                            testCommunication.Attachments.Add( cloneAttachment );
                        }

                        var testRecipient = new CommunicationRecipient();
                        if ( communication.Recipients.Any() )
                        {
                            var recipient = communication.Recipients.FirstOrDefault();
                            testRecipient.AdditionalMergeValuesJson = recipient.AdditionalMergeValuesJson;
                        }

                        testRecipient.Status = CommunicationRecipientStatus.Pending;
                        testRecipient.PersonAliasId = CurrentPersonAliasId.Value;
                        testRecipient.MediumEntityTypeId = MediumEntityTypeId;
                        testCommunication.Recipients.Add( testRecipient );

                        var communicationService = new CommunicationService( rockContext );
                        communicationService.Add( testCommunication );
                        rockContext.SaveChanges();

                        foreach ( var medium in testCommunication.GetMediums() )
                        {
                            medium.Send( testCommunication );
                        }

                        testRecipient = new CommunicationRecipientService( rockContext )
                            .Queryable().AsNoTracking()
                            .Where( r => r.CommunicationId == testCommunication.Id )
                            .FirstOrDefault();

                        if ( testRecipient != null && testRecipient.Status == CommunicationRecipientStatus.Failed && testRecipient.PersonAlias != null && testRecipient.PersonAlias.Person != null )
                        {
                            nbTestResult.NotificationBoxType = NotificationBoxType.Danger;
                            nbTestResult.Text = string.Format( "Test communication to <strong>{0}</strong> failed: {1}.", testRecipient.PersonAlias.Person.FullName, testRecipient.StatusNote );
                        }
                        else
                        {
                            nbTestResult.NotificationBoxType = NotificationBoxType.Success;
                            nbTestResult.Text = "Test communication has been sent.";
                        }
                        nbTestResult.Visible = true;

                        communicationService.Delete( testCommunication );
                        rockContext.SaveChanges();
                    }
                }
            }
        }


        /// <summary>
        /// Handles the Click event of the btnSubmit control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void btnSubmit_Click( object sender, EventArgs e )
        {
            if ( Page.IsValid )
            {
                ValidateFutureDelayDateTime();
                if ( !cvDelayDateTime.IsValid )
                {
                    return;
                }
                var rockContext = new RockContext();
                var communication = UpdateCommunication( rockContext );

                if ( communication != null )
                {
                    var mediumControl = GetMediumControl();
                    if ( mediumControl != null )
                    {
                        mediumControl.OnCommunicationSave( rockContext );
                    }

                    if ( _editingApproved && communication.Status == CommunicationStatus.PendingApproval )
                    {
                        rockContext.SaveChanges();

                        // Redirect back to same page without the edit param
                        var pageRef = new Rock.Web.PageReference();
                        pageRef.PageId = CurrentPageReference.PageId;
                        pageRef.RouteId = CurrentPageReference.RouteId;
                        pageRef.Parameters = new Dictionary<string, string>();
                        pageRef.Parameters.Add( "CommunicationId", communication.Id.ToString() );
                        Response.Redirect( pageRef.BuildUrl() );
                        Context.ApplicationInstance.CompleteRequest();
                    }
                    else
                    {
                        string message = string.Empty;

                        // Save the communication prior to checking recipients.
                        communication.Status = CommunicationStatus.Draft;
                        rockContext.SaveChanges();


                        var personService = new PersonService( rockContext );
                        var inactiveRecordStatusId = DefinedValueCache.Get( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_INACTIVE.AsGuid() ).Id;
                        var recipientPersonIds = this.Recipients.Select( r => r.PersonId ).ToList();
                        var inactiveRecipientCount = personService.Queryable( true ).AsNoTracking().Where( a => recipientPersonIds.Contains( a.Id ) && a.RecordStatusValueId == inactiveRecordStatusId ).Count();



                        bool inactiveApprove = GetAttributeValue( "InactiveRecipientsRequireApproval" ).AsBoolean();
                        if ( CheckApprovalRequired( communication.Recipients.Count()) && !IsUserAuthorized( "Approve" ) 
                            || ( inactiveApprove && inactiveRecipientCount > 0 && !IsUserAuthorized( "Approve" ) ))
                       {
                            communication.Status = CommunicationStatus.PendingApproval;
                            message = "Communication has been submitted for approval.";
                        }
                        else
                        {
                            communication.Status = CommunicationStatus.Approved;
                            communication.ReviewedDateTime = RockDateTime.Now;
                            communication.ReviewerPersonAliasId = CurrentPersonAliasId;

                            if ( communication.FutureSendDateTime.HasValue &&
                                communication.FutureSendDateTime > RockDateTime.Now )
                            {
                                message = string.Format( "Communication will be sent {0}.",
                                    communication.FutureSendDateTime.Value.ToRelativeDateString( 0 ) );
                            }
                            else
                            {
                                message = "Communication has been queued for sending.";
                            }

                            // Send the communication to SubSplash if this is sending to a topic
                            if ( bgRecipientOptions.SelectedValue == "Topic" )
                            {
                                SendPushNotificationToTopics( communication, GetTopics().Where( t => t.Id == rddlTopics.SelectedValueAsGuid() ).ToList() );
                            }
                        }

                        rockContext.SaveChanges();

                        // send approval email if needed (now that we have a communication id)
                        if ( communication.Status == CommunicationStatus.PendingApproval )
                        {
                            var approvalTransaction = new Rock.Transactions.SendCommunicationApprovalEmail();
                            approvalTransaction.CommunicationId = communication.Id;
                            Rock.Transactions.RockQueue.TransactionQueue.Enqueue( approvalTransaction );
                        }

                        if ( communication.Status == CommunicationStatus.Approved &&
                            ( !communication.FutureSendDateTime.HasValue || communication.FutureSendDateTime.Value <= RockDateTime.Now ) )
                        {
                            if ( GetAttributeValue( "SendWhenApproved" ).AsBoolean() )
                            {
                                var transaction = new Rock.Transactions.SendCommunicationTransaction();
                                transaction.CommunicationId = communication.Id;
                                transaction.PersonAlias = CurrentPersonAlias;
                                Rock.Transactions.RockQueue.TransactionQueue.Enqueue( transaction );
                            }
                        }

                        ShowResult( message, communication );
                    }
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the btnSave control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void btnSave_Click( object sender, EventArgs e )
        {
            ValidateFutureDelayDateTime();
            if ( !cvDelayDateTime.IsValid )
            {
                return;
            }
            var rockContext = new RockContext();
            var communication = UpdateCommunication( rockContext );

            if ( communication != null )
            {
                var mediumControl = GetMediumControl();
                if ( mediumControl != null )
                {
                    mediumControl.OnCommunicationSave( rockContext );
                }

                communication.Status = CommunicationStatus.Draft;
                rockContext.SaveChanges();

                ShowResult( "The communication has been saved", communication );
            }
        }

        protected void btnCancel_Click( object sender, EventArgs e )
        {
            if ( _editingApproved )
            {
                var communicationService = new CommunicationService( new RockContext() );
                var communication = communicationService.Get( CommunicationId.Value );
                if ( communication != null && communication.Status == CommunicationStatus.PendingApproval )
                {
                    // Redirect back to same page without the edit param
                    var pageRef = new Rock.Web.PageReference();
                    pageRef.PageId = CurrentPageReference.PageId;
                    pageRef.RouteId = CurrentPageReference.RouteId;
                    pageRef.Parameters = new Dictionary<string, string>();
                    pageRef.Parameters.Add( "CommunicationId", communication.Id.ToString() );
                    Response.Redirect( pageRef.BuildUrl() );
                    Context.ApplicationInstance.CompleteRequest();
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the btnEdit control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void btnEdit_Click( object sender, EventArgs e )
        {
            var dataContext = new RockContext();

            var service = new CommunicationService( dataContext );
            var communication = service.Get( CommunicationId.Value );
            if ( communication != null &&
                communication.Status == CommunicationStatus.PendingApproval &&
                IsUserAuthorized( "Approve" ) )
            {
                // Redirect back to same page without the edit param
                var pageRef = CurrentPageReference;
                pageRef.Parameters.Add( "edit", "true" );
                Response.Redirect( pageRef.BuildUrl() );
                Context.ApplicationInstance.CompleteRequest();
            }
        }

        /// <summary>
        /// Handles the Click event of the btnApprove control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void btnApprove_Click( object sender, EventArgs e )
        {
            if ( Page.IsValid && CommunicationId.HasValue )
            {
                var dataContext = new RockContext();

                var service = new CommunicationService( dataContext );
                var communication = service.Get( CommunicationId.Value );
                if ( communication != null )
                {
                    if ( communication.Status == CommunicationStatus.PendingApproval )
                    {
                        var prevStatus = communication.Status;
                        if ( IsUserAuthorized( "Approve" ) )
                        {
                            communication.Status = CommunicationStatus.Approved;
                            communication.ReviewedDateTime = RockDateTime.Now;
                            communication.ReviewerPersonAliasId = CurrentPersonAliasId;

                            dataContext.SaveChanges();

                            // TODO: Send notice to sender that communication was approved

                            ShowResult( "The communication has been approved", communication, NotificationBoxType.Success );
                        }
                        else
                        {
                            ShowResult( "Sorry, you are not authorized to approve this communication!", communication, NotificationBoxType.Danger );
                        }
                    }
                    else
                    {
                        ShowResult( string.Format( "This communication is already {0}!", communication.Status.ConvertToString() ),
                            communication, NotificationBoxType.Warning );
                    }
                }

            }
        }

        /// <summary>
        /// Handles the Click event of the btnDeny control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void btnDeny_Click( object sender, EventArgs e )
        {
            if ( Page.IsValid && CommunicationId.HasValue )
            {
                var dataContext = new RockContext();

                var service = new CommunicationService( dataContext );
                var communication = service.Get( CommunicationId.Value );
                if ( communication != null )
                {
                    if ( communication.Status == CommunicationStatus.PendingApproval )
                    {
                        if ( IsUserAuthorized( "Approve" ) )
                        {
                            communication.Status = CommunicationStatus.Denied;
                            communication.ReviewedDateTime = RockDateTime.Now;
                            communication.ReviewerPersonAliasId = CurrentPersonAliasId;

                            dataContext.SaveChanges();

                            // TODO: Send notice to sender that communication was denied

                            ShowResult( "The communication has been denied", communication, NotificationBoxType.Warning );
                        }
                        else
                        {
                            ShowResult( "Sorry, you are not authorized to approve or deny this communication!", communication, NotificationBoxType.Danger );
                        }
                    }
                    else
                    {
                        ShowResult( string.Format( "This communication is already {0}!", communication.Status.ConvertToString() ),
                            communication, NotificationBoxType.Warning );
                    }
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the btnCancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void btnCancelSend_Click( object sender, EventArgs e )
        {
            if ( Page.IsValid && CommunicationId.HasValue )
            {
                var dataContext = new RockContext();

                var service = new CommunicationService( dataContext );
                var communication = service.Get( CommunicationId.Value );
                if ( communication != null )
                {
                    if ( communication.Status == CommunicationStatus.Approved || communication.Status == CommunicationStatus.PendingApproval )
                    {
                        // Load the notifications using the Push Notifications Transport
                        var transport = TransportContainer.Instance.Components.FirstOrDefault( t => t.Value.Value.TypeGuid == GetAttributeValue( "Transport" ).AsGuidOrNull() ).Value.Value;
                        if ( transport != null && communication.ForeignGuid.HasValue )
                        {
                            transport.LoadAttributes();

                            var client = new RestClient( transport.GetAttributeValue( "APIEndpoint" ) );

                            var pushNotificationRequest = new RestRequest( "notifications/{id}", Method.DELETE );
                            pushNotificationRequest.AddHeader( "Content-Type", "application/json" );
                            pushNotificationRequest.AddHeader( "Authorization", "Bearer " + Encryption.DecryptString( transport.GetAttributeValue( "JWTToken" ) ) );
                            pushNotificationRequest.AddParameter( "id", communication.ForeignGuid.ToString(), ParameterType.UrlSegment );
                            pushNotificationRequest.RequestFormat = DataFormat.Json;

                            var response = client.Execute( pushNotificationRequest );
                        }

                        if ( !communication.Recipients
                            .Where( r => r.Status == CommunicationRecipientStatus.Delivered )
                            .Any() )
                        {

                            communication.Status = CommunicationStatus.Draft;
                            dataContext.SaveChanges();

                            ShowResult( "This communication has successfully been cancelled without any recipients receiving communication!", communication, NotificationBoxType.Success );

                            // communication is either new or ok to edit
                            ShowDetail( communication );
                            pnlEditForm.Visible = true;
                            pnlMessage.Visible = false;
                        }
                        else
                        {
                            communication.Recipients
                                .Where( r => r.Status == CommunicationRecipientStatus.Pending )
                                .ToList()
                                .ForEach( r => r.Status = CommunicationRecipientStatus.Cancelled );
                            dataContext.SaveChanges();

                            int delivered = communication.Recipients.Count( r => r.Status == CommunicationRecipientStatus.Delivered );
                            ShowResult( string.Format( "This communication has been cancelled, however the communication was delivered to {0} recipients!", delivered )
                                , communication, NotificationBoxType.Warning );
                        }
                    }
                    else
                    {
                        ShowResult( "This communication has already been cancelled!", communication, NotificationBoxType.Warning );
                    }
                }
            }

        }

        /// <summary>
        /// Handles the Click event of the btnCopy control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void btnCopy_Click( object sender, EventArgs e )
        {
            if ( Page.IsValid && CommunicationId.HasValue )
            {
                var dataContext = new RockContext();

                var service = new CommunicationService( dataContext );
                var communication = service.Get( CommunicationId.Value );
                if ( communication != null )
                {
                    var newCommunication = communication.Clone( false );
                    newCommunication.CreatedByPersonAlias = null;
                    newCommunication.CreatedByPersonAliasId = null;
                    newCommunication.CreatedDateTime = RockDateTime.Now;
                    newCommunication.ModifiedByPersonAlias = null;
                    newCommunication.ModifiedByPersonAliasId = null;
                    newCommunication.ModifiedDateTime = RockDateTime.Now;
                    newCommunication.Id = 0;
                    newCommunication.Guid = Guid.Empty;
                    newCommunication.SenderPersonAliasId = CurrentPersonAliasId;
                    newCommunication.Status = CommunicationStatus.Draft;
                    newCommunication.ReviewerPersonAliasId = null;
                    newCommunication.ReviewedDateTime = null;
                    newCommunication.ReviewerNote = string.Empty;
                    newCommunication.SendDateTime = null;
                    newCommunication.ForeignGuid = null;
                    newCommunication.ForeignId = null;
                    newCommunication.ForeignKey = null;

                    communication.Recipients.ToList().ForEach( r =>
                        newCommunication.Recipients.Add( new CommunicationRecipient()
                        {
                            PersonAliasId = r.PersonAliasId,
                            Status = CommunicationRecipientStatus.Pending,
                            StatusNote = string.Empty,
                            AdditionalMergeValuesJson = r.AdditionalMergeValuesJson
                        } ) );


                    foreach ( var attachment in communication.Attachments.ToList() )
                    {
                        var newAttachment = new CommunicationAttachment();
                        newAttachment.BinaryFileId = attachment.BinaryFileId;
                        newAttachment.CommunicationType = attachment.CommunicationType;
                        newCommunication.Attachments.Add( newAttachment );
                    }

                    service.Add( newCommunication );
                    dataContext.SaveChanges();

                    // Redirect to new communication
                    if ( CurrentPageReference.Parameters.ContainsKey( "CommunicationId" ) )
                    {
                        CurrentPageReference.Parameters[ "CommunicationId" ] = newCommunication.Id.ToString();
                    }
                    else
                    {
                        CurrentPageReference.Parameters.Add( "CommunicationId", newCommunication.Id.ToString() );
                    }

                    Response.Redirect( CurrentPageReference.BuildUrl() );
                    Context.ApplicationInstance.CompleteRequest();
                }
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Show the message panel.
        /// </summary>
        /// <param name="communication"></param>
        private void ShowMessageDetails( Rock.Model.Communication communication )
        {
            pdAuditDetails.SetEntity( communication, ResolveRockUrl( "~" ) );

            SetCommunicationAuditDisplayControlValue( lCreatedBy, communication.CreatedByPersonAlias, communication.CreatedDateTime, "Created By" );
            SetCommunicationAuditDisplayControlValue( lApprovedBy, communication.ReviewerPersonAlias, communication.ReviewedDateTime, "Approved By" );

            if ( communication.FutureSendDateTime.HasValue && communication.FutureSendDateTime.Value > RockDateTime.Now )
            {
                lFutureSend.Text = String.Format( "<div class='alert alert-success'><strong>Future Send</strong> This communication is scheduled to be sent {0} <small>({1})</small>.</div>", communication.FutureSendDateTime.Value.ToRelativeDateString(), communication.FutureSendDateTime.Value.ToString() );
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendFormat( "<dt>{0}</dt><dd>{1}</dd>", "Title", communication.PushTitle );
            sb.AppendFormat( "<dt>{0}</dt><dd>{1}</dd>", "Message", communication.PushMessage );

            var details = sb.ToString();

            if ( string.IsNullOrWhiteSpace( details ) )
            {
                details = "<div class='alert alert-warning'>No message details are available for this communication</div>";
            }

            if ( communication.UrlReferrer.IsNotNullOrWhiteSpace() )
            {
                details += string.Format( "<small>Originated from <a href='{0}'>this page</a></small>", communication.UrlReferrer );
            }

            lDetails.Text = details;
        }

        /// <summary>
        /// Shows the actions.
        /// </summary>
        /// <param name="communication">The communication.</param>
        private void ShowMessageActions( Rock.Model.Communication communication )
        {
            bool canApprove = IsUserAuthorized( "Approve" );

            // Set default visibility
            btnApprove.Visible = false;
            btnDeny.Visible = false;
            btnEdit.Visible = false;
            btnCancelSend.Visible = false;
            btnCopy.Visible = false;

            if ( communication != null )
            {
                switch ( communication.Status )
                {
                    case CommunicationStatus.Transient:
                    case CommunicationStatus.Draft:
                    case CommunicationStatus.Denied:
                        {
                            // This block isn't used for transient, draft or denied communications
                            break;
                        }
                    case CommunicationStatus.PendingApproval:
                        {
                            if ( canApprove )
                            {
                                btnApprove.Visible = true;
                                btnDeny.Visible = true;
                                btnEdit.Visible = true;
                            }
                            btnCancelSend.Visible = communication.IsAuthorized( Rock.Security.Authorization.EDIT, CurrentPerson );
                            break;
                        }
                    case CommunicationStatus.Approved:
                        {
                            // If there are still any pending recipients, allow canceling of send
                            var dataContext = new RockContext();

                            var hasPendingRecipients = new CommunicationRecipientService( dataContext ).Queryable()
                            .Where( r => r.CommunicationId == communication.Id ).Where( r => r.Status == CommunicationRecipientStatus.Pending ).Any();


                            // Load the notifications using the Push Notifications Transport
                            var transport = TransportContainer.Instance.Components.FirstOrDefault( t => t.Value.Value.TypeGuid == GetAttributeValue( "Transport" ).AsGuidOrNull() ).Value.Value;
                            if ( transport != null )
                            {
                                transport.LoadAttributes();

                                var client = new RestClient( transport.GetAttributeValue( "APIEndpoint" ) );

                                var pushNotificationRequest = new RestRequest( "notifications/{id}", Method.GET );
                                pushNotificationRequest.AddHeader( "Content-Type", "application/json" );
                                pushNotificationRequest.AddHeader( "Authorization", "Bearer " + Encryption.DecryptString( transport.GetAttributeValue( "JWTToken" ) ) );
                                pushNotificationRequest.AddParameter( "id", communication.ForeignGuid.ToString(), ParameterType.UrlSegment );
                                pushNotificationRequest.RequestFormat = DataFormat.Json;

                                var response = client.Get( pushNotificationRequest );

                                var notification = JsonConvert.DeserializeObject<com.subsplash.Model.Notification>(
                                    response.Content,
                                    new JsonSerializerSettings
                                    {
                                        ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                                        DateTimeZoneHandling = DateTimeZoneHandling.Local
                                    }
                                );

                                if ( !notification.Sent.HasValue || notification.Sent.Value == false )
                                {
                                    hasPendingRecipients = true;
                                }
                            }

                            btnCancelSend.Visible = hasPendingRecipients;

                            // Allow then to create a copy if they have VIEW (don't require full EDIT auth)
                            btnCopy.Visible = communication.IsAuthorized( Rock.Security.Authorization.VIEW, CurrentPerson );
                            break;
                        }
                }
            }
        }


        /// <summary>
        /// Sets the value of a control that displays audit information for a communication.
        /// </summary>
        /// <param name="literal"></param>
        /// <param name="personAlias"></param>
        /// <param name="datetime"></param>
        /// <param name="labelText"></param>
        private void SetCommunicationAuditDisplayControlValue( Literal literal, PersonAlias personAlias, DateTime? datetime, string labelText )
        {
            if ( personAlias != null )
            {
                SetPersonDateValue( literal, personAlias.Person, datetime, labelText );
            }
        }

        /// <summary>
        /// Sets the value of a control that displays audit information for a communication.
        /// </summary>
        /// <param name="literal"></param>
        /// <param name="person"></param>
        /// <param name="datetime"></param>
        /// <param name="labelText"></param>
        private void SetPersonDateValue( Literal literal, Person person, DateTime? datetime, string labelText )
        {
            if ( person != null )
            {
                literal.Text = String.Format( "<strong>{0}</strong> {1}", labelText, person.FullName );

                if ( datetime.HasValue )
                {
                    literal.Text += String.Format( " <small class='js-date-rollover' data-toggle='tooltip' data-placement='top' title='{0}'>({1})</small>", datetime.Value.ToString(), datetime.Value.ToRelativeDateString() );
                }
            }
        }

        /// <summary>
        /// Shows the detail.
        /// </summary>
        /// <param name="communication">The communication.</param>
        private void ShowDetail( Rock.Model.Communication communication )
        {
            Recipients.Clear();

            if ( communication != null && communication.Id > 0 )
            {
                this.AdditionalMergeFields = communication.AdditionalMergeFields.ToList();
                lTitle.Text = ( communication.Name ?? communication.Subject ?? "New Push Notification" ).FormatAsHtmlTitle();
                var context = new RockContext();
                var personalDeviceService = new PersonalDeviceService( context ).Queryable();
                var recipientList = new CommunicationRecipientService( context )
                    .Queryable()
                    .Where( r => r.CommunicationId == communication.Id )
                    .Select( a => new
                    {
                        a.PersonAlias.Person,
                        PersonHasSMS = a.PersonAlias.Person.PhoneNumbers.Any( p => p.IsMessagingEnabled ),
                        HasPersonalDevice = (
                             personalDeviceService
                                 .Where( pd => pd.PersonAliasId.HasValue && pd.PersonAliasId == a.PersonAliasId )
                                 .Any( pd => pd.NotificationsEnabled )
                         ),
                        a.Status,
                        a.StatusNote,
                        a.OpenedClient,
                        a.OpenedDateTime
                    } ).ToList();

                Recipients = recipientList.Select( recipient => new Recipient( recipient.Person, recipient.PersonHasSMS, recipient.HasPersonalDevice, recipient.Status, recipient.StatusNote, recipient.OpenedClient, recipient.OpenedDateTime ) ).ToList();
            }
            else
            {
                communication = new Rock.Model.Communication() { Status = CommunicationStatus.Transient };
                communication.SenderPersonAliasId = CurrentPersonAliasId;
                communication.EnabledLavaCommands = GetAttributeValue( "EnabledLavaCommands" );
                lTitle.Text = "Push Notification".FormatAsHtmlTitle();

                int? personId = PageParameter( "Person" ).AsIntegerOrNull();
                if ( personId.HasValue )
                {
                    communication.IsBulkCommunication = false;
                    var context = new RockContext();
                    var person = new PersonService( context ).Get( personId.Value );
                    if ( person != null )
                    {
                        var HasPersonalDevice = new PersonalDeviceService( context ).Queryable()
                            .Where( pd => pd.PersonAliasId.HasValue && pd.PersonAliasId == person.PrimaryAliasId && pd.NotificationsEnabled ).Any();
                        Recipients.Add( new Recipient( person, person.PhoneNumbers.Any( p => p.IsMessagingEnabled ), HasPersonalDevice, CommunicationRecipientStatus.Pending, string.Empty, string.Empty, null ) );
                    }
                }
            }

            CommunicationId = communication.Id;

            BindMediums();

            CommunicationData = new CommunicationDetails();
            CommunicationDetails.Copy( communication, CommunicationData );
            CommunicationData.EmailAttachmentBinaryFileIds = communication.EmailAttachmentBinaryFileIds;

            var template = communication.CommunicationTemplate;

            if ( template == null && !string.IsNullOrWhiteSpace( GetAttributeValue( "DefaultTemplate" ) ) )
            {
                template = new CommunicationTemplateService( new RockContext() ).Get( GetAttributeValue( "DefaultTemplate" ).AsGuid() );
            }

            // If a template guid was passed in, it overrides any default template.
            string templateGuid = PageParameter( "templateGuid" );
            if ( !string.IsNullOrEmpty( templateGuid ) )
            {
                var guid = new Guid( templateGuid );
                template = new CommunicationTemplateService( new RockContext() ).Queryable().Where( t => t.Guid == guid ).FirstOrDefault();
            }

            if ( template != null )
            {
                foreach ( ListItem item in ddlTemplate.Items )
                {
                    if ( item.Value == template.Id.ToString() )
                    {
                        item.Selected = true;
                        if ( communication.Status == CommunicationStatus.Transient )
                        {
                            GetTemplateData( template.Id, false );
                        }
                    }
                    else
                    {
                        item.Selected = false;
                    }
                }
            }

            MediumControl control = LoadMediumControl( true );
            InitializeControl( control );

            dtpFutureSend.SelectedDateTime = communication.FutureSendDateTime;

            ShowStatus( communication );
            ShowActions( communication );
        }

        /// <summary>
        /// Binds the mediums.
        /// </summary>
        private void BindMediums()
        {
            var mediums = new Dictionary<int, string>();
            var medium = MediumContainer.Instance.Components.Values.Where( m => m.Value.TypeName == "Rock.Communication.Medium.PushNotification" ).First();
            mediums.Add( medium.Value.EntityType.Id, medium.Metadata.ComponentName );
            MediumEntityTypeId = medium.Value.EntityType.Id;

            LoadTemplates();

            divMediums.Visible = false;

            rptMediums.DataSource = mediums;
            rptMediums.DataBind();
        }

        private void LoadTemplates()
        {
            bool visible = false;

            string prevSelection = ddlTemplate.SelectedValue;

            ddlTemplate.Items.Clear();
            ddlTemplate.Items.Add( new ListItem( string.Empty, string.Empty ) );

            if ( MediumEntityTypeId.HasValue )
            {
                var medium = MediumContainer.GetComponentByEntityTypeId( MediumEntityTypeId );
                if ( medium != null )
                {
                    foreach ( var template in new CommunicationTemplateService( new RockContext() )
                        .Queryable().AsNoTracking()
                        .Where(a => a.IsActive )
                        .OrderBy( t => t.Name ) )
                    {
                        if ( template.IsAuthorized( Authorization.VIEW, CurrentPerson ) )
                        {
                            visible = true;
                            var li = new ListItem( template.Name, template.Id.ToString() );
                            li.Selected = template.Id.ToString() == prevSelection;
                            ddlTemplate.Items.Add( li );
                        }
                    }
                }
            }

            ddlTemplate.Visible = _fullMode && visible;
        }

        /// <summary>
        /// Binds the recipients.
        /// </summary>
        private void BindRecipients()
        {
            int recipientCount = Recipients.Count();
            lNumRecipients.Text = recipientCount.ToString( "N0" ) +
                ( recipientCount == 1 ? " Person" : " People" );

            ppAddPerson.PersonId = Rock.Constants.None.Id;
            ppAddPerson.PersonName = "Add Person";

            int displayCount = int.MaxValue;

            if ( !ShowAllRecipients )
            {
                int.TryParse( GetAttributeValue( "DisplayCount" ), out displayCount );
            }

            if ( displayCount > 0 && displayCount < Recipients.Count )
            {
                rptRecipients.DataSource = Recipients.Take( displayCount ).ToList();
                lbShowAllRecipients.Visible = true;
            }
            else
            {
                rptRecipients.DataSource = Recipients.ToList();
                lbShowAllRecipients.Visible = false;
            }

            lbRemoveAllRecipients.Visible = _fullMode && Recipients.Where( r => r.Status == CommunicationRecipientStatus.Pending ).Any();

            rptRecipients.DataBind();

            StringBuilder rStatus = new StringBuilder();

            CheckApprovalRequired( Recipients.Count );
        }


        /// <summary>
        /// Binds the topics.
        /// </summary>
        private void BindTopics()
        {
            var topic = GetTopics().Select( t => new
            {
                t.Title,
                Id = (Guid?)t.Id
            } ).ToList();
            topic.Insert( 0, new { Title = "Select One", Id = (Guid?)null } );

            rddlTopics.DataSource = topic;
            rddlTopics.DataTextField = "Title";
            rddlTopics.DataValueField = "Id";
            rddlTopics.DataBind();
        }



        /// <summary>
        /// Shows the medium.
        /// </summary>
        private MediumControl LoadMediumControl( bool setData )
        {
            if ( setData )
            {
                phContent.Controls.Clear();
            }

            // The component to load control for
            MediumComponent component = null;
            string mediumName = string.Empty;

            // Get the current medium type
            EntityTypeCache entityType = null;
            if ( MediumEntityTypeId.HasValue )
            {
                entityType = EntityTypeCache.Get( MediumEntityTypeId.Value );
            }

            foreach ( var serviceEntry in MediumContainer.Instance.Components )
            {
                var mediumComponent = serviceEntry.Value;

                // Default to first component
                if ( component == null )
                {
                    component = mediumComponent.Value;
                    mediumName = mediumComponent.Metadata.ComponentName + " ";
                }

                // If invalid entity type, exit (and use first component found)
                if ( entityType == null )
                {
                    break;
                }
                else if ( entityType.Id == mediumComponent.Value.EntityType.Id )
                {
                    component = mediumComponent.Value;
                    mediumName = mediumComponent.Metadata.ComponentName + " ";
                    break;
                }
            }

            if ( component != null )
            {
                var mediumControl = component.GetControl( !_fullMode );
                if ( mediumControl is Rock.Web.UI.Controls.Communication.Email )
                {
                    ( ( Rock.Web.UI.Controls.Communication.Email ) mediumControl ).AllowCcBcc = GetAttributeValue( "AllowCcBcc" ).AsBoolean();
                }
                else if ( mediumControl is Rock.Web.UI.Controls.Communication.Sms )
                {
                    ( ( Rock.Web.UI.Controls.Communication.Sms ) mediumControl ).SelectedNumbers = GetAttributeValue( "AllowedSMSNumbers" ).SplitDelimitedValues( true ).AsGuidList();
                }
                mediumControl.ID = "commControl";
                mediumControl.IsTemplate = false;
                mediumControl.AdditionalMergeFields = this.AdditionalMergeFields.ToList();
                mediumControl.ValidationGroup = btnSubmit.ValidationGroup;
                if ( !GetAttributeValue( "ShowAttachmentUploader" ).AsBoolean() )
                {
                    var fuAttachments = mediumControl.FindControl( "fuAttachments_commControl" );
                    if ( fuAttachments != null )
                    {
                        fuAttachments.Visible = false;
                    }
                }

                // if this is an email with an HTML control and there are block settings to provide updated content directories set them
                if ( mediumControl is Rock.Web.UI.Controls.Communication.Email )
                {
                    var htmlControl = ( HtmlEditor ) mediumControl.FindControl( "htmlMessage_commControl" );

                    if ( htmlControl != null )
                    {
                        if ( GetAttributeValue( "DocumentRootFolder" ).IsNotNullOrWhiteSpace() )
                        {
                            htmlControl.DocumentFolderRoot = GetAttributeValue( "DocumentRootFolder" );
                        }

                        if ( GetAttributeValue( "ImageRootFolder" ).IsNotNullOrWhiteSpace() )
                        {
                            htmlControl.ImageFolderRoot = GetAttributeValue( "ImageRootFolder" );
                        }

                        if ( GetAttributeValue( "UserSpecificFolders" ).AsBooleanOrNull().HasValue )
                        {
                            htmlControl.UserSpecificRoot = GetAttributeValue( "UserSpecificFolders" ).AsBoolean();
                        }
                    }
                }

                phContent.Controls.Add( mediumControl );

                if ( setData )
                {
                    mediumControl.SetFromCommunication( CommunicationData );
                }

                // Set the medium in case it wasn't already set or the previous component type was not found
                MediumEntityTypeId = component.EntityType.Id;

                if ( component.Transport == null || !component.Transport.IsActive )
                {
                    nbInvalidTransport.Text = string.Format( "The {0}medium does not have an active transport configured. The communication will not be delivered until the transport is configured correctly.", mediumName );
                    nbInvalidTransport.Visible = true;
                }
                else
                {
                    nbInvalidTransport.Visible = false;
                }

                // Hide the Sound checkbox
                if ( mediumControl.FindControl( string.Format( "cbSound_{0}", mediumControl.ID ) ) != null )
                {
                    mediumControl.FindControl( string.Format( "cbSound_{0}", mediumControl.ID ) ).Visible = false;
                }

                // Make the Title requred
                if ( mediumControl.FindControl( string.Format( "tbTextTitle_{0}", mediumControl.ID ) ) != null )
                {
                    ( ( RockTextBox ) mediumControl.FindControl( string.Format( "tbTextTitle_{0}", mediumControl.ID ) ) ).Required = true;
                }

                

                return mediumControl;
            }

            return null;
        }

        /// <summary>
        /// Initializes the control with current persons information if this is first time that this medium is being viewed
        /// </summary>
        /// <param name="control">The control.</param>
        private void InitializeControl( MediumControl control )
        {
            if ( MediumEntityTypeId.HasValue && !ViewedEntityTypes.Contains( MediumEntityTypeId.Value ) )
            {
                if ( control != null && CurrentPerson != null )
                {
                    control.InitializeFromSender( CurrentPerson );
                }

                ViewedEntityTypes.Add( MediumEntityTypeId.Value );
            }
        }

        private MediumControl GetMediumControl()
        {
            if ( phContent.Controls.Count == 1 )
            {
                return phContent.Controls[0] as MediumControl;
            }
            return null;
        }

        /// <summary>
        /// Gets the medium data.
        /// </summary>
        private void GetMediumData()
        {
            var mediumControl = GetMediumControl();
            if ( mediumControl != null )
            {
                // If using simple mode, the control should be re-initialized from sender since sender fields 
                // are not presented for editing and user shouldn't be able to change them
                if ( !_fullMode && CurrentPerson != null )
                {
                    mediumControl.InitializeFromSender( CurrentPerson );
                }

                mediumControl.UpdateCommunication( CommunicationData );
            }
        }

        private void GetTemplateData( int templateId, bool loadControl = true )
        {
            var template = new CommunicationTemplateService( new RockContext() ).Get( templateId );
            if ( template != null )
            {
                // save what was entered for FromEmail and FromName in case the template blanks it out
                var enteredFromEmail = CommunicationData.FromEmail;
                var enteredFromName = CommunicationData.FromName;

                // copy all communication details from the Template to CommunicationData
                CommunicationDetails.Copy( template, CommunicationData );
                CommunicationData.FromName = template.FromName.ResolveMergeFields( Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson ) );
                CommunicationData.FromEmail = template.FromEmail.ResolveMergeFields( Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson ) );

                // if the FromName was cleared by the template, use the one that was there before the template was changed (similar logic to CommunicationEntryWizard)
                // Otherwise, if the template does have a FromName, we want to template's FromName to overwrite it (which CommunicationDetails.Copy already did)
                if ( CommunicationData.FromName.IsNullOrWhiteSpace() )
                {
                    CommunicationData.FromName = enteredFromName;
                }

                // if the FromEmail was cleared by the template, use the one that was there before the template was changed (similar logic to CommunicationEntryWizard)
                // Otherwise, if the template does have a FromEmail, we want to template's fromemail to overwrite it (which CommunicationDetails.Copy already did)
                if ( CommunicationData.FromEmail.IsNullOrWhiteSpace() )
                {
                    CommunicationData.FromEmail = enteredFromEmail;
                }

                CommunicationData.EmailAttachmentBinaryFileIds = template.EmailAttachmentBinaryFileIds;

                if ( loadControl )
                {
                    var mediumControl = LoadMediumControl( true );
                }
            }
        }

        private List<Topic> GetTopics()
        {
            // Load the topics using the Push Notifications Transport
            var transport = TransportContainer.Instance.Components.FirstOrDefault( t => t.Value.Value.TypeGuid == GetAttributeValue( "Transport" ).AsGuidOrNull() ).Value.Value;
            if ( transport != null )
            {
                transport.LoadAttributes();

                var client = new RestClient( transport.GetAttributeValue( "APIEndpoint" ) );
                var topicRequest = new RestRequest( "topics", Method.GET );
                topicRequest.AddHeader( "Content-Type", "application/json" );
                topicRequest.AddHeader( "Authorization", "Bearer " + Encryption.DecryptString( transport.GetAttributeValue( "JWTToken" ) ) );
                topicRequest.RequestFormat = DataFormat.Json;

                JsonSerializerSettings serializerSettings = new JsonSerializerSettings
                {
                    ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                    DateTimeZoneHandling = DateTimeZoneHandling.Local
                };

                var response = client.Get( topicRequest );

                var topics = JsonConvert.DeserializeObject<List<Topic>>( JObject.Parse( response.Content )["_embedded"]["topics"].ToString(), serializerSettings );

                return topics.Where( t => t.AppKey == transport.GetAttributeValue( "AppKey" ) ).ToList();
            }
            return null;
        }

        /// <summary>
        /// Shows the actions.
        /// </summary>
        /// <param name="communication">The communication.</param>
        private void ShowActions( Rock.Model.Communication communication )
        {
            // Determine if user is allowed to save changes, if not, disable 
            // submit and save buttons 
            if ( IsUserAuthorized( "Approve" ) ||
                ( CurrentPersonAliasId.HasValue && CurrentPersonAliasId == communication.SenderPersonAliasId ) ||
                IsUserAuthorized( Authorization.EDIT ) )
            {
                btnSubmit.Enabled = true;
                btnSave.Enabled = true && _fullMode;
            }
            else
            {
                btnSubmit.Enabled = false;
                btnSave.Enabled = false;
            }

            if ( _editingApproved && communication.Status == CommunicationStatus.PendingApproval )
            {
                btnSubmit.Text = "Save";
                btnSave.Visible = false;
                btnCancel.Visible = true;
            }
            else
            {
                btnSubmit.Text = "Submit";
                btnSave.Visible = true && _fullMode;
                btnCancel.Visible = false;
            }
        }

        /// <summary>
        /// Determines whether approval is required, and sets the submit button text appropriately
        /// </summary>
        /// <param name="communication">The communication.</param>
        /// <returns>
        ///   <c>true</c> if [is approval required] [the specified communication]; otherwise, <c>false</c>.
        /// </returns>
        private bool CheckApprovalRequired( int numberOfRecipients )
        {
            int maxRecipients = int.MaxValue;
            int.TryParse( GetAttributeValue( "MaximumRecipients" ), out maxRecipients );
            bool approvalRequired = numberOfRecipients > maxRecipients;

            if ( _editingApproved )
            {
                btnSubmit.Text = "Save";
            }
            else
            {
                btnSubmit.Text = ( approvalRequired && !IsUserAuthorized( "Approve" ) ? "Submit" : "Send" ) + " Communication";
            }

            return approvalRequired;
        }

        /// <summary>
        /// Updates a communication model with the user-entered values
        /// </summary>
        /// <param name="communicationService">The service.</param>
        /// <returns></returns>
        private Rock.Model.Communication UpdateCommunication( RockContext rockContext )
        {
            var communicationService = new CommunicationService( rockContext );
            var communicationAttachmentService = new CommunicationAttachmentService( rockContext );
            var communicationRecipientService = new CommunicationRecipientService( rockContext );

            Rock.Model.Communication communication = null;
            IQueryable<CommunicationRecipient> qryRecipients = null;

            if ( CommunicationId.HasValue && CommunicationId.Value != 0 )
            {
                communication = communicationService.Get( CommunicationId.Value );
            }

            if ( communication != null )
            {
                // Remove any deleted recipients
                HashSet<int> personIdHash = new HashSet<int>( Recipients.Select( a => a.PersonId ) );
                qryRecipients = communication.GetRecipientsQry( rockContext );

                foreach ( var item in qryRecipients.Select( a => new
                {
                    Id = a.Id,
                    PersonId = a.PersonAlias.PersonId
                } ) )
                {
                    if ( !personIdHash.Contains( item.PersonId ) )
                    {
                        var recipient = qryRecipients.Where( a => a.Id == item.Id ).FirstOrDefault();
                        communicationRecipientService.Delete( recipient );
                        communication.Recipients.Remove( recipient );
                    }
                }
            }

            if ( communication == null )
            {
                communication = new Rock.Model.Communication();
                communication.Status = CommunicationStatus.Transient;
                communication.SenderPersonAliasId = CurrentPersonAliasId;
                communicationService.Add( communication );
            }

            communication.EnabledLavaCommands = GetAttributeValue( "EnabledLavaCommands" );

            if ( qryRecipients == null )
            {
                qryRecipients = communication.GetRecipientsQry( rockContext );
            }

            // Add any new recipients
            HashSet<int> communicationPersonIdHash = new HashSet<int>( qryRecipients.Select( a => a.PersonAlias.PersonId ) );
            foreach ( var recipient in Recipients )
            {
                if ( !communicationPersonIdHash.Contains( recipient.PersonId ) )
                {
                    var person = new PersonService( rockContext ).Get( recipient.PersonId );
                    if ( person != null )
                    {
                        var communicationRecipient = new CommunicationRecipient();
                        communicationRecipient.PersonAlias = person.PrimaryAlias;
                        communication.Recipients.Add( communicationRecipient );
                    }
                }
            }

            communication.IsBulkCommunication = false;
            var medium = MediumContainer.GetComponentByEntityTypeId( MediumEntityTypeId );
            if ( medium != null )
            {
                communication.CommunicationType = medium.CommunicationType;
            }

            communication.CommunicationTemplateId = ddlTemplate.SelectedValue.AsIntegerOrNull();

            GetMediumData();

            foreach( var recipient in communication.Recipients )
            {
                recipient.MediumEntityTypeId = MediumEntityTypeId;
            }

            CommunicationDetails.Copy( CommunicationData, communication );

            // delete any attachments that are no longer included
            foreach ( var attachment in communication.Attachments.Where( a => !CommunicationData.EmailAttachmentBinaryFileIds.Contains( a.BinaryFileId ) ).ToList() )
            {
                communication.Attachments.Remove( attachment );
                communicationAttachmentService.Delete( attachment );
            }

            // add any new attachments that were added
            foreach ( var attachmentBinaryFileId in CommunicationData.EmailAttachmentBinaryFileIds.Where( a => !communication.Attachments.Any( x => x.BinaryFileId == a ) ) )
            {
                communication.AddAttachment( new CommunicationAttachment { BinaryFileId = attachmentBinaryFileId }, CommunicationType.Email );
            }

            DateTime? futureSendDate = dtpFutureSend.SelectedDateTime;
            if ( futureSendDate.HasValue && futureSendDate.Value.CompareTo( RockDateTime.Now ) > 0 )
            {
                communication.FutureSendDateTime = futureSendDate;
            }
            else
            {
                communication.FutureSendDateTime = null;
            }

            return communication;
        }

        private void ValidateFutureDelayDateTime()
        {
            DateTime? futureSendDate = dtpFutureSend.SelectedDateTime;
            TimeSpan? futureTime = dtpFutureSend.SelectedTime;

            if ( ( futureTime.HasValue && !futureSendDate.HasValue ) || ( !futureTime.HasValue && futureSendDate.HasValue ) )
            {
                cvDelayDateTime.IsValid = false;
                cvDelayDateTime.ErrorMessage = "The Delay Send Until value requires a future date and time.";
                return;
            }

            if ( futureSendDate.HasValue && futureSendDate.Value.CompareTo( RockDateTime.Now ) < 0 )
            {
                cvDelayDateTime.IsValid = false;
                cvDelayDateTime.ErrorMessage = "The Delay Send Until value must be a future date/time";
                return;
            }
        }

        private void ShowStatus( Rock.Model.Communication communication )
        {
            var status = communication != null ? communication.Status : CommunicationStatus.Draft;
            switch ( status )
            {
                case CommunicationStatus.Transient:
                case CommunicationStatus.Draft:
                    {
                        hlStatus.Text = "Draft";
                        hlStatus.LabelType = LabelType.Default;
                        break;
                    }
                case CommunicationStatus.PendingApproval:
                    {
                        hlStatus.Text = "Pending Approval";
                        hlStatus.LabelType = LabelType.Warning;
                        break;
                    }
                case CommunicationStatus.Approved:
                    {
                        hlStatus.Text = "Approved";
                        hlStatus.LabelType = LabelType.Success;
                        break;
                    }
                case CommunicationStatus.Denied:
                    {
                        hlStatus.Text = "Denied";
                        hlStatus.LabelType = LabelType.Danger;
                        break;
                    }
            }
        }

        /// <summary>
        /// Shows the result.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="communication">The communication.</param>
        private void ShowResult( string message, Rock.Model.Communication communication, NotificationBoxType notificationType = NotificationBoxType.Success)
        {
            ShowStatus( communication );

            pnlEdit.Visible = false;

            nbResult.Text = message;
            
            nbResult.NotificationBoxType = notificationType;

            CurrentPageReference.Parameters.AddOrReplace( "CommunicationId", communication.Id.ToString() );
            hlViewCommunication.NavigateUrl = CurrentPageReference.BuildUrl();

            // only show the Link if there is a CommunicationDetail block type on this page
            hlViewCommunication.Visible = this.PageCache.Blocks.Any( a => a.BlockType.Guid == Rock.SystemGuid.BlockType.COMMUNICATION_DETAIL.AsGuid() );

            pnlResult.Visible = true;

            ScriptManager.RegisterStartupScript(
                Page,
                GetType(),
                "scrollToResults",
                "scrollToResults();",
                true );

        }
        private void SendPushNotificationToTopics( Rock.Model.Communication communication, List<Topic> topics )
        {
            var transport = TransportContainer.Instance.Components.FirstOrDefault( t => t.Value.Value.TypeGuid == GetAttributeValue( "Transport" ).AsGuidOrNull() ).Value.Value;
            if ( transport != null )
            {
                JsonSerializerSettings serializerSettings = new JsonSerializerSettings
                {
                    ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                    DateTimeZoneHandling = DateTimeZoneHandling.Local
                };

                transport.LoadAttributes();

                var client = new RestClient( transport.GetAttributeValue( "APIEndpoint" ) );

                string publicAppRoot = GlobalAttributesCache.Get().GetValue( "PublicApplicationRoot" ).EnsureTrailingForwardslash();
                var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( null );

                var message = ResolveText( communication.PushMessage, communication.EnabledLavaCommands, mergeFields, publicAppRoot );
                var title = ResolveText( communication.PushTitle, communication.EnabledLavaCommands, mergeFields, publicAppRoot );

                var notification = new com.subsplash.Model.Notification();
                notification.AppKey = transport.GetAttributeValue( "AppKey" );
                notification.Body = title;
                notification.Title = title;
                notification.AdditionalDescription = message;
                notification.PublishedAt = communication.FutureSendDateTime ?? RockDateTime.Now;
                notification.Embedded = new NotificationEmbedded();
                notification.Embedded.Topics = topics;

                var sendPush = new RestRequest( "notifications", Method.POST );
                sendPush.AddHeader( "Content-Type", "application/json" );
                sendPush.AddHeader( "Authorization", "Bearer " + Encryption.DecryptString( transport.GetAttributeValue( "JWTToken" ) ) );
                sendPush.RequestFormat = DataFormat.Json;
                sendPush.AddParameter( "application/json", JsonConvert.SerializeObject( notification, serializerSettings ), ParameterType.RequestBody );

                var response = client.Execute( sendPush );

                
                notification = JsonConvert.DeserializeObject<com.subsplash.Model.Notification>(
                    response.Content,
                    new JsonSerializerSettings
                    {
                        ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                        DateTimeZoneHandling = DateTimeZoneHandling.Local
                    }
                );

                // Set the SendDateTime and ForeignGuid
                communication.SendDateTime = notification.PublishedAt;
                communication.ForeignGuid = notification.Id;
            }
        }


        /// <summary>
        /// Resolves the text.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <param name="person">The person.</param>
        /// <param name="enabledLavaCommands">The enabled lava commands.</param>
        /// <param name="mergeFields">The merge fields.</param>
        /// <param name="appRoot">The application root.</param>
        /// <param name="themeRoot">The theme root.</param>
        /// <returns></returns>
        public virtual string ResolveText( string content, string enabledLavaCommands, Dictionary<string, object> mergeFields, string appRoot = "", string themeRoot = "" )
        {
            string value = content.ResolveMergeFields( mergeFields, null, enabledLavaCommands );
            value = value.ReplaceWordChars();

            if ( themeRoot.IsNotNullOrWhiteSpace() )
            {
                value = value.Replace( "~~/", themeRoot );
            }

            if ( appRoot.IsNotNullOrWhiteSpace() )
            {
                value = value.Replace( "~/", appRoot );
                value = value.Replace( @" src=""/", @" src=""" + appRoot );
                value = value.Replace( @" src='/", @" src='" + appRoot );
                value = value.Replace( @" href=""/", @" href=""" + appRoot );
                value = value.Replace( @" href='/", @" href='" + appRoot );
            }

            return value;
        }
        #endregion

        #region Helper Classes

        /// <summary>
        /// Helper class used to maintain state of recipients
        /// </summary>
        [Serializable]
        protected class Recipient
        {
            /// <summary>
            /// Gets or sets the person id.
            /// </summary>
            /// <value>
            /// The person id.
            /// </value>
            public int PersonId { get; set; }

            /// <summary>
            /// Gets or sets the name of the person.
            /// </summary>
            /// <value>
            /// The name of the person.
            /// </value>
            public string PersonName { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether this person is deceased.
            /// </summary>
            /// <value>
            /// <c>true</c> if this instance is deceased; otherwise, <c>false</c>.
            /// </value>
            public bool IsDeceased { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether [has SMS number].
            /// </summary>
            /// <value>
            ///   <c>true</c> if [has SMS number]; otherwise, <c>false</c>.
            /// </value>
            public bool HasSmsNumber { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether [has notifications enabled].
            /// </summary>
            /// <value>
            ///   <c>true</c> if [has notifications enabled]; otherwise, <c>false</c>.
            /// </value>
            public bool HasNotificationsEnabled { get; set; }

            /// <summary>
            /// Gets or sets the email.
            /// </summary>
            /// <value>
            /// The email.
            /// </value>
            public string Email { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether email is active.
            /// </summary>
            /// <value>
            ///  <c>true</c> if email id active; otherwise, <c>false</c>.
            /// </value>
            public bool IsEmailActive { get; set; }

            /// <summary>
            /// Gets or sets the email note.
            /// </summary>
            /// <value>
            /// The email note.
            /// </value>
            public string EmailNote { get; set; }

            /// <summary>
            /// Gets or sets the email preference.
            /// </summary>
            /// <value>
            /// The email preference.
            /// </value>
            public EmailPreference EmailPreference { get; set; }

            /// <summary>
            /// Gets or sets the status.
            /// </summary>
            /// <value>
            /// The status.
            /// </value>
            public CommunicationRecipientStatus Status { get; set; }

            /// <summary>
            /// Gets or sets the status note.
            /// </summary>
            /// <value>
            /// The status note.
            /// </value>
            public string StatusNote { get; set; }

            /// <summary>
            /// Gets or sets the client the email was opened on.
            /// </summary>
            /// <value>
            /// The opened email client.
            /// </value>
            public string OpenedClient { get; set; }

            /// <summary>
            /// Gets or sets the date/time the email was opened on.
            /// </summary>
            /// <value>
            /// The date/time the email was opened.
            /// </value>
            public DateTime? OpenedDateTime { get; set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="Recipient" /> class.
            /// </summary>
            /// <param name="personId">The person id.</param>
            /// <param name="personName">Name of the person.</param>
            /// <param name="status">The status.</param>
            public Recipient( Person person, bool personHasSMS, bool personHasNotificationsEnabled, CommunicationRecipientStatus status, string statusNote = "", string openedClient = "", DateTime? openedDateTime = null )
            {
                PersonId = person.Id;
                PersonName = person.FullName;
                IsDeceased = person.IsDeceased;
                HasSmsNumber = personHasSMS;
                HasNotificationsEnabled = personHasNotificationsEnabled;
                Email = person.Email;
                IsEmailActive = person.IsEmailActive;
                EmailNote = person.EmailNote;
                EmailPreference = person.EmailPreference;
                Status = status;
                StatusNote = statusNote;
                OpenedClient = openedClient;
                OpenedDateTime = openedDateTime;
            }

            public static string PreferenceMessage( Recipient recipient )
            {
                switch ( recipient.EmailPreference )
                {
                    case EmailPreference.DoNotEmail:
                        return "Email Preference is set to 'Do Not Email!'";
                    case EmailPreference.NoMassEmails:
                        return "Email Preference is set to 'No Mass Emails!'";
                }

                return string.Empty;
            }
        }

        #endregion


        protected void bgRecipientOptions_SelectedIndexChanged( object sender, EventArgs e )
        {
            pnlRecipients.Visible = false;
            pnlTopic.Visible = false;

            if ( bgRecipientOptions.SelectedValue == "Individual" )
            {
                pnlRecipients.Visible = true;
            }
            else if ( bgRecipientOptions.SelectedValue == "Topic" )
            {
                pnlTopic.Visible = true;
            }
        }

        protected void rddlTopics_SelectedIndexChanged( object sender, EventArgs e )
        {
            var topic = GetTopics().FirstOrDefault( t => t.Id == rddlTopics.SelectedValueAsGuid() );
            var count = topic != null ? topic.NumSubscribers : 0;
            lNumTopicSubscribers.Text = String.Format( "{0:#,0}", count) + ( count == 1 ? " Subscriber" : " Subscribers" );
        }
    }
}
