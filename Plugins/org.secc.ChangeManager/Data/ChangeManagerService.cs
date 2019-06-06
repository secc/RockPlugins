using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rock.Data;

namespace org.secc.ChangeManager.Data
{
    public class ChangeManagerService<T> : Rock.Data.Service<T> where T : Rock.Data.Entity<T>, new()
    {
        public ChangeManagerService( RockContext context )
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
