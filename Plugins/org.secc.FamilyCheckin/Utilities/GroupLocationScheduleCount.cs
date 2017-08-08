using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.secc.FamilyCheckin.Utilities
{
    public class GroupLocationScheduleCount
    {
        public int GroupId { get; set; }
        public int LocationId { get; set; }
        public int ScheduleId { get; set; }
        public List<int> PersonIds { get; set; }
        public List<int> InRoomPersonIds { get; set; }
    }
}