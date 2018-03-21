
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json;
using org.secc.Trak1.Helpers;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
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
                        errorMessages.Add( "No valid background check exists for this workflow" );
                        return false;
                    }
                    var transactionId = backgroundCheck.RequestId;

                    var statusURL = provider.GetAttributeValue( "StatusURL" );
                    var subscriberCode = Encryption.DecryptString( provider.GetAttributeValue( "SubscriberCode" ) );
                    var companyCode = Encryption.DecryptString( provider.GetAttributeValue( "CompanyCode" ) );

                    Trak1ReportStatus status = null;
                    var resource = string.Format( "/{0}/{1}/{2}", subscriberCode, companyCode, transactionId );

                    using ( var client = new HttpClient( new HttpClientHandler() ) )
                    {
                        client.BaseAddress = new Uri( statusURL + resource );
                        var clientResponse = client.GetAsync( "" ).Result.Content.ReadAsStringAsync().Result;
                        status = JsonConvert.DeserializeObject<Trak1ReportStatus>( clientResponse );
                    }

                    SetWorkflowAttributeValue( action, GetAttributeValue( action, "ReportStatus" ).AsGuid(), status.ReportStatus );
                    backgroundCheck.Status = status.ReportStatus;

                    SetWorkflowAttributeValue( action, GetAttributeValue( action, "HitColor" ).AsGuid(), status.HitColor );
                    backgroundCheck.RecordFound = status.HitColor == "Green";

                    var reportURL = provider.GetAttributeValue( "ReportURL" );
                    Trak1Report response = null;
                    using ( var client = new HttpClient( new HttpClientHandler() ) )
                    {
                        client.BaseAddress = new Uri( reportURL + resource );
                        var clientResponse = client.GetAsync( "" ).Result.Content.ReadAsStringAsync().Result;
                        response = JsonConvert.DeserializeObject<Trak1Report>( clientResponse );
                    }

                    SetWorkflowAttributeValue( action, GetAttributeValue( action, "ReportUrl" ).AsGuid(), response.ReportUrl );
                    backgroundCheck.ResponseDate = Rock.RockDateTime.Now;
                    backgroundCheck.ResponseData = response.ReportUrl;

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