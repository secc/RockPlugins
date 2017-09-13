using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.secc.RoomScanner.Models
{
    public class Attendee
    {
        public Guid AttendanceGuid { get; set; }
        public int PersonId { get; set; }
        public string PersonName { get; set; }
        public bool DidAttend { get; set; }
        public bool CheckedOut { get; set; }
    }
}
