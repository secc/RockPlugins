using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using org.secc.SafetyAndSecurity.Data;
using Rock;
using Rock.Data;

namespace org.secc.SafetyAndSecurity.Model
{
    public class AlertMessageService : SafetyAndSecurityService<AlertMessage>
    {
        public AlertMessageService( RockContext context ) : base( context ) { }
    }
}