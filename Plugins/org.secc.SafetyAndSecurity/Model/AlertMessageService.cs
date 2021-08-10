using org.secc.SafetyAndSecurity.Data;
using Rock.Data;

namespace org.secc.SafetyAndSecurity.Model
{
    public class AlertMessageService : SafetyAndSecurityService<AlertMessage>
    {
        public AlertMessageService( RockContext context ) : base( context ) { }
    }
}