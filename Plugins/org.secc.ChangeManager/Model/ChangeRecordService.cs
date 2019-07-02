using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using org.secc.ChangeManager.Data;
using Rock.Data;

namespace org.secc.ChangeManager.Model
{
    public class ChangeRecordService : ChangeManagerService<ChangeRecord>
    {
        public ChangeRecordService( RockContext context ) : base( context )
        {
        }
    }
}
