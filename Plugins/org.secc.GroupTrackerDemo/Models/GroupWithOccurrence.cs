using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rock.Model;

namespace org.secc.GroupTrackerDemo.Models
{
    public class GroupWithOccurrence
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; }
        public int GroupTypeId { get; set; }
        public string GroupTypeName { get; set; }

        public int? OccurrenceId { get; set; }
        public int? LocationId { get; set; }
        public string LocationName { get; set; }
        public int? ScheduleId { get; set; }
        public string ScheduleName { get; set; }
        public DateTime? OccurrenceDate { get; set; }
        public List<GroupMemberWithAttendance> GroupMember { get; set; }
    }
}
