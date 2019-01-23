// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License shoud be included with this file.
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
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
        public DateTime TimeBookingEnd;   //Reserved End Time
        public DateTime TimeEventStart;   //Event Start Time
        public DateTime TimeEventEnd;       //Event End Time
        //public DateTime OpenTime; //Building Open Time
        //public DateTime CloseTime; //Building Close Time

        public string Rooms;
    }

    public class webEvent {
        public webEvent() { }
        public webEvent( DateTime timeBookingStart, DateTime timeBookingEnd, DateTime timeEventStart, DateTime timeEventEnd, string activityName, string locationName, bool? displayOnWeb) {
            this.TimeBookingStart = timeBookingStart;
            this.TimeBookingEnd = timeBookingEnd;
            this.TimeEventStart = timeEventStart;
            this.TimeEventEnd = timeEventEnd;
            this.ActivityName = activityName;
            this.LocationName = locationName;
            this.DisplayOnWeb = displayOnWeb;
        }

        public webEvent( DateTime timeBookingStart, DateTime timeBookingEnd, DateTime timeEventStart, DateTime timeEventEnd, string activityName, string locationName) : 
            this(timeBookingStart, timeBookingEnd, timeEventStart, timeEventEnd, activityName, locationName, null){
        }

        public DateTime TimeBookingStart { get; set; }
        public DateTime TimeBookingEnd { get; set; }
        public DateTime TimeEventStart { get; set; }
        public DateTime TimeEventEnd { get; set; }
        public string ActivityName { get; set; }
        public string LocationName { get; set; }
        public bool? DisplayOnWeb { get; set; }
    }
}
