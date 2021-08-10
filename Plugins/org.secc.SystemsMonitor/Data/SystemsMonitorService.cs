using Rock.Data;

namespace org.secc.SystemsMonitor.Data
{
    public class SystemsMonitorService<T> : Rock.Data.Service<T> where T : Rock.Data.Entity<T>, new()
    {
        public SystemsMonitorService( RockContext context )
            : base( context )
        {
        }

        public virtual bool CanDelete( T item, out string errorMessage )
        {
            errorMessage = string.Empty;
            return true;
        }
    }
}
