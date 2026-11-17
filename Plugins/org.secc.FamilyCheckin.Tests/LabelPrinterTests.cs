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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

using org.secc.FamilyCheckin.Utilities;
using Rock.Model;

using Xunit;

using CheckInLabel = Rock.CheckIn.CheckInLabel;

namespace org.secc.FamilyCheckin.Tests
{
    [CollectionDefinition( "Label printer sockets", DisableParallelization = true )]
    public class LabelPrinterSocketCollection
    {
    }

    /// <summary>
    /// ROCK-8771, ROCK-8848: runs LabelPrinter.PrintNetworkLabels against stand-in printers that
    /// listen on 127.0.0.1 and 127.0.0.2 and record what each connection receives. The label file
    /// lookup and the exception log are replaced, so no database is used.
    /// </summary>
    [Collection( "Label printer sockets" )]
    public class LabelPrinterTests : IDisposable
    {
        private static readonly IPAddress AddressA = IPAddress.Parse( "127.0.0.1" );
        private static readonly IPAddress AddressB = IPAddress.Parse( "127.0.0.2" );
        private const string PrinterA = "127.0.0.1";
        private const string PrinterB = "127.0.0.2";

        private static readonly TimeSpan Timeout = TimeSpan.FromSeconds( 5 );
        private static readonly TimeSpan EndTimeout = TimeSpan.FromSeconds( 2 );
        private static readonly TimeSpan QuietPeriod = TimeSpan.FromMilliseconds( 250 );

        private readonly int _port;
        private readonly StandInPrinter _printerA;
        private readonly StandInPrinter _printerB;
        private readonly Dictionary<Guid, string> _labelFiles = new Dictionary<Guid, string>();

        public LabelPrinterTests()
        {
            // Both stand-in printers share one port, as real printers do.
            for ( int attempt = 0; attempt < 20 && _printerB == null; attempt++ )
            {
                var listenerA = new TcpListener( AddressA, 0 );
                listenerA.Start();
                var port = ( ( IPEndPoint ) listenerA.LocalEndpoint ).Port;
                var listenerB = new TcpListener( AddressB, port );
                try
                {
                    listenerB.Start();
                }
                catch ( SocketException )
                {
                    listenerA.Stop();
                    continue;
                }

                _port = port;
                _printerA = new StandInPrinter( listenerA );
                _printerB = new StandInPrinter( listenerB );
            }

            if ( _printerB == null )
            {
                throw new InvalidOperationException( "Could not find a port that is free on both 127.0.0.1 and 127.0.0.2." );
            }
        }

        public void Dispose()
        {
            _printerA?.Dispose();
            _printerB?.Dispose();
        }

        #region Tests

        [Fact]
        public void TwoLabelsThenOtherPrinter_EachPrinterGetsItsLabelsAndLastLabelIsCut()
        {
            var a1 = AddLabelFile( "A1" );
            var a2 = AddLabelFile( "A2" );
            var b1 = AddLabelFile( "B1" );

            var printer = NewPrinter( ServerLabel( PrinterA, a1 ), ServerLabel( PrinterA, a2 ), ServerLabel( PrinterB, b1 ) );
            printer.PrintNetworkLabels();

            Assert.Equal( new[] { Cut( "A1" ) + Zpl( "A2" ) }, Jobs( _printerA, 1 ) );
            Assert.Equal( new[] { Zpl( "B1" ) }, Jobs( _printerB, 1 ) );
            Assert.Empty( printer.LoggedErrors );
        }

        [Fact]
        public void PrinterLabelsSeparatedByOtherPrinter_AreSentTogetherOnOneConnection()
        {
            var a1 = AddLabelFile( "A1" );
            var b1 = AddLabelFile( "B1" );
            var a2 = AddLabelFile( "A2" );

            var printer = NewPrinter( ServerLabel( PrinterA, a1 ), ServerLabel( PrinterB, b1 ), ServerLabel( PrinterA, a2 ) );
            printer.PrintNetworkLabels();

            Assert.Equal( new[] { Cut( "A1" ) + Zpl( "A2" ) }, Jobs( _printerA, 1 ) );
            Assert.Equal( new[] { Zpl( "B1" ) }, Jobs( _printerB, 1 ) );
            Assert.Empty( printer.LoggedErrors );
        }

        [Fact]
        public void AlternatingPrinters_EachPrinterGetsBothOfItsLabelsInOrder()
        {
            var a1 = AddLabelFile( "A1" );
            var b1 = AddLabelFile( "B1" );
            var a2 = AddLabelFile( "A2" );
            var b2 = AddLabelFile( "B2" );

            var printer = NewPrinter(
                ServerLabel( PrinterA, a1 ),
                ServerLabel( PrinterB, b1 ),
                ServerLabel( PrinterA, a2 ),
                ServerLabel( PrinterB, b2 ) );
            printer.PrintNetworkLabels();

            Assert.Equal( new[] { Cut( "A1" ) + Zpl( "A2" ) }, Jobs( _printerA, 1 ) );
            Assert.Equal( new[] { Cut( "B1" ) + Zpl( "B2" ) }, Jobs( _printerB, 1 ) );
            Assert.Empty( printer.LoggedErrors );
        }

        [Fact]
        public void SinglePrinter_ChildLabelsThenFamilyLabel_SendsExpectedBytesOnOneConnection()
        {
            var child1 = AddLabelFile( "Child1" );
            var child2 = AddLabelFile( "Child2" );
            var family = AddLabelFile( "Family" );

            var printer = NewPrinter( ServerLabel( PrinterA, child1 ), ServerLabel( PrinterA, child2 ), ServerLabel( PrinterA, family ) );
            printer.PrintNetworkLabels();

            var expected =
                "^XA\n^FO10,10^FDChild1^FS\n^XB^XZ" +
                "^XA\n^FO10,10^FDChild2^FS\n^XB^XZ" +
                "^XA\n^FO10,10^FDFamily^FS\n^XZ\n";
            Assert.Equal( new[] { expected }, Jobs( _printerA, 1 ) );
            Assert.Empty( Jobs( _printerB, 0 ) );
            Assert.Empty( printer.LoggedErrors );
        }

        [Fact]
        public void SinglePrinter_LastLabelHasNoPrinter_LastPrintedLabelIsCut()
        {
            var a1 = AddLabelFile( "A1" );
            var a2 = AddLabelFile( "A2" );
            var x = AddLabelFile( "X" );

            var printer = NewPrinter( ServerLabel( PrinterA, a1 ), ServerLabel( PrinterA, a2 ), ServerLabel( null, x ) );
            printer.PrintNetworkLabels();

            Assert.Equal( new[] { Cut( "A1" ) + Zpl( "A2" ) }, Jobs( _printerA, 1 ) );
            Assert.Empty( printer.LoggedErrors );
        }

        [Fact]
        public void ClientLabelsLabelsWithoutPrinterAndLabelsWithoutContent_AreNotSent()
        {
            var clientFile = AddLabelFile( "Client" );
            var noPrinterFile = AddLabelFile( "NoPrinter" );
            var missingFile = Guid.NewGuid();
            var a1 = AddLabelFile( "A1" );

            var clientLabel = ServerLabel( PrinterB, clientFile );
            clientLabel.PrintFrom = PrintFrom.Client;

            var printer = NewPrinter(
                clientLabel,
                ServerLabel( null, noPrinterFile ),
                ServerLabel( string.Empty, noPrinterFile ),
                ServerLabel( PrinterB, missingFile ),
                ServerLabel( PrinterA, a1 ) );
            printer.PrintNetworkLabels();

            Assert.Equal( new[] { Zpl( "A1" ) }, Jobs( _printerA, 1 ) );
            Assert.Empty( Jobs( _printerB, 0 ) );
            Assert.Empty( printer.LoggedErrors );
        }

        [Fact]
        public void MergeFields_AreFormattedAndBlankFieldsRemoved()
        {
            var file = Guid.NewGuid();
            _labelFiles[file] = "^XA\n^FO10,10^FDName^FS\n^FO20,20^GB100,2,2^FS\n^FT30,30^FDNote^FS\n^XZ\n";
            var label = ServerLabel( PrinterA, file );
            label.MergeFields.Add( "Name", "Zoé" );
            label.MergeFields.Add( "Note", "" );

            var printer = NewPrinter( label );
            printer.PrintNetworkLabels();

            Assert.Equal( new[] { "^XA\n^FO10,10^FDZo\\82^FS\n^FT30,30^FD^FS\n^XZ\n" }, Jobs( _printerA, 1 ) );
            Assert.Empty( printer.LoggedErrors );
        }

        [Fact]
        public void RefusedPrinter_IsLoggedAndNextPrinterStillPrints()
        {
            _printerB.Dispose();
            var b1 = AddLabelFile( "B1" );
            var a1 = AddLabelFile( "A1" );

            var printer = NewPrinter( ServerLabel( PrinterB, b1 ), ServerLabel( PrinterA, a1 ) );
            printer.PrintNetworkLabels();

            Assert.Equal( new[] { Zpl( "A1" ) }, Jobs( _printerA, 1 ) );
            var error = Assert.Single( printer.LoggedErrors );
            Assert.Equal( "Could not print to the label printer at 127.0.0.2.", error.Message );
            Assert.IsType<SocketException>( error.InnerException );
        }

        [Fact]
        public void ConnectionTimeout_IsLoggedNothingIsSentAndConnectionIsClosed()
        {
            var a1 = AddLabelFile( "A1" );
            var b1 = AddLabelFile( "B1" );

            var printer = NewPrinter( ServerLabel( PrinterA, a1 ), ServerLabel( PrinterB, b1 ) );
            var waits = 0;
            printer.WaitOverride = connect =>
            {
                // Let the first connection complete, then report it as timed out.
                var connected = connect.AsyncWaitHandle.WaitOne( Timeout );
                return Interlocked.Increment( ref waits ) == 1 ? false : connected;
            };
            printer.PrintNetworkLabels();

            Assert.True( _printerA.WaitForConnections( 1, Timeout ), "printer A did not see the connection" );
            var connectionA = _printerA.Connections.Single();
            Assert.True( connectionA.WaitForEnd( EndTimeout ), "the timed-out connection was not closed" );
            Assert.Equal( 0, connectionA.ByteCount );

            Assert.Equal( new[] { Zpl( "B1" ) }, Jobs( _printerB, 1 ) );

            var error = Assert.Single( printer.LoggedErrors );
            Assert.Equal( "Could not connect to the label printer at 127.0.0.1.", error.Message );
            Assert.Null( error.InnerException );
        }

        [Fact]
        public void DefaultConnectionWait_GivesUpAfterFiveSeconds()
        {
            var printer = NewPrinter();
            using ( var connect = new UnansweredConnect() )
            {
                var stopwatch = Stopwatch.StartNew();
                var connected = printer.DefaultWait( connect );
                stopwatch.Stop();

                Assert.False( connected );
                Assert.InRange( stopwatch.Elapsed, TimeSpan.FromSeconds( 4.5 ), TimeSpan.FromSeconds( 10 ) );
            }
        }

        [Fact]
        public void SuccessfulPrint_ClosesEachPrinterConnection()
        {
            var a1 = AddLabelFile( "A1" );
            var b1 = AddLabelFile( "B1" );

            var printer = NewPrinter( ServerLabel( PrinterA, a1 ), ServerLabel( PrinterB, b1 ) );
            printer.PrintNetworkLabels();

            Assert.True( _printerA.WaitForConnections( 1, Timeout ), "printer A did not see a connection" );
            Assert.True( _printerB.WaitForConnections( 1, Timeout ), "printer B did not see a connection" );
            Assert.True( _printerA.Connections.Single().WaitForEnd( EndTimeout ), "printer A's connection was not closed" );
            Assert.True( _printerB.Connections.Single().WaitForEnd( EndTimeout ), "printer B's connection was not closed" );
            Assert.Empty( printer.LoggedErrors );
        }

        [Fact]
        public void UnparsablePrinterAddress_IsLoggedAndNextPrinterStillPrints()
        {
            var x1 = AddLabelFile( "X1" );
            var a1 = AddLabelFile( "A1" );

            var printer = NewPrinter( ServerLabel( "not-an-ip", x1 ), ServerLabel( PrinterA, a1 ) );
            printer.PrintNetworkLabels();

            Assert.Equal( new[] { Zpl( "A1" ) }, Jobs( _printerA, 1 ) );
            var error = Assert.Single( printer.LoggedErrors );
            Assert.Equal( "Could not print to the label printer at not-an-ip.", error.Message );
            Assert.IsType<FormatException>( error.InnerException );
        }

        [Fact]
        public void DefaultPrinterPort_Is9100()
        {
            Assert.Equal( 9100, new LabelPrinter().PrinterPort );
        }

        #endregion

        #region Helpers

        private static string Zpl( string name )
        {
            return "^XA\n^FO10,10^FD" + name + "^FS\n^XZ\n";
        }

        /// <summary>
        /// A label as sent when another label follows it on the same printer: trimmed, with a
        /// Delay Cut command before the ending tag.
        /// </summary>
        private static string Cut( string name )
        {
            return "^XA\n^FO10,10^FD" + name + "^FS\n^XB^XZ";
        }

        private Guid AddLabelFile( string name )
        {
            var fileGuid = Guid.NewGuid();
            _labelFiles[fileGuid] = Zpl( name );
            return fileGuid;
        }

        private static CheckInLabel ServerLabel( string printerAddress, Guid fileGuid )
        {
            return new CheckInLabel
            {
                PrintFrom = PrintFrom.Server,
                PrinterAddress = printerAddress,
                FileGuid = fileGuid,
                MergeFields = new Dictionary<string, string>()
            };
        }

        private TestLabelPrinter NewPrinter( params CheckInLabel[] labels )
        {
            return new TestLabelPrinter( _labelFiles )
            {
                PrinterPort = _port,
                Labels = labels.ToList()
            };
        }

        /// <summary>
        /// Waits for the expected number of connections to end, then for a short quiet period, and
        /// returns what every connection the printer accepted received.
        /// </summary>
        private static List<string> Jobs( StandInPrinter printer, int expectedConnections )
        {
            Assert.True( printer.WaitForConnections( expectedConnections, Timeout ), "the printer did not see the expected connections" );
            foreach ( var connection in printer.Connections )
            {
                Assert.True( connection.WaitForEnd( Timeout ), "a printer connection was not closed" );
            }

            Thread.Sleep( QuietPeriod );
            return printer.Connections.Select( c => c.Text ).ToList();
        }

        #endregion

        #region Test doubles

        private sealed class TestLabelPrinter : LabelPrinter
        {
            private readonly Dictionary<Guid, string> _labelFiles;

            public TestLabelPrinter( Dictionary<Guid, string> labelFiles )
            {
                _labelFiles = labelFiles;
            }

            public List<Exception> LoggedErrors { get; } = new List<Exception>();

            public Func<IAsyncResult, bool> WaitOverride { get; set; }

            protected override string GetLabelContent( Guid fileGuid )
            {
                string content;
                return _labelFiles.TryGetValue( fileGuid, out content ) ? content : null;
            }

            protected override bool WaitForConnection( IAsyncResult connect )
            {
                return WaitOverride != null ? WaitOverride( connect ) : base.WaitForConnection( connect );
            }

            /// <summary>
            /// Runs LabelPrinter's own connection wait.
            /// </summary>
            public bool DefaultWait( IAsyncResult connect )
            {
                return base.WaitForConnection( connect );
            }

            protected override void LogPrintError( Exception ex )
            {
                LoggedErrors.Add( ex );
            }
        }

        /// <summary>
        /// A connection attempt that never completes: its wait handle is never signaled.
        /// </summary>
        private sealed class UnansweredConnect : IAsyncResult, IDisposable
        {
            private readonly ManualResetEvent _handle = new ManualResetEvent( false );

            public object AsyncState
            {
                get { return null; }
            }

            public WaitHandle AsyncWaitHandle
            {
                get { return _handle; }
            }

            public bool CompletedSynchronously
            {
                get { return false; }
            }

            public bool IsCompleted
            {
                get { return false; }
            }

            public void Dispose()
            {
                _handle.Dispose();
            }
        }

        /// <summary>
        /// A stand-in network label printer: accepts connections on a loopback listener and
        /// records the bytes each connection receives until the client closes it.
        /// </summary>
        internal sealed class StandInPrinter : IDisposable
        {
            private readonly TcpListener _listener;
            private readonly List<PrinterConnection> _connections = new List<PrinterConnection>();
            private bool _stopped;

            public StandInPrinter( TcpListener listener )
            {
                _listener = listener;
                new Thread( AcceptLoop ) { IsBackground = true }.Start();
            }

            public List<PrinterConnection> Connections
            {
                get
                {
                    lock ( _connections )
                    {
                        return _connections.ToList();
                    }
                }
            }

            public bool WaitForConnections( int count, TimeSpan timeout )
            {
                var deadline = DateTime.UtcNow + timeout;
                while ( Connections.Count < count )
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

                    var connection = new PrinterConnection( client );
                    lock ( _connections )
                    {
                        if ( _stopped )
                        {
                            connection.Dispose();
                            return;
                        }

                        _connections.Add( connection );
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

        internal sealed class PrinterConnection : IDisposable
        {
            private readonly TcpClient _client;
            private readonly MemoryStream _received = new MemoryStream();
            private readonly ManualResetEventSlim _ended = new ManualResetEventSlim( false );

            public PrinterConnection( TcpClient client )
            {
                _client = client;
                new Thread( ReadLoop ) { IsBackground = true }.Start();
            }

            public string Text
            {
                get
                {
                    lock ( _received )
                    {
                        return Encoding.ASCII.GetString( _received.ToArray() );
                    }
                }
            }

            public int ByteCount
            {
                get
                {
                    lock ( _received )
                    {
                        return ( int ) _received.Length;
                    }
                }
            }

            public bool WaitForEnd( TimeSpan timeout )
            {
                return _ended.Wait( timeout );
            }

            private void ReadLoop()
            {
                var buffer = new byte[4096];
                try
                {
                    var stream = _client.GetStream();
                    int read;
                    while ( ( read = stream.Read( buffer, 0, buffer.Length ) ) > 0 )
                    {
                        lock ( _received )
                        {
                            _received.Write( buffer, 0, read );
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

        #endregion
    }
}
