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

using org.secc.LinkList.Utility;

using Xunit;

namespace org.secc.LinkList.Tests
{
    public class ClickPayloadTests
    {
        private const string ValidGuid = "3b1c5f70-9d2e-4a6b-8c1d-2e3f4a5b6c7d";

        [Fact]
        public void Parses_Valid_Payload()
        {
            Assert.True( ClickPayload.TryParse( $"{{\"matrixItemGuid\":\"{ValidGuid}\"}}", out var guid ) );
            Assert.Equal( Guid.Parse( ValidGuid ), guid );
        }

        [Fact]
        public void Parses_Braced_And_Uppercase_Guid_Forms()
        {
            Assert.True( ClickPayload.TryParse( $"{{\"matrixItemGuid\":\"{{{ValidGuid.ToUpperInvariant()}}}\"}}", out var guid ) );
            Assert.Equal( Guid.Parse( ValidGuid ), guid );
        }

        [Fact]
        public void Ignores_Extra_Properties()
        {
            Assert.True( ClickPayload.TryParse( $"{{\"other\":1,\"matrixItemGuid\":\"{ValidGuid}\",\"x\":[1,2]}}", out var guid ) );
            Assert.Equal( Guid.Parse( ValidGuid ), guid );
        }

        [Theory]
        [InlineData( null )]
        [InlineData( "" )]
        [InlineData( "   " )]
        [InlineData( "{}" )]
        [InlineData( "{\"matrixItemGuid\":\"not-a-guid\"}" )]
        [InlineData( "{\"matrixItemGuid\":null}" )]
        [InlineData( "{\"matrixItemGuid\":123}" )]
        [InlineData( "\"just-a-string\"" )]
        [InlineData( "[1,2,3]" )]
        [InlineData( "not json at all" )]
        [InlineData( "{\"matrixItemGuid\":\"" )] // truncated
        public void Rejects_Invalid_Input( string body )
        {
            Assert.False( ClickPayload.TryParse( body, out var guid ) );
            Assert.Equal( Guid.Empty, guid );
        }

        [Fact]
        public void Rejects_Oversized_Body()
        {
            var padded = "{\"matrixItemGuid\":\"" + ValidGuid + "\",\"pad\":\"" + new string( 'x', ClickPayload.MaxBodyLength ) + "\"}";
            Assert.False( ClickPayload.TryParse( padded, out _ ) );
        }

        [Fact]
        public void Accepts_Body_At_Exact_Size_Limit()
        {
            var body = $"{{\"matrixItemGuid\":\"{ValidGuid}\"}}";
            body = body.Insert( body.Length - 1, ",\"pad\":\"" + new string( 'x', ClickPayload.MaxBodyLength - body.Length - 9 ) + "\"" );
            Assert.Equal( ClickPayload.MaxBodyLength, body.Length );
            Assert.True( ClickPayload.TryParse( body, out var guid ) );
            Assert.Equal( Guid.Parse( ValidGuid ), guid );
        }
    }
}
