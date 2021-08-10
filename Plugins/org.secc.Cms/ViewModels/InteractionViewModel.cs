using System;
using org.secc.Warehouse.Model;

namespace org.secc.Cms.ViewModels
{
    public class InteractionViewModel
    {
        public DateTime Date { get; set; }
        public int Visits { get; set; }
        public int StaffVisitors { get; set; }
        public int MemberVisitors { get; set; }
        public int AttendeeVisitors { get; set; }
        public int ProspectVisitors { get; set; }
        public int AnonymousVisitors { get; set; }

        public InteractionViewModel( DailyInteraction dailyInteraction )
        {
            Date = dailyInteraction.Date;
            Visits = dailyInteraction.Visits;
            StaffVisitors = dailyInteraction.StaffVisitors;
            MemberVisitors = dailyInteraction.MemberVisitors;
            AttendeeVisitors = dailyInteraction.AttendeeVisitors;
            ProspectVisitors = dailyInteraction.ProspectVisitors;
            AnonymousVisitors = dailyInteraction.AnonymousVisitors;
        }
    }
}
