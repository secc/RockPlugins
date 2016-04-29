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
    [CommunicationTemplateField("Template", "Communication template to use.")]

    class EmailRegistration : ActionComponent
    {
        public override bool Execute( RockContext rockContext, WorkflowAction action, object entity, out List<string> errorMessages )
        {
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

            var communicationService = new CommunicationService( rockContext );
            var recipientService = new CommunicationRecipientService( rockContext );

            IQueryable<CommunicationRecipient> qryRecipients = null;

            var template = new CommunicationTemplateService( rockContext ).Get( GetActionAttributeValue( action, "Template" ).AsGuid() );

            if ( template == null )
            {
                errorMessages.Add( "Could not find template" );
                return false;
            }

            Communication communication = new Rock.Model.Communication();
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
                communication.Subject = template.Subject.ResolveMergeFields(pdfWorkflowObject.MergeObjects);
                communication.MediumData = template.MediumData;
                if ( !communication.MediumData.ContainsKey( "Subject" ) )
                {
                    communication.MediumData.Add( "Subject", communication.Subject );
                }

                if ( communication.MediumData.ContainsKey( "HtmlMessage" ) )
                {
                    communication.MediumData["HtmlMessage"] = communication.MediumData["HtmlMessage"].ResolveMergeFields( pdfWorkflowObject.MergeObjects );
                }

                if ( communication.MediumData.ContainsKey( "TextMessage" ) )
                {
                    communication.MediumData["TextMessage"] = communication.MediumData["TextMessage"].ResolveMergeFields( pdfWorkflowObject.MergeObjects );
                }

                communication.Status = CommunicationStatus.Approved;
                communication.ReviewedDateTime = RockDateTime.Now;

                int managerAliasId = 1;
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

            return true;
        }

        private void SendEmail( Registration registration, PDFWorkflowObject pdfWorkflowObject, WorkflowAction action, RockContext rockContext )
        {
            
        }
    }
}
