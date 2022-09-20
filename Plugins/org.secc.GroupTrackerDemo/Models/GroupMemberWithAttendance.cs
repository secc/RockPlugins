using System;

namespace org.secc.GroupTrackerDemo.Models
{
    public class GroupMemberWithAttendance
    {
        public int GroupMemberId { get; set; }
        public int PersonId { get; set; }
        public string NickName { get; set; }
        public string LastName { get; set; }
        public DateTime? BirthDate { get; set; }
        public int? PhotoId { get; set; }
        public string MobilePhone { get; set; }
        public int? AttendanceId { get; set; }
        public DateTime? StartDateTime { get; set; }
        public DateTime? EndDateTime { get; set; }
        public bool? DidAttend { get; set; }
        public DateTime? PresentDateTime { get; set; }
    }
}