using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rock.Data;

namespace org.secc.xAPI.Data
{
    public class xAPIService<T> : Rock.Data.Service<T> where T : Rock.Data.Entity<T>, new()
    {
        public xAPIService( RockContext context )
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
