
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using org.secc.Trak1.Helpers;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Workflow;

namespace org.secc.Trak1.Workflow.Action
{
    /// <summary>
    /// Sends a Background Check Request.
    /// </summary>
    [ActionCategory( "Background Check" )]
    [Description( "Gets the status from Trak-1" )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Trak-1 Status" )]

    [ComponentField( "Rock.Security.BackgroundCheckContainer, Rock", "Background Check Provider", "The Background Check provider to use", false, "", "", 0, "Provider" )]

    [WorkflowAttribute( "Report Status", "Status of the background check request", true, "", "", 1, null,
        new string[] { "Rock.Field.Types.TextFieldType" } )]

    [WorkflowAttribute( "Hit Color", "Hit Color returned from completed background check.", true, "", "", 1, null,
        new string[] { "Rock.Field.Types.TextFieldType" } )]

    [WorkflowAttribute( "Report Url", "Hit Color returned from completed background check.", true, "", "", 1, null,
        new string[] { "Rock.Field.Types.TextFieldType" } )]
    public class Trak1Status : ActionComponent
    {
        /// <summary>
        /// Executes the specified workflow.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="action">The action.</param>
        /// <param name="entity">The entity.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns></returns>
        public override bool Execute( RockContext rockContext, WorkflowAction action, Object entity, out List<string> errorMessages )
        {
            errorMessages = new List<string>();

            string providerGuid = GetAttributeValue( action, "Provider" );
            if ( !string.IsNullOrWhiteSpace( providerGuid ) )
            {
                var provider = BackgroundCheckContainer.GetComponent( providerGuid );
                if ( provider != null )
                {
                    BackgroundCheckService backgroundCheckService = new BackgroundCheckService( rockContext );
                    var backgroundCheck = backgroundCheckService.Queryable().Where( b => b.WorkflowId == action.Activity.Workflow.Id ).FirstOrDefault();
                    if ( backgroundCheck == null )
                    {
                        //If for some reason a background check entity isn't available
                        //Add a log and set the report Status To Error.
                        //This allows the workflow to handle issues gracefully
                        action.AddLogEntry( "No valid background check exists for this workflow", true );
                        SetWorkflowAttributeValue( action, GetAttributeValue( action, "ReportStatus" ).AsGuid(), "Error" );
                        return true;
                    }
                    var transactionId = backgroundCheck.RequestId;
                    if ( transactionId.AsGuid() == Guid.Empty )
                    {
                        //Trak-1 never accepted the request (no transaction id was returned), so
                        //polling would never complete. Set the report Status To Error instead.
                        action.AddLogEntry( "The background check for this workflow has no Trak-1 transaction id", true );
                        SetWorkflowAttributeValue( action, GetAttributeValue( action, "ReportStatus" ).AsGuid(), "Error" );
                        backgroundCheck.Status = "Error";
                        return true;
                    }

                    var statusURL = provider.GetAttributeValue( "StatusURL" );
                    var reportURL = provider.GetAttributeValue( "ReportURL" );
                    var subscriberCode = Encryption.DecryptString( provider.GetAttributeValue( "SubscriberCode" ) );
                    var companyCode = Encryption.DecryptString( provider.GetAttributeValue( "CompanyCode" ) );
                    var resource = string.Format( "/{0}/{1}/{2}", subscriberCode, companyCode, transactionId );

                    Trak1StatusUpdate update;
                    try
                    {
                        // Fetches the status, and the report only once it is complete. Nothing is
                        // written back unless every request succeeds.
                        update = Trak1StatusUpdate.Fetch( Trak1Client.Default, statusURL + resource, reportURL + resource );
                    }
                    catch ( Exception ex )
                    {
                        ExceptionLogService.LogException( ex, null );
                        errorMessages.Add( ex is Trak1Exception ? ex.Message : ex.GetBaseException().Message );
                        return false;
                    }

                    SetWorkflowAttributeValue( action, GetAttributeValue( action, "ReportStatus" ).AsGuid(), update.ReportStatus );
                    backgroundCheck.Status = update.ReportStatus.Left( 25 );

                    SetWorkflowAttributeValue( action, GetAttributeValue( action, "HitColor" ).AsGuid(), update.HitColor );

                    if ( update.IsComplete )
                    {
                        backgroundCheck.RecordFound = update.RecordFound;

                        SetWorkflowAttributeValue( action, GetAttributeValue( action, "ReportUrl" ).AsGuid(), update.ReportUrl );
                        if ( !backgroundCheck.ResponseDate.HasValue || backgroundCheck.ResponseData != update.ReportUrl )
                        {
                            backgroundCheck.ResponseDate = RockDateTime.Now;
                        }
                        backgroundCheck.ResponseData = update.ReportUrl;
                    }

                    return true;
                }
                else
                {
                    errorMessages.Add( "Invalid Background Check Provider!" );
                }
            }
            else
            {
                errorMessages.Add( "Invalid Background Check Provider Guid!" );
            }

            return false;
        }
    }
}