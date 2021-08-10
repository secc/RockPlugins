// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License shoud be included with this file.
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
using System.Data;
using System.Linq;
using Quartz;
using Rock;
using Rock.Attribute;
using Rock.Communication;
using Rock.Data;
using Rock.Model;

namespace org.secc.Jobs
{
    [CustomCheckboxListField( "Connection Opportunities", "Select the connection opportunities you would like to include.", "SELECT Guid AS Value, Name AS Text FROM ConnectionOpportunity WHERE IsActive = 1;" )]
    [SystemCommunicationField( "Email", "The email to send to the connectors", true )]
    [BooleanField( "Save Communication History", "Should a record of this communication be saved to the recipient's profile", false, "" )]

    [DisallowConcurrentExecution]
    public class ConnectionsDigest : IJob
    {
        public void Execute( IJobExecutionContext context )
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;
            var rockContext = new RockContext();

            var connectionOpportunities = dataMap.GetString( "ConnectionOpportunities" ).SplitDelimitedValues();
            var systemEmail = dataMap.GetString( "Email" ).AsGuidOrNull();
            if ( !systemEmail.HasValue )
            {
                throw new Exception( "System Email is required!" );
            }


            // get job type id
            int jobId = Convert.ToInt16( context.JobDetail.Description );
            var jobService = new ServiceJobService( rockContext );
            var job = jobService.Get( jobId );

            DateTime _midnightToday = RockDateTime.Today.AddDays( 1 );
            var currentDateTime = RockDateTime.Now;
            var recipients = new List<RockEmailMessageRecipient>();
            ConnectionRequestService connectionRequestService = new ConnectionRequestService( rockContext );
            PersonService personService = new PersonService( rockContext );
            var connectionRequestsQry = connectionRequestService.Queryable().Where( cr =>
                                       connectionOpportunities.Contains( cr.ConnectionOpportunity.Guid.ToString() )
                                       && cr.ConnectorPersonAliasId != null
                                       && (
                                            cr.ConnectionState == ConnectionState.Active
                                            || ( cr.ConnectionState == ConnectionState.FutureFollowUp && cr.FollowupDate.HasValue && cr.FollowupDate.Value < _midnightToday )
                                        ) );


            var connectionRequestGroups = connectionRequestsQry.GroupBy( cr => cr.ConnectorPersonAlias.PersonId );
            foreach ( var group in connectionRequestGroups )
            {
                Person person = personService.Get( group.Key );
                List<ConnectionOpportunity> opportunities = group.Select( a => a.ConnectionOpportunity ).Distinct().ToList();
                var newConnectionRequests = group.Where( cr => cr.CreatedDateTime >= job.LastRunDateTime ).GroupBy( cr => cr.ConnectionOpportunityId ).ToList();
                // Get all the idle connections
                var idleConnectionRequests = group
                                    .Where( cr => (
                                            ( cr.ConnectionRequestActivities.Any() && cr.ConnectionRequestActivities.Max( ra => ra.CreatedDateTime ) < currentDateTime.AddDays( -cr.ConnectionOpportunity.ConnectionType.DaysUntilRequestIdle ) ) )
                                            || ( !cr.ConnectionRequestActivities.Any() && cr.CreatedDateTime < currentDateTime.AddDays( -cr.ConnectionOpportunity.ConnectionType.DaysUntilRequestIdle ) )
                                           )
                                    .Select( a => new { ConnectionOpportunityId = a.ConnectionOpportunityId, Id = a.Id } )
                                    .GroupBy( cr => cr.ConnectionOpportunityId ).ToList();

                // get list of requests that have a status that is considered critical.
                var criticalConnectionRequests = group
                                        .Where( r =>
                                            r.ConnectionStatus.IsCritical
                                        )
                                        .Select( a => new { ConnectionOpportunityId = a.ConnectionOpportunityId, Id = a.Id } )
                                        .GroupBy( cr => cr.ConnectionOpportunityId ).ToList();

                var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( null );
                mergeFields.Add( "ConnectionOpportunities", opportunities );
                mergeFields.Add( "ConnectionRequests", group.GroupBy( cr => cr.ConnectionOpportunity ).ToList() );
                mergeFields.Add( "NewConnectionRequests", newConnectionRequests );
                mergeFields.Add( "IdleConnectionRequestIds", idleConnectionRequests );
                mergeFields.Add( "CriticalConnectionRequestIds", criticalConnectionRequests );
                mergeFields.Add( "Person", person );
                mergeFields.Add( "LastRunDate", job.LastRunDateTime );


                recipients.Add( new RockEmailMessageRecipient( person, mergeFields ) );

            }

            var emailMessage = new RockEmailMessage( systemEmail.Value );
            emailMessage.SetRecipients( recipients );
            emailMessage.CreateCommunicationRecord = dataMap.GetString( "SaveCommunicationHistory" ).AsBoolean();
            emailMessage.Send();

            context.Result = string.Format( "Sent " + recipients.Count + " connection digest emails." );
        }
    }
}
