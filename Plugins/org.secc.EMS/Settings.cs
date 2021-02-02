using System.Collections.Generic;
using System.Configuration;

namespace org.secc.EMS
{
    public static class Settings
    {
        public static string UserName { get => ConfigurationManager.AppSettings["EmsUser"]; }
        public static string Password { get => ConfigurationManager.AppSettings["EmsPassword"]; }

        public static List<string> HVAC_enabled_booking_status_ids { get => new List<string> { "1" }; }

        public static List<string> Default_web_event_status_ids { get => new List<string> { "1" }; }

    }
}
