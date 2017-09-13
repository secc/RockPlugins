using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.secc.RoomScanner.Models
{
    public class Request
    {
        public string AttendanceGuid { get; set; }
        public int LocationId { get; set; }
        public bool Override { get; set; }
        public string PIN { get; set; }
    }
}
