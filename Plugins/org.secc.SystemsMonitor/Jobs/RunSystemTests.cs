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
using org.secc.SystemsMonitor.Model;
using Quartz;
using Rock;
using Rock.Attribute;
using Rock.Communication;
using Rock.Data;
using Rock.Model;

namespace org.secc.Jobs
{
    [SystemCommunicationField( "Notification Communication", "Communication to send if a test meets an alarm condition" )]
    [GroupField( "Notification Group", "People communicate to if an alarm condition is met." )]

    [DisallowConcurrentExecution]
    public class RunSystemTests : IJob
    {
        public void Execute( IJobExecutionContext context )
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;
            var rockContext = new RockContext();

            var systemCommunication = dataMap.GetString( "NotificationCommunication" ).AsGuid();
            var notificationGroup = dataMap.GetString( "NotificationGroup" ).AsGuid();

            SystemTestService systemTestService = new SystemTestService( rockContext );
            SystemTestHistoryService systemTestHistoryService = new SystemTestHistoryService( rockContext );

            var systemTests = systemTestService.Queryable()
                .Where( t => t.RunIntervalMinutes.HasValue )
                .ToList();

            int count = 0;
            List<string> alarms = new List<string>();

            foreach ( var test in systemTests )
            {
                var cutOffDate = RockDateTime.Now.AddMinutes( test.RunIntervalMinutes.Value * -1 );
                var histories = systemTestHistoryService.Queryable()
                    .Where( h => h.SystemTestId == test.Id && h.CreatedDateTime > cutOffDate ).Any();
                if ( !histories )
                {
                    count++;
                    var result = test.Run();

                    if ( test.MeetsAlarmCondition( result ) )
                    {
                        alarms.Add( test.Name );
                    }
                }
            }

            if ( alarms.Any() )
            {
                SendNotifications( alarms, systemCommunication, notificationGroup );
                context.Result = string.Format( $"Ran {count} test{( count != 1 ? "s" : "" )}. Alarms: {string.Join( ", ", alarms )}" );
            }
            else
            {
                context.Result = string.Format( $"Ran {count} test{( count != 1 ? "s" : "" )}." );
            }

        }

        private void SendNotifications( List<string> alarms, Guid systemCommunication, Guid notificationGroup )
        {
            var emailMessage = new RockEmailMessage( systemCommunication );
            var recipients = new List<RockMessageRecipient>();

            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( null, null );
            mergeFields.Add( "Alarms", alarms );

            RockContext rockContext = new RockContext();
            GroupService groupService = new GroupService( rockContext );
            var people = groupService.Queryable()
                .Where( g => g.Guid == notificationGroup )
                .SelectMany( g => g.Members )
                .Select( m => m.Person )
                .ToList();

            foreach ( var person in people )
            {
                recipients.Add( new RockEmailMessageRecipient( person, mergeFields ) );
            }

            emailMessage.SetRecipients( recipients );
            emailMessage.CreateCommunicationRecord = true;
            emailMessage.Send();
        }
    }
}
