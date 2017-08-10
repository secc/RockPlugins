using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.secc.RoomScanner.Models
{
    public class AttendanceCodes
    {
        public String NickName { get; set; }
        public String LastName { get; set; }
        public List<string> Codes { get; set; }
    }
}
