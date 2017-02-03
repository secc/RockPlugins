//Convert XML into strongly typed class: 
//1) Save the result XML from the web service 
//(see commented code in GetBookings) and save it
//2) C:\Program Files (x86)\Microsoft SDKs\Windows\v7.0A\Bin\x64>xsd "C:\MyData\Temp\EMS\myFile.xml" /out:"C:\MyData\Temp\EMS"
//3) C:\Program Files (x86)\Microsoft SDKs\Windows\v7.0A\Bin\x64>xsd "C:\MyData\Temp\EMS\myFile.xsd" /c /out:"C:\MyData\Temp\EMS"

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Data;
using System.Xml.Serialization;

namespace org.secc.EMS {
    internal class APIMethods {
        internal Bookings GetAllBookings(DateTime beginDate, DateTime endDate, int? buildingID, bool viewComboRoomComponents, out string errorMessage) {
            Bookings bookings;
            var soapClient = new org.secc.EMS.EMSServiceRef.ServiceSoapClient();
            var bookingsXML = soapClient.GetAllBookings(Settings1.Default.api_user, Settings1.Default.api_pw,
                beginDate, endDate, (buildingID.HasValue ? buildingID.Value : -1), viewComboRoomComponents);
            if (bookingsXML.StartsWith("<Errors>")) {
                var errorSerializer = new XmlSerializer(typeof(Errors));
                bookings = null;
                errorMessage = "Error occurred: " + ((Errors)errorSerializer.Deserialize(new StringReader(bookingsXML))).Items[0].Message;
            } else {
                var bookingsSerializer = new XmlSerializer(typeof(Bookings));
                bookings = (Bookings)bookingsSerializer.Deserialize(new StringReader(bookingsXML));
                if (bookings.Items == null)
                    bookings = null;
                //errorMessage = (bookings == null) ? "There are no events" : "";   //Not really an error
                errorMessage = "";
            }

            //if (dataSet.Tables.Count == 0)
            //    errorMessage = "No data returned";
            //else if (dataSet.Tables[0].TableName == "Error")
            //    errorMessage = "Error occurred: " + dataSet.Tables["Error"].Rows[0].ItemArray[0].ToString();  //TODO //Get error message + dataSet.Tables["Error"].
            //else

            return bookings;
        }

        internal EventTypes GetEventTypes(out string errorMessage) {
            EventTypes eventTypes;
            var soapClient = new org.secc.EMS.EMSServiceRef.ServiceSoapClient();
            var eventTypesXML = soapClient.GetEventTypes(Settings1.Default.api_user, Settings1.Default.api_pw);
            if (eventTypesXML.StartsWith("<Errors>")) {
                var errorSerializer = new XmlSerializer(typeof(Errors));
                eventTypes = null;
                errorMessage = "Error occurred: " + ((Errors)errorSerializer.Deserialize(new StringReader(eventTypesXML))).Items[0].Message;
            } else {
                var eventTypesSerializer = new XmlSerializer(typeof(EventTypes));
                eventTypes = (EventTypes)eventTypesSerializer.Deserialize(new StringReader(eventTypesXML));
                if (eventTypes.Items == null)
                    eventTypes = null;
                errorMessage = (eventTypes == null) ? "There are no event types" : "";
            }
//errorMessage = eventTypesXML;

            return eventTypes;
        }

//        internal DataSet GetAllBookings(DateTime beginDate, DateTime endDate, int? buildingID, bool viewComboRoomComponents, out string errorMessage, ref object debugObj) {
//            var soapClient = new org.secc.EMS.EMSServiceRef.ServiceSoapClient();
//            var dataSet = new DataSet("Bookings");
//            dataSet.ReadXml(new StringReader(soapClient.GetAllBookings(Settings1.Default.api_user, Settings1.Default.api_pw,
//                beginDate, endDate, (buildingID.HasValue ? buildingID.Value : -1), viewComboRoomComponents)));

//var xmlSerializer = new XmlSerializer(typeof(Bookings));
//debugObj = xmlSerializer.Deserialize(new StringReader(soapClient.GetAllBookings(Settings1.Default.api_user, Settings1.Default.api_pw,
//    beginDate, endDate, (buildingID.HasValue ? buildingID.Value : -1), viewComboRoomComponents)));

//            if (dataSet.Tables.Count == 0)
//                errorMessage = "No data returned";
//            else if (dataSet.Tables[0].TableName == "Error")
//                errorMessage = "Error occurred: " + dataSet.Tables["Error"].Rows[0].ItemArray[0].ToString();  //TODO //Get error message + dataSet.Tables["Error"].
//            else
//                errorMessage = "";

//            ////To Save XML
//            //errorMessage = (new StringReader(soapClient.GetAllBookings(Settings1.Default.api_user, Settings1.Default.api_pw,
//            //    beginDate, endDate, (buildingID.HasValue ? buildingID.Value : -1), viewComboRoomComponents))).ReadToEnd();

//            return dataSet;
//        }


    }

}
