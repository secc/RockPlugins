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
using System.IO;
using System.Data;

namespace org.secc.EMS
{
    public class API
    {
        public List<webEvent> GetWebEvents( DateTime beginDate, DateTime endDate, List<int> buildingIDs, bool viewComboRoomComponents,
          bool filterByDisplayOnWeb, out string errorMessage )
        {
            List<string> eventStatusIDs = null;
            return GetWebEvents( beginDate, endDate, buildingIDs, eventStatusIDs, viewComboRoomComponents, filterByDisplayOnWeb, out errorMessage );
        }

        public List<webEvent> GetWebEvents( DateTime beginDate, DateTime endDate, List<int> buildingIDs, List<string> eventStatusIDs,
          bool viewComboRoomComponents, bool filterByDisplayOnWeb, out string errorMessage )
        {
            errorMessage = "";
            List<BookingsData> events = new List<BookingsData>();
            Bookings tmpBookings = null;
            if ( buildingIDs == null || buildingIDs.Count == 0 )
            {
                tmpBookings = new APIMethods().GetAllBookings( beginDate, endDate, -1, viewComboRoomComponents, out errorMessage );
                if ( tmpBookings != null && tmpBookings.Items != null )
                    events = tmpBookings.Items.ToList();
            }
            else
                foreach ( var buildingID in buildingIDs )
                {
                    tmpBookings = new APIMethods().GetAllBookings( beginDate, endDate, buildingID, viewComboRoomComponents, out errorMessage );
                    if ( tmpBookings != null && tmpBookings.Items != null )
                        events.AddRange( tmpBookings.Items.ToList() );
                }

            if ( filterByDisplayOnWeb )
            {
                if ( errorMessage == string.Empty && events != null )
                {
                    return FilterEventsForWeb( events, false, true, eventStatusIDs, out errorMessage );
                }
                else
                    return null;
            }
            else
            {
                if ( errorMessage == string.Empty && events != null )
                {
                    return FilterEventsForWeb( events, false, false, eventStatusIDs, out errorMessage );
                }
                else
                {
                    return null;
                }
            }
        }

        public List<webEvent> FilterEventsForWeb( List<BookingsData> events, bool showSetup, bool showWebEnabledOnly, List<string> eventStatusIDs, out string errorMessage )
        {
            if ( eventStatusIDs == null || eventStatusIDs.Count == 0 )
            {
                eventStatusIDs = Settings.Default_web_event_status_ids;
            }

            var eventTypes = new APIMethods().GetEventTypes( out errorMessage );
            var webEnabledEventTypes = ( ( EventTypes ) eventTypes ).Items.Where( et => et.DisplayOnWeb == "true" ).Select( et => et.ID );
            var webEvents = events.Where( b => ( !showWebEnabledOnly || webEnabledEventTypes.Contains( b.EventTypeID ) ) &&
                     ( showSetup || !b.EventName.Trim().StartsWith( "SET UP" ) ) &&
                     ( ( eventStatusIDs.Count == 0 || eventStatusIDs.Contains( b.StatusID.ToString() ) ) ) )
                .OrderBy( b => DateTime.Parse( b.TimeEventStart ) ).ThenBy( b => b.EventName )
                .Select( b => new webEvent( DateTime.Parse( b.TimeBookingStart ), DateTime.Parse( b.TimeBookingEnd ), DateTime.Parse( b.TimeEventStart ), DateTime.Parse( b.TimeEventEnd ), b.EventName, b.RoomCode, true ) ).ToList<webEvent>();
            return webEvents;
        }


        public IEnumerable<zoneEvent> GetHVACZones( DateTime beginDate, DateTime endDate, int? buildingID, bool viewComboRoomComponents, out string errorMessage )
        {
            errorMessage = "";
            var events = new APIMethods().GetAllBookings( beginDate, endDate, ( buildingID.HasValue ? buildingID.Value : -1 ), viewComboRoomComponents, out errorMessage );
            if ( errorMessage == string.Empty && events != null )
            {
                return CombineAdjacentEventsInZone( events );
            }
            else
            {
                return null;
            }
        }

        public List<zoneEvent> CombineAdjacentEventsInZone( Bookings events )
        {
            var zoneEvents =
                ( from b in ( ( Bookings ) events ).Items
                  where b.HVACZone != string.Empty && !b.EventName.Trim().StartsWith( "SET UP" )
                      && ( Settings.HVAC_enabled_booking_status_ids.Count == 0 || Settings.HVAC_enabled_booking_status_ids.Contains( b.StatusID.ToString() ) )
                  from z in b.HVACZone.Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries )
                  orderby z.Trim(), DateTime.Parse( b.TimeBookingStart ), DateTime.Parse( b.TimeEventEnd )
                  select new zoneEvent( z.Trim(), DateTime.Parse( b.TimeBookingStart ), DateTime.Parse( b.TimeEventEnd ), b.BuildingCode + "-" + b.RoomCode ) ).ToList<zoneEvent>();

            var zoneEventsCombined = new List<zoneEvent>();
            zoneEvent currZone = null;
            foreach ( var booking in zoneEvents )
            {
                if ( currZone == null )
                    currZone = new zoneEvent( booking.HVACZone, booking.TimeBookingStart, booking.TimeEventEnd, booking.Rooms );
                if ( currZone.HVACZone != booking.HVACZone || booking.TimeBookingStart.CompareTo( currZone.TimeEventEnd ) > 0 )
                {
                    //Save current zone
                    zoneEventsCombined.Add( currZone );
                    //Initialize new zone
                    currZone = new zoneEvent( booking.HVACZone, booking.TimeBookingStart, booking.TimeEventEnd, booking.Rooms );
                }
                else
                {
                    //Contiguous event in HVAC zone
                    if ( booking.TimeEventEnd > currZone.TimeEventEnd )
                        currZone.TimeEventEnd = booking.TimeEventEnd;
                    currZone.Rooms += ( currZone.Rooms != string.Empty ? "," : "" ) + booking.Rooms;
                }
            }
            //Save the last zone
            if ( currZone != null )
                zoneEventsCombined.Add( currZone );

            return zoneEventsCombined;
        }
    }
}
