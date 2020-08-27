using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using org.secc.SystemsMonitor.Model;
using Rock.Attribute;
using Rock.Extension;

namespace org.secc.SystemsMonitor
{
    public abstract class SystemTestComponent : Rock.Extension.Component
    {
        public abstract string Name { get; }
        public abstract string Icon { get; }
        public abstract SystemTestResult RunTest( SystemTest monitorTest );
        public virtual List<AlarmCondition> SupportedAlarmConditions
        {
            get => new List<AlarmCondition> {
                AlarmCondition.Never,
                AlarmCondition.Fail,
                AlarmCondition.ScoreAbove,
                AlarmCondition.ScoreBelow
            };
        }
    }
}
