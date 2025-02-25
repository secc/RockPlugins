﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Quartz;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Jobs;
using Rock.Model;
using Rock.Web.Cache;


namespace org.secc.Communication.Jobs
{

    [DisplayName( "SECC | Send Schedule Reminders" )]
    [Description( "Sends Schedule Reminders a specified number of days before an event to people in a dataview." )]

    [ScheduleField( "Schedule to Send Reminder For",
        Description = "The schedule to send the reminder for.",
        IsRequired = true,
        Key = AttributeKeys.ScheduleKey,
        Order = 0 )]
    [SystemCommunicationField( "System Communication Template",
        Description = "The system communication template that includes the message/messages that are to be sent.",
        IsRequired = true,
        Key = AttributeKeys.CommunicationTemplateKey,
        Order = 1 )]
    [DataViewField( "Dataview - Distribution List",
        Description = "The distribution list of recipients who should be sent the communication",
        IsRequired = true,
        EntityType = typeof( Person ),
        Key = AttributeKeys.DistributionDataviewKey,
        Order = 2 )]
    [IntegerField( "Days before to send reminder",
        Description = "The number of calendar days before the event to send the reminder communication. Default is 1",
        DefaultIntegerValue = 1,
        IsRequired = false,
        Key = AttributeKeys.SendDaysKey,
        Order = 3 )]
    [TimeField( "Send Time",
        Description = "Time of day to send reminder message. Default 8:00 am",
        IsRequired = false,
        DefaultValue = "08:00 am",
        Key = AttributeKeys.SendTimeKey,
        Order = 4 )]
    [CheckListField( "Communication Methods",
        "Which communication methods to use. If none selected, all configured methods are used.",
         "Email,SMS",
        true,
        "",
        "",
        Key = AttributeKeys.CommunicationMethodKey,
        Order = 5 )]

    [DisallowConcurrentExecution]
    public class SendScheduleReminder : IJob
    {
        public static class AttributeKeys
        {
            public const string ScheduleKey = "Schedule";
            public const string CommunicationTemplateKey = "CommunicationTemplate";
            public const string DistributionDataviewKey = "DistributionDataview";
            public const string SendDaysKey = "DaysBeforeToSend";
            public const string SendTimeKey = "SendTime";
            public const string CommunicationMethodKey = "CommunicationMethod";

        }

        private TimeSpan _defaultTimeSpan = new TimeSpan( 8, 0, 0 );

        private Schedule EventSchedule { get; set; }
        private SystemCommunication CommunicationTemplate { get; set; }
        private Guid DataviewGuid { get; set; }
        private int SendDaysAhead { get; set; }
        private TimeSpan? SendTime { get; set; }
        private List<string> CommunicationMethods { get; set; }


        public void Execute( IJobExecutionContext context )
        {
            var datamap = context.JobDetail.JobDataMap;
            var rockContext = new RockContext();
            var sendReminders = false;

            var scheduleGuid = datamap.GetString( AttributeKeys.ScheduleKey ).AsGuid();
            EventSchedule = new ScheduleService( rockContext ).Get( scheduleGuid );


            if (EventSchedule == null)
            {
                throw new RockJobWarningException( "Schedule Not Found." );
            }

            CommunicationTemplate = new SystemCommunicationService( rockContext ).Get( datamap.GetString( AttributeKeys.CommunicationTemplateKey ).AsGuid() );

            if(CommunicationTemplate == null || CommunicationTemplate.IsActive == false)
            {
                throw new RockJobWarningException( "System Communication Not Found or is Inactive." );
            }

            DataviewGuid = datamap.GetString( AttributeKeys.DistributionDataviewKey ).AsGuid();
            SendDaysAhead = datamap.GetString( AttributeKeys.SendDaysKey ).AsInteger();
            CommunicationMethods = datamap.GetString( AttributeKeys.CommunicationMethodKey ).SplitDelimitedValues().ToList();

            SendTime = datamap.GetString( AttributeKeys.SendTimeKey ).AsTimeSpan();

            if(!SendTime.HasValue)
            {
                SendTime = _defaultTimeSpan;
            }

            var enddate = DateTime.Today.AddDays( (SendDaysAhead) + 1 ).Date;
            var upcomingOccurrences = EventSchedule.GetScheduledStartTimes( DateTime.Today.Date, enddate );

            foreach (var occurrence in upcomingOccurrences)
            {
                if((occurrence.Date - DateTime.Today.Date).Days == SendDaysAhead)
                {
                    sendReminders = true;
                    break;
                }
            }

            if(!sendReminders)
            {
                context.Result = "No Reminders due to be sent.";
                return;
            }

            CreateCommunications();

            context.Result = $"Reminder Communications created for Schedule {EventSchedule.Name} - Will be sent at {RockDateTime.Today.Add(SendTime.Value).ToShortDateTimeString()}";

        }

        private void CreateCommunications()
        {
            var rockContext = new RockContext();
            var systemAdminAlias = new PersonService( rockContext ).Get( 1 ).PrimaryAliasId;
            var dataview = new DataViewService( rockContext ).Get( DataviewGuid );

            var recipients = ((IQueryable<Person>) dataview.GetQuery( new DataViewGetQueryArgs { DbContext = rockContext } ))
                .ToList();

            var communicationService = new CommunicationService( rockContext );
            var emailMediumType = EntityTypeCache.Get( typeof( Rock.Communication.Medium.Email ) );
            var smsMediumType = EntityTypeCache.Get( typeof( Rock.Communication.Medium.Sms ) );

            if (CommunicationTemplate.Body.IsNotNullOrWhiteSpace())
            {
                if (!CommunicationMethods.Any() || CommunicationMethods.Contains( "Email", StringComparer.InvariantCultureIgnoreCase ))
                {
                    var emailCommunication = new Rock.Model.Communication()
                    {
                        Name = CommunicationTemplate.Title,
                        CommunicationType = CommunicationType.Email,
                        SenderPersonAliasId = systemAdminAlias,
                        FromName = CommunicationTemplate.FromName,
                        FromEmail = CommunicationTemplate.From,
                        Subject = CommunicationTemplate.Subject,
                        Message = CommunicationTemplate.Body,
                        SystemCommunicationId = CommunicationTemplate.Id,
                        Status = CommunicationStatus.Approved,
                        FutureSendDateTime = RockDateTime.Today.Add( SendTime.Value ),
                        CreatedByPersonAliasId = systemAdminAlias,
                        ModifiedByPersonAliasId = systemAdminAlias,

                    };

                    emailCommunication.Recipients = new List<CommunicationRecipient>();

                    foreach (var p in recipients)
                    {
                        emailCommunication.Recipients.Add( new CommunicationRecipient() {
                            PersonAliasId = p.PrimaryAliasId,
                            AdditionalMergeValuesJson = "{}",
                            MediumEntityTypeId = emailMediumType.Id,
                            Status = CommunicationRecipientStatus.Pending} );
                    }
                    communicationService.Add( emailCommunication );
                    rockContext.SaveChanges();
                }
            }

            if(CommunicationTemplate.SMSMessage.IsNotNullOrWhiteSpace())
            {
                if(!CommunicationMethods.Any() || CommunicationMethods.Contains("SMS", StringComparer.InvariantCultureIgnoreCase))
                {
                    var smsCommunication = new Rock.Model.Communication()
                    {
                        Name = CommunicationTemplate.Title,
                        Subject = string.Empty,
                        CommunicationType = CommunicationType.SMS,
                        SenderPersonAliasId = systemAdminAlias,
                        SMSFromDefinedValueId = CommunicationTemplate.SMSFromDefinedValueId,
                        SMSMessage = CommunicationTemplate.SMSMessage,
                        SystemCommunicationId = CommunicationTemplate.Id,
                        Status = CommunicationStatus.Approved,
                        FutureSendDateTime = RockDateTime.Today.Add( SendTime.Value ),
                        CreatedByPersonAliasId = systemAdminAlias,
                        ModifiedByPersonAliasId = systemAdminAlias,
                    };

                    smsCommunication.Recipients = new List<CommunicationRecipient>();

                    foreach (var p in recipients)
                    {
                        smsCommunication.Recipients.Add( new CommunicationRecipient()
                        {
                            PersonAliasId = p.PrimaryAliasId,
                            AdditionalMergeValuesJson = "{}",
                            MediumEntityTypeId = smsMediumType.Id,
                            Status = CommunicationRecipientStatus.Pending
                        } );
                    }
                    communicationService.Add( smsCommunication );
                    rockContext.SaveChanges();
                }
            }

            
        }
    }
}
