using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using org.secc.ChangeManager.Data;
using Rock.Data;

namespace org.secc.ChangeManager.Model
{
    public class ChangeRequestService : ChangeManagerService<ChangeRequest>
    {
        public ChangeRequestService( RockContext context ) : base( context )
        {
        }
    }
}