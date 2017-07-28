using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.secc.RoomScanner.Models
{
    public class AttendanceEntry
    {
        public int Id { get; set; }
        public int PersonId { get; set; }
        public string NickName { get; set; }
        public string LastName { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime? EndDateTime { get; set; }
        public bool DidAttend { get; set; }
        public string AttendanceGuid { get; set; }
        public bool InWorship { get; set; }
    }
}
