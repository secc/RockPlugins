using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using org.secc.SystemsMonitor.Model;

namespace org.secc.SystemsMonitor.Helpers
{
    class TestResult : DotLiquid.Drop
    {
        public string Name { get; set; }

        public AlarmNotification AlarmNotification { get; set; }

        public TestResult( string name, AlarmNotification? alarmNotification )
        {
            Name = name;
            AlarmNotification = ( AlarmNotification ) alarmNotification;
        }
    }
}
