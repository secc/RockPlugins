using org.secc.SystemsMonitor.Data;
using Rock.Data;

namespace org.secc.SystemsMonitor.Model
{
    public class SystemTestHistoryService : SystemsMonitorService<SystemTestHistory>
    {
        public SystemTestHistoryService( RockContext context ) : base( context )
        {
        }
    }
}
