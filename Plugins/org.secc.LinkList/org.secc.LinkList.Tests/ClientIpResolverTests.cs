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
using org.secc.LinkList.Utility;

using Xunit;

namespace org.secc.LinkList.Tests
{
    /// <summary>
    /// ROCK-8881: the DNS-free, validating client-IP resolver for the anonymous
    /// endpoints.
    /// </summary>
    public class ClientIpResolverTests
    {
        [Fact]
        public void Forwarded_Address_Wins_Over_Remote_Address()
        {
            Assert.Equal( "203.0.113.9", ClientIpResolver.Resolve( "203.0.113.9", "10.0.0.1" ) );
        }

        [Fact]
        public void Falls_Back_To_Remote_Address_When_No_Header()
        {
            Assert.Equal( "10.0.0.1", ClientIpResolver.Resolve( null, "10.0.0.1" ) );
            Assert.Equal( "10.0.0.1", ClientIpResolver.Resolve( "", "10.0.0.1" ) );
            Assert.Equal( "10.0.0.1", ClientIpResolver.Resolve( "   ", "10.0.0.1" ) );
        }

        [Theory]
        [InlineData( "169.254.18.24", "169.254.18.24" )]
        [InlineData( "169.254.18.24:28372", "169.254.18.24" )]
        [InlineData( "fe80::260:97ff:fe02:6ea5", "fe80::260:97ff:fe02:6ea5" )]
        [InlineData( "[fe80::260:97ff:fe02:6ea5]:28372", "fe80::260:97ff:fe02:6ea5" )]
        public void Handles_The_Documented_Wire_Forms( string header, string expected )
        {
            // The four shapes Rock documents on GetXForwardedForIpAddress. Worth
            // pinning here because a port suffix left attached would fail the
            // IPAddress.TryParse guard and silently discard every proxied
            // address.
            Assert.Equal( expected, ClientIpResolver.Resolve( header, "10.0.0.1" ) );
        }

        [Fact]
        public void Takes_The_First_Token_Of_A_Proxy_Chain()
        {
            // CDN in front of a web farm: the client is the leftmost entry.
            Assert.Equal( "68.14.20.30", ClientIpResolver.Resolve( "68.14.20.30, 147.243.1.2, 147.243.1.3:57275", "10.0.0.1" ) );
        }

        [Theory]
        [InlineData( "not-an-ip" )]
        [InlineData( "'; DROP TABLE Interaction --" )]
        [InlineData( "<script>alert(1)</script>" )]
        [InlineData( "999.999.999.999" )]
        [InlineData( "localhost" )]
        public void Unparseable_Forwarded_Value_Falls_Back_Instead_Of_Being_Persisted( string header )
        {
            // The resolved value is written to Interaction.IpAddress (truncated
            // to 45 chars, so corruption would be silent) and later fed to
            // Rock's IP-geolocation job. An attacker-authored string must never
            // reach either.
            Assert.Equal( "10.0.0.1", ClientIpResolver.Resolve( header, "10.0.0.1" ) );
        }

        [Fact]
        public void Returns_Null_When_Nothing_Parses()
        {
            // Null is safe: the limiter treats a blank key as always-allowed, so
            // an unresolvable address suppresses no analytics.
            Assert.Null( ClientIpResolver.Resolve( "not-an-ip", "also-not-an-ip" ) );
            Assert.Null( ClientIpResolver.Resolve( null, null ) );
            Assert.Null( ClientIpResolver.Resolve( "", "   " ) );
        }

        [Fact]
        public void Loopback_Is_Returned_As_Is_And_Costs_No_Dns()
        {
            // Rock's WebRequestHelper.GetClientIpAddress blanks "::1" and then
            // does a synchronous Dns.GetHostEntry plus an ExceptionLog write -
            // which an anonymous caller triggers at will by sending
            // "X-Forwarded-For: ::1". This resolver has no such path; the value
            // parses, so it is simply returned.
            Assert.Equal( "::1", ClientIpResolver.Resolve( "::1", "10.0.0.1" ) );
            Assert.Equal( "::1", ClientIpResolver.Resolve( null, "::1" ) );
            Assert.Equal( "127.0.0.1", ClientIpResolver.Resolve( null, "127.0.0.1" ) );
        }

        [Fact]
        public void Surrounding_Whitespace_Is_Trimmed()
        {
            Assert.Equal( "203.0.113.9", ClientIpResolver.Resolve( null, "  203.0.113.9  " ) );
        }
    }
}
