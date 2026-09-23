using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;



namespace RockWeb.Plugins.org_secc.Event
{
    [DisplayName( "Generate Event Pass" )]
    [Category( "SECC > Event" )]
    [Description( "Request an Event Pass " )]

    [TextField( "Block Title",
        Description = "The title for the block instance.",
        IsRequired = false,
        DefaultValue = "Request Event Pass",
        Key = AttributeKeys.BlockTitleKey,
        Order = 0 )]
    [WorkflowTypeField( "Event Pass Worfklow Type",
        Description = "Workflow Type that processes Event Pass request.",
        IsRequired = true,
        Key = AttributeKeys.EventPassWorkflowKey,
        Order = 1 )]
    [AttributeField( "Pass Person Attribute",
        Description = "The Person Attribute that contains the Event Pass.",
        EntityTypeGuid = Rock.SystemGuid.EntityType.PERSON,
        Key = AttributeKeys.PassAttributeKey,
        Order = 2 )]
    [BooleanField( "Contact Fields Editable",
        Description = "Are email and mobile phone numbers editable.",
        DefaultBooleanValue = false,
        Key = AttributeKeys.ContactFieldsEditableKey,
        Order = 3 )]
    public partial class RequestEventPass : RockBlock
    {
        private static class AttributeKeys
        {
            public const string BlockTitleKey = "BlockTitle";
            public const string EventPassWorkflowKey = "EventPassWorkflow";
            public const string PassAttributeKey = "PassAttribute";
            public const string ContactFieldsEditableKey = "ContactFieldsEditable";
        }

        protected Guid? PersonAliasGuid
        {
            get
            {
                return PageParameter( "Person" ).AsGuidOrNull();
            }
        }

        #region Control Methods
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );
        }

        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            nbNotLoggedIn.Visible = false;
            nbMessages.Visible = false;
            if (!IsPostBack)
            {
                lTitle.Text = GetAttributeValue( AttributeKeys.BlockTitleKey );
                // An event pass is one text, so the recurring-program wording would misdescribe it.
                cbSmsConsent.Text = org.secc.Communication.SmsDisclosure.OneTimeConsentText( "your event pass" );
                ProcessRequest();
            }

        }
        #endregion



        #region Internal Methods

        private void GeneratePass()
        {
            var rockContext = new RockContext();
            Person person = GetPerson( rockContext );

            var workflowGuid = GetAttributeValue( AttributeKeys.EventPassWorkflowKey ).AsGuid();
            var workflowAttributes = new Dictionary<string, string>
            {
                { "EmailAddress", tbEmail.Text.Trim() },
                { "PhoneNumber", tbPhone.Text.Trim() },
                { "DeliveryPreference", cblDeliveryMethod.SelectedValue }
            };


            person.LaunchWorkflow( workflowGuid, $"Event Pass - {person.FullName}", workflowAttributes, null );

        }

        private void DownloadPass( Guid binaryFileGuid )
        {
            Response.Redirect( ResolveRockUrl( $"~/GetFile.ashx?Guid={binaryFileGuid}&attachment=true" ), true );
        }

        private Person GetPerson( RockContext rockContext )
        {
            if(PersonAliasGuid.HasValue)
            {
                return new PersonAliasService( rockContext ).GetPerson( PersonAliasGuid.Value );
            }
            else
            {
                return new PersonService(rockContext).Get( CurrentPerson.Guid );
            }
        }

        private void ProcessRequest()
        {
            var rockContext = new RockContext();
            pnlRequest.Visible = false;
            if (CurrentPerson == null)
            {
                nbNotLoggedIn.Visible = true;
                return;
            }

            var attribute = AttributeCache.Get( GetAttributeValue( AttributeKeys.PassAttributeKey ) );
            if (attribute == null)
            {
                if (this.UserCanAdministrate)
                {
                    nbMessages.Title = "Missing Person Attribute";
                    nbMessages.Text = "Event Pass Person Attribute not found.";
                    nbMessages.NotificationBoxType = NotificationBoxType.Danger;
                    nbMessages.Visible = true;
                }
                return;
            }

            var workflow = WorkflowTypeCache.Get( GetAttributeValue( AttributeKeys.EventPassWorkflowKey ) );
            if (workflow == null)
            {
                if (this.UserCanAdministrate)
                {
                    nbMessages.Title = "Missing Workflow";
                    nbMessages.Text = "Event Pass Workflow not found.";
                    nbMessages.NotificationBoxType = NotificationBoxType.Danger;
                    nbMessages.Visible = true;
                }
                return;
            }

            Person person = GetPerson( rockContext );

            person.LoadAttributes( rockContext );
            var passBinaryFileGuid = person.GetAttributeValue( attribute.Key ).AsGuidOrNull();


            if (!passBinaryFileGuid.HasValue)
            {
                LoadRequestForm(person);
            }
            else
            {
                DownloadPass( passBinaryFileGuid.Value );
            }

        }

        private void LoadRequestForm(Person p)
        {
            pnlRequest.Visible = true;
            var contactFieldsAreEditable = GetAttributeValue( AttributeKeys.ContactFieldsEditableKey ).AsBoolean();

            lName.Text = p.FullName;

            if(p.Email.IsNotNullOrWhiteSpace())
            {
                tbEmail.Text = p.Email;
                tbEmail.ReadOnly = !contactFieldsAreEditable;
            }
            else
            {
                tbEmail.ReadOnly = false;
            }



            var mobilePhone = p.GetPhoneNumber( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() );
            if (mobilePhone != null )
            {
                tbPhone.Text = mobilePhone.NumberFormatted;
                tbPhone.ReadOnly = !contactFieldsAreEditable;
            }
            else
            {
                tbPhone.ReadOnly = false;
            }

        }
        #endregion


        protected void lbApplePass_Click( object sender, EventArgs e )
        {
            // Collect every problem so the person fixes them in one pass, not one per submit.
            var missingFields = new List<string>();
            var isSms = cblDeliveryMethod.SelectedValue.Equals( "sms", StringComparison.InvariantCultureIgnoreCase );

            if (cblDeliveryMethod.SelectedValue.Equals( "email", StringComparison.InvariantCultureIgnoreCase ) && tbEmail.Text.IsNullOrWhiteSpace())
            {
                missingFields.Add( "Email Address is required." );
            }

            if (isSms && tbPhone.Text.IsNullOrWhiteSpace())
            {
                missingFields.Add( "Mobile Phone Number is required." );
            }

            // Carrier rule: no text without an affirmative, never-pre-ticked agreement. Enforced
            // only on the SMS path -- an emailed pass needs no SMS consent. Consent is not a
            // missing field, so it is reported separately rather than under that heading.
            var consentMissing = isSms && !cbSmsConsent.Checked;

            if (missingFields.Any() || consentMissing)
            {
                var html = string.Empty;
                if (missingFields.Any())
                {
                    html += "<p><strong>Required Fields Missing</strong></p><ul><li>"
                        + string.Join( "</li><li>", missingFields ) + "</li></ul>";
                }
                if (consentMissing)
                {
                    html += "<p><strong>Consent Required</strong></p>"
                        + "<p>Please check the box agreeing to receive a text message to deliver your pass by text.</p>";
                }

                nbMessages.Title = string.Empty;
                nbMessages.Text = html;
                nbMessages.NotificationBoxType = NotificationBoxType.Validation;
                nbMessages.Visible = true;
                return;
            }

            GeneratePass();
            var messageType = string.Empty;
            switch (cblDeliveryMethod.SelectedValue.ToLower())
            {
                case "emal":
                    messageType = "an email";
                    break;
                case "sms":
                    messageType = "a text message";
                    break;
                default:
                    messageType = $"a {cblDeliveryMethod.SelectedItem.Text.ToLower()}";
                    break;
            }

            nbMessages.Title = "Pass Request Submitted";
            nbMessages.Text = $"<p>Your pass request has been submitted. You will recieve {messageType} with a link to download your pass.";
            nbMessages.NotificationBoxType = NotificationBoxType.Success;
            nbMessages.Visible = true;

        }
    }
}