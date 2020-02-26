using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.secc.ChangeManager.Utilities
{
    public class SecurityChangeDetail
    {
        public int ChangeRequestId { get; set; }
        public string PreviousEmail { get; set; }
        public string CurrentEmail { get; set; }
        public List<string> ChangeDetails { get; set; }
    }
}
