using System;
using System.Collections.Generic;
using System.ComponentModel;
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
                ProcessRequest();
            }

        }
        #endregion

        #region Internal Metods

        private void GeneratePass()
        {
            var workflowGuid = GetAttributeValue( AttributeKeys.EventPassWorkflowKey ).AsGuid();
            var workflowAttributes = new Dictionary<string, string>
            {
                { "EmailAddress", tbEmail.Text.Trim() },
                { "PhoneNumber", tbPhone.Text.Trim() },
                { "DeliveryPreference", cblDeliveryMethod.SelectedValue }
            };


            CurrentPerson.LaunchWorkflow( workflowGuid, $"Event Pass - {CurrentPerson.FullName}", workflowAttributes );

        }

        private void DownloadPass( Guid binaryFileGuid )
        {
            Response.Redirect( ResolveRockUrl( $"~/GetFile.ashx?Guid={binaryFileGuid}&attachment=true" ), true );
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

            var person = new PersonService( rockContext ).Get( CurrentPerson.Guid );
            person.LoadAttributes( rockContext );
            var passBinaryFileGuid = person.GetAttributeValue( attribute.Key ).AsGuidOrNull();


            if (!passBinaryFileGuid.HasValue)
            {
                LoadRequestForm();
            }
            else
            {
                DownloadPass( passBinaryFileGuid.Value );
            }

        }

        private void LoadRequestForm()
        {
            pnlRequest.Visible = true;
            var contactFieldsAreEditable = GetAttributeValue( AttributeKeys.ContactFieldsEditableKey ).AsBoolean();

            lName.Text = CurrentPerson.FullName;

            if(CurrentPerson.Email.IsNotNullOrWhiteSpace())
            {
                tbEmail.Text = CurrentPerson.Email;
                tbEmail.ReadOnly = !contactFieldsAreEditable;
            }
            else
            {
                tbEmail.ReadOnly = false;
            }



            var mobilePhone = CurrentPerson.GetPhoneNumber( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() );
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
            var errorMessage = string.Empty;
            if (cblDeliveryMethod.SelectedValue.Equals( "email", StringComparison.InvariantCultureIgnoreCase ) && tbEmail.Text.IsNullOrWhiteSpace())
            {
                errorMessage = "Email Address is required.";
            }

            if (cblDeliveryMethod.SelectedValue.Equals( "sms", StringComparison.InvariantCultureIgnoreCase ) && tbPhone.Text.IsNullOrWhiteSpace())
            {
                errorMessage = "Mobile Phone Number is required.";
            }

            if (!errorMessage.IsNullOrWhiteSpace())
            {
                nbMessages.Title = "<strong>Required Fields Missing</strong>";
                nbMessages.Text = $"<p>{errorMessage}</p>";
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