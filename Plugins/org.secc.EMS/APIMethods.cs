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
//Convert XML into strongly typed class: 
//1) Save the result XML from the web service 
//(see commented code in GetBookings) and save it
//2) C:\Program Files (x86)\Microsoft SDKs\Windows\v7.0A\Bin\x64>xsd "C:\MyData\Temp\EMS\myFile.xml" /out:"C:\MyData\Temp\EMS"
//3) C:\Program Files (x86)\Microsoft SDKs\Windows\v7.0A\Bin\x64>xsd "C:\MyData\Temp\EMS\myFile.xsd" /c /out:"C:\MyData\Temp\EMS"

using System;
using System.IO;
using System.Xml.Serialization;

namespace org.secc.EMS
{
    internal class APIMethods
    {
        internal Bookings GetAllBookings( DateTime beginDate, DateTime endDate, int? buildingID, bool viewComboRoomComponents, out string errorMessage )
        {
            Bookings bookings;
            var soapClient = new org.secc.EMS.EMSServiceRef.ServiceSoapClient();
            var bookingsXML = soapClient.GetAllBookings( Settings.UserName, Settings.Password,
                beginDate, endDate, ( buildingID.HasValue ? buildingID.Value : -1 ), viewComboRoomComponents );
            if ( bookingsXML.StartsWith( "<Errors>" ) )
            {
                var errorSerializer = new XmlSerializer( typeof( Errors ) );
                bookings = null;
                errorMessage = "Error occurred: " + ( ( Errors ) errorSerializer.Deserialize( new StringReader( bookingsXML ) ) ).Items[0].Message;
            }
            else
            {
                var bookingsSerializer = new XmlSerializer( typeof( Bookings ) );
                bookings = ( Bookings ) bookingsSerializer.Deserialize( new StringReader( bookingsXML ) );
                if ( bookings.Items == null )
                    bookings = null;
                errorMessage = "";
            }
            return bookings;
        }

        internal EventTypes GetEventTypes( out string errorMessage )
        {
            EventTypes eventTypes;
            var soapClient = new org.secc.EMS.EMSServiceRef.ServiceSoapClient();
            var eventTypesXML = soapClient.GetEventTypes( Settings.UserName, Settings.Password );
            if ( eventTypesXML.StartsWith( "<Errors>" ) )
            {
                var errorSerializer = new XmlSerializer( typeof( Errors ) );
                eventTypes = null;
                errorMessage = "Error occurred: " + ( ( Errors ) errorSerializer.Deserialize( new StringReader( eventTypesXML ) ) ).Items[0].Message;
            }
            else
            {
                var eventTypesSerializer = new XmlSerializer( typeof( EventTypes ) );
                eventTypes = ( EventTypes ) eventTypesSerializer.Deserialize( new StringReader( eventTypesXML ) );
                if ( eventTypes.Items == null )
                    eventTypes = null;
                errorMessage = ( eventTypes == null ) ? "There are no event types" : "";
            }

            return eventTypes;
        }
    }
}
