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
using System.Collections.Generic;
using System.Data;
using System.Linq;
using org.secc.SystemsMonitor.Helpers;
using org.secc.SystemsMonitor.Model;
using Quartz;
using Rock;
using Rock.Attribute;
using Rock.Communication;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;

namespace org.secc.Jobs
{
    [SystemCommunicationField( "Notification Communication", "Communication to send if a test meets an alarm condition", Order = 0 )]
    [GroupField( "Notification Group", "People communicate to if an alarm condition is met.", Order = 1 )]
    [DefinedValueField( Rock.SystemGuid.DefinedType.COMMUNICATION_SMS_FROM, "From Number", "Number that sends the SMS alerts.", IsRequired = true, Order = 2 )]


    [DisallowConcurrentExecution]
    public class RunSystemTests : IJob
    {
        private JobDataMap dataMap;

        public void Execute( IJobExecutionContext context )
        {
            dataMap = context.JobDetail.JobDataMap;
            var rockContext = new RockContext();

            SystemTestService systemTestService = new SystemTestService( rockContext );
            SystemTestHistoryService systemTestHistoryService = new SystemTestHistoryService( rockContext );

            var systemTests = systemTestService.Queryable()
                .Where( t => t.RunIntervalMinutes.HasValue )
                .ToList();

            int count = 0;
            //List<string> alarms = new List<string>();
            List<TestResult> alarms = new List<TestResult>();
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
                        alarms.Add( new TestResult( test.Name, test.AlarmNotification ) );
                    }
                }
            }

            if ( alarms.Any() )
            {
                SendNotifications( alarms );
                context.Result = string.Format( $"Ran {count} test{( count != 1 ? "s" : "" )}. Alarms: {string.Join( ", ", alarms.Select( a => a.Name ) )}" );
            }
            else
            {
                context.Result = string.Format( $"Ran {count} test{( count != 1 ? "s" : "" )}." );
            }

        }

        private void SendNotifications( List<TestResult> alarms )
        {
            var notificationGroup = dataMap.GetString( "NotificationGroup" ).AsGuid();
            RockContext rockContext = new RockContext();
            GroupService groupService = new GroupService( rockContext );
            var people = groupService.Queryable()
                .Where( g => g.Guid == notificationGroup )
                .SelectMany( g => g.Members )
                .Select( m => m.Person )
                .ToList();

            var emailAlarms = alarms
                .Where( a => ( a.AlarmNotification & AlarmNotification.Email ) == AlarmNotification.Email )
                .ToList();
            if ( emailAlarms.Any() )
            {
                SendNotificationEmail( emailAlarms, people );
            }

            var smsAlarms = alarms
                .Where( a => ( a.AlarmNotification & AlarmNotification.SMS ) == AlarmNotification.SMS )
                .ToList();
            if ( smsAlarms.Any() )
            {
                SendNotificationSms( smsAlarms, people );
            }
        }

        private void SendNotificationEmail( List<TestResult> alarms, List<Person> people )
        {
            var systemCommunication = dataMap.GetString( "NotificationCommunication" ).AsGuid();
            var emailMessage = new RockEmailMessage( systemCommunication );
            var recipients = new List<RockMessageRecipient>();

            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( null, null );
            mergeFields.Add( "Alarms", alarms );

            foreach ( var person in people )
            {
                recipients.Add( new RockEmailMessageRecipient( person, mergeFields ) );
            }

            emailMessage.SetRecipients( recipients );
            emailMessage.CreateCommunicationRecord = true;
            emailMessage.Send();
        }

        private void SendNotificationSms( List<TestResult> alarms, List<Person> people )
        {
            var fromNumber = dataMap.GetString( "FromNumber" ).AsGuid();
            var smsMessage = new RockSMSMessage();
            smsMessage.FromNumber = DefinedValueCache.Get( fromNumber );

            var recipients = new List<RockMessageRecipient>();

            smsMessage.Message = "System Monitor Alert:\n";
            foreach ( var alarm in alarms )
            {
                smsMessage.Message += alarm.Name + "\n";
            }
            smsMessage.CreateCommunicationRecord = true;
            smsMessage.CommunicationName = "System Monitor Alert Notification Message";

            foreach ( var person in people )
            {
                var mergeObject = new Dictionary<string, object> { { "Person", person } };
                smsMessage.AddRecipient( new RockEmailMessageRecipient( person, mergeObject ) );
            }

            smsMessage.Send();

        }

    }
}
