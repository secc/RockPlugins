using org.secc.SystemsMonitor.Data;
using Rock.Data;

namespace org.secc.SystemsMonitor.Model
{
    public class SystemTestService : SystemsMonitorService<SystemTest>
    {
        public SystemTestService( RockContext context ) : base( context )
        {
        }
    }

}
