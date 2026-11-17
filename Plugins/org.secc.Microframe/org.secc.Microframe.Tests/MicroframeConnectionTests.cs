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
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using org.secc.Microframe.Utilities;

using Xunit;

namespace org.secc.Microframe.Tests
{
    [CollectionDefinition( "Sign sockets", DisableParallelization = true )]
    public class SignSocketCollection
    {
    }

    /// <summary>
    /// ROCK-8848: runs MicroframeConnection.UpdateMessages against a stand-in sign on 127.0.0.1
    /// that records the commands each connection receives and answers from a script.
    /// </summary>
    [Collection( "Sign sockets" )]
    public class MicroframeConnectionTests
    {
        private const string Pin = "1234";

        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds( 5 );
        private static readonly TimeSpan EndTimeout = TimeSpan.FromSeconds( 2 );

        [Fact]
        public void ReplyInTwoSegments_IsReadToItsEnd()
        {
            using ( var sign = new StandInSign( ( connection, command ) =>
                command == "GLA" ? new[] { "GLA12", "34,\0" } : new[] { "OK\0" } ) )
            {
                new MicroframeConnection( "127.0.0.1", sign.Port, Pin ).UpdateMessages( new List<string> { "5678" } );

                Assert.True( sign.WaitForCommands( 3, Timeout ), "commands seen: " + string.Join( " | ", sign.AllCommands() ) );
                Assert.Equal( new[] { "GLA", "S- 1234", "S+ 5678" }, sign.AllCommands() );
            }
        }

        [Fact]
        public void ReplyLongerThanTheReceiveBuffer_IsReadToItsEnd()
        {
            var tags = Enumerable.Range( 1000, 60 ).Select( t => t.ToString() ).ToList();
            var reply = "GLA" + string.Concat( tags.Select( t => t + "," ) ) + "\0";
            Assert.Equal( 304, reply.Length );

            using ( var sign = new StandInSign( ( connection, command ) =>
                command == "GLA" ? new[] { reply } : new[] { "OK\0" } ) )
            {
                var codes = Enumerable.Range( 1000, 59 ).Select( t => t.ToString() ).ToList();
                codes.Add( "2000" );
                new MicroframeConnection( "127.0.0.1", sign.Port, Pin ).UpdateMessages( codes );

                Assert.True( sign.WaitForCommands( 3, Timeout ), "commands seen: " + string.Join( " | ", sign.AllCommands() ) );
                Assert.Equal( new[] { "GLA", "S- 1059", "S+ 2000" }, sign.AllCommands() );
            }
        }

        [Fact]
        public void ReplyInOneSegment_IsRead()
        {
            using ( var sign = new StandInSign( ( connection, command ) =>
                command == "GLA" ? new[] { "GLA1234,\0" } : new[] { "OK\0" } ) )
            {
                new MicroframeConnection( "127.0.0.1", sign.Port, Pin ).UpdateMessages( new List<string> { "5678" } );

                Assert.True( sign.WaitForCommands( 3, Timeout ), "commands seen: " + string.Join( " | ", sign.AllCommands() ) );
                Assert.Equal( new[] { "GLA", "S- 1234", "S+ 5678" }, sign.AllCommands() );
                Assert.Equal( new byte[] { 12, 34 }, sign.Connections.Single().Pin );
                Assert.True( sign.Connections.Single().WaitForEnd( EndTimeout ), "the connection was not closed" );
            }
        }

        [Fact]
        public void CommandRepliesWithoutAZeroByte_EveryCommandIsSent()
        {
            AssertEveryCommandIsSent( "GLA1234,\0", "OK" );
        }

        [Fact]
        public void RepliesWithBytesAfterTheZeroByte_EveryCommandIsSent()
        {
            AssertEveryCommandIsSent( "GLA1234,\0\r\n", "OK\r\n" );
        }

        /// <summary>
        /// Runs an update against a sign that answers GLA with <paramref name="listReply"/> and every
        /// other command with <paramref name="commandReply"/>, and checks that the client sends all of
        /// its commands on one connection and then closes it.
        /// </summary>
        private static void AssertEveryCommandIsSent( string listReply, string commandReply )
        {
            using ( var sign = new StandInSign( ( connection, command ) =>
                command == "GLA" ? new[] { listReply } : new[] { commandReply } ) )
            {
                new MicroframeConnection( "127.0.0.1", sign.Port, Pin ).UpdateMessages( new List<string> { "5678" } );

                Assert.True( sign.WaitForCommands( 3, Timeout ), "commands seen: " + string.Join( " | ", sign.AllCommands() ) );
                Assert.Equal( new[] { "GLA", "S- 1234", "S+ 5678" }, sign.AllCommands() );
                Assert.True( sign.Connections.Single().WaitForEnd( EndTimeout ), "the connection was not closed" );
            }
        }

        [Fact]
        public void Retry_ClosesThePreviousConnection()
        {
            // The first connection answers with an empty reply, which makes the client reconnect.
            using ( var sign = new StandInSign( ( connection, command ) =>
                connection == 0 ? new[] { "\0" } : command == "GLA" ? new[] { "GLA1234,\0" } : new[] { "OK\0" } ) )
            {
                new MicroframeConnection( "127.0.0.1", sign.Port, Pin ).UpdateMessages( new List<string> { "5678" } );

                Assert.True( sign.WaitForConnections( 2, Timeout ), "the client did not reconnect" );
                Assert.True( sign.Connections[0].WaitForEnd( EndTimeout ), "the first connection was not closed" );

                Assert.True( sign.WaitForCommands( 4, Timeout ), "commands seen: " + string.Join( " | ", sign.AllCommands() ) );
                Assert.Equal( new[] { "GLA", "S- 1234", "S+ 5678" }, sign.Connections[1].Commands );
            }
        }

        [Fact]
        public void GivingUpAfterRetries_ClosesEveryConnection()
        {
            using ( var sign = new StandInSign( ( connection, command ) => new[] { "\0" } ) )
            {
                new MicroframeConnection( "127.0.0.1", sign.Port, Pin ).UpdateMessages( new List<string> { "5678" } );

                Assert.True( sign.WaitForConnections( 10, Timeout ), "connections seen: " + sign.Connections.Count );
                var connections = sign.Connections;
                for ( int i = 0; i < connections.Count; i++ )
                {
                    Assert.True( connections[i].WaitForEnd( EndTimeout ), "connection " + ( i + 1 ) + " was not closed" );
                }

                Assert.Equal( 10, sign.Connections.Count );
            }
        }

        /// <summary>
        /// A stand-in sign: accepts connections on 127.0.0.1, reads frames of two PIN bytes and an
        /// ASCII command ending in a zero byte, records each command, and writes the scripted reply
        /// segments with a pause between them.
        /// </summary>
        private sealed class StandInSign : IDisposable
        {
            private readonly TcpListener _listener = new TcpListener( IPAddress.Parse( "127.0.0.1" ), 0 );
            private readonly Func<int, string, string[]> _reply;
            private readonly List<SignConnection> _connections = new List<SignConnection>();
            private bool _stopped;

            public StandInSign( Func<int, string, string[]> reply )
            {
                _reply = reply;
                _listener.Start();
                Port = ( ( IPEndPoint ) _listener.LocalEndpoint ).Port;
                new Thread( AcceptLoop ) { IsBackground = true }.Start();
            }

            public int Port { get; }

            public List<SignConnection> Connections
            {
                get
                {
                    lock ( _connections )
                    {
                        return _connections.ToList();
                    }
                }
            }

            public List<string> AllCommands()
            {
                return Connections.SelectMany( c => c.Commands ).ToList();
            }

            public bool WaitForConnections( int count, TimeSpan timeout )
            {
                return WaitUntil( () => Connections.Count >= count, timeout );
            }

            public bool WaitForCommands( int count, TimeSpan timeout )
            {
                return WaitUntil( () => AllCommands().Count >= count, timeout );
            }

            private static bool WaitUntil( Func<bool> condition, TimeSpan timeout )
            {
                var deadline = DateTime.UtcNow + timeout;
                while ( !condition() )
                {
                    if ( DateTime.UtcNow > deadline )
                    {
                        return false;
                    }

                    Thread.Sleep( 10 );
                }

                return true;
            }

            private void AcceptLoop()
            {
                while ( true )
                {
                    TcpClient client;
                    try
                    {
                        client = _listener.AcceptTcpClient();
                    }
                    catch ( Exception ex ) when ( ex is SocketException || ex is ObjectDisposedException || ex is InvalidOperationException )
                    {
                        return;
                    }

                    lock ( _connections )
                    {
                        if ( _stopped )
                        {
                            client.Close();
                            return;
                        }

                        _connections.Add( new SignConnection( client, _connections.Count, _reply ) );
                    }
                }
            }

            public void Dispose()
            {
                lock ( _connections )
                {
                    _stopped = true;
                }

                _listener.Stop();
                foreach ( var connection in Connections )
                {
                    connection.Dispose();
                }
            }
        }

        private sealed class SignConnection : IDisposable
        {
            private readonly TcpClient _client;
            private readonly int _index;
            private readonly Func<int, string, string[]> _reply;
            private readonly List<string> _commands = new List<string>();
            private readonly ManualResetEventSlim _ended = new ManualResetEventSlim( false );

            public SignConnection( TcpClient client, int index, Func<int, string, string[]> reply )
            {
                _client = client;
                _client.NoDelay = true;
                _index = index;
                _reply = reply;
                new Thread( ReadLoop ) { IsBackground = true }.Start();
            }

            public byte[] Pin { get; private set; }

            public List<string> Commands
            {
                get
                {
                    lock ( _commands )
                    {
                        return _commands.ToList();
                    }
                }
            }

            public bool WaitForEnd( TimeSpan timeout )
            {
                return _ended.Wait( timeout );
            }

            private void ReadLoop()
            {
                var buffer = new byte[1024];
                var pin = new List<byte>();
                var command = new StringBuilder();
                try
                {
                    var stream = _client.GetStream();
                    int read;
                    while ( ( read = stream.Read( buffer, 0, buffer.Length ) ) > 0 )
                    {
                        for ( int i = 0; i < read; i++ )
                        {
                            if ( pin.Count < 2 )
                            {
                                pin.Add( buffer[i] );
                            }
                            else if ( buffer[i] != 0 )
                            {
                                command.Append( ( char ) buffer[i] );
                            }
                            else
                            {
                                Pin = pin.ToArray();
                                var text = command.ToString();
                                lock ( _commands )
                                {
                                    _commands.Add( text );
                                }

                                pin.Clear();
                                command.Clear();

                                var segments = _reply( _index, text );
                                for ( int s = 0; s < segments.Length; s++ )
                                {
                                    if ( s > 0 )
                                    {
                                        Thread.Sleep( 200 );
                                    }

                                    var bytes = Encoding.ASCII.GetBytes( segments[s] );
                                    stream.Write( bytes, 0, bytes.Length );
                                }
                            }
                        }
                    }
                }
                catch ( Exception ex ) when ( ex is IOException || ex is SocketException || ex is ObjectDisposedException || ex is InvalidOperationException )
                {
                    // A reset or a teardown also ends the connection.
                }
                finally
                {
                    _ended.Set();
                }
            }

            public void Dispose()
            {
                _client.Close();
            }
        }
    }
}
