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
using System.Reflection;
using org.secc.RecurringCommunications.Model;
using Quartz;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Reporting;
using Rock.Tasks;
using Rock.Web.Cache;

namespace org.secc.RecurringCommunications.Jobs
{
    [DisallowConcurrentExecution]
    [IntegerField("SQL Command Timeout",
        Description = "The maximum amount of time that the RockContext can run a query prior to timing out. Default is 30 seconds.",
        IsRequired = false,
        DefaultIntegerValue = 30,
        Key = "SQLCommandTimeout",
        Order = 0)]
    public class SendRecurringCommunications : IJob
    {
        int _commandTimeout = 30;

        public void Execute( IJobExecutionContext context )
        {
            JobDataMap dataMap = context.JobDetail.JobDataMap;
            _commandTimeout = dataMap.GetIntegerFromString( "SQLCommandTimeout" );

            RockContext rockContext = new RockContext();
            rockContext.Database.CommandTimeout = _commandTimeout;
            RecurringCommunicationService recurringCommunicationService = new RecurringCommunicationService( rockContext );
            var communications = recurringCommunicationService.Queryable( "Schedule" ).ToList();
            int count = 0;
            var errors = new List<RecurringCommunication>();

            foreach (var communication in communications)
            {
                var lastExpectedRun = communication.Schedule
                    .GetScheduledStartTimes( RockDateTime.Now.AddDays( -1 ), RockDateTime.Now )
                    .LastOrDefault();
                if (lastExpectedRun != null && lastExpectedRun > DateTime.MinValue)
                {
                    if (communication.LastRunDateTime == null || communication.LastRunDateTime <= lastExpectedRun)
                    {
                        communication.LastRunDateTime = RockDateTime.Now;
                        rockContext.SaveChanges();
                        try
                        {
                            EnqueRecurringCommunication( communication.Id );
                        }
                        catch (Exception e)
                        {
                            ExceptionLogService.LogException( new Exception( $"Error Sending Recurring Communication: ID {communication.Id}", e ) );
                            errors.Add( communication );
                        }
                        count++;
                    }

                }
            }
            var errorText = "";
            if ( errors.Any() )
            {
                errorText = "There were errors with the following Recurring Communications: ";
                errorText += string.Join( ", ", errors.Select( c => $"{c.Id}:{c.Name}" ) );
            }
            context.Result = $"Sent {count} communication{( count == 1 ? "" : "s" )} {errorText}";
        }


        private void EnqueRecurringCommunication( int id )
        {
            RockContext rockContext = new RockContext();
            rockContext.Database.CommandTimeout = _commandTimeout;
            CommunicationService communicationService = new CommunicationService( rockContext );
            RecurringCommunicationService recurringCommunicationService = new RecurringCommunicationService( rockContext );
            var recurringCommunication = recurringCommunicationService.Get( id );
            if ( recurringCommunication == null )
            {
                return;
            }



            var communication = new Communication
            {
                SenderPersonAlias = recurringCommunication.CreatedByPersonAlias,
                Name = recurringCommunication.Name,
                CommunicationType = recurringCommunication.CommunicationType,
                FromName = recurringCommunication.FromName,
                FromEmail = recurringCommunication.FromEmail,
                Subject = recurringCommunication.Subject,
                Message = recurringCommunication.EmailBody,
                SMSFromDefinedValueId = recurringCommunication.PhoneNumberValueId,
                SMSMessage = recurringCommunication.SMSBody,
                PushTitle = recurringCommunication.PushTitle,
                PushSound = recurringCommunication.PushSound,
                PushMessage = recurringCommunication.PushMessage,
                Status = CommunicationStatus.Approved
            };

            DataTransformComponent transform = null;
            if ( recurringCommunication.TransformationEntityTypeId.HasValue )
            {
                transform = DataTransformContainer.GetComponent( recurringCommunication.TransformationEntityType.Name );
                communication.AdditionalMergeFields = new List<string>() { "AppliesTo" };
            }


            communicationService.Add( communication );

            var emailMediumEntityType = EntityTypeCache.Get( Rock.SystemGuid.EntityType.COMMUNICATION_MEDIUM_EMAIL.AsGuid() );
            var smsMediumEntityType = EntityTypeCache.Get( Rock.SystemGuid.EntityType.COMMUNICATION_MEDIUM_SMS.AsGuid() );
            var pushNotificationMediumEntityType = EntityTypeCache.Get( Rock.SystemGuid.EntityType.COMMUNICATION_MEDIUM_PUSH_NOTIFICATION.AsGuid() );

            var dataview = ( IQueryable<Person> ) recurringCommunication.DataView.GetQuery( new DataViewGetQueryArgs { DbContext = rockContext } );


            if ( transform != null )
            {
                var recipients = new List<CommunicationRecipient>();
                var personService = new PersonService( rockContext );
                var paramExpression = personService.ParameterExpression;

                foreach ( Person dvPerson in dataview )
                {
                    var whereExp = Rock.Reporting.FilterExpressionExtractor.Extract<Rock.Model.Person>( personService.Queryable().Where( p => p.Id == dvPerson.Id ), paramExpression, "p" );
                    var transformExp = transform.GetExpression( personService, paramExpression, whereExp );

                    MethodInfo getMethod = personService.GetType().GetMethod( "Get", new Type[] { typeof( System.Linq.Expressions.ParameterExpression ), typeof( System.Linq.Expressions.Expression ) } );

                    if ( getMethod != null )
                    {
                        var getResult = getMethod.Invoke( personService, new object[] { paramExpression, transformExp } );
                        var qry = getResult as IQueryable<Person>;

                        foreach ( var p in qry.ToList() )
                        {
                            var fieldValues = new Dictionary<string, object>();
                            fieldValues.Add( "AppliesTo", dvPerson );
                            recipients.Add( new CommunicationRecipient()
                            {
                                PersonAlias = p.PrimaryAlias,
                                MediumEntityTypeId = p.CommunicationPreference == CommunicationType.SMS ? smsMediumEntityType.Id : emailMediumEntityType.Id,
                                AdditionalMergeValues = fieldValues
                            } );

                        }
                    }

                    communication.Recipients = recipients;
                }
            }
            else
            {
                communication.Recipients = dataview
                    .ToList()
                    .Select( p =>
                        new CommunicationRecipient
                        {
                            PersonAlias = p.PrimaryAlias,
                            MediumEntityTypeId = p.CommunicationPreference == CommunicationType.SMS ? smsMediumEntityType.Id : emailMediumEntityType.Id
                        } )
                    .ToList();
            }
            Dictionary<int, CommunicationType?> communicationListGroupMemberCommunicationTypeLookup = new Dictionary<int, CommunicationType?>();

            foreach ( var recipient in communication.Recipients )
            {
                if ( communication.CommunicationType == CommunicationType.Email )
                {
                    recipient.MediumEntityTypeId = emailMediumEntityType.Id;
                }
                else if ( communication.CommunicationType == CommunicationType.SMS )
                {
                    recipient.MediumEntityTypeId = smsMediumEntityType.Id;
                }
                else if ( communication.CommunicationType == CommunicationType.PushNotification )
                {
                    recipient.MediumEntityTypeId = pushNotificationMediumEntityType.Id;
                }
                else if ( communication.CommunicationType == CommunicationType.RecipientPreference )
                {
                    //Do nothing we already defaulted to the recipient's preference
                }
                else
                {
                    throw new Exception( "Unexpected CommunicationType: " + communication.CommunicationType.ConvertToString() );
                }
            }
            rockContext.SaveChanges();

            var message = new ProcessSendCommunication.Message
            {
                CommunicationId = communication.Id
            };
            message.Send();
        }

    }
}
