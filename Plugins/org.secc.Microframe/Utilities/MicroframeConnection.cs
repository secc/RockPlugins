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
using System.Text;
using System.Threading.Tasks;

namespace org.secc.Microframe.Utilities
{
    internal class StateObject
    {
        public Socket socket = null;
        public const int BufferSize = 256;
        public byte[] buffer = new byte[BufferSize];
        public StringBuilder sb = new StringBuilder();
        public EndPoint endPoint = null;
        public List<string> messages = new List<string>();
        public List<string> codes = new List<string>();
        public int resets = 0;
        public bool update = false;
        public byte[] PIN;
    }

    public class MicroframeConnection
    {
        private IPEndPoint _remoteEP;
        private byte[] _PIN;

        public MicroframeConnection( string IP, int port, string PIN )
        {
            IPAddress ipAddress = IPAddress.Parse( IP );
            _remoteEP = new IPEndPoint( ipAddress, port );
            _PIN = ConvertPIN( PIN );
        }

        private byte[] ConvertPIN( string PIN )
        {
            if ( PIN.Length != 4 )
            {
                return new byte[2] { 0x00, 0x00 };
            }

            string PinA = PIN.Substring( 0, 2 );
            string PinB = PIN.Substring( 2, 2 );

            byte byteA = Convert.ToByte( Convert.ToInt32( PinA ));
            byte byteB = Convert.ToByte( Convert.ToInt32( PinB ) );

            return new byte[2] { byteA, byteB };
        }

        public void UpdateMessages( List<string> codes )
        {
            try
            {
                StateObject state = new StateObject();
                state.codes = codes;
                state.update = true;
                state.endPoint = _remoteEP;
                state.PIN = _PIN;
                state.messages.Add( "GLA" );
                Connect( state );
            }
            catch ( Exception e )
            {
                Console.WriteLine( e.ToString() );
            }

        }

        private static void Connect( StateObject state )
        {
            try
            {
                //Watchdog counter
                if ( state.resets == 10 )
                {
                    Console.WriteLine( "reconnect failed" );
                    return;
                }
                Console.WriteLine( "Connecting" );
                Socket client = new Socket( AddressFamily.InterNetwork,
                            SocketType.Stream, ProtocolType.Tcp );
                state.socket = client;
                client.BeginConnect( state.endPoint,
                            new AsyncCallback( ConnectCallback ), state );
            }
            catch
            {

            }
        }


        private static void ConnectCallback( IAsyncResult ar )
        {
            StateObject state = ( StateObject ) ar.AsyncState;
            try
            {
                var client = state.socket;
                client.EndConnect( ar );

                if ( state.messages.Any() )
                {
                    Send( state );
                }
                else
                {
                    CloseConnection( state );
                }
            }
            catch
            {
                state.resets++;
                Connect( state );
            }
        }

        private static void Send( StateObject state )
        {
            try
            {
                if ( state.messages.Any() )
                {
                    var message = state.messages[0];
                    byte[] byteData = state.PIN
                        .Concat( Encoding.ASCII.GetBytes( message ) )
                        .Concat( new byte[] { 0x00 } )
                        .ToArray();

                    state.sb = new StringBuilder();

                    var client = state.socket;
                    client.BeginSend( byteData, 0, byteData.Length, 0,
                        new AsyncCallback( SendToRecv ), state );
                }
            }
            catch
            {
                state.resets++;
                Connect( state );
            }
        }

        private static void SendToRecv( IAsyncResult ar )
        {
            StateObject state = ( StateObject ) ar.AsyncState;
            try
            {

                var client = state.socket;
                client.EndSend( ar );
                client.BeginReceive( state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback( RecvCallback ), state );
            }
            catch
            {
                state.resets++;
                Connect( state );
            }
        }

        private static void RecvCallback( IAsyncResult ar )
        {
            StateObject state = ( StateObject ) ar.AsyncState;
            try
            {
                var client = state.socket;
                int bytesRead = client.EndReceive( ar );

                if ( bytesRead > 0 )
                {
                    state.sb.Append( Encoding.ASCII.GetString( state.buffer, 0, bytesRead ) );

                    if ( state.buffer.Last() != '\0' )
                    {
                        client.BeginReceive( state.buffer, 0, StateObject.BufferSize, 0,
                            new AsyncCallback( RecvCallback ), state );
                        return;
                    }
                }
                if ( state.sb.Length > 1 )
                {
                    Console.WriteLine( "Response: " + state.sb.ToString() );
                    state.resets = 0;
                    ParseResponse( state );
                }
                else
                {
                    state.resets++;
                    Connect( state );
                }
            }
            catch
            {
                state.resets++;
                Connect( state );
            }

        }
        private static void ParseResponse( StateObject state )
        {
            state.messages.RemoveAt( 0 );
            string response = state.sb.ToString();
            if ( response.IndexOf( "GLA" ) == 0 )
            {
                int end = response.IndexOf( '\0' );
                response = response.Substring( 3, end - 3 );
                var tags = response.Split( new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries ).ToList();
                foreach ( var tag in tags )
                {
                    if ( !state.codes.Contains( tag ) )
                    {
                        state.messages.Add( "S- " + tag );
                    }
                }
                if ( state.update )
                {
                    foreach ( var code in state.codes )
                    {
                        if ( !tags.Contains( code ) )
                        {
                            var str = code;
                            if ( code.Length > 4 )
                            {
                                str = code.Substring( 0, 4 );
                            }
                            state.messages.Add( "S+ " + str );
                        }
                    }
                }
            }
            if ( state.messages.Any() )
            {
                Send( state );
            }
            else
            {
                CloseConnection( state );
            }
        }

        private static void CloseConnection( StateObject state )
        {
            var client = state.socket;
            client.Shutdown( SocketShutdown.Both );
            client.Close();
        }
    }
}
