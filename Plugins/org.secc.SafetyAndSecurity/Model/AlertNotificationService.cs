using org.secc.SafetyAndSecurity.Data;

namespace org.secc.SafetyAndSecurity.Model
{
    public class AlertNotificationService : SafetyAndSecurityService<AlertNotification>
    {
        public AlertNotificationService( Rock.Data.DbContext context ) : base( context ) { }


    }
}
