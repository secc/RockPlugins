using Quartz;
using Rock;
using Rock.Attribute;
using Rock.Communication;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;


namespace org.secc.Jobs
{
    public class GroupLeaderNotificationSummary 
    {

        public int PersonId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int StudentCount { get; set; }
        public CommunicationType CommunicationType { get; set; }
        public string MedicationScheduleName { get; set; }

    }

    /// <summary>
    /// Job to notify the Group Leaders 
    /// </summary>
    /// 
    [GroupField("Parent Group", "Parent Group of the Event's Small Groups.", true, Key = "ParentGroup")]
    [CustomDropdownListField("Communication List", "Communication list for group leaders who want to receive message by SMS/Text",
        @"SELECT Id as [Value], Name as [Text] FROM [Group] WHERE GroupTypeId = 227 ORDER BY Name", true, Key = "CommunicationListId")]
    [IntegerField("Medication Checkin Days",
        "Only show medication alerts for people who used Medication Checkin in the last x number of days.",
        false, 14, Key = "MedicationCheckinDays")]
    [DefinedValueField("81B51822-50D7-4BE6-A462-04077405BB7E", "Medication Dispersal Schedule",
        "The medication schedule to send notifications for.", true, false, Key = "MedicationSchedule")]
    [SystemCommunicationField("Communication Template", "Template for the system communication to send to the group leader.", true,
        Key = "CommunicationTemplate")]

    [DisallowConcurrentExecution]
    public class GroupLeaderMedicationNotifications : IJob
    {
        private Guid MedicationMatrixTemplateGuid = "d2ed9b3d-309a-4a70-bda4-2c090acd1384".AsGuid();

        public static class JobAttributes
        {
            public static SystemCommunication CommunicationTemplate { get; set; }
            public static int MedicationCheckinDays { get; set; }
            public static DefinedValueCache MedicationScheduleValue { get; set; }
            public static Group ParentGroup { get; set; }
            public static Group CommunicationList { get; set; }

        }


        public class PersonMedicationMatrixSummary
        {
            public int PersonId { get; set; }
            public Guid MatrixGuid { get; set; }
        }



        public void Execute(IJobExecutionContext context)
        {
            var rockContext = new RockContext();
            var dataMap = context.JobDetail.JobDataMap;
            LoadAttributes(dataMap, rockContext);
            var groupInformation = GetGroupLeaderInformation(rockContext);

            foreach (var leader in groupInformation)
            {
                if (leader.CommunicationType == CommunicationType.Email)
                {
                    SendEmail(leader);
                }
                else
                {
                    SendSMS(leader);
                }
            }

            groupInformation.Count();

        }

        private List<GroupLeaderNotificationSummary> GetGroupLeaderInformation(RockContext rockContext)
        {
            var groupIds = new GroupService(rockContext).GetAllDescendentGroupIds(JobAttributes.ParentGroup.Id, false);
            var groupNotificationSummaries = new List<GroupLeaderNotificationSummary>();
            var groupMemberService = new GroupMemberService(rockContext);
            var attributeValueService = new AttributeValueService(rockContext);

            var personEntityType = EntityTypeCache.Get(typeof(Person));
            var earliestCheckinDate = RockDateTime.Now.AddDays(-JobAttributes.MedicationCheckinDays).Date;
            var medicationScheduleAttribute = GetMatrixItemAttribute("Schedule");

            foreach (var groupId in groupIds)
            {
                var groupMembers = groupMemberService.Queryable()
                    .Include(m => m.Person)
                    .Where(m => m.GroupId == groupId)
                    .Where(m => m.GroupMemberStatus == GroupMemberStatus.Active);

                var leaders = groupMembers.Where(m => m.GroupRole.IsLeader).ToList();

                var medicationCheckinDateValues = attributeValueService.Queryable()
                    .Where(v => v.Attribute.Key == "LastMedicationCheckin")
                    .Where(v => v.Attribute.EntityTypeId == personEntityType.Id)
                    .Where(v => v.ValueAsDateTime >= earliestCheckinDate);

                var medicationMatrixAttributeValues = attributeValueService.Queryable()
                    .Where(v => v.Attribute.Key == "Medications")
                    .Where(v => v.Attribute.EntityTypeId == personEntityType.Id);


                var medicationScheduleValues = attributeValueService.Queryable()
                    .Where(v => v.AttributeId == medicationScheduleAttribute.Id);

                var selectedScheduleGuidString = JobAttributes.MedicationScheduleValue.Guid.ToString();

                var groupMemberWithMeds = groupMembers
                    .Where(m => !m.GroupRole.IsLeader)
                    .Join(medicationCheckinDateValues, m => m.PersonId, ci => ci.EntityId,
                       (m, ci) => new { GroupMember = m, CheckinValue = ci })
                    .Join(medicationMatrixAttributeValues, gm => gm.GroupMember.PersonId, av => av.EntityId,
                        (m, ma) => new { GroupMember = m.GroupMember, CheckinValue = m.CheckinValue, MatrixValue = ma })
                    .Join(new AttributeMatrixItemService(rockContext).Queryable(), m => m.MatrixValue.Value, ami => ami.AttributeMatrix.Guid.ToString(),
                        (m, ami) => new { GroupMember = m.GroupMember, CheckinValue = m.CheckinValue, MatrixItem = ami })
                    .Join(medicationScheduleValues, m => m.MatrixItem.Id, s => s.EntityId,
                        (m, s) => new { m.GroupMember, m.CheckinValue, ScheduleValue = s.Value })
                    .Where(m => m.ScheduleValue == selectedScheduleGuidString)
                    .GroupBy(m => m.GroupMember)
                    .Select(m => new
                    {
                        PersonId = m.Key.PersonId,
                        MedCount = m.Count()
                    }).ToList();

                if (groupMemberWithMeds.Count > 0)
                {
                    foreach (var leader in leaders)
                    {
                        groupNotificationSummaries.Add(new GroupLeaderNotificationSummary
                        {
                            PersonId = leader.PersonId,
                            FirstName = leader.Person.NickName,
                            LastName = leader.Person.LastName,
                            StudentCount = groupMemberWithMeds.Count(),
                            CommunicationType = LeaderAllowsSMSAlerts(leader.PersonId) ? CommunicationType.SMS : CommunicationType.Email,
                            MedicationScheduleName = JobAttributes.MedicationScheduleValue.Value
                        });
                    }
                }
            }

            return groupNotificationSummaries;


        }

        private Rock.Model.Attribute GetMatrixItemAttribute(string key)
        {
            var rockContext = new RockContext();
            var matrixTemplateId = new AttributeMatrixTemplateService(rockContext).Get(MedicationMatrixTemplateGuid).Id.ToString();

            var matrixItemEntity = EntityTypeCache.Get(typeof(AttributeMatrixItem));
            var attribute = new AttributeService(rockContext).Queryable().AsNoTracking()
                .Where(a => a.EntityTypeId == matrixItemEntity.Id)
                .Where(a => a.Key == key)
                .Where(a => a.EntityTypeQualifierColumn == "AttributeMatrixTemplateId")
                .Where(a => a.EntityTypeQualifierValue == matrixTemplateId)
                .SingleOrDefault();

            return attribute;


        }

        private bool LeaderAllowsSMSAlerts(int personId)
        {
            var rockContext = new RockContext();
            var groupMember = new GroupMemberService(rockContext).GetByGroupIdAndPersonId(JobAttributes.CommunicationList.Id, personId)
                .FirstOrDefault();

            if (groupMember != null && groupMember.GroupMemberStatus == GroupMemberStatus.Active)
            {
                return true;
            }

            return false;

        }

        private void LoadAttributes(JobDataMap dataMap, RockContext rockContext)
        {
            JobAttributes.CommunicationTemplate = new SystemCommunicationService(rockContext)
                .Get(dataMap.GetString("CommunicationTemplate").AsGuid());
            JobAttributes.MedicationCheckinDays = dataMap.GetInt("MedicationCheckinDays");
            JobAttributes.MedicationScheduleValue = DefinedValueCache.Get(dataMap.GetString("MedicationSchedule").AsGuid());
            JobAttributes.ParentGroup = new GroupService(rockContext)
                .Get(dataMap.GetString("ParentGroup").AsGuid());
            JobAttributes.CommunicationList = new GroupService(rockContext).Get(dataMap.GetInt("CommunicationListId"));

        }

        private void SendEmail(GroupLeaderNotificationSummary leader)
        {
            var person = new PersonService(new RockContext()).Get(leader.PersonId);
            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields(null);
            mergeFields.Add("Person", person);
            mergeFields.Add("LeaderFirstName", leader.FirstName);
            mergeFields.Add("LeaderLastName", leader.LastName);
            mergeFields.Add("PersonId", leader.PersonId);
            mergeFields.Add("StudentCount", leader.StudentCount);
            mergeFields.Add("MessageType", leader.CommunicationType == CommunicationType.SMS ? "SMS" : "Email");
            mergeFields.Add("MedicationScheduleName", leader.MedicationScheduleName);
            mergeFields.Add("Today", RockDateTime.Now.ToShortDateString());
            var emailMessage = new RockEmailMessage(JobAttributes.CommunicationTemplate.Guid);
            emailMessage.AddRecipient(new RockEmailMessageRecipient(person, mergeFields));

            var errors = new List<string>();
            emailMessage.Send(out errors);

        }

        private void SendSMS(GroupLeaderNotificationSummary leader)
        {
            if (!JobAttributes.CommunicationTemplate.SMSFromDefinedValueId.HasValue)
            {
                throw new Exception("SMS From Number not configured in System Communication.");
            }

            var person = new PersonService(new RockContext()).Get(leader.PersonId);
            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields(null);

            mergeFields.Add("Person", person);
            mergeFields.Add("LeaderFirstName", leader.FirstName);
            mergeFields.Add("LeaderLastName", leader.LastName);
            mergeFields.Add("PersonId", leader.PersonId);
            mergeFields.Add("StudentCount", leader.StudentCount);
            mergeFields.Add("MessageType", leader.CommunicationType == CommunicationType.SMS ? "SMS" : "Email");
            mergeFields.Add("MedicationScheduleName", leader.MedicationScheduleName);
            mergeFields.Add("Today", RockDateTime.Now.ToShortDateString());

            var smsMessage = new RockSMSMessage(JobAttributes.CommunicationTemplate);

            var smsNumber = new PhoneNumberService(new RockContext()).Queryable()
                .Where(n => n.PersonId == person.Id)
                .Where(n => n.IsMessagingEnabled)
                .OrderBy(n => n.NumberTypeValue.Order)
                .FirstOrDefault();

            if (smsNumber != null)
            {
                smsMessage.FromNumber = DefinedValueCache.Get(JobAttributes.CommunicationTemplate.SMSFromDefinedValueId.Value);
                smsMessage.AddRecipient(new RockSMSMessageRecipient(person, smsNumber.FullNumber, mergeFields));
                smsMessage.Send();
            }
            else
            {
                SendEmail(leader);
            }

        }
    }
}
