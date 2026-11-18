using org.secc.SafetyAndSecurity.Data;

namespace org.secc.SafetyAndSecurity.Model
{
    public class AlertMessageService : SafetyAndSecurityService<AlertMessage>
    {
        public AlertMessageService( Rock.Data.DbContext context ) : base( context ) { }
    }
}
