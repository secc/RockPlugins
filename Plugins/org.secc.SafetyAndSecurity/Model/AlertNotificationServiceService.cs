using org.secc.SafetyAndSecurity.Data;
using Rock.Data;

namespace org.secc.SafetyAndSecurity.Model
{
    public class AlertNotificationService : SafetyAndSecurityService<AlertNotification>
    {
        public AlertNotificationService( Rock.Data.DbContext context ) : base( context ) { }


    }
}