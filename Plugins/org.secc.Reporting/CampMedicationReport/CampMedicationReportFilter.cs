using System;


namespace org.secc.Reporting
{
    public class CampMedicationReportFilter
    {
        public int RegistrationTemplateId { get; set; }
        public int RegistrationInstanceId { get; set; }
        public int? CampusId { get; set; }
        public Guid? ScheduleGuid { get; set; }
    }

}