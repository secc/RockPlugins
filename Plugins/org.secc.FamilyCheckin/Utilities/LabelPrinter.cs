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
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Newtonsoft.Json;
using Rock;
using Rock.CheckIn;
using Rock.Data;
using Rock.Model;

namespace org.secc.FamilyCheckin.Utilities
{
    public class LabelPrinter
    {
        public CheckInState CheckInState { get; set; }

        public HttpRequest Request { get; set; }
        
        public List<CheckInLabel> Labels { get; set; }

        /// <summary>
        /// Gets a count of the current labesl to print over network
        /// </summary>
        public virtual int NetworkLabelCount
        {
            get
            {
                if ( Labels == null )
                {
                    return 0;
                }
                return Labels.Count( l => l.PrintFrom == Rock.Model.PrintFrom.Server );
            }
        }
        /// <summary>
        /// Gets a count of the current labels to print via usb
        /// </summary>
        public virtual int ClientLabelCount
        {
            get
            {
                if ( Labels == null )
                {
                    return 0;
                }
                return Labels.Count( l => l.PrintFrom == Rock.Model.PrintFrom.Client );
            }
        }

        public LabelPrinter()
        {

        }

        /// <summary>
        /// Creates a label printer object that will process labels upon creation and contains action to print over network or recieve javascript for usb printing
        /// </summary>
        /// <param name="CurrentCheckinState"></param>
        /// <param name="Request"></param>
        public LabelPrinter( CheckInState currentCheckinState, HttpRequest request )
        {
            CheckInState = currentCheckinState;
            Request = request;
            Labels = new List<CheckInLabel>();
            ProcessLabels();
        }

        /// <summary>
        /// performs the action of processing the labels to prepare for printing
        /// </summary>
        private void ProcessLabels()
        {
            foreach ( var selectedFamily in CheckInState.CheckIn.Families.Where( p => p.Selected ) )
            {
                List<CheckInPerson> selectedPeople = selectedFamily.People.Where( p => p.Selected ).ToList();

                using ( var rockContext = new RockContext() )
                {
                    foreach ( CheckInPerson selectedPerson in selectedPeople )
                    {
                        foreach ( var groupType in selectedPerson.GroupTypes.Where( gt => gt.Selected ) )
                        {
                            if ( groupType.Labels != null )
                            {
                                foreach ( var label in groupType.Labels )
                                {
                                    var file = new BinaryFileService( rockContext ).Get( label.FileGuid );
                                    Labels.Add( label );
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns javascript to print labels over usb
        /// </summary>
        /// <returns></returns>
        public string GetClientScript()
        {
            // Print client labels
            if ( Labels.Any( l => l.PrintFrom == Rock.Model.PrintFrom.Client ) )
            {
                var clientLabels = Labels.Where( l => l.PrintFrom == PrintFrom.Client ).ToList();
                var urlRoot = string.Format( "{0}://{1}", Request.Url.Scheme, Request.Url.Authority );
                clientLabels.ForEach( l => l.LabelFile = urlRoot + l.LabelFile );
                return AddLabelScript( clientLabels.ToJson() );
            }
            return "";
        }
        /// <summary>
        /// Fires the action that prints the labels to networked printers
        /// </summary>
        public void PrintNetworkLabels()
        {
            var printQueue = new Dictionary<string, StringBuilder>();
            // Print server labels
            if ( Labels.Any( l => l.PrintFrom == Rock.Model.PrintFrom.Server ) )
            {
                string delayCut = @"^XB";
                string endingTag = @"^XZ";
                var printerIp = string.Empty;
                var labelContent = new StringBuilder();

                // make sure labels have a valid ip
                var lastLabel = Labels.Last();
                foreach ( var label in Labels.Where( l => l.PrintFrom == PrintFrom.Server && !string.IsNullOrEmpty( l.PrinterAddress ) ) )
                {
                    var labelCache = KioskLabel.Get( label.FileGuid );
                    if ( labelCache != null )
                    {
                        if ( printerIp != label.PrinterAddress )
                        {
                            printQueue.AddOrReplace( label.PrinterAddress, labelContent );
                            printerIp = label.PrinterAddress;
                            labelContent = new StringBuilder();
                        }

                        var printContent = labelCache.FileContent;
                        foreach ( var mergeField in label.MergeFields )
                        {
                            if ( !string.IsNullOrWhiteSpace( mergeField.Value ) )
                            {
                                printContent = Regex.Replace( printContent, string.Format( @"(?<=\^FD){0}(?=\^FS)", mergeField.Key ), ZebraFormatString( mergeField.Value ) );
                            }
                            else
                            {
                                printContent = Regex.Replace( printContent, string.Format( @"\^FO.*\^FS\s*(?=\^FT.*\^FD{0}\^FS)", mergeField.Key ), string.Empty );
                                printContent = Regex.Replace( printContent, string.Format( @"\^FD{0}\^FS", mergeField.Key ), "^FD^FS" );
                            }
                        }

                        // send a Delay Cut command at the end to prevent cutting intermediary labels
                        if ( label != lastLabel )
                        {
                            printContent = Regex.Replace( printContent.Trim(), @"\" + endingTag + @"$", delayCut + endingTag );
                        }

                        labelContent.Append( printContent );
                    }
                }

                printQueue.AddOrReplace( printerIp, labelContent );
            }

            if ( printQueue.Any() )
            {
                SendNetworkPrintData( printQueue );
                printQueue.Clear();
            }
        }


        /// <summary>
        /// Prints the labels.
        /// </summary>
        /// <param name="families">The families.</param>
        private void SendNetworkPrintData( Dictionary<string, StringBuilder> printerContent )
        {
            foreach ( var printerIp in printerContent.Keys.Where( k => !string.IsNullOrEmpty( k ) ) )
            {
                StringBuilder labelContent;
                if ( printerContent.TryGetValue( printerIp, out labelContent ) )
                {
                    var socket = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp );
                    var printerIpEndPoint = new IPEndPoint( IPAddress.Parse( printerIp ), 9100 );
                    var result = socket.BeginConnect( printerIpEndPoint, null, null );
                    bool success = result.AsyncWaitHandle.WaitOne( 5000, true );

                    if ( socket.Connected )
                    {
                        var ns = new NetworkStream( socket );
                        byte[] toSend = System.Text.Encoding.ASCII.GetBytes( labelContent.ToString() );
                        ns.Write( toSend, 0, toSend.Length );
                    }
                    else
                    {
                        //phPrinterStatus.Controls.Add(new LiteralControl(string.Format("Can't connect to printer: {0}", printerIp)));
                    }

                    if ( socket != null && socket.Connected )
                    {
                        socket.Shutdown( SocketShutdown.Both );
                        socket.Close();
                    }
                }
            }
        }

        /// <summary>
        /// Adds the label script.
        /// </summary>
        /// <param name="jsonObject">The json object.</param>
        private string AddLabelScript( string jsonObject )
        {
            string script = string.Format( @"
            var labelData = {0};
		    function printLabels() {{
		        ZebraPrintPlugin.printTags(
            	    JSON.stringify(labelData),
            	    function(result) {{
			        }},
			        function(error) {{
				        // error is an array where:
				        // error[0] is the error message
				        // error[1] determines if a re-print is possible (in the case where the JSON is good, but the printer was not connected)
			            console.log('An error occurred: ' + error[0]);
                        navigator.notification.alert(
                            'An error occurred while printing the labels.' + error[0],  // message
                            alertDismissed,         // callback
                            'Error',            // title
                            'Ok'                  // buttonName
                        );
			        }}
                );
	        }}
try{{
            printLabels();
}} catch(e){{}}
            ", jsonObject );
            return script;
        }

        /// <summary>
        /// Formats the Zebra string.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="isJson">if set to <c>true</c> [is json].</param>
        /// <returns></returns>
        private static string ZebraFormatString( string input, bool isJson = false )
        {
            if ( isJson )
            {
                return input.Replace( "é", @"\\82" );  // fix acute e
            }
            else
            {
                return input.Replace( "é", @"\82" );  // fix acute e
            }
        }
    }
}
