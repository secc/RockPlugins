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
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using org.secc.LinkList.Utility;

using Xunit;

namespace org.secc.LinkList.Tests
{
    /// <summary>
    /// ROCK-8881: pins the bounded read that <c>PostClick</c> performs before
    /// touching the body.
    ///
    /// The gate this replaced checked <c>Content-Length</c> and could not work
    /// on this host. System.Web.Http.WebHost wraps the request body in
    /// SeekableBufferedRequestStream, which hardcodes <c>CanSeek</c> to true
    /// while leaving <c>Length</c> to fall through to
    /// <c>HttpRequest.ContentLength</c> - 0 for a chunked request. So the header
    /// always computed to a value (making the 411 branch unreachable) and for a
    /// chunked request always to 0, which sailed straight past the 2048 cap.
    /// These tests cover both shapes against the framework primitive that now
    /// enforces it.
    /// </summary>
    public class ClickBodyLimitTests
    {
        /// <summary>
        /// The exact argument <c>PostClick</c> passes to
        /// <c>LoadIntoBufferAsync</c>.
        /// </summary>
        private const int Cap = ClickPayload.MaxBodyLength + 1;

        private const string RealBeacon = "{\"matrixItemGuid\":\"3b1c5f70-9d2e-4a6b-8c1d-2e3f4a5b6c7d\"}";

        /// <summary>
        /// Forward-only stream with no discoverable length, so
        /// <c>StreamContent.TryComputeLength</c> fails and
        /// <c>Headers.ContentLength</c> is null - the shape a
        /// <c>Transfer-Encoding: chunked</c> request presents, where the old
        /// pre-read gate had nothing to check.
        /// </summary>
        private sealed class UnknownLengthStream : Stream
        {
            private readonly MemoryStream _inner;

            public UnknownLengthStream( byte[] data ) => _inner = new MemoryStream( data );

            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();

            public override long Position
            {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override void Flush() { }
            public override int Read( byte[] buffer, int offset, int count ) => _inner.Read( buffer, offset, count );
            public override long Seek( long offset, SeekOrigin origin ) => throw new NotSupportedException();
            public override void SetLength( long value ) => throw new NotSupportedException();
            public override void Write( byte[] buffer, int offset, int count ) => throw new NotSupportedException();
        }

        private static StreamContent Chunked( byte[] body ) => new StreamContent( new UnknownLengthStream( body ) );

        [Fact]
        public async Task Oversized_Chunked_Body_Is_Rejected()
        {
            var content = Chunked( new byte[1024 * 1024] );

            // Precondition: this is the case the Content-Length gate could not
            // see. If this ever starts reporting a length, the premise of the
            // fix has changed.
            Assert.Null( content.Headers.ContentLength );

            await Assert.ThrowsAsync<HttpRequestException>( () => content.LoadIntoBufferAsync( Cap ) );
        }

        [Fact]
        public async Task Oversized_Declared_Body_Is_Rejected_Before_Any_Read()
        {
            var content = new ByteArrayContent( new byte[1024 * 1024] );
            Assert.Equal( 1024 * 1024, content.Headers.ContentLength );

            await Assert.ThrowsAsync<HttpRequestException>( () => content.LoadIntoBufferAsync( Cap ) );
        }

        [Fact]
        public async Task Body_At_The_Cap_Is_Accepted()
        {
            var content = Chunked( new byte[ClickPayload.MaxBodyLength] );
            await content.LoadIntoBufferAsync( Cap );

            Assert.Equal( ClickPayload.MaxBodyLength, ( await content.ReadAsByteArrayAsync() ).Length );
        }

        [Fact]
        public async Task A_Real_Beacon_Buffers_And_Reads_Back_Intact()
        {
            var content = Chunked( Encoding.UTF8.GetBytes( RealBeacon ) );
            await content.LoadIntoBufferAsync( Cap );

            // Buffering must not disturb the body: the controller reads the
            // string afterwards, and LoadIntoBufferAsync short-circuits on
            // IsBuffered so repeated reads are safe. That matters more than
            // usual here - because CanSeek lies on the real request stream, a
            // second unbuffered serialize would drain the whole body unbounded.
            Assert.Equal( RealBeacon, await content.ReadAsStringAsync() );
            Assert.Equal( RealBeacon, await content.ReadAsStringAsync() );
            Assert.True( ClickPayload.TryParse( await content.ReadAsStringAsync(), out var guid ) );
            Assert.Equal( Guid.Parse( "3b1c5f70-9d2e-4a6b-8c1d-2e3f4a5b6c7d" ), guid );
        }
    }
}
