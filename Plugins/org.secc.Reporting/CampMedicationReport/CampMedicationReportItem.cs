using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Rock;


namespace org.secc.Reporting
{
    public class CampMedicationReportItem
    {
        public int Id { get; set; }
        public int PersonId { get; set; }
        public string LastName { get; set; }
        public string NickName { get; set; }
        public int Gender { get; set; }
        public DateTime? BirthDate { get; set; }
        public int RegistrationTemplateId { get; set; }
        public string RegistrationTemplateName { get; set; }
        public int RegistrationInstanceId { get; set; }
        public string RegistrationInstanceName { get; set; }
        public string Medication { get; set; }
        public string Instructions { get; set; }
        public string Schedule { get; set; }
        public int? GroupId { get; set; }
        public string GroupName { get; set; }
        public string LeaderName { get; set; }
        public int? CampusId { get; set; }
        public string CampusName { get; set; }
        public string FullName
        {
            get
            {
                return string.Concat( NickName, " ", LastName );
            }
        }
        [JsonIgnore]
        public List<Guid> DelimitedScheduleGuids
        {
            get
            {
                return Schedule.SplitDelimitedValues().AsGuidList();
            }
        }

        public bool Breakfast
        {
            get
            {
                return DelimitedScheduleGuids
                    .Contains( "0dd03b01-0467-4536-88c3-61bb414d2f2a".AsGuid() );
            }
        }

        public bool Lunch
        {
            get
            {
                return DelimitedScheduleGuids
                    .Contains( "c51ecbc4-f1f2-4ea3-820d-8fb5ba6e0a99".AsGuid() );
            }
        }

        public bool Dinner
        {
            get
            {
                return DelimitedScheduleGuids
                    .Contains( "ed6b440b-7401-4e0b-87f1-0ee2f6dd3b65".AsGuid() );
            }
        }

        public bool Bedtime
        {
            get
            {
                return DelimitedScheduleGuids
                    .Contains( "4d1033a6-3e03-43df-9982-7f5f4360497a".AsGuid() );
            }
        }

        public bool AsNeeded
        {
            get
            {
                return DelimitedScheduleGuids
                    .Contains( "f7d7f281-929d-4eb4-8e5c-464a9abec0f1".AsGuid() );
            }
        }

    }

}