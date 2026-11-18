// <copyright>
// Copyright Southeast Christian Church
//
// Licensed under the  Southeast Christian Church License (the "License");
// you may not use this file except in compliance with the License.
// A copy of the License should be included with this file.
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
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using Rock;
using Rock.CheckIn;
using Rock.Data;
using Rock.Model;
using CheckInLabel = Rock.CheckIn.CheckInLabel;

namespace org.secc.FamilyCheckin.Utilities
{
    public class LabelPrinter
    {
        public CheckInState CheckInState { get; set; }

        public HttpRequest Request { get; set; }

        public List<CheckInLabel> Labels { get; set; }

        /// <summary>
        /// Gets or sets the TCP port that network label printers listen on.
        /// </summary>
        internal int PrinterPort { get; set; } = 9100;

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
            // Print server labels
            if ( Labels.Any( l => l.PrintFrom == Rock.Model.PrintFrom.Server ) )
            {
                string delayCut = @"^XB";
                string endingTag = @"^XZ";

                // each printer's labels, with the printers in the order they first appear
                var printerAddresses = new List<string>();
                var printerLabels = new Dictionary<string, List<string>>();

                // make sure labels have a valid ip
                foreach ( var label in Labels.Where( l => l.PrintFrom == PrintFrom.Server && !string.IsNullOrEmpty( l.PrinterAddress ) ) )
                {
                    var printContent = GetLabelContent( label.FileGuid );
                    if ( printContent != null )
                    {
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

                        List<string> contents;
                        if ( !printerLabels.TryGetValue( label.PrinterAddress, out contents ) )
                        {
                            contents = new List<string>();
                            printerLabels.Add( label.PrinterAddress, contents );
                            printerAddresses.Add( label.PrinterAddress );
                        }

                        contents.Add( printContent );
                    }
                }

                foreach ( var printerAddress in printerAddresses )
                {
                    var contents = printerLabels[printerAddress];
                    var labelContent = new StringBuilder();
                    for ( int i = 0; i < contents.Count; i++ )
                    {
                        // send a Delay Cut command on all but this printer's last label so its strip is cut once, at the end
                        labelContent.Append( i < contents.Count - 1
                            ? Regex.Replace( contents[i].Trim(), @"\" + endingTag + @"$", delayCut + endingTag )
                            : contents[i] );
                    }

                    SendToPrinter( printerAddress, labelContent.ToString() );
                }
            }
        }

        /// <summary>
        /// Sends label content to the network printer at the specified address. A failure is written
        /// to the exception log, and the connection is always closed.
        /// </summary>
        /// <param name="address">The printer's IP address.</param>
        /// <param name="content">The label content.</param>
        private void SendToPrinter( string address, string content )
        {
            try
            {
                using ( var socket = new Socket( AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp ) )
                {
                    var result = socket.BeginConnect( new IPEndPoint( IPAddress.Parse( address ), PrinterPort ), null, null );
                    if ( !WaitForConnection( result ) )
                    {
                        LogPrintError( new Exception( "Could not connect to the label printer at " + address + "." ) );
                        return;
                    }

                    socket.EndConnect( result );
                    using ( var ns = new NetworkStream( socket ) )
                    {
                        byte[] toSend = System.Text.Encoding.ASCII.GetBytes( content );
                        ns.Write( toSend, 0, toSend.Length );
                    }

                    socket.Shutdown( SocketShutdown.Both );
                }
            }
            catch ( Exception ex )
            {
                LogPrintError( new Exception( "Could not print to the label printer at " + address + ".", ex ) );
            }
        }

        /// <summary>
        /// Gets the ZPL content of the label file with the specified unique identifier, or null if there is none.
        /// </summary>
        protected virtual string GetLabelContent( Guid fileGuid ) => KioskLabel.Get( fileGuid )?.FileContent;

        /// <summary>
        /// Waits up to five seconds for a printer connection and returns whether it completed.
        /// </summary>
        protected virtual bool WaitForConnection( IAsyncResult connect ) => connect.AsyncWaitHandle.WaitOne( 5000, true );

        /// <summary>
        /// Writes a printing error to the Rock exception log.
        /// </summary>
        protected virtual void LogPrintError( Exception ex ) => ExceptionLogService.LogException( ex );

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
