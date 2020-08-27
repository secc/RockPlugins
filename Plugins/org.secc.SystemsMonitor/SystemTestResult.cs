using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace org.secc.SystemsMonitor
{
    public class SystemTestResult
    {
        public int Score { get; set; } = 0;
        public bool Passed { get; set; }
        public string Message { get; set; }
    }

}
