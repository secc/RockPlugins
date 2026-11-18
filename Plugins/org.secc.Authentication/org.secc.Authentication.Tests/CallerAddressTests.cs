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
using Xunit;

namespace org.secc.Authentication.Tests
{
    /// <summary>
    /// CallerAddress.ForRequest for each shape of X-Forwarded-For and REMOTE_ADDR.
    /// </summary>
    public class CallerAddressTests
    {
        [Fact]
        public void NoRequest_IsUnknown()
        {
            Assert.Equal( "unknown", CallerAddress.ForRequest( null ) );
        }

        [Theory]
        // No forwarded entry: the connecting address, trimmed, else "unknown".
        [InlineData( null, "203.0.113.5", "203.0.113.5" )]
        [InlineData( "", "203.0.113.5", "203.0.113.5" )]
        [InlineData( "   ", "203.0.113.5", "203.0.113.5" )]
        [InlineData( null, " 203.0.113.5 ", "203.0.113.5" )]
        [InlineData( null, "::1", "::1" )]
        [InlineData( null, null, "unknown" )]
        [InlineData( null, "", "unknown" )]
        [InlineData( " ", " ", "unknown" )]
        // The last entry, without its port.
        [InlineData( "203.0.113.7", "10.0.0.1", "203.0.113.7" )]
        [InlineData( "203.0.113.7:51234", "10.0.0.1", "203.0.113.7" )]
        [InlineData( " 6.6.6.6 , 203.0.113.7:443 ", "10.0.0.1", "203.0.113.7" )]
        [InlineData( "6.6.6.6,7.7.7.7:1,203.0.113.7:443", "10.0.0.1", "203.0.113.7" )]
        [InlineData( "198.51.100.9, 192.0.2.2:4431", "10.0.0.1", "192.0.2.2" )]
        [InlineData( "2001:db8::5", "10.0.0.1", "2001:db8::5" )]
        [InlineData( "6.6.6.6, 2001:db8::5", "10.0.0.1", "2001:db8::5" )]
        [InlineData( "2001:db8::5, 203.0.113.7:443", "10.0.0.1", "203.0.113.7" )]
        [InlineData( "[2001:db8::5]", "10.0.0.1", "2001:db8::5" )]
        [InlineData( "[2001:db8::5]:1111", "10.0.0.1", "2001:db8::5" )]
        [InlineData( "[2001:db8::5]:2222", "10.0.0.1", "2001:db8::5" )]
        // A blank last entry falls back to the connecting address, not to the entry before it.
        [InlineData( "203.0.113.7,", "10.0.0.1", "10.0.0.1" )]
        [InlineData( "203.0.113.7, ", "10.0.0.1", "10.0.0.1" )]
        [InlineData( ",", "10.0.0.1", "10.0.0.1" )]
        [InlineData( "203.0.113.7,", null, "unknown" )]
        // So does a last entry that leaves no address once its port or brackets are removed.
        [InlineData( ":443", "10.0.0.1", "10.0.0.1" )]
        [InlineData( "6.6.6.6, []", "10.0.0.1", "10.0.0.1" )]
        public void ForRequest_UsesTheLastForwardedEntry_ElseTheConnectingAddress( string forwardedFor, string remoteAddress, string expected )
        {
            Assert.Equal( expected, CallerAddress.ForRequest( new FakeRequest( forwardedFor, remoteAddress ) ) );
        }

        [Theory]
        [InlineData( "X_Forwarded_For" )]
        [InlineData( "X-Real-IP" )]
        [InlineData( "X-Azure-ClientIP" )]
        [InlineData( "Forwarded" )]
        public void ForRequest_IgnoresOtherHeaders( string header )
        {
            var request = new FakeRequest( remoteAddress: "10.0.0.2" );
            request.Headers[header] = "203.0.113.9";

            Assert.Equal( "10.0.0.2", CallerAddress.ForRequest( request ) );
        }

        [Fact]
        public void ForRequest_ReadsTheHeader_NotTheServerVariable()
        {
            var request = new FakeRequest( remoteAddress: "10.0.0.3" );
            request.ServerVariables["HTTP_X_FORWARDED_FOR"] = "203.0.113.9";

            Assert.Equal( "10.0.0.3", CallerAddress.ForRequest( request ) );
        }

        [Fact]
        public void ForRequest_ReadsTheHeader_WhateverTheCaseOfItsName()
        {
            var request = new FakeRequest( remoteAddress: "10.0.0.4" );
            request.Headers["x-forwarded-for"] = "6.6.6.6, 203.0.113.10:80";

            Assert.Equal( "203.0.113.10", CallerAddress.ForRequest( request ) );
        }
    }
}
