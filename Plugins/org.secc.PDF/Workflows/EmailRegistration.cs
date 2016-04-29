using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Workflow;
using Rock.Attribute;
using Rock.Web.Cache;

namespace org.secc.PDF
{
    [ActionCategory( "PDF" )]
    [Description( "Emails form to the person who completed the registration." )]
    [Export( typeof( Rock.Workflow.ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Email Registration" )]
    [TextField( "Subject", "Email Subject, Lava Enabled", true, "Registration for {{Person.FullName}}", order: 0 )]
    [CodeEditorField( "Body", "Email Body, Lava Enabled", Rock.Web.UI.Controls.CodeEditorMode.Text, order: 1 )]
    [TextField( "From Name", "Name the email will be sent from.", false, "", order: 2 )]
    [TextField( "From Email", "Address the email will be sent from.", false, "", order: 3 )]

    class EmailRegistration : ActionComponent
    {
        public override bool Execute( RockContext rockContext, WorkflowAction action, object entity, out List<string> errorMessages )
        {
            errorMessages = new List<string>();


            errorMessages = new List<string>();

            PDFWorkflowObject pdfWorkflowObject = new PDFWorkflowObject();

            //A PDF merge can enter in two ways, kicked off with trigger or called from a block
            //If it is called from a block we will get our information from a PDFWorkflowObject
            //Otherwise we will need to get our information from the workflow attributes
            if ( entity is PDFWorkflowObject )
            {
                pdfWorkflowObject = Utility.GetPDFFormMergeFromEntity( entity, out errorMessages );
            }
            else
            {
                pdfWorkflowObject = new PDFWorkflowObject( action, rockContext );
            }

            Registration registration;
            int registrationRegistrantId = action.Activity.Workflow.GetAttributeValue( "RegistrationRegistrantId" ).AsInteger();

            var registrationRegistrant = new RegistrationRegistrantService( rockContext ).Get( registrationRegistrantId );
            registration = registrationRegistrant.Registration;

            SendEmail( registration, pdfWorkflowObject, action, rockContext );

            return true;
        }

        private void SendEmail( Registration registration, PDFWorkflowObject pdfWorkflowObject, WorkflowAction action, RockContext rockContext )
        {
            string subject = GetActionAttributeValue( action, "Subject" ).ResolveMergeFields( pdfWorkflowObject.MergeObjects );
            string body = GetActionAttributeValue( action, "Body" ).ResolveMergeFields( pdfWorkflowObject.MergeObjects );

            var communicationService = new CommunicationService( rockContext );
            var recipientService = new CommunicationRecipientService( rockContext );

            Rock.Model.Communication communication = null;
            IQueryable<CommunicationRecipient> qryRecipients = null;

            communication = new Rock.Model.Communication();
            communication.Status = CommunicationStatus.Transient;
            communicationService.Add( communication );

            qryRecipients = communication.GetRecipientsQry( rockContext );

            communication.IsBulkCommunication = false;

            communication.FutureSendDateTime = null;

            if ( communication != null )
            {

                var communicationRecipient = new CommunicationRecipient();
                communicationRecipient.PersonAliasId = registration.PersonAliasId ?? 0;
                communication.Recipients.Add( communicationRecipient );

                communication.MediumEntityTypeId = EntityTypeCache.Read( "Rock.Communication.Medium.Email" ).Id;
                communication.MediumData.Clear();
                communication.Subject = subject;
                communication.MediumData.Add( "TextMessage", body );

                var fromName = GetActionAttributeValue( action, "FromName" );
                if ( string.IsNullOrWhiteSpace( fromName ) )
                {
                    fromName = GlobalAttributesCache.Read( rockContext ).GetValue( "OrganizationName" );
                }
                communication.MediumData.Add( "FromName", fromName );

                var fromEmail = GetActionAttributeValue( action, "FromEmail" );
                if ( string.IsNullOrWhiteSpace( fromEmail ) )
                {
                    fromEmail = GlobalAttributesCache.Read( rockContext ).GetValue( "OrganizationEmail" );
                }
                communication.MediumData.Add( "FromAddress", fromEmail );

                communication.Status = CommunicationStatus.Approved;
                communication.ReviewedDateTime = RockDateTime.Now;

                int managerAliasId=1;
                if ( pdfWorkflowObject.MergeObjects.ContainsKey( "RegistrationInstance" ) )
                {
                    managerAliasId = ( ( RegistrationInstance ) pdfWorkflowObject.MergeObjects["RegistrationInstance"] ).CreatedByPersonAliasId ?? 1;
                }

                communication.ReviewerPersonAliasId = managerAliasId;

                rockContext.SaveChanges();
                var transaction = new Rock.Transactions.SendCommunicationTransaction();
                transaction.CommunicationId = communication.Id;
                Rock.Transactions.RockQueue.TransactionQueue.Enqueue( transaction );
            }
        }
    }
}
