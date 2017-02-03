using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace org.secc.EMS {
    public class zoneEvent {
        public zoneEvent(string HVACZone, DateTime TimeBookingStart, DateTime TimeEventEnd, string Rooms) {
            this.HVACZone = HVACZone;
            this.TimeBookingStart = TimeBookingStart;
            this.TimeEventEnd = TimeEventEnd;
            this.Rooms = Rooms;
        }

        //public zoneEvent(string HVACZone, DateTime TimeEventStart, DateTime TimeEventEnd, string Rooms) {
        //    this.HVACZone = HVACZone;
        //    this.TimeEventStart = TimeEventStart;
        //    this.TimeEventEnd = TimeEventEnd;
        //    this.Rooms = Rooms;
        //}

        public string HVACZone;
        public DateTime TimeBookingStart;   //Reserved Start Time
        //public DateTime TimeBookingEnd;   //Reserved End Time
        //public DateTime TimeEventStart;   //Event Start Time
        public DateTime TimeEventEnd;       //Event End Time
        //public DateTime OpenTime; //Building Open Time
        //public DateTime CloseTime; //Building Close Time

        public string Rooms;
    }

    public class webEvent {
        public webEvent() { }
        public webEvent(DateTime timeEventStart, string activityName, string locationName, bool? displayOnWeb) {
            this.TimeEventStart = timeEventStart;
            this.ActivityName = activityName;
            this.LocationName = locationName;
            this.DisplayOnWeb = displayOnWeb;
        }

        public webEvent(DateTime timeEventStart, string activityName, string locationName) : 
            this(timeEventStart, activityName, locationName, null){
        }

        public DateTime TimeEventStart { get; set; }
        public string ActivityName { get; set; }
        public string LocationName { get; set; }
        public bool? DisplayOnWeb { get; set; }
    }
}
