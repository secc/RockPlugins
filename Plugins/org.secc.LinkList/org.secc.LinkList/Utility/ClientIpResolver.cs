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
using System.Net;

namespace org.secc.LinkList.Utility
{
    /// <summary>
    /// ROCK-8881: resolves the client IP for the anonymous LinkList endpoints.
    /// Pure (two strings in, one string out) so it is unit-testable without an
    /// HttpContext.
    ///
    /// <b>DNS-free by design.</b> Rock's
    /// <c>WebRequestHelper.GetClientIpAddress</c> blanks the address when it is
    /// empty or exactly <c>::1</c> and then performs a synchronous
    /// <c>Dns.GetHostName</c> + <c>Dns.GetHostEntry</c> on the request thread,
    /// logging any SocketException to the ExceptionLog table. Because that
    /// helper also returns the first <c>X-Forwarded-For</c> token verbatim, an
    /// anonymous caller sending <c>X-Forwarded-For: ::1</c> in a loop turns
    /// every request into a blocking DNS round trip plus a database insert -
    /// an amplification primitive on the exact endpoints this ticket set out to
    /// protect, and one the rate limiter cannot stop because the IP has to be
    /// resolved before the limiter can be consulted. This mirrors the formula
    /// Rock itself uses in <c>Net/ClientInformation.cs</c>, which has no such
    /// fallback.
    ///
    /// <b>The parse guard matters beyond the limiter.</b> The value returned
    /// here is persisted to <c>Interaction.IpAddress</c> (truncated to 45
    /// characters, so corruption would be silent) and later consumed by Rock's
    /// <c>PopulateInteractionSessionData</c> job, which feeds it to a configured
    /// IP-geolocation component. Rejecting anything that is not a parseable
    /// address keeps attacker-authored strings out of the analytics tables and
    /// out of a metered lookup quota. It does NOT make the value trustworthy -
    /// a well-formed spoofed address still passes; see
    /// <see cref="LinkListRateLimitPolicy"/> for that accepted residual risk.
    /// </summary>
    public static class ClientIpResolver
    {
        /// <summary>
        /// The client address from <paramref name="forwardedForHeader"/> if it
        /// yields a parseable IP, otherwise <paramref name="remoteAddress"/> if
        /// that does, otherwise null. A null result suppresses nothing: the
        /// limiter treats a blank key as always-allowed.
        /// </summary>
        public static string Resolve( string forwardedForHeader, string remoteAddress )
        {
            // Rock's parser is public, pure, and already handles the four wire
            // forms it documents (bare IPv4/IPv6, IPv4 with port, bracketed
            // IPv6 with port) plus the comma-delimited CDN/web-farm proxy
            // chain. Only GetClientIpAddress - the wrapper - touches DNS.
            var forwarded = Rock.Utility.WebRequestHelper.GetXForwardedForIpAddress( forwardedForHeader );

            return AsIpAddress( forwarded ) ?? AsIpAddress( remoteAddress );
        }

        private static string AsIpAddress( string value )
        {
            if ( string.IsNullOrWhiteSpace( value ) )
            {
                return null;
            }

            var trimmed = value.Trim();
            return IPAddress.TryParse( trimmed, out _ ) ? trimmed : null;
        }
    }
}
